namespace KpVotes.Twitter;

public interface ITwitterClient
{
    Task PostTweet(string text);
}