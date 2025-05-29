using AngleSharp;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;
using Quartz;
using SocialOpinionAPI.Services.Tweet;

namespace KpVotes;

public class KpVotesJob(ILogger<KpVotesJob> logger, KpVotesJobOptions options, TweetService twitter) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            logger.LogInformation("Begin GetAndPost {Trigger}", context.Trigger.Key);
            await GetAndPost(context.CancellationToken);
            logger.LogInformation("End GetAndPost {Trigger}", context.Trigger.Key);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "End GetAndPost");
        }
    }

    async Task GetAndPost(CancellationToken cancel)
    {
        logger.LogInformation("Begin GetSiteVotes");
        var siteVotes = await GetSiteVotes(cancel);
        logger.LogInformation("End GetSiteVotes: {SiteVotesCount}", siteVotes.Length);

        logger.LogInformation("Begin GetFileVotes");
        var fileVotes = await GetFileVotes(cancel);
        logger.LogInformation("End GetFileVotes: {FileVotesCount}", fileVotes?.Length);

        logger.LogInformation("Begin SendVoteToTwitter");
        var allVotes = (fileVotes ?? siteVotes).ToHashSet(x => new { x.Uri, x.Vote });
        foreach (var vote in siteVotes)
            if (allVotes.Add(vote))
                SendVoteToTwitter(vote);
        logger.LogInformation("End SendVoteToTwitter");

        logger.LogInformation("Begin SaveFileVotes: {FileVotesCount}:", allVotes.Count);
        await SaveFileVotes(allVotes, cancel);
        logger.LogInformation("End SaveFileVotes");
    }

    void SendVoteToTwitter(KpVote vote)
    {
        logger.LogInformation("Begin send {VoteUri}", vote.Uri);
        var starts = "".PadLeft(vote.Vote, '\u2605') + "".PadRight(10 - vote.Vote, '\u2606');
        var uri = new Uri(options.KpUri, vote.Uri);
        var text = $"{vote.Name}.\r\nМоя оценка {vote.Vote} из 10 {starts} #kinopoisk\r\n{uri}";
        twitter.PostTextOnlyTweet(text);
        logger.LogInformation("End send {VoteUri}", vote.Uri);
    }

    async Task<KpVote[]> GetSiteVotes(CancellationToken cancel)
    {
        if (options.SkipLoad)
            return [];
        var uri = new Uri(options.KpUri, options.VotesUri);
        var driverOptions = new FirefoxOptions();
        driverOptions.AddArgument("--headless"); // Запуск без окна
        using var driver = new FirefoxDriver(driverOptions);
        driver.Navigate().GoToUrl(uri);
        // Ждем полной загрузки страницы (можно добавить явное ожидание нужного элемента)
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        wait.Until(drv =>
            drv.FindElements(By.CssSelector(".historyVotes .item")).Any()
            || drv.FindElements(By.CssSelector(".CheckboxCaptcha-Button")).Any());
        var pageSource = driver.PageSource;

        // Парсим HTML с AngleSharp
        var context = BrowsingContext.New(Configuration.Default);
        var doc = await context.OpenAsync(req => req.Content(pageSource), cancel);
        Console.WriteLine(doc.ToHtml());
        var query =
            from item in doc.QuerySelectorAll(".historyVotes .item")
            let name = item.QuerySelector(".nameRus a")
            let vote = item.QuerySelector(".vote")
            where name != null && vote != null
            select new KpVote
            (
                name.GetAttribute("href"),
                name.TextContent,
                int.Parse(vote.TextContent)
            );
        return query.Reverse().ToArray();
    }

    async Task<KpVote[]> GetFileVotes(CancellationToken cancel)
    {
        if (!File.Exists(options.CachePath)) return null;
        var text = await File.ReadAllTextAsync(options.CachePath, cancel);
        return JsonConvert.DeserializeObject<KpVote[]>(text);
    }

    async Task SaveFileVotes(HashSet<KpVote> allVotes, CancellationToken cancel)
    {
        var text = JsonConvert.SerializeObject(allVotes);
        await File.WriteAllTextAsync(options.CachePath, text, cancel);
    }
}