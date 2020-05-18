using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MeMetrics.Updater.Application.Interfaces;
using MeMetrics.Updater.Application.Objects;
using MeMetrics.Updater.Application.Objects.PersonalCapital;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace MeMetrics.Updater.Infrastructure.PersonalCapital
{
    public class PersonalCapitalApi : IPersonalCapitalApi
    {
        /*
         * Personal Capital auth process is dynamic, but is predictable and consistent. There are two log in flows: 
         * The user enters a username and password or
         * The user enters a username and password and then requires a pin that was sent to the email of the account
         * The pin challenge is triggered if the user trying to log in does not send a cookie value PMData which holds
         * some information about the user trying to login. Unfortunately, the PMData value seems to be encrypted. However,
         * when the PMData cookie gets generated, it has a lifetime of an entire year. Attaching this cookie allows a user
         * to login with just a username and password.
         */
        private readonly string _baseUrl = "https://home.personalcapital.com";
        private string _csrf;
        private string _cookie;
        private readonly HttpClient _client;
        private readonly ILogger _logger;
        public PersonalCapitalApi(
            ILogger logger,
            IHttpClientFactory httpClientFactory)
        {
            _client = httpClientFactory.CreateClient(Constants.HttpClients.DisabledAutomaticCookieHandling);
            _logger = logger;
        }

        public async Task Authenticate(string username, string password, string pmData)
        {
            await GetInitialCsrf();
            var cookieHeader = await IdentifyUserAndGetUserCsrf(username, pmData);
            _cookie = $"PMData={pmData};{cookieHeader}";
            await AuthenticatePassword(password);
        }

        public async Task AuthenticateChallenge(string code, string password)
        {
            await AuthenticateEmail(code);
            await AuthenticatePassword(password);
        }

        public async Task<AccountsOverview> GetAccounts()
        {
            var data = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("lastServerChangeId", "-1"),
                new KeyValuePair<string, string>("apiClient", "WEB"),
                new KeyValuePair<string, string>("csrf",  _csrf),
            };
            var request = new HttpRequestMessage(HttpMethod.Post, _baseUrl + "/api/newaccount/getAccounts")
            {
                Content = new FormUrlEncodedContent(data)
            };
            request.Headers.Add("Cookie", _cookie);

            var response = await _client.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<AccountsOverview>(result);
        }

        public async Task<UserTransactions> GetUserTransactions(string startDate, string endDate, List<string> userAccountIds)
        {
            var data = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("lastServerChangeId", "73"),
                new KeyValuePair<string, string>("startDate", startDate),
                new KeyValuePair<string, string>("endDate", endDate),
                new KeyValuePair<string, string>("apiClient", "WEB"),
                new KeyValuePair<string, string>("csrf",  _csrf),
            };
            var request = new HttpRequestMessage(HttpMethod.Post, _baseUrl + "/api/transaction/getUserTransactions")
            {
                Content = new FormUrlEncodedContent(data)
            };
            request.Headers.Add("Cookie", _cookie);

            var response = await _client.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<UserTransactions>(result);
        }

        private async Task GetInitialCsrf()
        {
            var response = await _client.GetAsync(_baseUrl);
            var result = await response.Content.ReadAsStringAsync();
            var regex = new Regex("globals.csrf='([a-f0-9-]+)'");
            _csrf = regex.Match(result).Groups[1].ToString();
        }

        private async Task<string> IdentifyUserAndGetUserCsrf(string username, string pmData)
        {
            var identifyusercsrf = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("username", username),
                new KeyValuePair<string, string>("csrf", _csrf),
                new KeyValuePair<string, string>("apiClient", "WEB"),
                new KeyValuePair<string, string>("bindDevice", "false"),
                new KeyValuePair<string, string>("skipLinkAccount", "false"),
                new KeyValuePair<string, string>("redirectTo", ""),
                new KeyValuePair<string, string>("skipFirstUse", ""),
                new KeyValuePair<string, string>("referrerId", "")
            };

            var request = new HttpRequestMessage(HttpMethod.Post, _baseUrl + "/api/login/identifyUser")
            {
                Content = new FormUrlEncodedContent(identifyusercsrf)
            };
            request.Headers.Add("Cookie", $"PMData={pmData};");
            var response = await _client.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();
            var jObj = JObject.FromObject(JsonConvert.DeserializeObject(result));
            var setCookieHeaders = response.Headers.GetValues("Set-Cookie");

            _csrf = jObj["spHeader"]["csrf"].ToString();
            return string.Join(";", setCookieHeaders.Select(header => header.Split(';')[0]));

        }

        private async Task GenerateChallengeEmail()
        {
            var data = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("challengeReason", "DEVICE_AUTH"),
                new KeyValuePair<string, string>("challengeMethod", "OP"),
                new KeyValuePair<string, string>("challengeType", "EMAIL"),
                new KeyValuePair<string, string>("apiClient", "WEB"),
                new KeyValuePair<string, string>("bindDevice", "false"),
                new KeyValuePair<string, string>("csrf",  _csrf),
            };
            var request = new HttpRequestMessage(HttpMethod.Post, _baseUrl + "/api/credential/challengeEmail")
            {
                Content = new FormUrlEncodedContent(data)
            };

            var response = await _client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {

            }
        }

        private async Task AuthenticateEmail(string code)
        {
            var data = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("challengeReason", "DEVICE_AUTH"),
                new KeyValuePair<string, string>("challengeMethod", "OP"),
                new KeyValuePair<string, string>("apiClient", "WEB"),
                new KeyValuePair<string, string>("bindDevice", "false"),
                new KeyValuePair<string, string>("code",  code),
                new KeyValuePair<string, string>("csrf",  _csrf),
            };
            var request = new HttpRequestMessage(HttpMethod.Post, _baseUrl + "/api/credential/authenticateEmailByCode")
            {
                Content = new FormUrlEncodedContent(data)
            };

            var response = await _client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {

            }
        }

        private async Task AuthenticatePassword(string password)
        {
            var data = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("bindDevice", "true"),
                new KeyValuePair<string, string>("deviceName", ""),
                new KeyValuePair<string, string>("redirectTo", ""),
                new KeyValuePair<string, string>("skipFirstUse", ""),
                new KeyValuePair<string, string>("skipLinkAccount",  "false"),
                new KeyValuePair<string, string>("referrerId",  ""),
                new KeyValuePair<string, string>("passwd",  password),
                new KeyValuePair<string, string>("apiClient",  "WEB"),
                new KeyValuePair<string, string>("csrf",  _csrf),
            };
            var request = new HttpRequestMessage(HttpMethod.Post, _baseUrl + "/api/credential/authenticatePassword")
            {
                Content = new FormUrlEncodedContent(data)
            };
            request.Headers.Add("Cookie", _cookie);

            var response = await _client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {

            }
        }
    }
}
