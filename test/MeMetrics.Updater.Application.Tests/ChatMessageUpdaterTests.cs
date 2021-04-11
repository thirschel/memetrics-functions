using System;
using System.Threading.Tasks;
using AutoMapper;
using MeMetrics.Updater.Application.Interfaces;
using MeMetrics.Updater.Application.Objects;
using MeMetrics.Updater.Application.Objects.GroupMe;
using MeMetrics.Updater.Application.Objects.MeMetrics;
using MeMetrics.Updater.Application.Profiles;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using Serilog;
using Xunit;

namespace MeMetrics.Updater.Application.Tests
{
    public class ChatMessageUpdaterTests
    {
        public readonly IMapper _mapper;
        public readonly Mock<IMeMetricsApi> _memetricsApiMock;
        public readonly Mock<IGroupMeApi> _groupMeApiMock;
        public readonly Mock<ILogger> _loggerMock;
        public ChatMessageUpdaterTests()
        {
            _memetricsApiMock = new Mock<IMeMetricsApi>();
            _groupMeApiMock = new Mock<IGroupMeApi>();
            _loggerMock = new Mock<ILogger>();
            var configuration = new MapperConfiguration(cfg => { cfg.AddProfile<ChatMessageProfile>(); });
            _mapper = new Mapper(configuration);
        }

        [Fact]
        public async Task GetAndSaveChatMessages_ShouldSaveChatMessagesCorrectly()
        {
            var config = Options.Create(new EnvironmentConfiguration()
            {
                GroupMe_Access_Token = "Token",
                Gmail_Call_Log_Label = "CallLogLabel",
            });

            var groupId = "1";
            var groupName = "Group";

            _groupMeApiMock.Setup(x => x.GetGroups()).ReturnsAsync(new GroupResponse()
            {
                Groups = new Group[]
                {
                    new Group(){ Id = groupId, Name = groupName}
                }
            });

            var messageId = "1";
            var userId = "";
            var senderId = "2";
            var name = "Tess Ting";
            var text = "Hello";
            var createdTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            _groupMeApiMock.Setup(x => x.GetMessages(groupId, null)).ReturnsAsync(new MessageResponse()
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
            };

            Func<ChatMessage, bool> validate = chatMessage => {
                Assert.Equal(JsonConvert.SerializeObject(expectedMessage), JsonConvert.SerializeObject(chatMessage));
                return true;
            };

            var updater = new ChatMessageUpdater(_loggerMock.Object, config, _groupMeApiMock.Object, _memetricsApiMock.Object, _mapper);
            await updater.GetAndSaveChatMessages();

            _groupMeApiMock.Verify(x => x.Authenticate(config.Value.GroupMe_Access_Token), Times.Once);
            _memetricsApiMock.Verify(x => x.SaveChatMessage(It.Is<ChatMessage>(x => validate(x))), Times.Once);
        }

        [Fact]
        public async Task GetAndSaveChatMessages_ShouldReturn_IfNoMessagesFound()
        {
            var config = Options.Create(new EnvironmentConfiguration()
            {
                GroupMe_Access_Token = "Token",
                Gmail_Call_Log_Label = "CallLogLabel",
            });

            var groupId = "1";
            var groupName = "Group";

            _groupMeApiMock.Setup(x => x.GetGroups()).ReturnsAsync(new GroupResponse()
            {
                Groups = new Group[]
                {
                    new Group(){ Id = groupId, Name = groupName}
                }
            });

            _groupMeApiMock.Setup(x => x.GetMessages(groupId, null)).ReturnsAsync(new MessageResponse()
            {
                Response = new Response()
                {
                    Messages = new Objects.GroupMe.Message[]
                    {
                    }
                }
            });

            var updater = new ChatMessageUpdater(_loggerMock.Object, config, _groupMeApiMock.Object, _memetricsApiMock.Object, _mapper);
            await updater.GetAndSaveChatMessages();

            _memetricsApiMock.Verify(x => x.SaveChatMessage(It.IsAny<ChatMessage>()), Times.Never);
        }

