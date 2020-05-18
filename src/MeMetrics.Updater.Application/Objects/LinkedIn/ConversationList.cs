using System;
using Newtonsoft.Json;

namespace MeMetrics.Updater.Application.Objects.LinkedIn
{
    public class ConversationList
    {
        [JsonProperty("metadata")]
        public Metadata Metadata { get; set; }

        [JsonProperty("elements")]
        public Element[] Elements { get; set; }

        [JsonProperty("paging")]
        public Paging Paging { get; set; }
    }

    public class Element
    {
        [JsonProperty("receipts")]
        public Receipt[] Receipts { get; set; }

        [JsonProperty("archived")]
        public bool Archived { get; set; }

        [JsonProperty("notificationStatus")]
        public string NotificationStatus { get; set; }

        [JsonProperty("read")]
        public bool Read { get; set; }

        [JsonProperty("blocked")]
        public bool Blocked { get; set; }

        [JsonProperty("entityUrn")]
        public string EntityUrn { get; set; }

        [JsonProperty("totalEventCount")]
        public long TotalEventCount { get; set; }

        [JsonProperty("withNonConnection")]
        public bool WithNonConnection { get; set; }

        [JsonProperty("muted")]
        public bool Muted { get; set; }

        [JsonProperty("events")]
        public Event[] Events { get; set; }

        [JsonProperty("participants")]
        public Participant[] Participants { get; set; }

        [JsonProperty("unreadCount", NullValueHandling = NullValueHandling.Ignore)]
        public long? UnreadCount { get; set; }
    }

    public class Event
    {
        [JsonProperty("createdAt")]
        public long CreatedAt { get; set; }

        [JsonProperty("entityUrn")]
        public string EntityUrn { get; set; }

        [JsonProperty("subtype")]
        public string Subtype { get; set; }

        [JsonProperty("eventContent")]
        public EventContent EventContent { get; set; }

        [JsonProperty("from")]
        public Participant From { get; set; }

        [JsonProperty("previousEventInConversation", NullValueHandling = NullValueHandling.Ignore)]
        public string PreviousEventInConversation { get; set; }

        [JsonProperty("quickReplyRecommendations", NullValueHandling = NullValueHandling.Ignore)]
        public QuickReplyRecommendation[] QuickReplyRecommendations { get; set; }

        [JsonProperty("originToken", NullValueHandling = NullValueHandling.Ignore)]
        public string OriginToken { get; set; }
    }

    public class EventContent
    {
        [JsonProperty("com.linkedin.voyager.messaging.event.MessageEvent")]
        public MessageEvent MessageEvent { get; set; }
    }

    public class MessageEvent
    {
        [JsonProperty("customContent", NullValueHandling = NullValueHandling.Ignore)]
        public CustomContent CustomContent { get; set; }

        [JsonProperty("subject", NullValueHandling = NullValueHandling.Ignore)]
        public string Subject { get; set; }

        [JsonProperty("body")]
        public string Body { get; set; }

        [JsonProperty("attributedBody")]
        public AttributedBody AttributedBody { get; set; }
    }

    public class AttributedBody
    {
        [JsonProperty("attributes")]
        public object[] Attributes { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }
    }

    public class CustomContent
    {
        [JsonProperty("com.linkedin.voyager.messaging.event.message.InmailContent")]
        public InmailContent InmailContent { get; set; }
    }

    public class InmailContent
    {
        [JsonProperty("inmailProductType", NullValueHandling = NullValueHandling.Ignore)]
        public string InmailProductType { get; set; }

        [JsonProperty("recruiterInmail", NullValueHandling = NullValueHandling.Ignore)]
        public bool? RecruiterInmail { get; set; }

        [JsonProperty("requestContactInfo", NullValueHandling = NullValueHandling.Ignore)]
        public bool? RequestContactInfo { get; set; }

        [JsonProperty("clickToReplyInmail")]
        public bool ClickToReplyInmail { get; set; }

        [JsonProperty("inmailType")]
        public string InmailType { get; set; }

        [JsonProperty("actionType", NullValueHandling = NullValueHandling.Ignore)]
        public string ActionType { get; set; }
    }

    public class Participant
    {
        [JsonProperty("com.linkedin.voyager.messaging.MessagingMember")]
        public MessagingMember MessagingMember { get; set; }
    }

    public class MessagingMember
    {
        [JsonProperty("entityUrn")]
        public string EntityUrn { get; set; }

        [JsonProperty("miniProfile")]
        public MiniProfile MiniProfile { get; set; }

        [JsonProperty("nameInitials")]
        public string NameInitials { get; set; }
    }

    public class MiniProfile
    {
        [JsonProperty("firstName")]
        public string FirstName { get; set; }

        [JsonProperty("lastName")]
        public string LastName { get; set; }

        [JsonProperty("occupation")]
        public string Occupation { get; set; }

        [JsonProperty("objectUrn")]
        public string ObjectUrn { get; set; }

        [JsonProperty("entityUrn")]
        public string EntityUrn { get; set; }

        [JsonProperty("backgroundImage", NullValueHandling = NullValueHandling.Ignore)]
        public BackgroundImage BackgroundImage { get; set; }

        [JsonProperty("publicIdentifier")]
        public string PublicIdentifier { get; set; }

        [JsonProperty("picture", NullValueHandling = NullValueHandling.Ignore)]
        public BackgroundImage Picture { get; set; }

        [JsonProperty("trackingId")]
        public string TrackingId { get; set; }
    }

    public class BackgroundImage
    {
        [JsonProperty("com.linkedin.common.VectorImage")]
        public VectorImage VectorImage { get; set; }
    }

    public class VectorImage
    {
        [JsonProperty("artifacts")]
        public Artifact[] Artifacts { get; set; }

        [JsonProperty("rootUrl")]
        public Uri RootUrl { get; set; }
    }

    public class Artifact
    {
        [JsonProperty("width")]
        public long Width { get; set; }

        [JsonProperty("fileIdentifyingUrlPathSegment")]
        public string FileIdentifyingUrlPathSegment { get; set; }

        [JsonProperty("height")]
        public long Height { get; set; }
    }

    public class QuickReplyRecommendation
    {
        [JsonProperty("replyType")]
        public string ReplyType { get; set; }

        [JsonProperty("objectUrn")]
        public string ObjectUrn { get; set; }

        [JsonProperty("content")]
        public Content Content { get; set; }
    }

    public class Content
    {
        [JsonProperty("text")]
        public string Text { get; set; }
    }

    public class Receipt
    {
        [JsonProperty("fromParticipant")]
        public FromParticipant FromParticipant { get; set; }

        [JsonProperty("fromEntity")]
        public string FromEntity { get; set; }

        [JsonProperty("seenReceipt")]
        public SeenReceipt SeenReceipt { get; set; }
    }

    public class FromParticipant
    {
        [JsonProperty("string")]
        public string String { get; set; }
    }

    public class SeenReceipt
    {
        [JsonProperty("seenAt")]
        public long SeenAt { get; set; }

        [JsonProperty("eventUrn")]
        public string EventUrn { get; set; }
    }

    public class Metadata
    {
        [JsonProperty("unreadCount")]
        public long UnreadCount { get; set; }
    }

    public class Paging
    {
        [JsonProperty("total")]
        public long Total { get; set; }

        [JsonProperty("count")]
        public long Count { get; set; }

        [JsonProperty("start")]
        public long Start { get; set; }

        [JsonProperty("links")]
        public object[] Links { get; set; }
    }
}