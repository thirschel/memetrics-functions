using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Google.Apis.Gmail.v1.Data;
using MeMetrics.Updater.Application.Interfaces;
using MeMetrics.Updater.Application.Objects;
using MeMetrics.Updater.Application.Objects.MeMetrics;
using MeMetrics.Updater.Application.Profiles;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using Serilog;
using Xunit;
using Message = Google.Apis.Gmail.v1.Data.Message;

namespace MeMetrics.Updater.Application.Tests
{
    public class CallUpdaterTests
    {
        public static Mock<ILogger> _loggerMock;
        public static Mock<IMeMetricsApi> _memetricsApiMock;
        public static Mock<IGmailApi> _gmailApiMock;
        public static IMapper _mapper;
        public CallUpdaterTests()
        {
            var configuration = new MapperConfiguration(cfg => { cfg.AddProfile<CallProfile>(); });
            _mapper = new Mapper(configuration);
            _memetricsApiMock = new Mock<IMeMetricsApi>();
            _gmailApiMock = new Mock<IGmailApi>();
            _loggerMock = new Mock<ILogger>();
        }

        [Fact]
        public async Task GetAndSaveCalls_ShouldNotSaveCall_IfCallWasNotCompleted()
        {
            var config = Options.Create(new EnvironmentConfiguration()
            {
                Gmail_History_Refresh_Token = "HistoryToken",
                Gmail_Call_Log_Label = "CallLogLabel",
            });

            var messageId = "1";

            _gmailApiMock.Setup(x => x.GetLabels()).ReturnsAsync(new ListLabelsResponse()
            {
                Labels = new List<Label>()
                {
                    new Label(){ Name = config.Value.Gmail_Call_Log_Label, Id = config.Value.Gmail_Call_Log_Label}
                }
            });

            _gmailApiMock.Setup(x => x.GetEmails(config.Value.Gmail_Call_Log_Label, null)).ReturnsAsync(new ListMessagesResponse()
            {
                Messages = new List<Message>()
                {
                    new Message() { Id = messageId }
                }
            });

            _gmailApiMock.Setup(x => x.GetEmail(messageId)).ReturnsAsync(new Message()
            {
                InternalDate = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                Snippet = "3128675309(missed call)"
            });

            var updater = new CallUpdater(_loggerMock.Object, config, _gmailApiMock.Object, _memetricsApiMock.Object, _mapper);
            await updater.GetAndSaveCalls();

            _gmailApiMock.Verify(x => x.Authenticate(config.Value.Gmail_History_Refresh_Token), Times.Once);
            _memetricsApiMock.Verify(x => x.SaveCalls(It.IsAny<List<Call>>()), Times.Never);
        }

        [Fact]
        public async Task GetAndSaveCalls_ShouldSaveCallsSuccessfully()
        {
            var config = Options.Create(new EnvironmentConfiguration()
            {
                Gmail_History_Refresh_Token = "HistoryToken",
                Gmail_Call_Log_Label = "CallLogLabel",
            });

            var messageId = "1";

            _gmailApiMock.Setup(x => x.GetLabels()).ReturnsAsync(new ListLabelsResponse()
            {
                Labels = new List<Label>()
                {
                    new Label(){ Name = config.Value.Gmail_Call_Log_Label, Id = config.Value.Gmail_Call_Log_Label}
                }
            });

            _gmailApiMock.Setup(x => x.GetEmails(config.Value.Gmail_Call_Log_Label, null)).ReturnsAsync(new ListMessagesResponse()
            {
                Messages = new List<Message>()
                {
                    new Message() { Id = messageId }
                }
            });

            var occurredDate = "2020-1-1";

            _gmailApiMock.Setup(x => x.GetEmail(messageId)).ReturnsAsync(new Message()
            {
                Id = messageId,
                InternalDate = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                Snippet = "25s (00:00:25) 3128675309 (incoming call)",
                Payload = new MessagePart() { Headers = new List<MessagePartHeader>()
                {
                    new MessagePartHeader(){ Name = Constants.EmailHeader.Date, Value = occurredDate }
                }}
            });

            var expectedCall = new Call()
            {
                CallId = messageId,
                OccurredDate = DateTimeOffset.Parse(occurredDate),
                Duration = 25,
                PhoneNumber = "13128675309",
                IsIncoming = true
            };

            Func<List<Call>, bool> validate = calls => {
                Assert.Equal(JsonConvert.SerializeObject(expectedCall), JsonConvert.SerializeObject(calls.First()));
                return true;
            };

            var updater = new CallUpdater(_loggerMock.Object, config, _gmailApiMock.Object, _memetricsApiMock.Object, _mapper);
            await updater.GetAndSaveCalls();

            _memetricsApiMock.Verify(x => x.SaveCalls(It.Is<List<Call>>(calls => validate(calls))), Times.Once);
        }

