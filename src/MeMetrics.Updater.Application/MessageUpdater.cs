using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MeMetrics.Updater.Application.Helpers;
using MeMetrics.Updater.Application.Interfaces;
using MeMetrics.Updater.Application.Objects;
using MeMetrics.Updater.Application.Objects.MeMetrics;
using Microsoft.Extensions.Options;
using Serilog;
using Message = MeMetrics.Updater.Application.Objects.MeMetrics.Message;

namespace MeMetrics.Updater.Application
{
    public class MessageUpdater : IMessageUpdater
    {
        private readonly ILogger _logger;
        private readonly IGmailApi _gmailApi;
        private readonly IMeMetricsApi _memetricsApi;
        private readonly IOptions<EnvironmentConfiguration> _configuration;

        public MessageUpdater(
            ILogger logger, 
            IOptions<EnvironmentConfiguration> configuration, 
            IGmailApi gmailApi,
            IMeMetricsApi memetricsApi)
        {
            _logger = logger;
            _configuration = configuration;
            _gmailApi = gmailApi;
            _memetricsApi = memetricsApi;
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

            while (!hasFoundAllTodaysCalls && !string.IsNullOrEmpty(response.NextPageToken))
            {
                for (var i = 0; i < messages.Count; i++)
                {
                    if (hasFoundAllTodaysCalls) return;
                    var email = await _gmailApi.GetEmail(messages[i].Id);
                    hasFoundAllTodaysCalls = DateTimeOffset.FromUnixTimeMilliseconds((long)email.InternalDate) < DateTimeOffset.UtcNow.AddDays(-2);

                    var headers = email.Payload.Headers.ToDictionary(x => x.Name, y => y.Value);
                    var from = headers[Constants.EmailHeader.From];
                    var to = headers[Constants.EmailHeader.To];
                    var threadId = headers[Constants.EmailHeader.ThreadId];
                    var occurredDate = DateTimeOffset.Parse(headers[Constants.EmailHeader.Date]);
                    var isIncoming = from.ToLower() != _configuration.Value.Gmail_Sms_Email_Address;
                    var withHeader = isIncoming ? from : to;
                    var phoneNumber = Utility.FormatStringToPhoneNumber(headers[Constants.EmailHeader.PhoneNumber]);
                    var isMedia = email.Payload.Parts != null && email.Payload.Parts.Any(p => p.Body?.AttachmentId != null);
                    var attachments = isMedia ? new List<Attachment>() : await GetAttachments(messages[i].Id, email);
                    var nameRegex = new Regex(@"(.*) <.*>", RegexOptions.IgnoreCase);
                    var name = nameRegex.Match(withHeader).Groups[1].Value.Trim();
                    var body = GetBody(email);

                    var message = new Message
                    {
                        MessageId = messages[i].Id,
                        OccurredDate = occurredDate,
                        PhoneNumber = phoneNumber,
                        Name = name,
                        IsIncoming = isIncoming,
                        Text = body,
                        IsMedia = isMedia,
                        TextLength = body.Length,
                        ThreadId = int.Parse(threadId),
                        Attachments = attachments
                    };
                    await _memetricsApi.SaveMessage(message);
                    transactionCount++;
                }

                response = await _gmailApi.GetEmails(smsLabel, response.NextPageToken);
                messages.Clear();
                messages.AddRange(response.Messages);
            }

            _logger.Information($"{transactionCount} messages successfully saved");
        }

        private string GetBody(Google.Apis.Gmail.v1.Data.Message email)
        {
            var body = string.Empty;
            if (email.Payload.Parts != null)
            {
                var textPart = email.Payload.Parts.FirstOrDefault(p => p.MimeType.Contains("text/plain"));
                body = textPart != null ? textPart.Body.Data : body;
            }
            else
            {
                body = email.Payload.Body.Data ?? string.Empty;
            }
            return body == string.Empty ? string.Empty : Utility.Decode(body);
        }

        private async Task<List<Attachment>> GetAttachments(string messageId, Google.Apis.Gmail.v1.Data.Message email)
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