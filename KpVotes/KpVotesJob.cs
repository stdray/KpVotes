using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using AngleSharp;
using Microsoft.Extensions.Logging;
using Quartz;
using SocialOpinionAPI.Services.Tweet;

namespace KpVotes;

public class KpVotesJob(ILogger<KpVotesJob> log, KpVotesJobOptions options, TweetService twitter) : IJob
{
    readonly IEqualityComparer<KpVote> _comparer =
        new KeyEqualityComparer<KpVote, Tuple<string, int>>(x => Tuple.Create(x.Uri, x.Vote));

    readonly JsonSerializerOptions _serializerOptions = new()
    {
        Encoder = JavaScriptEncoder.Create(
            UnicodeRanges.BasicLatin,
            UnicodeRanges.Cyrillic,
            UnicodeRanges.GeneralPunctuation),
        WriteIndented = true
    };

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            log.LogInformation("Begin execute");
            await ExecuteInternal(context);
            log.LogInformation("Execute success");
        }
        catch (Exception ex)
        {
            log.LogInformation(ex, "Execute failed");
        }
    }

    async Task ExecuteInternal(IJobExecutionContext context)
    {
        log.LogInformation("Read stored votes from {VotesPath}", options.VotesPath);
        var storedVotes = await GetStoredVotes();
        log.LogInformation("Stored votes count: {StoredVotesCount}", storedVotes.Count);

        log.LogInformation("Get user votes from {UserVotesUri}", options.VotesUri);
        var kpVotes = await ParseKpVotes(context.CancellationToken);
        log.LogInformation("User votes count: {UserVotesCount}", kpVotes.Count);

        log.LogInformation("Begin post votes");
        var updatedVotes = PostVotes(storedVotes, kpVotes);
        log.LogInformation("End post votes");

        log.LogInformation("Begin update stored votes in {VotesPath}", options.VotesPath);
        await UpdateStoredVotes(updatedVotes);
        log.LogInformation("New stored votes count: {NewStoredVotesCount}", updatedVotes.Count - storedVotes.Count);
    }

    ISet<KpVote> PostVotes(IEnumerable<KpVote> storedVotes, IEnumerable<KpVote> kpVotes)
    {
        var votes = storedVotes.ToHashSet(_comparer);
        if (votes.Any())
        {
            foreach (var vote in kpVotes)
                if (votes.Add(vote))
                    PostVote(vote);
        }
        else
        {
            log.LogInformation("Stored votes not found");
            votes.UnionWith(kpVotes);
        }
        return votes;
    }

    void PostVote(KpVote vote)
    {
        log.LogInformation("Begin post vote: {VoteName}", vote.Name);
        var uri = new Uri(options.KpUri, vote.Uri);
        var text = $"Моя оценка «{vote.Name}» на Кинопоиске — {vote.Vote}\r\n{uri}";
        twitter.PostTextOnlyTweet(text);
        log.LogInformation("End post vote: {VoteName}", vote.Name);
    }


    async Task<IReadOnlyCollection<KpVote>> ParseKpVotes(CancellationToken cancel)
    {
        if (options.SkipParse)
            return Array.Empty<KpVote>();
        var uri = new Uri(options.KpUri, options.VotesUri).ToString();
        var config = Configuration.Default.WithDefaultLoader();
        using var context = BrowsingContext.New(config);
        using var document = await context.OpenAsync(uri, cancel);
        var query =
            from item in document.QuerySelectorAll(".historyVotes .item")
            let name = item.QuerySelector(".nameRus a")
            let vote = item.QuerySelector(".vote")
            select new KpVote
            (
                name.GetAttribute("href"),
                name.TextContent,
                int.Parse(vote.TextContent)
            );
        return query.Reverse().ToList();
    }

    async Task<IReadOnlyCollection<KpVote>> GetStoredVotes() => File.Exists(options.VotesPath)
        ? JsonSerializer.Deserialize<List<KpVote>>(await File.ReadAllTextAsync(options.VotesPath), _serializerOptions)
        : [];

    async Task UpdateStoredVotes(IEnumerable<KpVote> votes) =>
        await File.WriteAllTextAsync(options.VotesPath, JsonSerializer.Serialize(votes, _serializerOptions));
}