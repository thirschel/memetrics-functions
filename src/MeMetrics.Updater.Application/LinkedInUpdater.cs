using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MeMetrics.Updater.Application.Helpers;
using MeMetrics.Updater.Application.Interfaces;
using MeMetrics.Updater.Application.Objects;
using MeMetrics.Updater.Application.Objects.Enums;
using MeMetrics.Updater.Application.Objects.MeMetrics;
using Microsoft.Extensions.Options;
using Serilog;

namespace MeMetrics.Updater.Application
{
    public class RecruitmentMessageUpdater : IRecruitmentMessageUpdater
    {
        private int transactionCount = 0;

        private readonly ILogger _logger;
        private readonly ILinkedInApi _linkedInApi;
        private readonly IGmailApi _gmailApi;
        private readonly IMeMetricsApi _memetricsApi;
        private readonly IOptions<EnvironmentConfiguration> _configuration;

        public RecruitmentMessageUpdater(
            ILogger logger, 
            IOptions<EnvironmentConfiguration> configuration, 
            ILinkedInApi linkedInApi,
            IGmailApi gmailApi,
            IMeMetricsApi memetricsApi)
        {
            _logger = logger;
            _configuration = configuration;
            _linkedInApi = linkedInApi;
            _gmailApi = gmailApi;
            _memetricsApi = memetricsApi;
        }


