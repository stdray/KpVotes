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
        var options = context.Configuration.GetSection(nameof(KpVotesJobOptions)).Get<KpVotesJobOptions>();
        Console.WriteLine("ConfigureServices: {0}", context.HostingEnvironment.EnvironmentName);
        services.AddSingleton(context.Configuration.GetSection("TwitterCredentials").Get<OAuthInfo>());
        services.AddSingleton(options);
        services.AddSingleton<IHtmlParser, HtmlParser>();
        services.AddSingleton<IKpClient, SeleniumClient>();
        services.AddSingleton<TweetService>();
        services.AddSingleton<KpVotesJob>();
    })
    .Build()
    .Services
    .GetRequiredService<KpVotesJob>()
    .ExecuteAsync(CancellationToken.None);