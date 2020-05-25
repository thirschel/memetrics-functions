using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using Bogus;
using MeMetrics.Updater.Application.Objects;
using MeMetrics.Updater.Application.Objects.Gmail;
using MeMetrics.Updater.Infrastructure.Gmail;
using MeMetrics.Updater.Infrastructure.GroupMe;
using MeMetrics.Updater.Infrastructure.Lyft;
using MeMetrics.Updater.Infrastructure.Tests.Helpers;
using MeMetrics.Updater.Infrastructure.Uber;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using Xunit;

namespace MeMetrics.Updater.Infrastructure.Tests
{
    public class GmailApiTests
    {

        [Fact]
        public async void Authenticate_ShouldSetCredentials()
        {
            // ARRANGE
            var faker = new Faker();
            var config = Options.Create(new EnvironmentConfiguration()
            {
                Gmail_Client_Id = faker.Random.AlphaNumeric(32),
                Gmail_Client_Secret = faker.Random.AlphaNumeric(32),
            });
            var accessToken = faker.Random.AlphaNumeric(200);
            var htmlResponse = "{\"access_token\": \""+ accessToken + "\"}";
            var refreshToken = faker.Random.AlphaNumeric(100);
            var handlerMock = TestHelpers.GetMockHttpClient(htmlResponse);
            var httpClient = new HttpClient(handlerMock.Object);
            var loggerMock = new Mock<ILogger>();
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            var formData = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("client_id", config.Value.Gmail_Client_Id),
                new KeyValuePair<string, string>("client_secret", config.Value.Gmail_Client_Secret),
                new KeyValuePair<string, string>("refresh_token", refreshToken),
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
            };
            var requestContent = new FormUrlEncodedContent(formData).ReadAsStringAsync().GetAwaiter().GetResult();
            httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);
            var api = new GmailApi(config, httpClientFactoryMock.Object, loggerMock.Object);

            // ACT
            await api.Authenticate(refreshToken);

            // ASSERT
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri == new Uri($"https://www.googleapis.com/oauth2/v4/token")
                    && ContentEqualsReference(req.Content.ReadAsStringAsync().GetAwaiter().GetResult(), requestContent)
                ),
                ItExpr.IsAny<CancellationToken>()
            );
            Assert.Equal(accessToken, api._token.AccessToken);
        }

        [Fact]
        public async void GetEmail_ShouldSendRequestCorrectly()
        {
            // ARRANGE
            var faker = new Faker();
            var config = Options.Create(new EnvironmentConfiguration()
            {
                Gmail_Client_Id = faker.Random.AlphaNumeric(32),
                Gmail_Client_Secret = faker.Random.AlphaNumeric(32),
            });
            var messageId = faker.Random.AlphaNumeric(10);
            var token = faker.Random.AlphaNumeric(100);
            var htmlResponse = "{}";
            var handlerMock = TestHelpers.GetMockHttpClient(htmlResponse);
            var httpClient = new HttpClient(handlerMock.Object);
            var loggerMock = new Mock<ILogger>();
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);
            var api = new GmailApi(config, httpClientFactoryMock.Object, loggerMock.Object);
            api._token = new OAuthToken() {AccessToken = token};

            // ACT
            await api.GetEmail(messageId);

            // ASSERT
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.Headers.First(x => x.Key == "Authorization").Value.First() == $"Bearer {token}" &&
                    req.RequestUri == new Uri($"https://www.googleapis.com/gmail/v1/users/me/messages/{messageId}")
                ),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        [Fact]
        public async void GetLabels_ShouldSendRequestCorrectly()
        {
            // ARRANGE
            var faker = new Faker();
            var config = Options.Create(new EnvironmentConfiguration()
            {
                Gmail_Client_Id = faker.Random.AlphaNumeric(32),
                Gmail_Client_Secret = faker.Random.AlphaNumeric(32),
            });
            var messageId = faker.Random.AlphaNumeric(10);
            var token = faker.Random.AlphaNumeric(100);
            var htmlResponse = "{}";
            var handlerMock = TestHelpers.GetMockHttpClient(htmlResponse);
            var httpClient = new HttpClient(handlerMock.Object);
            var loggerMock = new Mock<ILogger>();
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);
            var api = new GmailApi(config, httpClientFactoryMock.Object, loggerMock.Object);
            api._token = new OAuthToken() { AccessToken = token };

            // ACT
            await api.GetLabels();

            // ASSERT
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.Headers.First(x => x.Key == "Authorization").Value.First() == $"Bearer {token}" &&
                    req.RequestUri == new Uri($"https://www.googleapis.com/gmail/v1/users/me/labels")
                ),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        [Fact]
        public async void GetEmails_ShouldSendRequestCorrectly()
        {
            // ARRANGE
            var faker = new Faker();
            var config = Options.Create(new EnvironmentConfiguration()
            {
                Gmail_Client_Id = faker.Random.AlphaNumeric(32),
                Gmail_Client_Secret = faker.Random.AlphaNumeric(32),
            });
            var labelId = faker.Random.AlphaNumeric(10);
            var token = faker.Random.AlphaNumeric(100);
            var htmlResponse = "{}";
            var handlerMock = TestHelpers.GetMockHttpClient(htmlResponse);
            var httpClient = new HttpClient(handlerMock.Object);
            var loggerMock = new Mock<ILogger>();
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);
            var api = new GmailApi(config, httpClientFactoryMock.Object, loggerMock.Object);
            api._token = new OAuthToken() { AccessToken = token };

            // ACT
            await api.GetEmails(labelId);

            // ASSERT
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.Headers.First(x => x.Key == "Authorization").Value.First() == $"Bearer {token}" &&
                    req.RequestUri == new Uri($"https://www.googleapis.com/gmail/v1/users/me/messages?labelIds={labelId}")
                ),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        [Fact]
        public async void GetEmails_ShouldSendRequestCorrectly_IfPageTokenSupplied()
        {
            // ARRANGE
            var faker = new Faker();
            var config = Options.Create(new EnvironmentConfiguration()
            {
                Gmail_Client_Id = faker.Random.AlphaNumeric(32),
                Gmail_Client_Secret = faker.Random.AlphaNumeric(32),
            });
            var labelId = faker.Random.AlphaNumeric(10);
            var pageToken = faker.Random.AlphaNumeric(10);
            var token = faker.Random.AlphaNumeric(100);
            var htmlResponse = "{}";
            var handlerMock = TestHelpers.GetMockHttpClient(htmlResponse);
            var httpClient = new HttpClient(handlerMock.Object);
            var loggerMock = new Mock<ILogger>();
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);
            var api = new GmailApi(config, httpClientFactoryMock.Object, loggerMock.Object);
            api._token = new OAuthToken() { AccessToken = token };

            // ACT
            await api.GetEmails(labelId, pageToken);

            // ASSERT
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.Headers.First(x => x.Key == "Authorization").Value.First() == $"Bearer {token}" &&
                    req.RequestUri == new Uri($"https://www.googleapis.com/gmail/v1/users/me/messages?labelIds={labelId}&pageToken={pageToken}")
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
