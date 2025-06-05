namespace KpVotes.Kinopoisk;

public interface IKpParser
{
    IReadOnlyCollection<KpVote> Parse(string html);
}