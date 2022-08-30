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
using MeMetrics.Updater.Application.Objects.LinkedIn;

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
        private bool _hasFoundAllTodaysCallsLinkedIn = false;

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

        public async Task<UpdaterResponse> GetAndSaveLinkedInMessages()
        {
            try {
                _logger.Information("Starting LinkedIn message updater");
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
                _logger.Information($"Finished LinkedIn message updater. {transactionCount} messages successfully saved");
                return new UpdaterResponse() { Successful = true };
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to get and save LinkedIn messages");
                return new UpdaterResponse() { Successful = false, ErrorMessage = e.Message };
            }
        }

        internal async Task<int> GetAndSaveRecruitmentMessages(long createdBeforeTime, int transactionCount)
        {
            var messages = await _linkedInApi.GetConversations(createdBeforeTime);
            var tasks = new List<Task<int>>();
            for (var i = 0; i < messages.Elements.Length; i++)
            {
                tasks.Add(ProcessLinkedInConversation(messages.Elements[i]));
            }

            await Task.WhenAll(tasks);

            foreach (var task in tasks)
            {
                var result = (task).Result;
                transactionCount += result;
            }

            var lastMessage = messages.Elements[messages.Elements.Length - 1].Events[0];
            if (messages.Elements.Any() && !_hasFoundAllTodaysCallsLinkedIn)
            {
                return await GetAndSaveRecruitmentMessages(lastMessage.CreatedAt, transactionCount);
            }

            return transactionCount;
        }

        public async Task<UpdaterResponse> GetAndSaveEmailMessages()
        {
            try {
                _logger.Information("Starting recruiter message updater");
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
                    var tasks = new List<Task<RecruitmentMessage>>();
                    for (var i = 0; i < messages.Count; i++)
                    {
                        tasks.Add(ProcessMessage(messages[i].Id));
                    }

                    await Task.WhenAll(tasks);

                    List<RecruitmentMessage> messagesToSave = new List<RecruitmentMessage>();
                    foreach (var task in tasks)
                    {
                        var result = (task).Result;
                        if (result != null)
                        {
                            messagesToSave.Add(result);
                        }
                    }
                    if(messagesToSave.Any())
                    {
                        await _memetricsApi.SaveRecruitmentMessages(messagesToSave);
                    }
                    transactionCount+= messagesToSave.Count;

                    // Some messages were skipped because they happened before the query date
                    if(messages.Count != messagesToSave.Count)
                    {
                        _logger.Information($"Finished recruiter message updater. {transactionCount} messages successfully saved");
                        return new UpdaterResponse() { Successful = true };
                    }

                    messages.Clear();

                    if (!string.IsNullOrEmpty(response.NextPageToken))
                    {
                        response = await _gmailApi.GetEmails(recruiterLabel, response.NextPageToken);
                        messages.AddRange(response.Messages);
                    }
                }

                _logger.Information($"Finished recruiter message updater. {transactionCount} messages successfully saved");
                return new UpdaterResponse() { Successful = true };
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to get and save email recruitment messages");
                return new UpdaterResponse() { Successful = false, ErrorMessage = e.Message };
            }
        }

        internal async Task<int> ProcessLinkedInConversation(Element message)
        {
            var conversationRegex = new Regex("urn:li:fs_conversation:(.*)");
            var conversationId = conversationRegex.Match(message.EntityUrn).Groups[1].ToString();
            var events = await _linkedInApi.GetConversationEvents(conversationId);

            if (message?.Participants[0]?.MessagingMember?.MiniProfile?.ObjectUrn == null)
            {
                return 0;
            }
            var recruiter = message.Participants.FirstOrDefault(p => p.MessagingMember.MiniProfile.ObjectUrn != _configuration.Value.LinkedIn_ObjectUrn);

            var tasks = new List<Task<RecruitmentMessage>>();
            foreach (var element in events.Elements)
            {
                tasks.Add(ProcessLinkedInMessage(recruiter.MessagingMember.MiniProfile, element));
            }

            await Task.WhenAll(tasks);

            List<RecruitmentMessage> messagesToSave = new List<RecruitmentMessage>();
            foreach (var task in tasks)
            {
                var result = (task).Result;
                if (result != null)
                {
                    messagesToSave.Add(result);
                }
            }
            if(messagesToSave.Any()) 
            {
                await _memetricsApi.SaveRecruitmentMessages(messagesToSave);
            }
            return messagesToSave.Count;
        }

        internal async Task<RecruitmentMessage> ProcessLinkedInMessage(MiniProfile recruiterProfile, ConversationEvents.Element message)
        {
            var subTypes = Enum.GetValues(typeof(LinkedInMessageSubType)).Cast<LinkedInMessageSubType>().Select(x => x.GetDescription());
            var hasFoundAllTodaysMessages = DateTimeOffset.FromUnixTimeMilliseconds(message.CreatedAt) < DateTimeOffset.UtcNow.AddDays(-_daysToQuery);
            if (!subTypes.Contains(message.Subtype))
            {
                return null;
            }
            if (hasFoundAllTodaysMessages)
            {
                _hasFoundAllTodaysCallsLinkedIn = true;
                return null;
            }

            var recruitmentMessage = _mapper.Map<RecruitmentMessage>(message);
            recruitmentMessage.IsIncoming = message.From.MessagingMember.MiniProfile.ObjectUrn != _configuration.Value.LinkedIn_ObjectUrn;
            return _mapper.Map(recruiterProfile, recruitmentMessage);
        }

        internal async Task<RecruitmentMessage> ProcessMessage(string messageId)
        {
            var email = await _gmailApi.GetEmail(messageId);
            var hasFoundAllTodaysCalls = DateTimeOffset.FromUnixTimeMilliseconds(email.InternalDate.Value) < DateTimeOffset.UtcNow.AddDays(-_daysToQuery);
            if (hasFoundAllTodaysCalls)
            {
                return null;
            }

            return _mapper.Map<RecruitmentMessage>(email, opt => opt.Items["Email"] = _configuration.Value.Gmail_Recruiter_Email_Address);
        }
    }
}