using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AutoMapper;
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

        private readonly ILogger _logger;
        private readonly IOptions<EnvironmentConfiguration> _configuration;
        private readonly ILinkedInApi _linkedInApi;
        private readonly IGmailApi _gmailApi;
        private readonly IMeMetricsApi _memetricsApi;
        private readonly IMapper _mapper;
        private readonly int _daysToQuery = 2;

        public RecruitmentMessageUpdater(
            ILogger logger,
            IOptions<EnvironmentConfiguration> configuration,
            ILinkedInApi linkedInApi,
            IGmailApi gmailApi,
            IMeMetricsApi memetricsApi,
            IMapper mapper)
        {
            _logger = logger;
            _configuration = configuration;
            _linkedInApi = linkedInApi;
            _gmailApi = gmailApi;
            _memetricsApi = memetricsApi;
            _mapper = mapper;
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
            var transactionCount = await GetAndSaveRecruitmentMessages(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), 0);
            _logger.Information($"{transactionCount} recruiter messages successfully saved");
        }

        public async Task<int> GetAndSaveRecruitmentMessages(long createdBeforeTime, int transactionCount)
        {
            var hasFoundAllTodaysCalls = false;
            var messages = await _linkedInApi.GetConversations(createdBeforeTime);
            for (var i = 0; i < messages.Elements.Length; i++)
            {
                if (hasFoundAllTodaysCalls)
                {
                    return transactionCount;
                }
                var conversationRegex = new Regex("urn:li:fs_conversation:(\\w?\\d+_?\\d*)");
                var conversationId = conversationRegex.Match(messages.Elements[i].EntityUrn).Groups[1].ToString();
                var events = await _linkedInApi.GetConversationEvents(conversationId);

                if (messages.Elements[i]?.Participants[0]?.MessagingMember?.MiniProfile?.ObjectUrn == null)
                {
                    continue;
                }
                var recruiter = messages.Elements[i].Participants.FirstOrDefault(p => p.MessagingMember.MiniProfile.ObjectUrn != _configuration.Value.LinkedIn_ObjectUrn);

                foreach (var element in events.Elements)
                {
                    var subTypes = Enum.GetValues(typeof(LinkedInMessageSubType)).Cast<LinkedInMessageSubType>().Select(x => x.GetDescription());
                    hasFoundAllTodaysCalls = DateTimeOffset.FromUnixTimeMilliseconds(element.CreatedAt) < DateTimeOffset.UtcNow.AddDays(-_daysToQuery);
                    if (!subTypes.Contains(element.Subtype) || hasFoundAllTodaysCalls)
                    {
                        continue;
                    }

                    var recruitmentMessage = _mapper.Map<RecruitmentMessage>(element);
                    recruitmentMessage = _mapper.Map(recruiter.MessagingMember.MiniProfile, recruitmentMessage);
                    
                    await _memetricsApi.SaveRecruitmentMessage(recruitmentMessage);
                    transactionCount++;
                }
            }

            var lastMessage = messages.Elements[messages.Elements.Length - 1].Events[0];
            if (messages.Elements.Any())
            {
                return await GetAndSaveRecruitmentMessages(lastMessage.CreatedAt, transactionCount);
            }

            return transactionCount;
        }

        public async Task GetAndSaveEmailMessages()
        {
            var transactionCount = 0;
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
                    hasFoundAllTodaysCalls = DateTimeOffset.FromUnixTimeMilliseconds(email.InternalDate.Value) < DateTimeOffset.UtcNow.AddDays(-_daysToQuery);
                    if (hasFoundAllTodaysCalls) return;
                    
                    var message = _mapper.Map<RecruitmentMessage>(email, opt => opt.Items["Email"] = _configuration.Value.Gmail_Recruiter_Email_Address);

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