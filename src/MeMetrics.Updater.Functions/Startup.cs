﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using MeMetrics.Updater.Application;
using MeMetrics.Updater.Application.Interfaces;
using MeMetrics.Updater.Application.Objects;
using MeMetrics.Updater.Functions;
using MeMetrics.Updater.Infrastructure.Gmail;
using MeMetrics.Updater.Infrastructure.GroupMe;
using MeMetrics.Updater.Infrastructure.LinkedIn;
using MeMetrics.Updater.Infrastructure.MeMetrics;
using MeMetrics.Updater.Infrastructure.PersonalCapital;
using MeMetrics.Updater.Infrastructure.Uber;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

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
            builder.Services.AddScoped<ILinkedInApi, LinkedInApi>();
            builder.Services.AddScoped<IGroupMeApi, GroupMeApi>();
            builder.Services.AddScoped<IMeMetricsApi, MeMetricsApi>();

            // IHttpClientFactory named instances
            builder.Services.AddHttpClient(Constants.HttpClients.DisabledAutomaticCookieHandling)
                .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler()
                {
                    UseCookies = false,
                });
        }
    }
}