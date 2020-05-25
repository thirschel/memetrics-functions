using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AutoMapper;
using MeMetrics.Updater.Application.Helpers;
using MeMetrics.Updater.Application.Interfaces;
using MeMetrics.Updater.Application.Objects;
using MeMetrics.Updater.Application.Objects.MeMetrics;
using Microsoft.Extensions.Options;
using Serilog;
using Message = Google.Apis.Gmail.v1.Data.Message;

namespace MeMetrics.Updater.Application
{
    public class CallUpdater : ICallUpdater
    {
        private readonly ILogger _logger;
        private readonly IOptions<EnvironmentConfiguration> _configuration;
        private readonly IGmailApi _gmailApi;
        private readonly IMeMetricsApi _memetricsApi;
        private readonly IMapper _mapper;

        public CallUpdater(
            ILogger logger,
            IOptions<EnvironmentConfiguration> configuration,
            IGmailApi gmailApi,
            IMeMetricsApi memetricsApi,
            IMapper mapper)
        {
            _logger = logger;
            _configuration = configuration;
            _gmailApi = gmailApi;
            _memetricsApi = memetricsApi;
            _mapper = mapper;
        }

        public async Task GetAndSaveCalls()
        {
            await _gmailApi.Authenticate(_configuration.Value.Gmail_History_Refresh_Token);
            var transactionCount = 0;
            var labels = await _gmailApi.GetLabels();
            var callLabelId = labels.Labels.FirstOrDefault(l => l.Name == _configuration.Value.Gmail_Call_Log_Label)?.Id;

            var response = await _gmailApi.GetEmails(callLabelId);
            var messages = new List<Message>();
            messages.AddRange(response.Messages);

            var hasFoundAllTodaysCalls = false;
            var snippetRegex = new Regex(@"(\d+)s \(\d+:\d+:\d+\) (\d+) \((incoming|outgoing) call\)",RegexOptions.IgnoreCase);

            while (!hasFoundAllTodaysCalls && messages.Any())
            {
                for (var i = 0; i < messages.Count; i++)
                {
                    if (hasFoundAllTodaysCalls) return;
                    var email = await _gmailApi.GetEmail(messages[i].Id);
                    hasFoundAllTodaysCalls = DateTimeOffset.FromUnixTimeMilliseconds((long)email.InternalDate) < DateTimeOffset.UtcNow.AddDays(-2);

                    var emailMatch = snippetRegex.Match(email.Snippet);

                    // Cases that don't match are missed or rejected calls or emails that are not formatted correctly
                    if (!emailMatch.Success || hasFoundAllTodaysCalls)
                    {
                        continue;
                    }

                    var call = _mapper.Map<Call>(email);

                    await _memetricsApi.SaveCall(call);
                    transactionCount++;
                }
                messages.Clear();

                if (!string.IsNullOrEmpty(response.NextPageToken))
                {
                    response = await _gmailApi.GetEmails(callLabelId, response.NextPageToken);
                    messages.AddRange(response.Messages);
                }
            }

            _logger.Information($"{transactionCount} calls successfully saved");
        }
    }
}