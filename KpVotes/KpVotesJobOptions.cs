namespace KpVotes;

public record KpVotesJobOptions(
    Uri KpUri,
    Uri VotesUri,
    string CachePath,
    bool SkipLoad,
    string? UserAgent = null);