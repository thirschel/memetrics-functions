using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using Bogus;
using MeMetrics.Updater.Application.Objects;
using MeMetrics.Updater.Infrastructure.Lyft;
using MeMetrics.Updater.Infrastructure.Tests.Helpers;
using Moq;
using Moq.Protected;
using Serilog;
using Xunit;

namespace MeMetrics.Updater.Infrastructure.Tests
{
    public class LyftApiTests
    {

        [Fact]
        public async void Authenticate_ShouldSetCredentials()
        {
            // ARRANGE
            var faker = new Faker();
            var htmlResponse = "{}";
            var cookie = faker.Random.String2(100);
            var handlerMock = TestHelpers.GetMockHttpClient(htmlResponse);
            var httpClient = new HttpClient(handlerMock.Object);
            var loggerMock = new Mock<ILogger>();
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();

            httpClientFactoryMock.Setup(x => x.CreateClient(Constants.HttpClients.DisabledAutomaticCookieHandling)).Returns(httpClient);
            var api = new LyftApi(httpClientFactoryMock.Object, loggerMock.Object);

            // ACT
            await api.Authenticate(cookie);

            // ASSERT
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Headers.First(x => x.Key == "Cookie").Value.First() == cookie &&
                    req.Method == HttpMethod.Get &&
                    req.RequestUri == new Uri("https://www.lyft.com/api/auth?refresh=true")
                ),
                ItExpr.IsAny<CancellationToken>()
            );
            Assert.Contains(cookie, api._cookie);
        }

        [Fact]
        public async void GetTrips_ShouldSendCorrectRequest()
        {
            // ARRANGE
            var faker = new Faker();
            var htmlResponse = "{}";
            var skip = faker.Random.Int(0, 1000);
            var cookie = faker.Random.String2(100);
            var handlerMock = TestHelpers.GetMockHttpClient(htmlResponse);
            var httpClient = new HttpClient(handlerMock.Object);
            var loggerMock = new Mock<ILogger>();
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();

            httpClientFactoryMock.Setup(x => x.CreateClient(Constants.HttpClients.DisabledAutomaticCookieHandling)).Returns(httpClient);
            var api = new LyftApi(httpClientFactoryMock.Object, loggerMock.Object);
            api._cookie = cookie;

            // ACT
            await api.GetTrips(skip);

            // ASSERT
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Headers.First(x => x.Key == "Cookie").Value.First() == cookie &&
                    req.Method == HttpMethod.Get &&
                    req.RequestUri == new Uri($"https://www.lyft.com/api/passenger_rides?skip={skip}")
                ),
                ItExpr.IsAny<CancellationToken>()
            );
            Assert.Contains(cookie, api._cookie);
        }
    }
}
