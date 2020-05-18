using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using MeMetrics.Updater.Application.Objects.Lyft;
using Newtonsoft.Json;

namespace MeMetrics.Updater.Infrastructure.Lyft
{
    public class Riders
    {
        private readonly string baseUrl = "https://lyft.com";
        private readonly CookieContainer _cookieContainer = new CookieContainer();
        private string _cookie;
        private readonly string _basicAuth;
        private readonly string userId = "84ae775e-1c89-4361-86d5-2e87192a4511";
        private string _csrf = "3a9ff132-fdf9-446f-b3cb-7a65af988605";
        private readonly HttpClient _client;

        public Riders(string cookie)
        {
            _client = new HttpClient(new HttpClientHandler() { CookieContainer = _cookieContainer })
            {
                BaseAddress = new Uri(baseUrl)
            };
            _cookie = cookie;
        }

        public async Task<PassengerRidesResponse> GetPassengerRides()
        {
            var cookieContainer = new CookieContainer();
            var client = new HttpClient(new HttpClientHandler() { CookieContainer = cookieContainer })
            {
                BaseAddress = new Uri(baseUrl)
            };
            var uri = new Uri("https://riders.uber.com/api/getTrip");
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

        public Task<RideHistoryResponse> GetRides(string startTime, string endTime)
        {
            throw new NotImplementedException();
        }
    }
}