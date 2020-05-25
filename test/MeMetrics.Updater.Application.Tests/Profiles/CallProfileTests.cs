using System;
using System.Collections.Generic;
using AutoMapper;
using Bogus;
using Google.Apis.Gmail.v1.Data;
using MeMetrics.Updater.Application.Objects;
using MeMetrics.Updater.Application.Profiles;
using Xunit;

namespace MeMetrics.Updater.Application.Tests.Profiles
{
    public class CallProfileTests
    {
        [Fact]
        public void Email_ShouldMapTo_Call()
        {
            var faker = new Faker("en");
            var id = faker.Random.Int(0).ToString();
            var emailAddress = faker.Internet.ExampleEmail();
            var phone = faker.Phone.PhoneNumber("##########");
            var dateWithMs = faker.Date.PastOffset();
            var timespan = faker.Date.Timespan();
            var callOrigin = faker.PickRandom(new List<string>()
            {
                "incoming",
                "outgoing"
            });
            var date = new DateTimeOffset(dateWithMs.Year, dateWithMs.Month, dateWithMs.Day, dateWithMs.Hour, dateWithMs.Minute, dateWithMs.Second, dateWithMs.Offset);
            var email = new Message()
            {
                Id = id,
                InternalDate = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                Snippet = $"{timespan.Seconds}s ({timespan:hh\\:mm\\:ss}) {phone} ({callOrigin} call)",
                Payload = new MessagePart()
                {
                    Headers = new List<MessagePartHeader>()
                    {
                        new MessagePartHeader(){ Name = Constants.EmailHeader.Date, Value = date.ToString("ddd, dd MMM yyy HH:mm:ss zzz") },
                    }
                }
            };
            var configuration = new MapperConfiguration(cfg => { cfg.AddProfile<CallProfile>(); });
            var mapper = new Mapper(configuration);
            var call = mapper.Map<Objects.MeMetrics.Call>(email, opts => opts.Items["Email"] = emailAddress);
            Assert.Equal(id, call.CallId);
            // Not concerned about millisecond precision since it doesn't come on the header
            Assert.Equal(date, call.OccurredDate);
            Assert.Equal($"1{phone}", call.PhoneNumber);
            Assert.Equal(timespan.Seconds, call.Duration);
            Assert.Equal(callOrigin == "incoming", call.IsIncoming);
        }
    }
}