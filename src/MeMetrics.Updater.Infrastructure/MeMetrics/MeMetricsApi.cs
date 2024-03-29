﻿using System;
using System.Collections.Generic;
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


        public async Task SaveCalls(IList<Call> calls)
        {
            await PostRequest($"{_baseUrl}/api/v2/calls", calls);
        }

        public async Task SaveMessages(IList<Message> messages)
        {
            await PostRequest($"{_baseUrl}/api/v2/messages", messages);
        }

        public async Task SaveChatMessage(ChatMessage chatMessage)
        {
            await PostRequest($"{_baseUrl}/api/v1/chat-messages", chatMessage);
        }

        public async Task SaveRide(Ride ride)
        {
            await PostRequest($"{_baseUrl}/api/v1/rides", ride);
        }

        public async Task SaveTransactions(List<Transaction> transactions)
        {
            await PostRequest($"{_baseUrl}/api/v2/transactions", transactions);
        }

        public async Task SaveRecruitmentMessages(List<RecruitmentMessage> recruitmentMessages)
        {
            await PostRequest($"{_baseUrl}/api/v2/recruitment-messages", recruitmentMessages);
        }

        public async Task Cache()
        {
            await PostRequest($"{_baseUrl}/api/v1/cache", null);
        }

        public async Task PostRequest(string uri, Object body)
        {
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