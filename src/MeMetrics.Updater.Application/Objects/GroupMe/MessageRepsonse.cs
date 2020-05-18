using System;
using Newtonsoft.Json;

namespace MeMetrics.Updater.Application.Objects.GroupMe
{
    public class MessageResponse
    {
        [JsonProperty("response")]
        public Response Response { get; set; }

        [JsonProperty("meta")]
        public Meta Meta { get; set; }
    }

    public class Response
    {
        [JsonProperty("count")]
        public long Count { get; set; }

        [JsonProperty("messages")]
        public Message[] Messages { get; set; }
    }

    public class Message
    {
        [JsonProperty("attachments")]
        public object[] Attachments { get; set; }

        [JsonProperty("avatar_url")]
        public Uri AvatarUrl { get; set; }

        [JsonProperty("created_at")]
        public long CreatedAt { get; set; }

        [JsonProperty("favorited_by")]
        public long[] FavoritedBy { get; set; }

        [JsonProperty("group_id")]
        public long GroupId { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("sender_id")]
        public string SenderId { get; set; }

        [JsonProperty("sender_type")]
        public string SenderType { get; set; }

        [JsonProperty("source_guid")]
        public string SourceGuid { get; set; }

        [JsonProperty("system")]
        public bool System { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("user_id")]
        public string UserId { get; set; }

        [JsonProperty("event", NullValueHandling = NullValueHandling.Ignore)]
        public Event Event { get; set; }
    }

    public class Event
    {
        [JsonProperty("type")]
        public string Type { get; set; }

    }

}