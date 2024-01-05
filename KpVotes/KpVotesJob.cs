using AngleSharp.Html.Parser;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SocialOpinionAPI.Services.Tweet;

namespace KpVotes;

public class KpVotesJob(
    ILogger<KpVotesJob> logger,
    KpVotesJobOptions options,
    HttpClient http,
    TweetService twitter,
    IHtmlParser parser)
{
    public async Task ExecuteAsync(CancellationToken cancel)
    {
        try
        {
            logger.LogInformation("Begin GetAndPost");
            await GetAndPost(cancel);
            logger.LogInformation("End GetAndPost");
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
        var allVotes = (fileVotes ?? siteVotes).ToHashSet(x => new { x.Uri, x.Vote });
        logger.LogInformation("Begin SendVoteToTwitter");
        foreach (var vote in siteVotes)
            if (allVotes.Add(vote))
                SendVoteToTwitter(vote);
        logger.LogInformation("End SendVoteToTwitter");
        await SaveFileVotes(allVotes, cancel);
    }

    void SendVoteToTwitter(KpVote vote)
    {
        logger.LogInformation("Begin send {VoteName}", vote.Name);
        var starts = "".PadLeft(vote.Vote, '\u2605') + "".PadRight(10 - vote.Vote, '\u2606');
        var uri = new Uri(options.KpUri, vote.Uri);
        var text = $"{vote.Name}.\r\nМоя оценка {vote.Vote} из 10 {starts} #kinopoisk\r\n{uri}";
        twitter.PostTextOnlyTweet(text);
        logger.LogInformation("End send {VoteName}", vote.Name);
    }

    async Task<KpVote[]> GetSiteVotes(CancellationToken cancel)
    {
        if (options.SkipLoad)
            return [];
        var uri = new Uri(options.KpUri, options.VotesUri);
        var html = await http.GetStringAsync(uri, cancel);
        var doc = await parser.ParseDocumentAsync(html, cancel);
        var query =
            from item in doc.QuerySelectorAll(".historyVotes .item")
            let name = item.QuerySelector(".nameRus a")
            let vote = item.QuerySelector(".vote")
            where name != null && vote != null
            select new KpVote
            (
                Uri: name.GetAttribute("href"),
                Name: name.TextContent,
                Vote: int.Parse(vote.TextContent)
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