using AngleSharp;
using AngleSharp.Html.Parser;

namespace KpVotes.Kinopoisk;

public class KpParser : IKpParser
{
    public IReadOnlyCollection<KpVote> Parse(string html)
    {
        var parser = new HtmlParser();
        var doc = parser.ParseDocument(html);
        var query =
            from item in doc.QuerySelectorAll(Const.VotesSelector)
            let name = item.QuerySelector(".nameRus a")
            let vote = item.QuerySelector(".vote") ?? item.QuerySelector(".myVote")
            where name != null && vote != null
            select new KpVote
            (   
                name.GetAttribute("href"),
                name.TextContent,
                int.Parse(vote.TextContent)
            );
        return query.Reverse().ToArray();
    }
}