        [Fact]
        public async Task GetAndSaveChatMessages_ShouldOnlyChatMessage_IfChatMessageIsNotOlderThanTwoDays()
        {
            var config = Options.Create(new EnvironmentConfiguration()
            {
                GroupMe_Access_Token = "Token",
                Gmail_Call_Log_Label = "CallLogLabel",
            });

            var groupId = "1";
            var groupName = "Group";

            _groupMeApiMock.Setup(x => x.GetGroups()).ReturnsAsync(new GroupResponse()
            {
                Groups = new Group[]
                {
                    new Group(){ GroupId = groupId, Name = groupName}
                }
            });

            _groupMeApiMock.Setup(x => x.GetMessages(groupId, null)).ReturnsAsync(new MessageResponse()
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

            var updater = new ChatMessageUpdater(_loggerMock.Object, config, _groupMeApiMock.Object, _memetricsApiMock.Object, _mapper);
            await updater.GetAndSaveChatMessages();

            _memetricsApiMock.Verify(x => x.SaveChatMessage(It.IsAny<ChatMessage>()), Times.Never);
        }

        
        // GroupMe events get return from the api along with messages. eg "Tim" left the group
        [Fact]
        public async Task GetAndSaveChatMessages_ShouldNotSaveChatMessage_IfMessageIsGroupMeEvent()
        {
            var _memetricsApiMock = new Mock<IMeMetricsApi>();
            var _groupMeApiMock = new Mock<IGroupMeApi>();
            var _loggerMock = new Mock<ILogger>();
            var config = Options.Create(new EnvironmentConfiguration()
            {
                GroupMe_Access_Token = "Token",
                Gmail_Call_Log_Label = "CallLogLabel",
            });

            var groupId = "1";
            var groupName = "Group";

            _groupMeApiMock.Setup(x => x.GetGroups()).ReturnsAsync(new GroupResponse()
            {
                Groups = new Group[]
                {
                    new Group(){ Id = groupId, Name = groupName}
                }
            });

            _groupMeApiMock.Setup(x => x.GetMessages(groupId, null)).ReturnsAsync(new MessageResponse()
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

            var updater = new ChatMessageUpdater(_loggerMock.Object, config, _groupMeApiMock.Object, _memetricsApiMock.Object, _mapper);
            await updater.GetAndSaveChatMessages();

            _memetricsApiMock.Verify(x => x.SaveChatMessage(It.IsAny<ChatMessage>()), Times.Never);
        }

        [Fact]
        public async Task GetAndSaveChatMessages_ShouldUseLastMessageId_IfOutOfMessages()
        {
            var config = Options.Create(new EnvironmentConfiguration()
            {
                GroupMe_Access_Token = "Token",
                Gmail_Call_Log_Label = "CallLogLabel",
            });

            var groupId = "1";
            var groupName = "Group";

            _groupMeApiMock.Setup(x => x.GetGroups()).ReturnsAsync(new GroupResponse()
            {
                Groups = new Group[]
                {
                    new Group(){ Id = groupId, Name = groupName}
                }
            });

            var firstMessageId = "1";

            _groupMeApiMock.Setup(x => x.GetMessages(groupId, null)).ReturnsAsync(new MessageResponse()
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

            _groupMeApiMock.Setup(x => x.GetMessages(groupId, firstMessageId)).ReturnsAsync(new MessageResponse()
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

            var updater = new ChatMessageUpdater(_loggerMock.Object, config, _groupMeApiMock.Object, _memetricsApiMock.Object, _mapper);
            await updater.GetAndSaveChatMessages();

            _memetricsApiMock.Verify(x => x.SaveChatMessage(It.IsAny<ChatMessage>()), Times.Exactly(2));
        }


        [Fact]
        public async Task GetAndSaveChatMessages_ShouldReturnSuccessfully_WhenCatchingException()
        {
            var config = Options.Create(new EnvironmentConfiguration()
            {
                GroupMe_Access_Token = "Token",
                Gmail_Call_Log_Label = "CallLogLabel",
            });

            _groupMeApiMock.Setup(x => x.GetGroups()).ThrowsAsync(new Exception());

            var updater = new ChatMessageUpdater(_loggerMock.Object, config, _groupMeApiMock.Object, _memetricsApiMock.Object, _mapper);

            var response = await updater.GetAndSaveChatMessages();

            Assert.False(response.Successful);
        }
    }
}