        public async Task GetAndSaveLinkedInMessages()
        {
            var requiresAdditionalAuthentication = await _linkedInApi.Authenticate(_configuration.Value.LinkedIn_Username, _configuration.Value.LinkedIn_Password);
            if (requiresAdditionalAuthentication)
            {
                // Wait for the email to be sent and received
                await Task.Delay(15000);

                await _gmailApi.Authenticate(_configuration.Value.Gmail_Main_Refresh_Token);
                var labels = await _gmailApi.GetLabels();
                var labelId = labels.Labels.FirstOrDefault(l => l.Name == _configuration.Value.Gmail_LinkedIn_Label)?.Id;
                var messages = await _gmailApi.GetEmails(labelId);
                var message = await _gmailApi.GetEmail(messages.Messages[0].Id);
                var body = Utility.Decode(message.Payload.Parts[0].Body.Data);
                var regex = new Regex("Please use this verification code to complete your sign in: (\\d+)");
                var code = regex.Match(body).Groups[1].ToString();

                await _linkedInApi.SubmitPin(code);
            }
            await GetAndSaveRecruitmentMessages(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            _logger.Information($"{transactionCount} recruiter messages successfully saved");
        }

        public async Task GetAndSaveRecruitmentMessages(long createdBeforeTime)
        {
            var hasFoundAllTodaysCalls = false;
            var messages = await _linkedInApi.GetConversations(createdBeforeTime);
            for (var i = 0; i < messages.Elements.Length; i++)
            {
                if (hasFoundAllTodaysCalls)
                {
                    return;
                }
                var conversationRegex = new Regex("urn:li:fs_conversation:(\\w?\\d+_?\\d*)");
                var converationId = conversationRegex.Match(messages.Elements[i].EntityUrn).Groups[1].ToString();
                var events = await _linkedInApi.GetConversationEvents(converationId);

                if (messages.Elements[i]?.Participants[0]?.MessagingMember?.MiniProfile?.ObjectUrn == null)
                {
                    continue;
                }

                var recruiter = messages.Elements[i].Participants[0].MessagingMember.MiniProfile;
                var recruiterIdRegex = new Regex("urn:li:member:(\\d+)");
                var recruiterId = recruiterIdRegex.Match(recruiter.ObjectUrn).Groups[1].ToString();
                foreach (var element in events.Elements)
                {
                    hasFoundAllTodaysCalls = DateTimeOffset.FromUnixTimeMilliseconds(element.CreatedAt) < DateTimeOffset.UtcNow.AddDays(-2);
                    if (element.Subtype != "INMAIL" && element.Subtype != "INMAIL_REPLY" && element.Subtype != "MEMBER_TO_MEMBER" || hasFoundAllTodaysCalls)
                    {
                        continue;
                    }

                    var messageIdRegex = new Regex("urn:li:fs_event:\\(\\d+,(\\w+|\\d+|_)");
                    var messageId = messageIdRegex.Match(element.EntityUrn).Groups[1].ToString();

                    var message = new RecruitmentMessage()
                    {
                        RecruiterId = recruiterId,
                        RecruitmentMessageId = messageId,
                        RecruiterName = $"{recruiter.FirstName} {recruiter.LastName}",
                        RecruiterCompany = recruiter.Occupation,
                        MessageSource = RecruitmentMessageSource.LinkedIn,
                        Subject = element.EventContent.MessageEvent.Subject,
                        Body = element.EventContent.MessageEvent.Body == string.Empty ?
                            element.EventContent.MessageEvent.AttributedBody.Text :
                            element.EventContent.MessageEvent.Body,
                        OccurredDate = DateTimeOffset.FromUnixTimeMilliseconds(element.CreatedAt),
                        IsIncoming = element.From.MessagingMember.MiniProfile.ObjectUrn == recruiter.ObjectUrn
                    };
                    await _memetricsApi.SaveRecruitmentMessage(message);
                    transactionCount++;
                }
            }

            var lastMessage = messages.Elements[messages.Elements.Length - 1].Events[0];
            if (messages.Elements.Any())
            {
                await GetAndSaveRecruitmentMessages(lastMessage.CreatedAt);
            }
        }

        public async Task GetAndSaveEmailMessages()
        {
            await _gmailApi.Authenticate(_configuration.Value.Gmail_Main_Refresh_Token);
            var labels = await _gmailApi.GetLabels();
            var smsLabel = labels.Labels.FirstOrDefault(l => l.Name == "Recruiter")?.Id;

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
                    var headers = email.Payload.Headers.GroupBy(x => x.Name)
                        .ToDictionary(x => x.Key, x => string.Join("", x.Select(y => y.Value)));
                    var from = headers[Constants.EmailHeader.From];
                    var to = headers[Constants.EmailHeader.To];
                    var subject = headers[Constants.EmailHeader.Subject];
                    var date = headers[Constants.EmailHeader.Date];
                    var body = GetBody(email);
                    var regex = new Regex(@"(.*) <(.*)>", RegexOptions.IgnoreCase);
                    var withHeader = !from.Contains(_configuration.Value.Gmail_Recruiter_Email_Address) ? from : to;
                    withHeader = Regex.Replace(withHeader, "[\",]", "");
                    date =  Regex.Replace(date, @"\(\w{3}\)", "");
                    var match = regex.Match(withHeader);
                    var occurredDate = DateTimeOffset.Parse(date);
                    var message = new RecruitmentMessage()
                    {
                        RecruiterId = match.Groups[2].Value,
                        RecruitmentMessageId = messages[i].Id,
                        RecruiterName = match.Groups[1].Value,
                        RecruiterCompany = string.Empty,
                        MessageSource = RecruitmentMessageSource.DirectEmail,
                        Subject = subject,
                        Body = body,
                        OccurredDate = occurredDate,
                        IsIncoming = to.Contains(_configuration.Value.Gmail_Recruiter_Email_Address),
                    };
                    await _memetricsApi.SaveRecruitmentMessage(message);
                    transactionCount++;
      
                }

                response = await _gmailApi.GetEmails(smsLabel, response.NextPageToken);
                messages.Clear();
                messages.AddRange(response.Messages);
            }

            _logger.Information($"{transactionCount} messages successfully saved");
        }

        internal string GetBody(Google.Apis.Gmail.v1.Data.Message email)
        {
            var body = string.Empty;
            if (email.Payload.Parts != null)
            {
                body = GetBodyFromMessagePart(email.Payload.Parts.ToList());
            }
            else
            {
                body =  email.Payload.Body.Data ?? string.Empty;
            }

            return string.IsNullOrEmpty(body) ? string.Empty : Utility.Decode(body);
        }

        internal string GetBodyFromMessagePart(List<Google.Apis.Gmail.v1.Data.MessagePart> parts)
        {
            for (var i = 0; i < parts.Count; i++)
            {
                if (parts[i].MimeType.Contains("text/plain"))
                {
                    return parts[i].Body.Data;
                }
                if (parts[i].Parts != null)
                {
                    return GetBodyFromMessagePart(parts[i].Parts.ToList());
                }
            }

            return string.Empty;
        }
    }
}