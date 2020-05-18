using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using MeMetrics.Updater.Application.Objects.Uber;
using Newtonsoft.Json;

namespace MeMetrics.Updater.Infrastructure.Uber
{
    public class Uber
    {
        /*
         * Uber previously had a public developer API that anyone could sign up for an use. They have since closed it off to
         * only corporate developers who are actively making applications that utilize Uber.
         */
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _refreshToken;
        private readonly HttpClient _client;
        private OAuthToken _token;

        public Uber(string clientId, string clientSecret, string refreshToken, HttpClient client)
        {
            _clientId = clientId;
            _clientSecret = clientSecret;
            _refreshToken = refreshToken;
            _client = client ?? new HttpClient();
        }

        public async Task RefreshToken()
        {
            var form = new MultipartFormDataContent
            {
                {new StringContent(_clientSecret), "\"client_secret\""},
                {new StringContent(_clientId), "\"client_id\""},
                {new StringContent("refresh_token"), "\"grant_type\""},
                {new StringContent(_refreshToken), "\"refresh_token\""}
            };

            var values = new Dictionary<string, string>
            {
                { "client_secret", _clientSecret },
                { "client_id", _clientId },
                { "grant_type", "refresh_token" },
                { "refresh_token", _refreshToken }
            };

            var content = new FormUrlEncodedContent(values);
            var response = await _client.PostAsync("https://login.uber.com/oauth/v2/token", content);
            var result = await response.Content.ReadAsStringAsync();
            _token = JsonConvert.DeserializeObject<OAuthToken>(result);
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_token.AccessToken}");
        }

        /*
         * Uber removed their developer platform and their access to this API
         */
        [Obsolete]
        public async Task<HistoryResponse> GetTrips(int offset = 0)
        {
            var response = await _client.GetAsync($"https://api.uber.com/v1.2/history?limit=50&offset={offset}");
            var result = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<HistoryResponse>(result);
        }

    }
}
