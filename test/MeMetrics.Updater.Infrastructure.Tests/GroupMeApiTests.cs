using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using Bogus;
using MeMetrics.Updater.Application.Objects;
using MeMetrics.Updater.Infrastructure.GroupMe;
using MeMetrics.Updater.Infrastructure.Lyft;
using MeMetrics.Updater.Infrastructure.Tests.Helpers;
using MeMetrics.Updater.Infrastructure.Uber;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using Xunit;

namespace MeMetrics.Updater.Infrastructure.Tests
{
    public class GroupMeApiTests
    {

        [Fact]
        public async void Authenticate_ShouldSetCredentials()
        {
            // ARRANGE
            var faker = new Faker();
            var token = faker.Random.String2(100);
            var loggerMock = new Mock<ILogger>();
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();

            var api = new GroupMeApi(httpClientFactoryMock.Object, loggerMock.Object);

            // ACT
            api.Authenticate(token);

            // ASSERT
            Assert.Contains(token, api._token);
        }

        [Fact]
        public async void GetGroups_ShouldSendCorrectRequest()
        {
            // ARRANGE
            var faker = new Faker();
            var htmlResponse = "{}";
            var token = faker.Random.String2(100);
            var handlerMock = TestHelpers.GetMockHttpClient(htmlResponse);
            var httpClient = new HttpClient(handlerMock.Object);
            var loggerMock = new Mock<ILogger>();
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();

            httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);
            var api = new GroupMeApi(httpClientFactoryMock.Object, loggerMock.Object);
            api._token = token;

            // ACT
            await api.GetGroups();

            // ASSERT
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri == new Uri($"https://api.groupme.com/v3/groups?token={token}")
                ),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        [Fact]
        public async void GetMessages_ShouldSendCorrectRequest()
        {
            // ARRANGE
            var faker = new Faker();
            var htmlResponse = "{}";
            var token = faker.Random.String2(100);
            var groupId = faker.Random.String2(10);
            var handlerMock = TestHelpers.GetMockHttpClient(htmlResponse);
            var httpClient = new HttpClient(handlerMock.Object);
            var loggerMock = new Mock<ILogger>();
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();

            httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);
            var api = new GroupMeApi(httpClientFactoryMock.Object, loggerMock.Object);
            api._token = token;

            // ACT
            await api.GetMessages(groupId);

            // ASSERT
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri == new Uri($"https://api.groupme.com/v3/groups/{groupId}/messages?token={token}&limit=100")
                ),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        [Fact]
        public async void GetMessages_ShouldSendCorrectRequest_IfLastMessageIdSupplied()
        {
            // ARRANGE
            var faker = new Faker();
            var htmlResponse = "{}";
            var token = faker.Random.String2(100);
            var groupId = faker.Random.String2(10);
            var lastMessageId = faker.Random.String2(10);
            var handlerMock = TestHelpers.GetMockHttpClient(htmlResponse);
            var httpClient = new HttpClient(handlerMock.Object);
            var loggerMock = new Mock<ILogger>();
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();

            httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);
            var api = new GroupMeApi(httpClientFactoryMock.Object, loggerMock.Object);
            api._token = token;

            // ACT
            await api.GetMessages(groupId, lastMessageId);

            // ASSERT
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri == new Uri($"https://api.groupme.com/v3/groups/{groupId}/messages?token={token}&limit=100&before_id={lastMessageId}")
                ),
                ItExpr.IsAny<CancellationToken>()
            );
        }
    }
}
