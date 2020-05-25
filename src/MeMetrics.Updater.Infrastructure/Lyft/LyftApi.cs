using System;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MeMetrics.Updater.Application.Interfaces;
using MeMetrics.Updater.Application.Objects;
using MeMetrics.Updater.Application.Objects.Lyft;
using Newtonsoft.Json;
using Serilog;

[assembly: InternalsVisibleTo("MeMetrics.Updater.Infrastructure.Tests")]
namespace MeMetrics.Updater.Infrastructure.Lyft
{
    public class LyftApi : ILyftApi
    {
        /*
         * Lyft previously had a public developer API that anyone could sign up for an use. They have since closed it off to
         * only corporate developers who are actively making applications that utilize Lyft. However, Lyft has an online dashboard
         * for riders get help with previous trips. Trips are not actually displayed with data. However, in one area of the help screen, users are asked
         * which ride their request is in reference to. This exposes an endpoint to the trips that returns minimal data (missing coordinate data).
         * To log in normally, the site requires you to log in with the phone number or email address of the account and the password.
         * It will then send a 6 digit pin to the phone number associated with the account. Unfortunately, I have not found a consistent
         * way to back up that text message as soon as I get it. However, once logged in, the user
         * is provided with a cookie that can be used for auth. This cookie only has one property that matters `lyftAccessToken`. There seems
         * to be an auth endpoint that can refresh that token but I have not discovered the expiration of the token thus far. 
         */
        internal string _cookie;
        private readonly HttpClient _client;
        private readonly ILogger _logger;

        public LyftApi(
            IHttpClientFactory httpClientFactory,
            ILogger logger)
        {
            _client = httpClientFactory.CreateClient(Constants.HttpClients.DisabledAutomaticCookieHandling);
            _logger = logger;
        }

        public async Task Authenticate(string cookie)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri("https://www.lyft.com/api/auth?refresh=true"));
            request.Headers.Add("Cookie", cookie);
            await _client.SendAsync(request);
            _cookie = cookie;
        }

        public async Task<PassengerTrips> GetTrips(int skip = 0)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"https://www.lyft.com/api/passenger_rides?skip={skip}"));
            request.Headers.Add("Cookie", _cookie);

            var response = await _client.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<PassengerTrips>(result);
        }

    }
}