using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using Bogus;
using MeMetrics.Updater.Application.Objects;
using MeMetrics.Updater.Infrastructure.PersonalCapital;
using MeMetrics.Updater.Infrastructure.Tests.Helpers;
using Moq;
using Moq.Protected;
using Serilog;
using Xunit;

namespace MeMetrics.Updater.Infrastructure.Tests
{
    public class PersonalCapitalApiTests
    {
        private readonly Mock<ILogger> _loggerMock;
        public PersonalCapitalApiTests()
        {
            _loggerMock = new Mock<ILogger>();
        }

        [Fact]
        public async void GetInitialCsrf_ShouldSetCsrf()
        {
            // ARRANGE
            var htmlResponse = File.ReadAllText("Samples/personal-capital-login-page.txt");
            // The csrf string is hardcoded in Samples/uber-riders-trips.txt
            var csrf = "3d60b6df-856c-438d-b3b1-f7dd1f1d0e39";
            var handlerMock = TestHelpers.GetMockHttpClient(htmlResponse);
            var httpClient = new HttpClient(handlerMock.Object);
            var loggerMock = new Mock<ILogger>();
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();

            httpClientFactoryMock.Setup(x => x.CreateClient(Constants.HttpClients.DisabledAutomaticCookieHandling)).Returns(httpClient);
            var api = new PersonalCapitalApi(loggerMock.Object, httpClientFactoryMock.Object);

            // ACT
            await api.GetInitialCsrf();

            // ASSERT
            Assert.Equal(csrf, api._csrf);
        }

        [Fact]
        public async void IdentifyUserAndGetUserCsrf_ShouldSendCorrectRequest()
        {
            // ARRANGE
            var faker = new Faker();
            var setCookie = faker.Random.String2(100);
            var csrf = faker.Random.String2(40);
            var userCsrf = faker.Random.String2(40);
            var userName = faker.Internet.Email();
            var pmData = faker.Random.String2(100);
            var htmlResponse = "{\"spHeader\": { \"csrf\": \""+ userCsrf +"\" } }";
            var headers = new Dictionary<string, string>()
            {
                {"Set-Cookie", setCookie}
            };
            var handlerMock = TestHelpers.GetMockHttpClient(htmlResponse, headers);
            var httpClient = new HttpClient(handlerMock.Object);
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();

            httpClientFactoryMock.Setup(x => x.CreateClient(Constants.HttpClients.DisabledAutomaticCookieHandling)).Returns(httpClient);
            var api = new PersonalCapitalApi(_loggerMock.Object, httpClientFactoryMock.Object);
            
            var formData = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("username", userName),
                new KeyValuePair<string, string>("csrf", csrf),
                new KeyValuePair<string, string>("apiClient", "WEB"),
                new KeyValuePair<string, string>("bindDevice", "false"),
                new KeyValuePair<string, string>("skipLinkAccount", "false"),
                new KeyValuePair<string, string>("redirectTo", ""),
                new KeyValuePair<string, string>("skipFirstUse", ""),
                new KeyValuePair<string, string>("referrerId", "")
            };
            var requestContent = new FormUrlEncodedContent(formData).ReadAsStringAsync().GetAwaiter().GetResult();
            api._csrf = csrf;

            // ACT
            var cookieHeaders = await api.IdentifyUserAndGetUserCsrf(userName, pmData);

