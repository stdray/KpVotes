namespace KpVotes.Kinopoisk;

public class KpVotesJobOptions
{
    public Uri KpUri { get; init; } = new Uri("https://www.kinopoisk.ru");
    public required string VotesUri { get; init; }
    public TimeSpan Interval { get; init; } = new(1, 0, 0);
    public bool SkipLoad { get; init; }
    public string CachePath { get; init; } = "votes.json";
    public string PageVotesPath { get; init; } = "page_votes.json";
    
    public TimeSpan TwitterDelay { get; init; } = new(0, 0, 30);
}