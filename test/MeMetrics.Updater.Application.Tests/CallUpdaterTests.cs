using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Apis.Gmail.v1.Data;
using MeMetrics.Updater.Application.Interfaces;
using MeMetrics.Updater.Application.Objects;
using MeMetrics.Updater.Application.Objects.MeMetrics;
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
        [Fact]
        public async Task GetAndSaveCalls_ShouldNotSaveCall_IfCallWasNotCompleted()
        {
            var memetricsApiMock = new Mock<IMeMetricsApi>();
            var gmailApiMock = new Mock<IGmailApi>();
            var loggerMock = new Mock<ILogger>();
            var config = Options.Create(new EnvironmentConfiguration()
            {
                Gmail_History_Refresh_Token = "HistoryToken",
                Gmail_Call_Log_Label = "CallLogLabel",
            });

            var messageId = "1";

            gmailApiMock.Setup(x => x.GetLabels()).ReturnsAsync(new ListLabelsResponse()
            {
                Labels = new List<Label>()
                {
                    new Label(){ Name = config.Value.Gmail_Call_Log_Label, Id = config.Value.Gmail_Call_Log_Label}
                }
            });

            gmailApiMock.Setup(x => x.GetEmails(config.Value.Gmail_Call_Log_Label, null)).ReturnsAsync(new ListMessagesResponse()
            {
                Messages = new List<Message>()
                {
                    new Message() { Id = messageId }
                }
            });

            gmailApiMock.Setup(x => x.GetEmail(messageId)).ReturnsAsync(new Message()
            {
                InternalDate = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                Snippet = "3128675309(missed call)"
            });

            var updater = new CallUpdater(loggerMock.Object, gmailApiMock.Object, memetricsApiMock.Object, config);
            await updater.GetAndSaveCalls();

            gmailApiMock.Verify(x => x.Authenticate(config.Value.Gmail_History_Refresh_Token), Times.Once);
            memetricsApiMock.Verify(x => x.SaveCall(It.IsAny<Call>()), Times.Never);
        }

        [Fact]
        public async Task GetAndSaveCalls_ShouldSaveCallsSuccessfully()
        {
            var memetricsApiMock = new Mock<IMeMetricsApi>();
            var gmailApiMock = new Mock<IGmailApi>();
            var loggerMock = new Mock<ILogger>();
            var config = Options.Create(new EnvironmentConfiguration()
            {
                Gmail_History_Refresh_Token = "HistoryToken",
                Gmail_Call_Log_Label = "CallLogLabel",
            });

            var messageId = "1";

            gmailApiMock.Setup(x => x.GetLabels()).ReturnsAsync(new ListLabelsResponse()
            {
                Labels = new List<Label>()
                {
                    new Label(){ Name = config.Value.Gmail_Call_Log_Label, Id = config.Value.Gmail_Call_Log_Label}
                }
            });

            gmailApiMock.Setup(x => x.GetEmails(config.Value.Gmail_Call_Log_Label, null)).ReturnsAsync(new ListMessagesResponse()
            {
                Messages = new List<Message>()
                {
                    new Message() { Id = messageId }
                }
            });

            var occurredDate = "2020-1-1";

            gmailApiMock.Setup(x => x.GetEmail(messageId)).ReturnsAsync(new Message()
            {
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

            Func<Call, bool> validate = call => {
                Assert.Equal(JsonConvert.SerializeObject(expectedCall), JsonConvert.SerializeObject(call));
                return true;
            };

            var updater = new CallUpdater(loggerMock.Object, gmailApiMock.Object, memetricsApiMock.Object, config);
            await updater.GetAndSaveCalls();

            memetricsApiMock.Verify(x => x.SaveCall(It.Is<Call>(z => validate(z))), Times.Once);
        }

        [Fact]
        public async Task GetAndSaveCalls_ShouldOnlySaveCall_IfCallIsNotOlderThanTwoDays()
        {
            var memetricsApiMock = new Mock<IMeMetricsApi>();
            var gmailApiMock = new Mock<IGmailApi>();
            var loggerMock = new Mock<ILogger>();
            var config = Options.Create(new EnvironmentConfiguration()
            {
                Gmail_History_Refresh_Token = "HistoryToken",
                Gmail_Call_Log_Label = "CallLogLabel",
            });

            var messageId = "1";
            var oldMessageId = "2";

            gmailApiMock.Setup(x => x.GetLabels()).ReturnsAsync(new ListLabelsResponse()
            {
                Labels = new List<Label>()
                {
                    new Label(){ Name = config.Value.Gmail_Call_Log_Label, Id = config.Value.Gmail_Call_Log_Label}
                }
            });

            gmailApiMock.Setup(x => x.GetEmails(config.Value.Gmail_Call_Log_Label, null)).ReturnsAsync(new ListMessagesResponse()
            {
                Messages = new List<Message>()
                {
                    new Message() { Id = messageId },
                    new Message() { Id = oldMessageId },
                }
            });


            gmailApiMock.Setup(x => x.GetEmail(messageId)).ReturnsAsync(new Message()
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

            gmailApiMock.Setup(x => x.GetEmail(oldMessageId)).ReturnsAsync(new Message()
            {
                InternalDate = DateTimeOffset.Now.AddDays(-3).ToUnixTimeMilliseconds(),
                Snippet = "25s (00:00:25) 3128675309 (incoming call)",
            });

            var updater = new CallUpdater(loggerMock.Object, gmailApiMock.Object, memetricsApiMock.Object, config);
            await updater.GetAndSaveCalls();

            memetricsApiMock.Verify(x => x.SaveCall(It.IsAny<Call>()), Times.Once);
        }

        [Fact]
        public async Task GetAndSaveCalls_ShouldUseNextPageToken_IfOutOfMessages()
        {
            var memetricsApiMock = new Mock<IMeMetricsApi>();
            var gmailApiMock = new Mock<IGmailApi>();
            var loggerMock = new Mock<ILogger>();
            var config = Options.Create(new EnvironmentConfiguration()
            {
                Gmail_History_Refresh_Token = "HistoryToken",
                Gmail_Call_Log_Label = "CallLogLabel",
            });

            var messageId = "1";
            var secondMessageId = "2";
            var nextPageToken = "token";

            gmailApiMock.Setup(x => x.GetLabels()).ReturnsAsync(new ListLabelsResponse()
            {
                Labels = new List<Label>()
                {
                    new Label(){ Name = config.Value.Gmail_Call_Log_Label, Id = config.Value.Gmail_Call_Log_Label}
                }
            });

            gmailApiMock.Setup(x => x.GetEmails(config.Value.Gmail_Call_Log_Label, null)).ReturnsAsync(new ListMessagesResponse()
            {
                Messages = new List<Message>()
                {
                    new Message() { Id = messageId }
                },
                NextPageToken = nextPageToken
            });

            gmailApiMock.Setup(x => x.GetEmails(config.Value.Gmail_Call_Log_Label, nextPageToken)).ReturnsAsync(new ListMessagesResponse()
            {
                Messages = new List<Message>()
                {
                    new Message() { Id = secondMessageId }
                },
            });

            gmailApiMock.Setup(x => x.GetEmail(It.IsAny<string>())).ReturnsAsync(new Message()
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


            var updater = new CallUpdater(loggerMock.Object, gmailApiMock.Object, memetricsApiMock.Object, config);
            await updater.GetAndSaveCalls();

            gmailApiMock.Verify(x => x.GetEmails(config.Value.Gmail_Call_Log_Label, nextPageToken));
            memetricsApiMock.Verify(x => x.SaveCall(It.IsAny<Call>()), Times.Exactly(2));
        }
    }
}