            // ASSERT
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req =>
                        req.Headers.First(x => x.Key == "Cookie").Value.First() == $"PMData={pmData};" &&
                        req.RequestUri == new Uri("https://home.personalcapital.com/api/login/identifyUser") &&
                        ContentEqualsReference(req.Content.ReadAsStringAsync().GetAwaiter().GetResult(), requestContent)
                ),
                ItExpr.IsAny<CancellationToken>()
            );
            Assert.Equal(userCsrf, api._csrf);
            Assert.Equal(setCookie, cookieHeaders);
        }

        [Fact]
        public async void AuthenticatePassword_ShouldSendCorrectRequest()
        {
            // ARRANGE
            var faker = new Faker();
            var cookie = faker.Random.String2(100);
            var csrf = faker.Random.String2(40);
            var password = faker.Random.String2(40);
            var htmlResponse = "{}";
            var handlerMock = TestHelpers.GetMockHttpClient(htmlResponse);
            var httpClient = new HttpClient(handlerMock.Object);
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();

            httpClientFactoryMock.Setup(x => x.CreateClient(Constants.HttpClients.DisabledAutomaticCookieHandling)).Returns(httpClient);
            var api = new PersonalCapitalApi(_loggerMock.Object, httpClientFactoryMock.Object);


            var formData = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("bindDevice", "true"),
                new KeyValuePair<string, string>("deviceName", ""),
                new KeyValuePair<string, string>("redirectTo", ""),
                new KeyValuePair<string, string>("skipFirstUse", ""),
                new KeyValuePair<string, string>("skipLinkAccount",  "false"),
                new KeyValuePair<string, string>("referrerId",  ""),
                new KeyValuePair<string, string>("passwd",  password),
                new KeyValuePair<string, string>("apiClient",  "WEB"),
                new KeyValuePair<string, string>("csrf",  csrf),
            };
            var requestContent = new FormUrlEncodedContent(formData).ReadAsStringAsync().GetAwaiter().GetResult();
            api._csrf = csrf;
            api._cookie = cookie;

            // ACT
            await api.AuthenticatePassword(password);

            // ASSERT
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post &&
                        req.Headers.First(x => x.Key == "Cookie").Value.First() == cookie &&
                        req.RequestUri == new Uri("https://home.personalcapital.com/api/credential/authenticatePassword") &&
                        ContentEqualsReference(req.Content.ReadAsStringAsync().GetAwaiter().GetResult(), requestContent)
                ),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        [Fact]
        public async void GetAccounts_ShouldSendCorrectRequest()
        {
            // ARRANGE
            var faker = new Faker();
            var cookie = faker.Random.String2(100);
            var csrf = faker.Random.String2(40);
            var password = faker.Random.String2(40);
            var htmlResponse = "{}";
            var handlerMock = TestHelpers.GetMockHttpClient(htmlResponse);
            var httpClient = new HttpClient(handlerMock.Object);
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();

            httpClientFactoryMock.Setup(x => x.CreateClient(Constants.HttpClients.DisabledAutomaticCookieHandling)).Returns(httpClient);
            var api = new PersonalCapitalApi(_loggerMock.Object, httpClientFactoryMock.Object);


            var formData = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("lastServerChangeId", "-1"),
                new KeyValuePair<string, string>("apiClient", "WEB"),
                new KeyValuePair<string, string>("csrf",  csrf),
            };
            var requestContent = new FormUrlEncodedContent(formData).ReadAsStringAsync().GetAwaiter().GetResult();
            api._csrf = csrf;
            api._cookie = cookie;

            // ACT
            await api.GetAccounts();

            // ASSERT
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post &&
                        req.Headers.First(x => x.Key == "Cookie").Value.First() == cookie &&
                        req.RequestUri == new Uri("https://home.personalcapital.com/api/newaccount/getAccounts") &&
                        ContentEqualsReference(req.Content.ReadAsStringAsync().GetAwaiter().GetResult(), requestContent)
                ),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        [Fact]
        public async void GetUserTransactions_ShouldSendCorrectRequest()
        {
            // ARRANGE
            var faker = new Faker();
            var cookie = faker.Random.String2(100);
            var csrf = faker.Random.String2(40);
            var startDate = faker.Date.Past().ToString("yyyy-MM-dd");
            var endDate = DateTime.Now.ToString("yyyy-MM-dd");
            var htmlResponse = "{}";
            var handlerMock = TestHelpers.GetMockHttpClient(htmlResponse);
            var httpClient = new HttpClient(handlerMock.Object);
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();

            httpClientFactoryMock.Setup(x => x.CreateClient(Constants.HttpClients.DisabledAutomaticCookieHandling)).Returns(httpClient);
            var api = new PersonalCapitalApi(_loggerMock.Object, httpClientFactoryMock.Object);


            var formData = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("lastServerChangeId", "73"),
                new KeyValuePair<string, string>("startDate", startDate),
                new KeyValuePair<string, string>("endDate", endDate),
                new KeyValuePair<string, string>("apiClient", "WEB"),
                new KeyValuePair<string, string>("csrf",  csrf),
            };
            var requestContent = new FormUrlEncodedContent(formData).ReadAsStringAsync().GetAwaiter().GetResult();
            api._csrf = csrf;
            api._cookie = cookie;

            // ACT
            await api.GetUserTransactions(startDate, endDate);

            // ASSERT
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post &&
                        req.Headers.First(x => x.Key == "Cookie").Value.First() == cookie &&
                        req.RequestUri == new Uri("https://home.personalcapital.com/api/transaction/getUserTransactions") &&
                        ContentEqualsReference(req.Content.ReadAsStringAsync().GetAwaiter().GetResult(), requestContent)
                ),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        private bool ContentEqualsReference(string json, dynamic obj)
        {
            Assert.Equal(obj, json);
            return true;
        }
    }
}
