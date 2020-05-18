using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Gmail.v1.Data;
using MeMetrics.Updater.Application.Interfaces;
using MeMetrics.Updater.Application.Objects;
using MeMetrics.Updater.Application.Objects.Enums;
using MeMetrics.Updater.Application.Objects.MeMetrics;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using Serilog;
using Xunit;
using Message = Google.Apis.Gmail.v1.Data.Message;

namespace MeMetrics.Updater.Application.Tests
{
    public class RecruitmentMessageUpdaterTests
    {
        [Fact]
        public async Task GetAndSaveEmailMessages_ShouldSaveCallsSuccessfully()
        {
            var memetricsApiMock = new Mock<IMeMetricsApi>();
            var gmailApiMock = new Mock<IGmailApi>();
            var linkedinApiMock = new Mock<ILinkedInApi>();
            var loggerMock = new Mock<ILogger>();
            var config = Options.Create(new EnvironmentConfiguration()
            {
                Gmail_Main_Refresh_Token = "MainToken",
                Gmail_Recruiter_Label = "RecruiterLabel",
                Gmail_Recruiter_Email_Address = "myEmail@address.com"
            });

            var messageId = "1";

            gmailApiMock.Setup(x => x.GetLabels()).ReturnsAsync(new ListLabelsResponse()
            {
                Labels = new List<Label>()
                {
                    new Label(){ Name = config.Value.Gmail_Recruiter_Label, Id = config.Value.Gmail_Recruiter_Label}
                }
            });

            gmailApiMock.Setup(x => x.GetEmails(config.Value.Gmail_Recruiter_Label, null)).ReturnsAsync(new ListMessagesResponse()
            {
                Messages = new List<Message>()
                {
                    new Message() { Id = messageId }
                },
            });

            var internalDate = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            gmailApiMock.Setup(x => x.GetEmail(messageId)).ReturnsAsync(new Message()
            {
                InternalDate = internalDate,
                Payload = new MessagePart() { 
                    Headers = new List<MessagePartHeader>()
                    {
                        new MessagePartHeader(){ Name = Constants.EmailHeader.From, Value = "Tess Ting <tess@ting.com>" },
                        new MessagePartHeader(){ Name = Constants.EmailHeader.To, Value = config.Value.Gmail_Recruiter_Email_Address },
                        new MessagePartHeader(){ Name = Constants.EmailHeader.Subject, Value = "Hi There" },
                    },
                    Parts = new List<MessagePart>()
                    {
                        new MessagePart()
                        {
                            MimeType = Constants.EmailHeader.MimeType_Text,
                            Body = new MessagePartBody()
                            {
                                // Base64 string "Test"
                                Data = "VGVzdA=="
                            }
                        }
                    }
                }
            });

            var expectedMessage = new RecruitmentMessage()
            {
                RecruiterId = "tess@ting.com",
                RecruitmentMessageId = messageId,
                RecruiterName = "Tess Ting",
                RecruiterCompany = string.Empty,
                MessageSource = RecruitmentMessageSource.DirectEmail,
                Subject = "Hi There",
                Body = "Test",
                OccurredDate = DateTimeOffset.FromUnixTimeMilliseconds(internalDate),
                IsIncoming = true,
            };

            Func<RecruitmentMessage, bool> validate = call => {
                Assert.Equal(JsonConvert.SerializeObject(expectedMessage), JsonConvert.SerializeObject(call));
                return true;
            };

            var updater = new RecruitmentMessageUpdater(loggerMock.Object, config, linkedinApiMock.Object, gmailApiMock.Object, memetricsApiMock.Object);
            await updater.GetAndSaveEmailMessages();

            gmailApiMock.Verify(x => x.Authenticate(config.Value.Gmail_Main_Refresh_Token), Times.Once);
            memetricsApiMock.Verify(x => x.SaveRecruitmentMessage(It.Is<RecruitmentMessage>(z => validate(z))), Times.Once);
        }

