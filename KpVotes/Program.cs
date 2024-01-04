using KpVotes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Quartz;
using SocialOpinionAPI.Core;
using SocialOpinionAPI.Services.Tweet;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((_, config) => { config.AddEnvironmentVariables("KpVotes_"); })
    .ConfigureServices((context, services) =>
    {
        Console.WriteLine("Begin ConfigureServices: {0}", context.HostingEnvironment.EnvironmentName);
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddConfiguration(context.Configuration.GetSection("NLog"));
            loggingBuilder.AddNLog();
        });

        services.AddSingleton(context.Configuration.GetSection("TwitterCredentials").Get<OAuthInfo>());
        services.AddSingleton<TweetService>();

        var jobOptions = context.Configuration.GetSection(nameof(KpVotesJobOptions)).Get<KpVotesJobOptions>();
        services.AddSingleton(jobOptions);
        services.AddSingleton<KpVotesJob>();

        services.AddQuartz(quartz =>
        {
            quartz.ScheduleJob<KpVotesJob>(trigger => trigger
                .WithIdentity($"{nameof(KpVotesJob)}Cron")
                .WithCronSchedule(jobOptions.Cron, b => b.WithMisfireHandlingInstructionDoNothing()));
            if (jobOptions.StartNow)
                quartz.ScheduleJob<KpVotesJob>(trigger => trigger
                    .WithIdentity($"{nameof(KpVotesJob)}StartNow")
                    .WithSimpleSchedule(b => b.WithMisfireHandlingInstructionNextWithRemainingCount())
                    .StartAt(DateTimeOffset.UtcNow.AddSeconds(2)));
        });
        services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = true;
            options.AwaitApplicationStarted = true;
        });
        Console.WriteLine("End ConfigureServices");
    })
    .Build();

var logger = host.Services.GetService<ILogger<Program>>();
logger.LogInformation("Start");
host.Run();
logger.LogInformation("Stop");