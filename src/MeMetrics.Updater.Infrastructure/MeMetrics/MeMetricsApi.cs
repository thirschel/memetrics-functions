using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using MeMetrics.Updater.Application.Interfaces;
using MeMetrics.Updater.Application.Objects;
using MeMetrics.Updater.Application.Objects.MeMetrics;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;

namespace MeMetrics.Updater.Infrastructure.MeMetrics
{
    public class MeMetricsApi : IMeMetricsApi
    {
    
        private readonly string _baseUrl;
        private readonly string _apiKey;
        private readonly HttpClient _client;
        private readonly ILogger _logger;

        public MeMetricsApi(
            IOptions<EnvironmentConfiguration> configuration,
            IHttpClientFactory httpClientFactory,
            ILogger logger)
        {
            _apiKey = configuration.Value.MeMetrics_Api_Key;
            _baseUrl = configuration.Value.MeMetrics_Base_Url;
            _client = httpClientFactory.CreateClient();
            _logger = logger;
        }


        public async Task SaveCall(Call call)
        {
            await PostRequest($"{_baseUrl}/api/v1/calls", call);

        }

        public async Task SaveMessage(Message message)
        {
            await PostRequest($"{_baseUrl}/api/v1/messages", message);

        }

        public async Task SaveChatMessage(ChatMessage chatMessage)
        {
            await PostRequest($"{_baseUrl}/api/v1/chat-messages", chatMessage);

        }

        public async Task SaveRide(Ride ride)
        {
            await PostRequest($"{_baseUrl}/api/v1/rides", ride);

        }

        public async Task SaveTransaction(Transaction transaction)
        {
            await PostRequest($"{_baseUrl}/api/v1/transactions", transaction);

        }

        public async Task SaveRecruitmentMessage(RecruitmentMessage recruitmentMessage)
        {
            await PostRequest($"{_baseUrl}/api/v1/recruitment-messages", recruitmentMessage);
        }

        public async Task Cache()
        {
            await PostRequest($"{_baseUrl}/api/v1/cache", null);
        }

        public async Task PostRequest(string uri, Object body)
        {
            _logger.Information($"Making request to {uri}");
            var request = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json")
            };

            request.Headers.Add(Constants.MeMetrics.ApiKeyHeaderName, _apiKey);
            var result = await _client.SendAsync(request);
            var resultString = await result.Content.ReadAsStringAsync();

            if (!result.IsSuccessStatusCode)
            {
                _logger.Error($"Failed POST to {uri}: {resultString}");
                throw new Exception(resultString);
            }
        }
    }
}