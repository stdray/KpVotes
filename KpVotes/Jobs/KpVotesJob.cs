using KpVotes.Kinopoisk;
using KpVotes.System;
using KpVotes.Twitter;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Quartz;

namespace KpVotes.Jobs;

public class KpVotesJob(
    ILogger<KpVotesJob> logger,
    IOptionsSnapshot<KpVotesJobOptions> jobOptions,
    ITwitterClient twitter,
    IKpParser parser,
    IKpLoader loader)
    : IJob
{
    KpVotesJobOptions Options => jobOptions.Value;

    readonly JsonSerializerSettings _jsonSettings = new()
    {
        Formatting = Formatting.Indented,
    };

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
        try
        {
            logger.LogInformation("Begin GetSiteVotes");
            var siteVotes = await GetSiteVotes(cancel);
            logger.LogInformation("End GetSiteVotes: {SiteVotesCount}", siteVotes.Count);

            if (!siteVotes.Any())
            {
                logger.LogInformation("No site votes found");
                return;
            }

            logger.LogInformation("Begin GetFileVotes");
            var fileVotes = await GetFileVotes(Options.CachePath, cancel);
            logger.LogInformation("End GetFileVotes: {FileVotesCount}", fileVotes?.Length);

            logger.LogInformation("Begin SendVoteToTwitter");
            var allVotes = (fileVotes ?? siteVotes).ToHashSet(x => new { x.Uri, x.Vote });
            foreach (var vote in siteVotes)
                if (allVotes.Add(vote))
                {
                    await SendVoteToTwitter(vote);
                    logger.LogInformation("Begin SaveFileVotes: {FileVotesCount}:", allVotes.Count);
                    await SaveFileVotes(Options.CachePath, allVotes, cancel);
                    logger.LogInformation("End SaveFileVotes");
                    await Task.Delay(Options.TwitterDelay, cancel);
                }

            logger.LogInformation("End SendVoteToTwitter");

            Clean();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error GetAndPost");
            throw;
        }
    }

    async Task SendVoteToTwitter(KpVote vote)
    {
        logger.LogInformation("Begin send {VoteUri}", vote.Uri);
        var starts = "".PadLeft(vote.Vote, '\u2605') + "".PadRight(10 - vote.Vote, '\u2606');
        var uri = new Uri(Options.KpUri, vote.Uri);
        var text = $"{vote.Name}.\r\nМоя оценка {vote.Vote} из 10 {starts} #kinopoisk\r\n{uri}";
        await twitter.PostTweet(text);
        logger.LogInformation("End send {VoteUri}", vote.Uri);
    }

    async Task<IReadOnlyCollection<KpVote>> GetSiteVotes(CancellationToken cancel)
    {
        if (Options.SkipLoad)
            return [];
        if (File.Exists(Options.PageVotesPath))
        {
            var pageItems = await GetFileVotes(Options.PageVotesPath, cancel);
            if (pageItems?.Any() == true)
                return pageItems;
        }

        var html = await GetSiteHtml(cancel);
        var result = parser.Parse(html);

        if (result is KpParserResult.UserVotes votes)
        {
            var items = votes.Votes.ToHashSet();
            if (items.Any())
                await SaveFileVotes(Options.PageVotesPath, items, cancel);
            return items;
        }

        throw new InvalidOperationException("Captcha is not supported");
    }

    void Clean()
    {
        if (File.Exists(Options.PageVotesPath))
            File.Delete(Options.PageVotesPath);
    }

    async Task<string> GetSiteHtml(CancellationToken cancel)
    {
        var uri = new Uri(Options.KpUri, Options.VotesUri);
        return await loader.Load(uri, cancel);
    }

    async Task<KpVote[]?> GetFileVotes(string path, CancellationToken cancel)
    {
        if (!File.Exists(Options.CachePath)) return null;
        var text = await File.ReadAllTextAsync(path, cancel);
        return JsonConvert.DeserializeObject<KpVote[]>(text, _jsonSettings);
    }

    async Task SaveFileVotes(string path, HashSet<KpVote> allVotes, CancellationToken cancel)
    {
        var text = JsonConvert.SerializeObject(allVotes, _jsonSettings);
        await File.WriteAllTextAsync(path, text, cancel);
    }
}