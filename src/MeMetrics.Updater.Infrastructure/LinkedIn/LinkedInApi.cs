using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MeMetrics.Updater.Application.Interfaces;
using MeMetrics.Updater.Application.Objects.LinkedIn;
using Newtonsoft.Json;
using Serilog;

namespace MeMetrics.Updater.Infrastructure.LinkedIn
{
    public class LinkedInApi : ILinkedInApi
    {
        /*
         * LinkedIn's auth process is dynamic. There are two login flows: 
         * The user enters a username and password or
         * The user enters a username and password and then requires a pin that was sent to the email of the account
         * What triggers the pin challenge is not clear but it seems to generally happen when you try to log in
         * from a different geographical location. 
         */

        private readonly string baseUrl = "https://www.linkedin.com";
        internal string _csrf;
        internal List<string> _cookie;
        internal Dictionary<string, string> _challengeData;
        private readonly HttpClient _client;
        private readonly ILogger _logger;

        public LinkedInApi(
            ILogger logger,
            IHttpClientFactory httpClientFactory)
        {
            _client = httpClientFactory.CreateClient();
            _logger = logger;
        }

        public async Task<bool> Authenticate(string username, string password)
        {
            var requiresAdditionalAuthentication = await LoginAndGetUserCsrf(username, password);
            return requiresAdditionalAuthentication;
        }

       public async Task SubmitPin(string pin)
       {
            var formData = new Dictionary<string, string>();
            var url = string.Empty;

            if (_challengeData != null)
            {
                _challengeData.Add("pin", pin);
                formData = _challengeData;
                _challengeData = null;
                url = $"{baseUrl}/checkpoint/challenge/verify";
            }

            var uri = new Uri(url);
            var content = new FormUrlEncodedContent(formData);

            await _client.PostAsync(uri, content);
        }

        public async Task<ConversationList> GetConversations(long createdBeforeTime)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/voyager/api/messaging/conversations?keyVersion=LEGACY_INBOX&createdBefore={createdBeforeTime}");
            request.Headers.Add("Csrf-Token", _csrf);
            var response = await _client.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<ConversationList>(result);
        }

        public async Task<ConversationEvents> GetConversationEvents(string conversationId)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/voyager/api/messaging/conversations/{conversationId}/events");
            request.Headers.Add("Csrf-Token", _csrf);
            var response = await _client.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<ConversationEvents>(result);
        }

        private async Task<bool> LoginAndGetUserCsrf(string username, string password)
        {
            var loginPageResponse = await _client.GetAsync($"{baseUrl}/login");
            var loginHtml = await loginPageResponse.Content.ReadAsStringAsync();

            var loginData = GetHiddenInputFields(loginHtml);
            _csrf = loginData["csrfToken"];

            loginData.Add("session_key", username);
            loginData.Add("session_password", password);

            var content = new FormUrlEncodedContent(loginData);
            
            var loginSubmissionResponse = await _client.PostAsync($"{baseUrl}/checkpoint/lg/login-submit", content);
            var loginSubmissionHtml = await loginSubmissionResponse.Content.ReadAsStringAsync();

            if (loginSubmissionResponse.RequestMessage.RequestUri.AbsoluteUri.Contains("checkpoint/challenge"))
            {
                _logger.Information("Linkedin Challenge found");
                _logger.Information(loginSubmissionHtml);
                var formData = GetHiddenInputFields(loginSubmissionHtml);
                _challengeData = formData;
                return true;
            }
            return false;
        }

        private Dictionary<string, string> GetHiddenInputFields(string html)
        {
            var formData = new Dictionary<string, string>();
            var htmlResponse = Regex.Replace(html, "\\s+", " ");
            var hiddenInputs = new Regex("(<input type=\"hidden\" .*\\/>)").Match(htmlResponse);
            foreach (Group group in hiddenInputs.Groups)
            {
                var splits = group.Value.Split(">");
                foreach (var split in splits)
                {
                    var name = new Regex("name=\"(\\S*)\"").Match(split);
                    var value = new Regex("value=\"(\\S*)\"\\s?\\/").Match(split);
                    if (name.Success && value.Success && !formData.ContainsKey(name.Groups[1].Value))
                    {
                        formData.Add(name.Groups[1].Value, value.Groups[1].Value);
                    }
                }
            }
            return formData;
        }
    }
}
