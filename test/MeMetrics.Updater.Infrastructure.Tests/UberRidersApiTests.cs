using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using Bogus;
using MeMetrics.Updater.Application.Objects;
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
    public class UberRidersApiTests
    {

        [Fact]
        public async void Authenticate_ShouldSetCredentials()
        {
            // ARRANGE
            var htmlResponse = File.ReadAllText("Samples/uber-riders-trips.txt");
            // The csrf string is hardcoded in Samples/uber-riders-trips.txt
            var csrf = "1590098836-qtCX7-S1_TpQLRiRInNlbO4b3BxVT5tZB1zhGxlozfM";
            var jwtToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
            var cookie = "jwt-session=1; path=/; expires=Fri, 22 May 2020 21:47:43 GMT; secure; httponly";
            var userId = "1";
            var headers = new Dictionary<string, string>()
            {
                {"Set-Cookie", $"jwt-session={jwtToken}; path=/; expires=Fri, 22 May 2020 21:47:43 GMT; secure; httponly"}
            };
            var handlerMock = TestHelpers.GetMockHttpClient(htmlResponse, headers);
            var httpClient = new HttpClient(handlerMock.Object);
            var loggerMock = new Mock<ILogger>();
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();

            httpClientFactoryMock.Setup(x => x.CreateClient(Constants.HttpClients.DisabledAutomaticCookieHandling)).Returns(httpClient);
            var api = new UberRidersApi(httpClientFactoryMock.Object, loggerMock.Object);

            // ACT
            await api.Authenticate(cookie, userId);

            // ASSERT
            Assert.Equal(userId, api._userId);
            Assert.Contains(jwtToken, api._cookie);
            Assert.Equal(csrf, api._csrf);
        }

        [Fact]
        public async void GetTrips_ShouldSendCorrectRequest()
        {
            // ARRANGE
            var faker = new Faker();
            var htmlResponse = "{}";
            var handlerMock = TestHelpers.GetMockHttpClient(htmlResponse);
            var httpClient = new HttpClient(handlerMock.Object);
            var loggerMock = new Mock<ILogger>();
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();

            httpClientFactoryMock.Setup(x => x.CreateClient(Constants.HttpClients.DisabledAutomaticCookieHandling)).Returns(httpClient);
            var api = new UberRidersApi(httpClientFactoryMock.Object, loggerMock.Object);
            var requestContent = new
            {
                limit = 10,
                offset = faker.Random.Int(0, 1000).ToString()
            };
            var csrf = faker.Random.String2(40);
            var cookie = "jwt-session=1; path=/; expires=Fri, 22 May 2020 21:47:43 GMT; secure; httponly";
            api._csrf = csrf;
            api._cookie = cookie;

            // ACT
            await api.GetTrips(int.Parse(requestContent.offset));

            // ASSERT
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req =>
                        req.Headers.First(x => x.Key == "x-csrf-token").Value.First() == csrf &&
                        req.Headers.First(x => x.Key == "Cookie").Value.First() == cookie &&
                        req.RequestUri == new Uri("https://riders.uber.com/api/getTripsForClient") &&
                        ContentEqualsReference(req.Content.ReadAsStringAsync().GetAwaiter().GetResult(), requestContent)
                ),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        [Fact]
        public async void GetTripDetails_ShouldSendCorrectRequest()
        {
            // ARRANGE
            var faker = new Faker();
            var htmlResponse = "{}";
            var handlerMock = TestHelpers.GetMockHttpClient(htmlResponse);
            var httpClient = new HttpClient(handlerMock.Object);
            var loggerMock = new Mock<ILogger>();
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();

            httpClientFactoryMock.Setup(x => x.CreateClient(Constants.HttpClients.DisabledAutomaticCookieHandling)).Returns(httpClient);
            var api = new UberRidersApi(httpClientFactoryMock.Object, loggerMock.Object);
            var requestContent = new
            {
                tripUUID = faker.Random.Uuid().ToString(),
                tenancy = "uber/production"
            };
            var csrf = faker.Random.String2(40);
            var cookie = "jwt-session=1; path=/; expires=Fri, 22 May 2020 21:47:43 GMT; secure; httponly";
            api._csrf = csrf;
            api._cookie = cookie;

            // ACT
            await api.GetTripDetails(requestContent.tripUUID);

            // ASSERT
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>((req) =>
                    req.Headers.First(x => x.Key == "x-csrf-token").Value.First() == csrf &&
                    req.Headers.First(x => x.Key == "Cookie").Value.First() == cookie &&
                    req.RequestUri == new Uri("https://riders.uber.com/api/getTrip") &&
                    ContentEqualsReference(req.Content.ReadAsStringAsync().GetAwaiter().GetResult(), requestContent)
                ),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        private bool ContentEqualsReference(string json, dynamic obj)
        {
            Assert.Equal(JsonConvert.SerializeObject(obj), json);
            return true;
        }
    }
}
