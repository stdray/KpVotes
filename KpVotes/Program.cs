using AngleSharp.Html.Parser;
using KpVotes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SocialOpinionAPI.Core;
using SocialOpinionAPI.Services.Tweet;


await Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((_, config) => { config.AddEnvironmentVariables("KpVotes_"); })
    .ConfigureServices((context, services) =>
    {
        Console.WriteLine("ConfigureServices: {0}", context.HostingEnvironment.EnvironmentName);
        services.AddHttpClient();
        services.AddSingleton(context.Configuration.GetSection("TwitterCredentials").Get<OAuthInfo>());
        services.AddSingleton(context.Configuration.GetSection(nameof(KpVotesJobOptions)).Get<KpVotesJobOptions>());
        services.AddSingleton<IHtmlParser, HtmlParser>();
        services.AddSingleton<TweetService>();
        services.AddSingleton<KpVotesJob>();
    })
    .Build()
    .Services
    .GetRequiredService<KpVotesJob>()
    .ExecuteAsync(CancellationToken.None);
    