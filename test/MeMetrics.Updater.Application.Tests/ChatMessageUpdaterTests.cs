using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Apis.Gmail.v1.Data;
using MeMetrics.Updater.Application.Interfaces;
using MeMetrics.Updater.Application.Objects;
using MeMetrics.Updater.Application.Objects.GroupMe;
using MeMetrics.Updater.Application.Objects.MeMetrics;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using Serilog;
using Xunit;
using Message = Google.Apis.Gmail.v1.Data.Message;

namespace MeMetrics.Updater.Application.Tests
{
    public class ChatMessageUpdaterTests
    {
        [Fact]
        public async Task GetAndSaveChatMessages_ShouldSaveChatMessagesCorrectly()
        {
            var memetricsApiMock = new Mock<IMeMetricsApi>();
            var groupMeApiMock = new Mock<IGroupMeApi>();
            var loggerMock = new Mock<ILogger>();
            var config = Options.Create(new EnvironmentConfiguration()
            {
                GroupMe_Access_Token = "Token",
                Gmail_Call_Log_Label = "CallLogLabel",
            });

            var groupId = "1";
            var groupName = "Group";

            groupMeApiMock.Setup(x => x.GetGroups()).ReturnsAsync(new GroupResponse()
            {
                Groups = new Group[]
                {
                    new Group(){ GroupId = groupId, Name = groupName}
                }
            });

            var messageId = "1";
            var userId = "";
            var senderId = "2";
            var name = "Tess Ting";
            var text = "Hello";
            var createdTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            groupMeApiMock.Setup(x => x.GetMessages(groupId, null)).ReturnsAsync(new MessageResponse()
            {
                Response = new Response()
                {
                    Messages = new Objects.GroupMe.Message[]
                    {
                        new Objects.GroupMe.Message()
                        {
                            CreatedAt = createdTime,
                            Id = messageId,
                            UserId = userId,
                            Attachments = new object[0],
                            SenderId = senderId,
                            Name = name,
                            Text = text
                        }, 
                    }
                }
            });

            var expectedMessage = new ChatMessage()
            {
                ChatMessageId = messageId,
                GroupId = groupId,
                GroupName = groupName,
                IsIncoming = false,
                IsMedia = false,
                OccurredDate = DateTimeOffset.FromUnixTimeMilliseconds(createdTime),
                SenderId = senderId,
                SenderName = name,
                Text = text,
                TextLength = text.Length
            };

            Func<ChatMessage, bool> validate = chatMessage => {
                Assert.Equal(JsonConvert.SerializeObject(expectedMessage), JsonConvert.SerializeObject(chatMessage));
                return true;
            };

            var updater = new ChatMessageUpdater(loggerMock.Object, config, groupMeApiMock.Object, memetricsApiMock.Object );
            await updater.GetAndSaveChatMessages();

            groupMeApiMock.Verify(x => x.Authenticate(config.Value.GroupMe_Access_Token), Times.Once);
            memetricsApiMock.Verify(x => x.SaveChatMessage(It.Is<ChatMessage>(x => validate(x))), Times.Once);
        }

        [Fact]
        public async Task GetAndSaveChatMessages_ShouldReturn_IfNoMessagesFound()
        {
            var memetricsApiMock = new Mock<IMeMetricsApi>();
            var groupMeApiMock = new Mock<IGroupMeApi>();
            var loggerMock = new Mock<ILogger>();
            var config = Options.Create(new EnvironmentConfiguration()
            {
                GroupMe_Access_Token = "Token",
                Gmail_Call_Log_Label = "CallLogLabel",
            });

            var groupId = "1";
            var groupName = "Group";

            groupMeApiMock.Setup(x => x.GetGroups()).ReturnsAsync(new GroupResponse()
            {
                Groups = new Group[]
                {
                    new Group(){ GroupId = groupId, Name = groupName}
                }
            });

            groupMeApiMock.Setup(x => x.GetMessages(groupId, null)).ReturnsAsync(new MessageResponse()
            {
                Response = new Response()
                {
                    Messages = new Objects.GroupMe.Message[]
                    {
                    }
                }
            });

            var updater = new ChatMessageUpdater(loggerMock.Object, config, groupMeApiMock.Object, memetricsApiMock.Object);
            await updater.GetAndSaveChatMessages();

            memetricsApiMock.Verify(x => x.SaveChatMessage(It.IsAny<ChatMessage>()), Times.Never);
        }

