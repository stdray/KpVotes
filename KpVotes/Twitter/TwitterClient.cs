using System.Net;
using LinqToTwitter;
using LinqToTwitter.OAuth;
using Microsoft.Extensions.Options;

namespace KpVotes.Twitter;

public class TwitterClient(
    IOptionsSnapshot<TwitterCredentials> credentials,
    IOptionsSnapshot<ProxyOptions> proxyOptions) : ITwitterClient
{
    public async Task PostTweet(string text)
    {
        var auth = CreateAuthorizer();
        using var twitterCtx = new TwitterContext(auth);
        await twitterCtx.TweetAsync(text);
    }

    IAuthorizer CreateAuthorizer()
    {
        var cred = credentials.Value;
        var prx = proxyOptions.Value;
        return new SingleUserAuthorizer
        {
            CredentialStore = new InMemoryCredentialStore
            {
                ConsumerKey = cred.ConsumerKey,
                ConsumerSecret = cred.ConsumerSecret,
                OAuthToken = cred.AccessToken,
                OAuthTokenSecret = cred.AccessTokenSecret
            },
            Proxy = new WebProxy(prx.Host, prx.Port)
            {
                Credentials = new NetworkCredential(prx.Username, prx.Password)
            }
        };
    }
}