namespace KpVotes;

public class KpVotesJobOptions
{
    public string Cron { get; init; }
    public bool StartNow { get; init; }
    public Uri KpUri { get; init; }
    public Uri VotesUri { get; init; }
    public string VotesPath { get; init; }
    public bool SkipParse { get; init; }
}