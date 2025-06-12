namespace KpVotes.Kinopoisk;

public interface IKpParser
{
    KpParserResult Parse(string html);
}