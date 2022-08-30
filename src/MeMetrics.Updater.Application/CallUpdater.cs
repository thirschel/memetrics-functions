using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AutoMapper;
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
        private readonly int _daysToQuery = 2;
        private bool _hasFoundAllTodaysCalls = false;
        private Regex _snippetRegex = new Regex(@"(\d+)s \(\d+:\d+:\d+\) (\d+) \((incoming|outgoing) call\)",
                    RegexOptions.IgnoreCase);


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

        public async Task<UpdaterResponse> GetAndSaveCalls()
        {
            try
            {
                _logger.Information("Starting call updater");
                await _gmailApi.Authenticate(_configuration.Value.Gmail_History_Refresh_Token);
                var transactionCount = 0;
                var labels = await _gmailApi.GetLabels();
                var callLabelId = labels.Labels.FirstOrDefault(l => l.Name == _configuration.Value.Gmail_Call_Log_Label)?.Id;

                var response = await _gmailApi.GetEmails(callLabelId);
                var messages = new List<Message>();
                messages.AddRange(response.Messages);

                var hasFoundAllTodaysCalls = false;
   
                while (!hasFoundAllTodaysCalls && messages.Any())
                {
                    var tasks = new List<Task<Call>>();
                    for (var i = 0; i < messages.Count; i++)
                    {
                        tasks.Add(ProcessCall(messages[i].Id));
                    }

                    await Task.WhenAll(tasks);

                    var callsToSave = new List<Call>();
                    foreach (var task in tasks)
                    {
                        var result = (task).Result;
                        if (result != null)
                        {
                            callsToSave.Add(result);
                        }
                    }
                    transactionCount += callsToSave.Count;
                    if(callsToSave.Any())
                    {
                        await _memetricsApi.SaveCalls(callsToSave);
                    }

                    messages.Clear();

                    if (!string.IsNullOrEmpty(response.NextPageToken) && !_hasFoundAllTodaysCalls)
                    {
                        response = await _gmailApi.GetEmails(callLabelId, response.NextPageToken);
                        messages.AddRange(response.Messages);
                    }
                }

                _logger.Information($"Finished call updater. {transactionCount} call(s) successfully saved");
                return new UpdaterResponse() { Successful = true };
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to get and save calls");
                return new UpdaterResponse() { Successful = false, ErrorMessage = e.Message };
            }
        }

        internal async Task<Call> ProcessCall(string messageId)
        {
            var email = await _gmailApi.GetEmail(messageId);
            var hasFoundAllTodaysCalls = DateTimeOffset.FromUnixTimeMilliseconds((long)email.InternalDate) <
                                     DateTimeOffset.UtcNow.AddDays(-_daysToQuery);

            var emailMatch = _snippetRegex.Match(email.Snippet);

            // Cases that don't match are missed or rejected calls or emails that are not formatted correctly
            if (!emailMatch.Success)
            {
                return null;
            }
            if(hasFoundAllTodaysCalls)
            {
                _hasFoundAllTodaysCalls = true;
                return null;
            }

            var call = _mapper.Map<Call>(email);
            return call;
        }

    }
}