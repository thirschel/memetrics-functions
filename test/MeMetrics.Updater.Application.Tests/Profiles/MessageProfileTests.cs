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
    public class MessageProfileTests
    {
        [Fact]
        public void Email_ShouldMapToMessage()
        {
            // ARRANGE
            var faker = new Faker("en");
            var id = faker.Random.Int(0).ToString();
            var body = faker.Random.String2(100);
            var name = faker.Name.FullName();
            var emailAddress = faker.Internet.ExampleEmail();
            var phone = faker.Phone.PhoneNumber("##########");
            var threadId = faker.Random.Int(0).ToString();
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
                        new MessagePartHeader(){ Name = Constants.EmailHeader.From, Value = $"{name} <{phone}@unknown.email>" },
                        new MessagePartHeader(){ Name = Constants.EmailHeader.To, Value = emailAddress },
                        new MessagePartHeader(){ Name = Constants.EmailHeader.ThreadId, Value = threadId },
                        new MessagePartHeader(){ Name = Constants.EmailHeader.PhoneNumber, Value = phone },
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
            var configuration = new MapperConfiguration(cfg => { cfg.AddProfile<MessageProfile>(); });
            var mapper = new Mapper(configuration);
            
            // ACT
            var message = mapper.Map<Objects.MeMetrics.Message>(email, opts => opts.Items["Email"] = emailAddress);

            // ASSERT
            Assert.Equal(id, message.MessageId);
            Assert.Equal(body, message.Text);
            Assert.Equal(body.Length, message.TextLength);
            Assert.Equal(name, message.Name);
            Assert.Equal($"1{phone}", message.PhoneNumber);
            Assert.Equal(threadId, message.ThreadId.ToString());
            // Not concerned about millisecond precision since it doesn't come on the header
            Assert.Equal(date, message.OccurredDate);
            Assert.True(message.IsIncoming);
            Assert.False(message.IsMedia);
        }

        [Theory]
        [InlineData("PDT")]
        [InlineData("CEST")]
        public void Email_ShouldMapToMessage_WhenContainingTimezoneDescription(string timezoneDescription)
        {
            // ARRANGE
            var faker = new Faker("en");
            var id = faker.Random.Int(0).ToString();
            var body = faker.Random.String2(100);
            var name = faker.Name.FullName();
            var emailAddress = faker.Internet.ExampleEmail();
            var phone = faker.Phone.PhoneNumber("##########");
            var threadId = faker.Random.Int(0).ToString();
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
                        new MessagePartHeader(){ Name = Constants.EmailHeader.From, Value = $"{name} <{phone}@unknown.email>" },
                        new MessagePartHeader(){ Name = Constants.EmailHeader.To, Value = emailAddress },
                        new MessagePartHeader(){ Name = Constants.EmailHeader.ThreadId, Value = threadId },
                        new MessagePartHeader(){ Name = Constants.EmailHeader.PhoneNumber, Value = phone },
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
            var configuration = new MapperConfiguration(cfg => { cfg.AddProfile<MessageProfile>(); });
            var mapper = new Mapper(configuration);
            
            // ACT
            var message = mapper.Map<Objects.MeMetrics.Message>(email, opts => opts.Items["Email"] = emailAddress);

            // ASSERT
            Assert.Equal(id, message.MessageId);
            Assert.Equal(body, message.Text);
            Assert.Equal(body.Length, message.TextLength);
            Assert.Equal(name, message.Name);
            Assert.Equal($"1{phone}", message.PhoneNumber);
            Assert.Equal(threadId, message.ThreadId.ToString());
            // Not concerned about millisecond precision since it doesn't come on the header
            Assert.Equal(date, message.OccurredDate);
            Assert.True(message.IsIncoming);
            Assert.False(message.IsMedia);
        }
    }
}