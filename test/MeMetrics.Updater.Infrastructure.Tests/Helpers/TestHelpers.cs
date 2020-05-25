using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using Serilog;

namespace MeMetrics.Updater.Infrastructure.Tests.Helpers
{
    public class TestHelpers
    {
        public static Mock<HttpMessageHandler> GetMockHttpClient(string stringContent = null, Dictionary<string, string> headers = null)
        {
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            var response = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(stringContent),
                Headers = { }
            };

            if (headers != null)
            {
                foreach (var kvp in headers)
                {
                    response.Headers.Add(kvp.Key, kvp.Value);
                }
            }

            handlerMock
                .Protected()
                // Setup the PROTECTED method to mock
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(response)
                .Verifiable();
            return handlerMock;
        }
    }
}