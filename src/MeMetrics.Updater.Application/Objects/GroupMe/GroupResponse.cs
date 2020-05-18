using System;
using System.Net.Mail;
using Newtonsoft.Json;

namespace MeMetrics.Updater.Application.Objects.GroupMe
{
    public class GroupResponse
    {
        [JsonProperty("response")]
        public Group[] Groups { get; set; }

        [JsonProperty("meta")]
        public Meta Meta { get; set; }
    }

    public class Meta
    {
        [JsonProperty("code")]
        public long Code { get; set; }
    }

    public class Group
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("group_id")]
        public string GroupId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("phone_number")]
        public string PhoneNumber { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("image_url")]
        public Uri ImageUrl { get; set; }

        [JsonProperty("creator_user_id")]
        public long CreatorUserId { get; set; }

        [JsonProperty("created_at")]
        public long CreatedAt { get; set; }

        [JsonProperty("updated_at")]
        public long UpdatedAt { get; set; }

        [JsonProperty("office_mode")]
        public bool OfficeMode { get; set; }

        [JsonProperty("share_url")]
        public object ShareUrl { get; set; }

        [JsonProperty("share_qr_code_url")]
        public object ShareQrCodeUrl { get; set; }

        [JsonProperty("members")]
        public Member[] Members { get; set; }

        [JsonProperty("messages")]
        public Messages Messages { get; set; }

        [JsonProperty("max_members")]
        public long MaxMembers { get; set; }
    }

    public class Member
    {
        [JsonProperty("user_id")]
        public string UserId { get; set; }

        [JsonProperty("nickname")]
        public string Nickname { get; set; }

        [JsonProperty("image_url")]
        public string ImageUrl { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("muted")]
        public bool Muted { get; set; }

        [JsonProperty("autokicked")]
        public bool Autokicked { get; set; }

        [JsonProperty("roles")]
        public Role[] Roles { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class Messages
    {
        [JsonProperty("count")]
        public long Count { get; set; }

        [JsonProperty("last_message_id")]
        public string LastMessageId { get; set; }

        [JsonProperty("last_message_created_at")]
        public long LastMessageCreatedAt { get; set; }

        [JsonProperty("preview")]
        public Preview Preview { get; set; }
    }

    public class Preview
    {
        [JsonProperty("nickname")]
        public string Nickname { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("image_url")]
        public Uri ImageUrl { get; set; }

        [JsonProperty("attachments")]
        public Attachment[] Attachments { get; set; }
    }

    public enum Role { Admin, Owner, User };


}