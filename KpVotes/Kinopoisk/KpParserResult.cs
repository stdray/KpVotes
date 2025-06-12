namespace KpVotes.Kinopoisk;

public abstract record KpParserResult
{
    public record Captcha : KpParserResult;

    public record UserVotes(IReadOnlyCollection<KpVote> Votes) : KpParserResult;
}