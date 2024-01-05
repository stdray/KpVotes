using AngleSharp.Html.Parser;
using KpVotes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SocialOpinionAPI.Core;
using SocialOpinionAPI.Services.Tweet;


var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((_, config) => { config.AddEnvironmentVariables("KpVotes_"); })
    .ConfigureServices((context, services) =>
    {
        Console.WriteLine("Begin ConfigureServices: {0}", context.HostingEnvironment.EnvironmentName);
        services.AddHttpClient();
        services.AddSingleton(context.Configuration.GetSection("TwitterCredentials").Get<OAuthInfo>());
        services.AddSingleton(context.Configuration.GetSection(nameof(KpVotesJobOptions)).Get<KpVotesJobOptions>());
        services.AddSingleton<IHtmlParser, HtmlParser>();
        services.AddSingleton<TweetService>();
        services.AddSingleton<KpVotesJob>();
    })
    .Build();
await host.Services
    .GetRequiredService<KpVotesJob>()
    .ExecuteAsync(CancellationToken.None);
return 0;

    