        [Fact]
        public async Task GetAndSaveMessages_ShouldOnlySaveEmail_IfEmailIsNotOlderThanTwoDays()
        {
            var memetricsApiMock = new Mock<IMeMetricsApi>();
            var gmailApiMock = new Mock<IGmailApi>();
            var linkedinApiMock = new Mock<ILinkedInApi>();
            var loggerMock = new Mock<ILogger>();
            var config = Options.Create(new EnvironmentConfiguration()
            {
                Gmail_Main_Refresh_Token = "MainToken",
                Gmail_Recruiter_Label = "RecruiterLabel",
                Gmail_Recruiter_Email_Address = "myEmail@address.com"
            });

            var messageId = "1";

            gmailApiMock.Setup(x => x.GetLabels()).ReturnsAsync(new ListLabelsResponse()
            {
                Labels = new List<Label>()
                {
                    new Label(){ Name = config.Value.Gmail_Recruiter_Label, Id = config.Value.Gmail_Recruiter_Label}
                }
            });

            gmailApiMock.Setup(x => x.GetEmails(config.Value.Gmail_Recruiter_Label, null)).ReturnsAsync(new ListMessagesResponse()
            {
                Messages = new List<Message>()
                {
                    new Message() { Id = messageId }
                },
            });

            gmailApiMock.Setup(x => x.GetEmail(messageId)).ReturnsAsync(new Message()
            {
                InternalDate = DateTimeOffset.Now.AddDays(-3).ToUnixTimeMilliseconds(),
                Payload = new MessagePart()
                {
                    Headers = new List<MessagePartHeader>()
                    {
                        new MessagePartHeader(){ Name = Constants.EmailHeader.From, Value = "Tess Ting <tess@ting.com>" },
                        new MessagePartHeader(){ Name = Constants.EmailHeader.To, Value = config.Value.Gmail_Recruiter_Email_Address },
                        new MessagePartHeader(){ Name = Constants.EmailHeader.Subject, Value = "Hi There" },
                    },
                    Parts = new List<MessagePart>()
                    {
                        new MessagePart()
                        {
                            MimeType = Constants.EmailHeader.MimeType_Text,
                            Body = new MessagePartBody()
                            {
                                // Base64 string "Test"
                                Data = "VGVzdA=="
                            }
                        }
                    }
                }
            });

            var updater = new RecruitmentMessageUpdater(loggerMock.Object, config, linkedinApiMock.Object, gmailApiMock.Object, memetricsApiMock.Object);
            await updater.GetAndSaveEmailMessages();

            memetricsApiMock.Verify(x => x.SaveRecruitmentMessage(It.IsAny<RecruitmentMessage>()), Times.Never);
        }

        [Fact]
        public async Task GetAndSaveMessages_ShouldUseNextPageToken_IfOutOfMessages()
        {
            var memetricsApiMock = new Mock<IMeMetricsApi>();
            var gmailApiMock = new Mock<IGmailApi>();
            var linkedinApiMock = new Mock<ILinkedInApi>();
            var loggerMock = new Mock<ILogger>();
            var config = Options.Create(new EnvironmentConfiguration()
            {
                Gmail_Main_Refresh_Token = "MainToken",
                Gmail_Recruiter_Label = "RecruiterLabel",
                Gmail_Recruiter_Email_Address = "myEmail@address.com"
            });

            var messageId = "1";
            var secondMessageId = "2";
            var nextPageToken = "token";

            gmailApiMock.Setup(x => x.GetLabels()).ReturnsAsync(new ListLabelsResponse()
            {
                Labels = new List<Label>()
                {
                    new Label(){ Name = config.Value.Gmail_Recruiter_Label, Id = config.Value.Gmail_Recruiter_Label}
                }
            });

            gmailApiMock.Setup(x => x.GetEmails(config.Value.Gmail_Recruiter_Label, null)).ReturnsAsync(new ListMessagesResponse()
            {
                Messages = new List<Message>()
                {
                    new Message() { Id = messageId }
                },
                NextPageToken = nextPageToken
            });

            gmailApiMock.Setup(x => x.GetEmails(config.Value.Gmail_Recruiter_Label, nextPageToken)).ReturnsAsync(new ListMessagesResponse()
            {
                Messages = new List<Message>()
                {
                    new Message() { Id = secondMessageId }
                },
            });

            var internalDate = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            gmailApiMock.Setup(x => x.GetEmail(It.IsAny<string>())).ReturnsAsync(new Message()
            {
                InternalDate = internalDate,
                Payload = new MessagePart()
                {
                    Headers = new List<MessagePartHeader>()
                    {
                        new MessagePartHeader(){ Name = Constants.EmailHeader.From, Value = "Tess Ting <tess@ting.com>" },
                        new MessagePartHeader(){ Name = Constants.EmailHeader.To, Value = config.Value.Gmail_Recruiter_Email_Address },
                        new MessagePartHeader(){ Name = Constants.EmailHeader.Subject, Value = "Hi There" },
                    },
                    Parts = new List<MessagePart>()
                    {
                        new MessagePart()
                        {
                            MimeType = Constants.EmailHeader.MimeType_Text,
                            Body = new MessagePartBody()
                            {
                                // Base64 string "Test"
                                Data = "VGVzdA=="
                            }
                        }
                    }
                }
            });

            var updater = new RecruitmentMessageUpdater(loggerMock.Object, config, linkedinApiMock.Object, gmailApiMock.Object, memetricsApiMock.Object);
            await updater.GetAndSaveEmailMessages();

            memetricsApiMock.Verify(x => x.SaveRecruitmentMessage(It.IsAny<RecruitmentMessage>()), Times.Exactly(2));
        }
    }
}
