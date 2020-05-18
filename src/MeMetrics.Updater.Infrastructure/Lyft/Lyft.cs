using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using MeMetrics.Updater.Application.Interfaces;
using MeMetrics.Updater.Application.Objects.Lyft;
using Newtonsoft.Json;

namespace MeMetrics.Updater.Infrastructure.Lyft
{
    public class Lyft : ILyftApi
    {
        /*
         * Lyft previously had a public developer API that anyone could sign up for an use. They have since closed it off to
         * only corporate developers who are actively making applications that utilize Lyft.
         */
        private readonly HttpClient _client;
        private OAuthToken _token;

        private readonly string baseUrl = "https://lyft.com";
        private readonly CookieContainer _cookieContainer = new CookieContainer();
        private string _cookie;
        private readonly string userId = "84ae775e-1c89-4361-86d5-2e87192a4511";
        private string _csrf = "3a9ff132-fdf9-446f-b3cb-7a65af988605";

        public Lyft(string cookie, string basicAuth, HttpClient client)
        {
            _client = client ?? new HttpClient(new HttpClientHandler() { CookieContainer = _cookieContainer })
            {
                BaseAddress = new Uri(baseUrl)
            };
            _cookie = cookie;
        }

        public async Task RefreshAndSetOAuthToken()
        {
//            var body = new StringContent("{\"grant_type\":\"refresh_token\",\"refresh_token\":\"" + _refreshToken + "\"}",
//                Encoding.UTF8,
//                "application/json");
//            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
//
//            var byteArray = Encoding.ASCII.GetBytes(_basicAuth);
//            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
//
//            var response = await _client.PostAsync($"{baseUrl}/oauth/token", body);
//            var jsonString = await response.Content.ReadAsStringAsync();
//            _token = JsonConvert.DeserializeObject<OAuthToken>(jsonString);
//
//            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token.AccessToken);
        }

        public async Task<RideHistoryResponse> GetRides(string startTime, string endTime)
        {
            var response = await _client.GetAsync($"{baseUrl}/v1/rides?start_time={startTime}&end_time={endTime}&limit=50");
            var jsonString = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<RideHistoryResponse>(jsonString);
        }

        public async Task<PassengerRidesResponse> GetPassengerRides()
        {
            var cookieContainer = new CookieContainer();
            var client = new HttpClient(new HttpClientHandler() { CookieContainer = cookieContainer })
            {
                BaseAddress = new Uri(baseUrl)
            };
            var uri = new Uri("https://lyft.com/api/passenger_rides");
            var cookies = _cookie.Split(';');
            foreach (var cookie in cookies)
            {
                var values = cookie.Split(new[] { '=' }, 2);
                if (values[0] != " _ua")
                {
                    cookieContainer.Add(uri, new Cookie(values[0].Trim(), values[1].Trim()));
                }
            }
            var request = new HttpRequestMessage(HttpMethod.Get, uri)
            {
            };
            client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");
            var response = await client.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<PassengerRidesResponse>(result);

        }
    }
}
