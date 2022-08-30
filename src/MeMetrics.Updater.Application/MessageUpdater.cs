using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AutoMapper;
using MeMetrics.Updater.Application.Helpers;
using MeMetrics.Updater.Application.Interfaces;
using MeMetrics.Updater.Application.Objects;
using MeMetrics.Updater.Application.Objects.MeMetrics;
using Microsoft.Extensions.Options;
using Serilog;
using Message = MeMetrics.Updater.Application.Objects.MeMetrics.Message;

[assembly: InternalsVisibleTo("MeMetrics.Updater.Application.Tests")]
namespace MeMetrics.Updater.Application
{
    public class MessageUpdater : IMessageUpdater
    {
        private readonly ILogger _logger;
        private readonly IGmailApi _gmailApi;
        private readonly IMeMetricsApi _memetricsApi;
        private readonly IOptions<EnvironmentConfiguration> _configuration;
        private readonly IMapper _mapper;
        private readonly int _daysToQuery = 2;
        private bool _hasFoundAllTodaysCalls = false;

        public MessageUpdater(
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

        public async Task<UpdaterResponse> GetAndSaveMessages()
        {
            try {
                _logger.Information("Starting message updater");
                await _gmailApi.Authenticate(_configuration.Value.Gmail_History_Refresh_Token);
                var transactionCount = 0;

                var labels = await _gmailApi.GetLabels();
                var smsLabel = labels.Labels.FirstOrDefault(l => l.Name == _configuration.Value.Gmail_Sms_Label)?.Id;

                var response = await _gmailApi.GetEmails(smsLabel);
                var messages = new List<Google.Apis.Gmail.v1.Data.Message>();
                messages.AddRange(response.Messages);

                var hasFoundAllTodaysCalls = false;
                while (!hasFoundAllTodaysCalls && messages.Any())
                {
                    var tasks = new List<Task<Message>>();
                    for (var i = 0; i < messages.Count; i++)
                    {
                        tasks.Add(ProcessMessage(messages[i].Id));
                    }

                    await Task.WhenAll(tasks);

                    List<Message> messagesToSave = new List<Message>();
                    foreach (var task in tasks)
                    {
                        var result = (task).Result;
                        if(result != null)
                        {
                            messagesToSave.Add(result);
                        }
                    }
                    transactionCount += messagesToSave.Count;
                    if (messagesToSave.Any())
                    {
                        await _memetricsApi.SaveMessages(messagesToSave);
                    }
                    messages.Clear();

                    if (!string.IsNullOrEmpty(response.NextPageToken) && !_hasFoundAllTodaysCalls)
                    {
                        Console.WriteLine("Getting next page of messages");
                        response = await _gmailApi.GetEmails(smsLabel, response.NextPageToken);
                        messages.AddRange(response.Messages);
                    }
                }

                _logger.Information($"Finished message updater. {transactionCount} messages successfully saved");
                return new UpdaterResponse() { Successful = true };
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to get and save messages");
                return new UpdaterResponse() { Successful = false, ErrorMessage = e.Message };
            }
        }

        internal async Task<Message> ProcessMessage(string messageId)
        {
            var email = await _gmailApi.GetEmail(messageId);
            var shouldSkipMessage = DateTimeOffset.FromUnixTimeMilliseconds((long)email.InternalDate) < DateTimeOffset.UtcNow.AddDays(-_daysToQuery);
            if (shouldSkipMessage)
            {
                _hasFoundAllTodaysCalls = true;
                return null;
            }
            var phoneNumber = Utility.FormatStringToPhoneNumber(email.Payload.Headers.First(x => x.Name == Constants.EmailHeader.PhoneNumber).Value);
            if (phoneNumber.Length < 11 || Constants.PhoneNumberBlacklist.Contains(phoneNumber))
            {
                return null;
            }

            var message = _mapper.Map<Message>(email, opt => opt.Items["Email"] = _configuration.Value.Gmail_Sms_Email_Address);
            message.Attachments = await GetAttachments(messageId, email);
            return message;
        }

        internal async Task<List<Attachment>> GetAttachments(string messageId, Google.Apis.Gmail.v1.Data.Message email)
        {
            var attachments = new List<Attachment>();
            for (var j = 0; j < email.Payload.Parts?.Count; j++)
            {
                if (email.Payload.Parts[j]?.Body?.AttachmentId != null)
                {
                    var base64 = await _gmailApi.GetAttachment(messageId, email.Payload.Parts[j].Body.AttachmentId);
                    attachments.Add(new Attachment(email.Payload.Parts[j].Body.AttachmentId, base64, email.Payload.Parts[j].Filename));
                }
            }

            return attachments;
        }
    }
}