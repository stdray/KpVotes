namespace KpVotes.Twitter;

public class ProxyOptions
{
    public required string Host { get; init; }
    public int Port { get; init; }
    public required  string Username { get; init; }
    public required  string Password { get; init; }
}