using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MeMetrics.Updater.Application.Interfaces;
using MeMetrics.Updater.Application.Objects.GroupMe;
using Newtonsoft.Json;
using Serilog;

[assembly: InternalsVisibleTo("MeMetrics.Updater.Infrastructure.Tests")]
namespace MeMetrics.Updater.Infrastructure.GroupMe
{
    public class GroupMeApi : IGroupMeApi
    {
        private readonly string _baseUrl = "https://api.groupme.com/v3";
        internal string _token;
        private readonly HttpClient _client;
        private readonly ILogger _logger;


        public GroupMeApi(
            IHttpClientFactory httpClientFactory,
            ILogger logger)
        {
            _client = httpClientFactory.CreateClient();
            _logger = logger;
        }

        public void Authenticate(string token)
        {
            _token = token;
        }

        public async Task<GroupResponse> GetGroups()
        {
            return await SendAsync<GroupResponse>(HttpMethod.Get, $"/groups?token={_token}");
        }

        public async Task<MessageResponse> GetMessages(string groupId, string lastMessageId = null)
        {
            var url = lastMessageId != null
                ? $"/groups/{groupId}/messages?token={_token}&limit=100&before_id={lastMessageId}"
                : $"/groups/{groupId}/messages?token={_token}&limit=100";

            return await SendAsync<MessageResponse>(HttpMethod.Get, url);
        }

        private async Task<T> SendAsync<T>(HttpMethod httpMethod, string url)
        {
            var request = new HttpRequestMessage(httpMethod, $"{_baseUrl}{url}");

            var response = await _client.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(result);
        }
    }
}