        [Fact]
        public async Task GetAndSaveChatMessages_ShouldOnlyChatMessage_IfChatMessageIsNotOlderThanTwoDays()
        {
            var memetricsApiMock = new Mock<IMeMetricsApi>();
            var groupMeApiMock = new Mock<IGroupMeApi>();
            var loggerMock = new Mock<ILogger>();
            var config = Options.Create(new EnvironmentConfiguration()
            {
                GroupMe_Access_Token = "Token",
                Gmail_Call_Log_Label = "CallLogLabel",
            });

            var groupId = "1";
            var groupName = "Group";

            groupMeApiMock.Setup(x => x.GetGroups()).ReturnsAsync(new GroupResponse()
            {
                Groups = new Group[]
                {
                    new Group(){ GroupId = groupId, Name = groupName}
                }
            });

            groupMeApiMock.Setup(x => x.GetMessages(groupId, null)).ReturnsAsync(new MessageResponse()
            {
                Response = new Response()
                {
                    Messages = new Objects.GroupMe.Message[]
                    {
                        new Objects.GroupMe.Message()
                        {
                            CreatedAt = DateTimeOffset.Now.AddDays(-2).ToUnixTimeMilliseconds(),
                        },
                    }
                }
            });

            var updater = new ChatMessageUpdater(loggerMock.Object, config, groupMeApiMock.Object, memetricsApiMock.Object);
            await updater.GetAndSaveChatMessages();

            memetricsApiMock.Verify(x => x.SaveChatMessage(It.IsAny<ChatMessage>()), Times.Never);
        }

        
        // GroupMe events get return from the api along with messages. eg "Tim" left the group
        [Fact]
        public async Task GetAndSaveChatMessages_ShouldNotSaveChatMessage_IfMessageIsGroupMeEvent()
        {
            var memetricsApiMock = new Mock<IMeMetricsApi>();
            var groupMeApiMock = new Mock<IGroupMeApi>();
            var loggerMock = new Mock<ILogger>();
            var config = Options.Create(new EnvironmentConfiguration()
            {
                GroupMe_Access_Token = "Token",
                Gmail_Call_Log_Label = "CallLogLabel",
            });

            var groupId = "1";
            var groupName = "Group";

            groupMeApiMock.Setup(x => x.GetGroups()).ReturnsAsync(new GroupResponse()
            {
                Groups = new Group[]
                {
                    new Group(){ GroupId = groupId, Name = groupName}
                }
            });

            groupMeApiMock.Setup(x => x.GetMessages(groupId, null)).ReturnsAsync(new MessageResponse()
            {
                Response = new Response()
                {
                    Messages = new Objects.GroupMe.Message[]
                    {
                        new Objects.GroupMe.Message()
                        {
                            Id = "1",
                            CreatedAt = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                            Event = new Event(){Type = "Join"}
                        },
                    }
                }
            });

            var updater = new ChatMessageUpdater(loggerMock.Object, config, groupMeApiMock.Object, memetricsApiMock.Object);
            await updater.GetAndSaveChatMessages();

            memetricsApiMock.Verify(x => x.SaveChatMessage(It.IsAny<ChatMessage>()), Times.Never);
        }

        [Fact]
        public async Task GetAndSaveChatMessages_ShouldUseLastMessageId_IfOutOfMessages()
        {
            var memetricsApiMock = new Mock<IMeMetricsApi>();
            var groupMeApiMock = new Mock<IGroupMeApi>();
            var loggerMock = new Mock<ILogger>();
            var config = Options.Create(new EnvironmentConfiguration()
            {
                GroupMe_Access_Token = "Token",
                Gmail_Call_Log_Label = "CallLogLabel",
            });

            var groupId = "1";
            var groupName = "Group";

            groupMeApiMock.Setup(x => x.GetGroups()).ReturnsAsync(new GroupResponse()
            {
                Groups = new Group[]
                {
                    new Group(){ GroupId = groupId, Name = groupName}
                }
            });

            var firstMessageId = "1";

            groupMeApiMock.Setup(x => x.GetMessages(groupId, null)).ReturnsAsync(new MessageResponse()
            {
                Response = new Response()
                {
                    Messages = new Objects.GroupMe.Message[]
                    {
                        new Objects.GroupMe.Message()
                        {
                            CreatedAt = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                            Id = firstMessageId,
                            UserId = "",
                            Attachments = new object[0],
                            SenderId = "2",
                            Name = "Tess Ting",
                            Text = "Hello"
                        },
                    }
                }
            });

            groupMeApiMock.Setup(x => x.GetMessages(groupId, firstMessageId)).ReturnsAsync(new MessageResponse()
            {
                Response = new Response()
                {
                    Messages = new Objects.GroupMe.Message[]
                    {
                        new Objects.GroupMe.Message()
                        {
                            CreatedAt = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                            Id = "2",
                            UserId = "",
                            Attachments = new object[0],
                            SenderId = "2",
                            Name = "Tess Ting",
                            Text = "Hello again"
                        },
                    }
                }
            });

            var updater = new ChatMessageUpdater(loggerMock.Object, config, groupMeApiMock.Object, memetricsApiMock.Object);
            await updater.GetAndSaveChatMessages();

            memetricsApiMock.Verify(x => x.SaveChatMessage(It.IsAny<ChatMessage>()), Times.Exactly(2));
        }
    }
}
