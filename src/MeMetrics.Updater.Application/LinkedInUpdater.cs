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
                    var subTypes = Enum.GetValues(typeof(LinkedInMessageSubType)).Cast<LinkedInMessageSubType>().Select(x => x.GetDescription());
                    hasFoundAllTodaysCalls = DateTimeOffset.FromUnixTimeMilliseconds(element.CreatedAt) < DateTimeOffset.UtcNow.AddDays(-2);
                    if (!subTypes.Contains(element.Subtype) || hasFoundAllTodaysCalls)
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
            var recruiterLabel = labels.Labels.FirstOrDefault(l => l.Name == _configuration.Value.Gmail_Recruiter_Label)?.Id;

            var response = await _gmailApi.GetEmails(recruiterLabel);
            var messages = new List<Google.Apis.Gmail.v1.Data.Message>();
            messages.AddRange(response.Messages);

            var hasFoundAllTodaysCalls = false;
            while (!hasFoundAllTodaysCalls && messages.Any())
            {
                for (var i = 0; i < messages.Count; i++)
                {
                    var email = await _gmailApi.GetEmail(messages[i].Id);
                    hasFoundAllTodaysCalls = DateTimeOffset.FromUnixTimeMilliseconds(email.InternalDate.Value) < DateTimeOffset.UtcNow.AddDays(-2);
                    if (hasFoundAllTodaysCalls) return;
                    var headers = email.Payload.Headers.GroupBy(x => x.Name).ToDictionary(x => x.Key, x => string.Join("", x.Select(y => y.Value)));
                    var from = headers[Constants.EmailHeader.From];
                    var to = headers[Constants.EmailHeader.To];
                    var subject = headers[Constants.EmailHeader.Subject];
                    var body = EmailHelper.GetBody(email);
                    var regex = new Regex(@"(.*) <(.*)>", RegexOptions.IgnoreCase);
                    var withHeader = !from.Contains(_configuration.Value.Gmail_Recruiter_Email_Address) ? from : to;
                    withHeader = Regex.Replace(withHeader, "[\",]", "");
                    var match = regex.Match(withHeader);
                    var message = new RecruitmentMessage()
                    {
                        RecruiterId = match.Groups[2].Value,
                        RecruitmentMessageId = messages[i].Id,
                        RecruiterName = match.Groups[1].Value,
                        RecruiterCompany = string.Empty,
                        MessageSource = RecruitmentMessageSource.DirectEmail,
                        Subject = subject,
                        Body = body,
                        OccurredDate = DateTimeOffset.FromUnixTimeMilliseconds(email.InternalDate.Value),
                        IsIncoming = to.Contains(_configuration.Value.Gmail_Recruiter_Email_Address),
                    };
                    await _memetricsApi.SaveRecruitmentMessage(message);
                    transactionCount++;

                }

                messages.Clear();

                if (!string.IsNullOrEmpty(response.NextPageToken))
                {
                    response = await _gmailApi.GetEmails(recruiterLabel, response.NextPageToken);
                    messages.AddRange(response.Messages);
                }
            }

            _logger.Information($"{transactionCount} messages successfully saved");
        }
    }
}