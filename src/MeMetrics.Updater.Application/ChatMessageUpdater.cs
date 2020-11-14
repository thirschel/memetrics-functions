using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AutoMapper;
using MeMetrics.Updater.Application.Interfaces;
using MeMetrics.Updater.Application.Objects;
using MeMetrics.Updater.Application.Objects.GroupMe;
using MeMetrics.Updater.Application.Objects.MeMetrics;
using Microsoft.Extensions.Options;
using Serilog;

namespace MeMetrics.Updater.Application
{
    public class ChatMessageUpdater : IChatMessageUpdater
    {
        private readonly ILogger _logger;
        private readonly IGroupMeApi _groupMeApi;
        private readonly IMeMetricsApi _memetricsApi;
        private readonly IMapper _mapper;
        private readonly int _daysToQuery = 2;


        public ChatMessageUpdater(
            ILogger logger, 
            IOptions<EnvironmentConfiguration> configuration,
            IGroupMeApi groupMeApi,
            IMeMetricsApi memetricsApi,
            IMapper mapper)
        {
            _logger = logger;
            _groupMeApi = groupMeApi;
            _memetricsApi = memetricsApi;
            _mapper = mapper;
            _groupMeApi.Authenticate(configuration.Value.GroupMe_Access_Token);
        }

        public async Task GetAndSaveChatMessages()
        {
            var groupResponse = await _groupMeApi.GetGroups();
            var transactionCount = 0;
            for (var i = 0; i < groupResponse.Groups.Length; i++)
            {
                transactionCount = await GetAndSaveMessages(groupResponse.Groups[i], transactionCount);
            }

            _logger.Information($"{transactionCount} groupme messages successfully saved");
        }

        private async Task<int> GetAndSaveMessages(Group group, int transactionCount = 0, string lastMessageId = null)
        {
            var processedAllTodaysMessages = false;
            var messageResponse = await _groupMeApi.GetMessages(group.Id, lastMessageId);
            if (messageResponse == null || !messageResponse.Response.Messages.Any())
            {
                return transactionCount;
            }

            foreach (var message in messageResponse.Response.Messages)
            {
                processedAllTodaysMessages = DateTimeOffset.FromUnixTimeMilliseconds(message.CreatedAt) <
                                             DateTimeOffset.UtcNow.AddDays(-_daysToQuery);
                if (message.Event != null || processedAllTodaysMessages)
                {
                    continue;
                }

                var chatMessage = _mapper.Map<ChatMessage>(message);
                chatMessage.GroupId = group.Id;
                chatMessage.GroupName = group.Name;

                await _memetricsApi.SaveChatMessage(chatMessage);
                transactionCount++;
            }


            if (!processedAllTodaysMessages)
            {
                var newLastMessage = messageResponse.Response.Messages[messageResponse.Response.Messages.Length - 1];
                return await GetAndSaveMessages(group, transactionCount, newLastMessage.Id);
            }
            return transactionCount;
        }
    }
}