        [Fact]
        public async Task GetAndSaveCalls_ShouldOnlySaveCall_IfCallIsNotOlderThanTwoDays()
        {
            var config = Options.Create(new EnvironmentConfiguration()
            {
                Gmail_History_Refresh_Token = "HistoryToken",
                Gmail_Call_Log_Label = "CallLogLabel",
            });

            var messageId = "1";
            var oldMessageId = "2";

            _gmailApiMock.Setup(x => x.GetLabels()).ReturnsAsync(new ListLabelsResponse()
            {
                Labels = new List<Label>()
                {
                    new Label(){ Name = config.Value.Gmail_Call_Log_Label, Id = config.Value.Gmail_Call_Log_Label}
                }
            });

            _gmailApiMock.Setup(x => x.GetEmails(config.Value.Gmail_Call_Log_Label, null)).ReturnsAsync(new ListMessagesResponse()
            {
                Messages = new List<Message>()
                {
                    new Message() { Id = messageId },
                    new Message() { Id = oldMessageId },
                }
            });


            _gmailApiMock.Setup(x => x.GetEmail(messageId)).ReturnsAsync(new Message()
            {
                InternalDate = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                Snippet = "25s (00:00:25) 3128675309 (incoming call)",
                Payload = new MessagePart()
                {
                    Headers = new List<MessagePartHeader>()
                {
                    new MessagePartHeader(){ Name = Constants.EmailHeader.Date, Value = "2020-1-1" }
                }
                }
            });

            _gmailApiMock.Setup(x => x.GetEmail(oldMessageId)).ReturnsAsync(new Message()
            {
                InternalDate = DateTimeOffset.Now.AddDays(-3).ToUnixTimeMilliseconds(),
                Snippet = "25s (00:00:25) 3128675309 (incoming call)",
            });

            var updater = new CallUpdater(_loggerMock.Object, config, _gmailApiMock.Object, _memetricsApiMock.Object, _mapper);
            await updater.GetAndSaveCalls();

            _memetricsApiMock.Verify(x => x.SaveCalls(It.IsAny<List<Call>>()), Times.Once);
        }

        [Fact]
        public async Task GetAndSaveCalls_ShouldUseNextPageToken_IfOutOfMessages()
        {
            var config = Options.Create(new EnvironmentConfiguration()
            {
                Gmail_History_Refresh_Token = "HistoryToken",
                Gmail_Call_Log_Label = "CallLogLabel",
            });

            var messageId = "1";
            var secondMessageId = "2";
            var nextPageToken = "token";

            _gmailApiMock.Setup(x => x.GetLabels()).ReturnsAsync(new ListLabelsResponse()
            {
                Labels = new List<Label>()
                {
                    new Label(){ Name = config.Value.Gmail_Call_Log_Label, Id = config.Value.Gmail_Call_Log_Label}
                }
            });

            _gmailApiMock.Setup(x => x.GetEmails(config.Value.Gmail_Call_Log_Label, null)).ReturnsAsync(new ListMessagesResponse()
            {
                Messages = new List<Message>()
                {
                    new Message() { Id = messageId }
                },
                NextPageToken = nextPageToken
            });

            _gmailApiMock.Setup(x => x.GetEmails(config.Value.Gmail_Call_Log_Label, nextPageToken)).ReturnsAsync(new ListMessagesResponse()
            {
                Messages = new List<Message>()
                {
                    new Message() { Id = secondMessageId }
                },
            });

            _gmailApiMock.Setup(x => x.GetEmail(It.IsAny<string>())).ReturnsAsync(new Message()
            {
                InternalDate = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                Snippet = "25s (00:00:25) 3128675309 (incoming call)",
                Payload = new MessagePart()
                {
                    Headers = new List<MessagePartHeader>()
                {
                    new MessagePartHeader(){ Name = Constants.EmailHeader.Date, Value = "2020-1-1" }
                }
                }
            });


            var updater = new CallUpdater(_loggerMock.Object, config, _gmailApiMock.Object, _memetricsApiMock.Object, _mapper);
            await updater.GetAndSaveCalls();

            _gmailApiMock.Verify(x => x.GetEmails(config.Value.Gmail_Call_Log_Label, nextPageToken));
            _memetricsApiMock.Verify(x => x.SaveCalls(It.IsAny<List<Call>>()), Times.Exactly(2));
        }

        [Fact]
        public async Task GetAndSaveCalls_ShouldReturnSuccessfully_WhenCatchingException()
        {
            var config = Options.Create(new EnvironmentConfiguration()
            {
                Gmail_History_Refresh_Token = "HistoryToken",
                Gmail_Sms_Label = "SmsLabel",
                Gmail_Sms_Email_Address = "myEmail@address.com"
            });

            _gmailApiMock.Setup(x => x.GetLabels()).ThrowsAsync(new Exception());

            var updater = new CallUpdater(_loggerMock.Object, config, _gmailApiMock.Object, _memetricsApiMock.Object, _mapper);

            var response = await updater.GetAndSaveCalls();

            Assert.False(response.Successful);
        }
    }
}
