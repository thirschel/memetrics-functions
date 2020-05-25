using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MeMetrics.Updater.Application.Interfaces;
using MeMetrics.Updater.Application.Objects;
using MeMetrics.Updater.Application.Objects.Uber;
using Newtonsoft.Json;
using Serilog;
using Uber.API.Objects;

[assembly: InternalsVisibleTo("MeMetrics.Updater.Infrastructure.Tests")]
namespace MeMetrics.Updater.Infrastructure.Uber
{
    public class UberRidersApi : IUberRidersApi
    {
        /*
         * Uber previously had a public developer API that anyone could sign up for an use. They have since closed it off to
         * only corporate developers who are actively making applications that utilize Uber. However, Uber has an online dashboard
         * for riders to use to see their trip history and details about the trips. To log in normally, the site requires you to log in
         * with the phone number or email address of the account and the password. It will then send a 4 digit pin to the phone number associated with
         * the account. Unfortunately, I have not found a consistent way to back up that text message as soon as I get it. However, once logged in, the user
         * is provided with a cookie that can be used for auth. This cookie seems to last for a few months. Attaching this cookie to any requests will allow
         * for retrival of data as though the user were logged in through the normal authentication flow.
         */
        private readonly string baseUrl = "https://riders.uber.com";
        internal string _cookie;
        internal string _userId;
        internal string _csrf;
        private readonly HttpClient _client;
        private readonly ILogger _logger;

        public UberRidersApi(
            IHttpClientFactory httpClientFactory,
            ILogger logger)
        {
            _client = httpClientFactory.CreateClient(Constants.HttpClients.DisabledAutomaticCookieHandling);
            _logger = logger;
        }

        public async Task Authenticate(string cookie, string userId)
        {
            _userId = userId;
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri("https://riders.uber.com/trips?offset=0 "));
            request.Headers.Add("Cookie", cookie);
            request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");
            var response = await _client.SendAsync(request);

            var setCookieHeaders = response.Headers.GetValues("Set-Cookie");
            var splitCookie = cookie.Split(';').ToList();
            foreach (var setCookieHeader in setCookieHeaders.Select(header => header.Split(';')[0]))
            {
                var splitSetCookieHeader = setCookieHeader.Split('=');
                var index = splitCookie.FindIndex(c => c.Contains(splitSetCookieHeader[0]));
                if (index > -1)
                {
                    splitCookie[index] = setCookieHeader;
                }
            }
            var result = await response.Content.ReadAsStringAsync();
            var regex = new Regex("CSRF_TOKEN__\" type=\"application/json\">\\\\u0022(\\d+-(\\w|\\d|-)+)");

            _csrf = regex.Match(result).Groups[1].ToString();
            _cookie = string.Join(";", splitCookie);

        }

        public async Task<TripsResponse> GetTrips(int offset = 0)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, new Uri("https://riders.uber.com/api/getTripsForClient"))
            {
                Content = new StringContent(
                    "{\"limit\":10,\"offset\":\""+ offset +"\"}", Encoding.UTF8,
                    "application/json")
            };
            request.Headers.Add("Cookie", _cookie);
            request.Headers.Add("x-csrf-token", _csrf);

            var response = await _client.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<TripsResponse>(result);
        }

        public async Task<TripsDetailResponse> GetTripDetails(string tripId)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, new Uri("https://riders.uber.com/api/getTrip"))
            {
                Content = new StringContent("{\"tripUUID\":\"" + tripId + "\",\"uuid\":\"" + _userId + "\"}", Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Cookie", _cookie);
            request.Headers.Add("x-csrf-token", _csrf);

            var response = await _client.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<TripsDetailResponse>(result);
        }
    }
}