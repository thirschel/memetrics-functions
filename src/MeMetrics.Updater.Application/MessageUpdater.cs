using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
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

        public async Task GetAndSaveMessages()
        {
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
                for (var i = 0; i < messages.Count; i++)
                {
                    var email = await _gmailApi.GetEmail(messages[i].Id);
                    hasFoundAllTodaysCalls = DateTimeOffset.FromUnixTimeMilliseconds((long)email.InternalDate) < DateTimeOffset.UtcNow.AddDays(-2);
                    if (hasFoundAllTodaysCalls)
                    {
                        return;
                    }

                    var message = _mapper.Map<Message>(email, opt => opt.Items["Email"] = _configuration.Value.Gmail_Sms_Email_Address);
                    message.Attachments = await GetAttachments(messages[i].Id, email);
           
                    await _memetricsApi.SaveMessage(message);
                    transactionCount++;
                }

                messages.Clear();

                if (!string.IsNullOrEmpty(response.NextPageToken))
                {
                    response = await _gmailApi.GetEmails(smsLabel, response.NextPageToken);
                    messages.AddRange(response.Messages);
                }
            }

            _logger.Information($"{transactionCount} messages successfully saved");
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