using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Reflection;
using AutoMapper;
using MeMetrics.Updater.Application;
using MeMetrics.Updater.Application.Interfaces;
using MeMetrics.Updater.Application.Objects;
using MeMetrics.Updater.Application.Profiles;
using MeMetrics.Updater.Functions;
using MeMetrics.Updater.Infrastructure.Gmail;
using MeMetrics.Updater.Infrastructure.GroupMe;
using MeMetrics.Updater.Infrastructure.LinkedIn;
using MeMetrics.Updater.Infrastructure.Lyft;
using MeMetrics.Updater.Infrastructure.MeMetrics;
using MeMetrics.Updater.Infrastructure.PersonalCapital;
using MeMetrics.Updater.Infrastructure.Uber;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;

[assembly: WebJobsStartup(typeof(Startup))]
namespace MeMetrics.Updater.Functions
{
    [ExcludeFromCodeCoverage]
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // enable IHttpClientFactory
            builder.Services.AddHttpClient();

            var environmentConfiguration = new EnvironmentConfiguration();
            // enable IOptions
            builder.Services
                .AddOptions<EnvironmentConfiguration>()
                .Configure<IConfiguration>((settings, configuration) =>
                {
                    configuration.Bind(settings);
                    configuration.Bind(environmentConfiguration);
                });

            //enable serilog
            var config = TelemetryConfiguration.CreateDefault();
            // This needs to be set to see requests for some reason
            config.InstrumentationKey = Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY", EnvironmentVariableTarget.Process);
            var logger = new LoggerConfiguration()
                .WriteTo.Console()
                .Enrich.FromLogContext()
                .WriteTo.ApplicationInsights(config, TelemetryConverter.Traces)
                .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
                .CreateLogger();
            builder.Services.AddLogging(lb => lb.AddSerilog(logger));
            builder.Services.AddSingleton<ILogger>(c => logger);

            // Service DI
            builder.Services.AddScoped<IMessageUpdater, MessageUpdater>();
            builder.Services.AddScoped<ICallUpdater, CallUpdater>();
            builder.Services.AddScoped<IChatMessageUpdater, ChatMessageUpdater>();
            builder.Services.AddScoped<IRideUpdater, RideUpdater>();
            builder.Services.AddScoped<IRecruitmentMessageUpdater, RecruitmentMessageUpdater>();
            builder.Services.AddScoped<ITransactionUpdater, TransactionUpdater>();
            builder.Services.AddScoped<ICacheUpdater, CacheUpdater>();

            builder.Services.AddScoped<IPersonalCapitalApi, PersonalCapitalApi>();
            builder.Services.AddScoped<IGmailApi, GmailApi>();
            builder.Services.AddScoped<IUberRidersApi, UberRidersApi>();
            builder.Services.AddScoped<ILyftApi, LyftApi>();
            builder.Services.AddScoped<ILinkedInApi, LinkedInApi>();
            builder.Services.AddScoped<IGroupMeApi, GroupMeApi>();
            builder.Services.AddScoped<IMeMetricsApi, MeMetricsApi>();

            var configuration = new MapperConfiguration(cfg => { 
                cfg.AddProfile<CallProfile>(); 
                cfg.AddProfile<ChatMessageProfile>(); 
                cfg.AddProfile<MessageProfile>(); 
                cfg.AddProfile<RecruitmentMessageProfile>(); 
                cfg.AddProfile<RideProfile>(); 
                cfg.AddProfile<TransactionProfile>(); 
            });
            builder.Services.AddSingleton<IMapper>(new Mapper(configuration));

            // IHttpClientFactory named instances
            builder.Services.AddHttpClient(Constants.HttpClients.DisabledAutomaticCookieHandling)
                .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler()
                {
                    UseCookies = false,
                });
        }
    }
}