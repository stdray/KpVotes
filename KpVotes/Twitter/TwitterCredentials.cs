namespace KpVotes.Twitter;

public class TwitterCredentials
{
    public required string ConsumerKey { get; init; }
    public required string ConsumerSecret { get; init; }
    public required string AccessToken { get; init; }
    public required string AccessTokenSecret { get; init; }

}