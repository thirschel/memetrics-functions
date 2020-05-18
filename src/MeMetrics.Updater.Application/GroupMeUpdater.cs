using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using MeMetrics.Updater.Application.Interfaces;
using MeMetrics.Updater.Application.Objects;
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

        public ChatMessageUpdater(
            ILogger logger, 
            IOptions<EnvironmentConfiguration> configuration,
            IGroupMeApi groupMeApi,
            IMeMetricsApi memetricsApi)
        {
            _logger = logger;
            _groupMeApi = groupMeApi;
            _memetricsApi = memetricsApi;
            _groupMeApi.Authenticate(configuration.Value.GroupMe_Access_Token);
        }

        public async Task GetAndSaveChatMessages()
        {
            var groupResponse = await _groupMeApi.GetGroups();
            var transactionCount = 0;
            for (var i = 0; i < groupResponse.Groups.Length; i++)
            {
                transactionCount = await GetAndSaveMessages(groupResponse.Groups[i].GroupId, groupResponse.Groups[i].Name, transactionCount);
            }

            _logger.Information($"{transactionCount} groupme messages successfully saved");
        }

        private async Task<int> GetAndSaveMessages(string groupId, string groupName, int transactionCount = 0, string lastMessageId = null)
        {
            var processedAllTodaysMessages = false;
            var messageResponse = await _groupMeApi.GetMessages(groupId, lastMessageId);
            if (messageResponse == null || !messageResponse.Response.Messages.Any())
            {
                return transactionCount;
            }

            foreach (var responseMessage in messageResponse.Response.Messages)
            {
                processedAllTodaysMessages = DateTimeOffset.FromUnixTimeMilliseconds(responseMessage.CreatedAt) <
                                             DateTimeOffset.UtcNow.AddDays(-1);
                if (responseMessage.Event != null || processedAllTodaysMessages)
                {
                    continue;
                }

                var chatMessage = new ChatMessage()
                {
                    ChatMessageId = responseMessage.Id,
                    GroupId = groupId,
                    GroupName = groupName,
                    IsIncoming = responseMessage.UserId != "",
                    IsMedia = responseMessage.Attachments.Any(),
                    OccurredDate = DateTimeOffset.FromUnixTimeMilliseconds(responseMessage.CreatedAt),
                    SenderId = responseMessage.SenderId,
                    SenderName = responseMessage.Name,
                    Text = responseMessage.Text,
                    TextLength = responseMessage.Text?.Length ?? 0
                };


                await _memetricsApi.SaveChatMessage(chatMessage);
                transactionCount++;
            }


            if (!processedAllTodaysMessages)
            {
                var newLastMessage =
                    messageResponse.Response.Messages[messageResponse.Response.Messages.Length - 1];
                return await GetAndSaveMessages(groupId, groupName, transactionCount, newLastMessage.Id);
            }
            return transactionCount;
        }
    }
}