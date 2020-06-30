using System;
using System.Collections.Generic;
using AutoMapper;
using Bogus;
using Google.Apis.Gmail.v1.Data;
using MeMetrics.Updater.Application.Objects;
using MeMetrics.Updater.Application.Objects.Enums;
using MeMetrics.Updater.Application.Profiles;
using Xunit;

namespace MeMetrics.Updater.Application.Tests.Profiles
{
    public class RecruitmentMessageProfileTests
    {
        [Fact]
        public void Email_ShouldMapTo_RecruitmentMessage()
        {
            // ARRANGE
            var faker = new Faker("en");
            var id = faker.Random.Int(0).ToString();
            var body = faker.Random.String2(100);
            var subject = faker.Random.String2(100);
            var recruiterName = faker.Name.FullName();
            var name = faker.Name.FullName();
            var recruiterEmailAddress = faker.Internet.ExampleEmail();
            var emailAddress = faker.Internet.ExampleEmail();
            var dateWithMs = faker.Date.PastOffset();
            var date = new DateTimeOffset(dateWithMs.Year, dateWithMs.Month, dateWithMs.Day, dateWithMs.Hour, dateWithMs.Minute, dateWithMs.Second, dateWithMs.Offset);
            var bodyBytes = System.Text.Encoding.UTF8.GetBytes(body);
            var email = new Message()
            {
                Id = id,
                InternalDate = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                Payload = new MessagePart()
                {
                    Headers = new List<MessagePartHeader>()
                    {
                        new MessagePartHeader(){ Name = Constants.EmailHeader.Date, Value = date.ToString("ddd, dd MMM yyy HH:mm:ss zzz") },
                        new MessagePartHeader(){ Name = Constants.EmailHeader.From, Value = $"{recruiterName} <{recruiterEmailAddress}>" },
                        new MessagePartHeader(){ Name = Constants.EmailHeader.To, Value = $"{name} <{emailAddress}>" },
                        new MessagePartHeader(){ Name = Constants.EmailHeader.Subject, Value = subject },
                    },
                    Parts = new List<MessagePart>()
                    {
                        new MessagePart()
                        {
                            MimeType = Constants.EmailHeader.MimeType_Text,
                            Body = new MessagePartBody()
                            {
                                Data = Convert.ToBase64String(bodyBytes)
                            }
                        }
                    }
                }
            };
            var configuration = new MapperConfiguration(cfg => { cfg.AddProfile<RecruitmentMessageProfile>(); });
            var mapper = new Mapper(configuration);

            // ACT
            var message = mapper.Map<Objects.MeMetrics.RecruitmentMessage>(email, opts => opts.Items["Email"] = emailAddress);

            // ASSERT
            Assert.Equal(id, message.RecruitmentMessageId);
            Assert.Equal(body, message.Body);
            Assert.Equal(string.Empty, message.RecruiterCompany);
            Assert.Equal(RecruitmentMessageSource.DirectEmail, message.MessageSource);
            Assert.Equal(subject, message.Subject);
            Assert.Equal(recruiterName, message.RecruiterName);
            Assert.Equal(recruiterEmailAddress, message.RecruiterId);
            // Not concerned about millisecond precision since it doesn't come on the header
            Assert.Equal(date, message.OccurredDate);
            Assert.True(message.IsIncoming);
        }

        [Theory]
        [InlineData("PDT")]
        [InlineData("CEST")]
        public void Email_ShouldMapToRecruitmentMessage_WhenContainingTimezoneDescription(string timezoneDescription)
        {
            // ARRANGE
            var faker = new Faker("en");
            var id = faker.Random.Int(0).ToString();
            var body = faker.Random.String2(100);
            var subject = faker.Random.String2(100);
            var recruiterName = faker.Name.FullName();
            var name = faker.Name.FullName();
            var recruiterEmailAddress = faker.Internet.ExampleEmail();
            var emailAddress = faker.Internet.ExampleEmail();
            var dateWithMs = faker.Date.PastOffset();
            var date = new DateTimeOffset(dateWithMs.Year, dateWithMs.Month, dateWithMs.Day, dateWithMs.Hour, dateWithMs.Minute, dateWithMs.Second, dateWithMs.Offset);
            var bodyBytes = System.Text.Encoding.UTF8.GetBytes(body);
            var email = new Message()
            {
                Id = id,
                InternalDate = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                Payload = new MessagePart()
                {
                    Headers = new List<MessagePartHeader>()
                    {
                        new MessagePartHeader(){ Name = Constants.EmailHeader.Date, Value = $"{date.ToString("ddd, dd MMM yyy HH:mm:ss zzz")} ({timezoneDescription})" },
                        new MessagePartHeader(){ Name = Constants.EmailHeader.From, Value = $"{recruiterName} <{recruiterEmailAddress}>" },
                        new MessagePartHeader(){ Name = Constants.EmailHeader.To, Value = $"{name} <{emailAddress}>" },
                        new MessagePartHeader(){ Name = Constants.EmailHeader.Subject, Value = subject },
                    },
                    Parts = new List<MessagePart>()
                    {
                        new MessagePart()
                        {
                            MimeType = Constants.EmailHeader.MimeType_Text,
                            Body = new MessagePartBody()
                            {
                                Data = Convert.ToBase64String(bodyBytes)
                            }
                        }
                    }
                }
            };
            var configuration = new MapperConfiguration(cfg => { cfg.AddProfile<RecruitmentMessageProfile>(); });
            var mapper = new Mapper(configuration);

            // ACT
            var message = mapper.Map<Objects.MeMetrics.RecruitmentMessage>(email, opts => opts.Items["Email"] = emailAddress);

            // ASSERT
            // Not concerned about millisecond precision since it doesn't come on the header
            Assert.Equal(date, message.OccurredDate);
        }
    }
}