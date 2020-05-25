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
    public class ChatMessageProfileTests
    {
        [Fact]
        public void GroupMeMessage_ShouldMapTo_ChatMessage()
        {
            // ARRANGE
            var groupMeFaker = new Faker<Objects.GroupMe.Message>()
                .RuleFor(f => f.Id, f => f.Random.String2(32))
                .RuleFor(f => f.CreatedAt, f => f.Date.PastOffset().ToUnixTimeMilliseconds())
                .RuleFor(f => f.SenderId, f => f.Random.String2(32))
                .RuleFor(f => f.Name, f => f.Random.String2(100))
                .RuleFor(f => f.Text, f => f.Random.String2(100));

            var message = groupMeFaker.Generate();
            var configuration = new MapperConfiguration(cfg => { cfg.AddProfile<ChatMessageProfile>(); });
            var mapper = new Mapper(configuration);

            // ACT
            var chatmessage = mapper.Map<Objects.MeMetrics.ChatMessage>(message);

            // ASSERT
            Assert.Equal(message.Id, chatmessage.ChatMessageId);
            Assert.False(chatmessage.IsIncoming);
            Assert.False(chatmessage.IsMedia);
            Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(message.CreatedAt), chatmessage.OccurredDate);
            Assert.Equal(message.SenderId, chatmessage.SenderId);
            Assert.Equal(message.Name, chatmessage.SenderName);
            Assert.Equal(message.Text, chatmessage.Text);
            Assert.Equal(chatmessage.Text.Length, chatmessage.TextLength);
        }
    }
}