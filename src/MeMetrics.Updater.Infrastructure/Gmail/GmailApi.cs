using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Google.Apis.Gmail.v1.Data;
using MeMetrics.Updater.Application.Interfaces;
using MeMetrics.Updater.Application.Objects;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;
using OAuthToken = MeMetrics.Updater.Application.Objects.Gmail.OAuthToken;

[assembly: InternalsVisibleTo("MeMetrics.Updater.Infrastructure.Tests")]
namespace MeMetrics.Updater.Infrastructure.Gmail
{
    public class GmailApi : IGmailApi
    {
        private readonly string _baseUrl = "https://www.googleapis.com";
        internal readonly string _clientId;
        internal readonly string _clientSecret;
        internal OAuthToken _token;
        private readonly HttpClient _client;
        private readonly ILogger _logger;


        public GmailApi(
            IOptions<EnvironmentConfiguration> configuration, 
            IHttpClientFactory httpClientFactory,
            ILogger logger)
        {
            _clientId = configuration.Value.Gmail_Client_Id;
            _clientSecret = configuration.Value.Gmail_Client_Secret;
            _client = httpClientFactory.CreateClient();
            _logger = logger;
        }

        public async Task Authenticate(string refreshToken)
        {
            var data = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("client_secret", _clientSecret),
                new KeyValuePair<string, string>("refresh_token", refreshToken),
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
            };
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/oauth2/v4/token")
            {
                Content = new FormUrlEncodedContent(data)
            };
            var response = await _client.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();
            _token = JsonConvert.DeserializeObject<OAuthToken>(result);
        }

        public async Task<Message> GetEmail(string id)
        {
            return await SendAsync<Message>(HttpMethod.Get, $"/gmail/v1/users/me/messages/{id}");
        }

        public async Task<string> GetAttachment(string messageId, string attachmentId)
        {
            var response = await SendAsync<MessagePartBody>(HttpMethod.Get, $"/gmail/v1/users/me/messages/{messageId}/attachments/{attachmentId}");
            var attachData = response.Data.Replace('-', '+');
            attachData = attachData.Replace('_', '/');
            return attachData;
        }

        public async Task<ListLabelsResponse> GetLabels()
        {
            return await SendAsync<ListLabelsResponse>(HttpMethod.Get, "/gmail/v1/users/me/labels");
        }

        public async Task<ListMessagesResponse> GetEmails(string labelId, string pageToken = null)
        {
            var qp = pageToken != null ? $"&pageToken={pageToken}" : string.Empty;
            return await SendAsync<ListMessagesResponse>(HttpMethod.Get, $"/gmail/v1/users/me/messages?labelIds={labelId}{qp}");
        }

        private async Task<T> SendAsync<T>(HttpMethod httpMethod, string url,  HttpContent content = null)
        {
            var request = new HttpRequestMessage(httpMethod, $"{_baseUrl}{url}");

            if (content != null)
            {
                request.Content = content;
            }

            if (_token == null)
            {
                throw new Exception("Gmail Api is not authenticated. Call Authenticate before using other methods");
            }

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token.AccessToken);

            var response = await _client.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(result);
        }
    }
}
