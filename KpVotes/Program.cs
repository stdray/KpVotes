using AngleSharp.Html.Parser;
using KpVotes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Quartz;
using SocialOpinionAPI.Core;
using SocialOpinionAPI.Services.Tweet;


Host.CreateDefaultBuilder(args)
    .UseWindowsService()
    .ConfigureAppConfiguration((_, config) => { config.AddEnvironmentVariables("KpVotes_"); })
    .ConfigureLogging((_, logging) =>
    {
        logging.ClearProviders();
        logging.AddNLog();
    })
    .ConfigureServices((context, services) =>
    {
        Console.WriteLine("ConfigureServices: {0}", context.HostingEnvironment.EnvironmentName);
        services.AddHttpClient(string.Empty, http => http.Timeout = TimeSpan.FromMinutes(5));
        services.AddSingleton(context.Configuration.GetSection("TwitterCredentials").Get<OAuthInfo>());
        services.AddSingleton(context.Configuration.GetSection(nameof(KpVotesJobOptions)).Get<KpVotesJobOptions>());
        services.AddSingleton<IHtmlParser, HtmlParser>();
        services.AddSingleton<TweetService>();
        services.AddSingleton<KpVotesJob>();

        // Quartz.NET
        services.AddQuartz(q =>
        {
            var jobKey = new JobKey("KpVotesJob");
            q.AddJob<KpVotesJob>(opts => opts.WithIdentity(jobKey));
            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("KpVotesJob-trigger")
                .WithSimpleSchedule(x => x
                    .WithInterval(TimeSpan.FromHours(1))
                    .RepeatForever()
                )
            );
        });
        services.AddQuartzHostedService(q =>
        {
            q.WaitForJobsToComplete = true;
            q.AwaitApplicationStarted = true;
        });
    })
    .Build()
    .Run();
