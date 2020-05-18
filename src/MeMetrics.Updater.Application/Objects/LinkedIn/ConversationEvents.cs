using System;
using Newtonsoft.Json;

namespace MeMetrics.Updater.Application.Objects.LinkedIn
{
    public class ConversationEvents
    {
        [JsonProperty("elements")]
        public Element[] Elements { get; set; }

        [JsonProperty("paging")]
        public Paging Paging { get; set; }

        public class Element
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
            public From From { get; set; }
        }

        public class EventContent
        {
            [JsonProperty("com.linkedin.voyager.messaging.event.MessageEvent")]
            public MessageEvent MessageEvent { get; set; }
        }

        public class MessageEvent
        {
            [JsonProperty("customContent")]
            public CustomContent CustomContent { get; set; }

            [JsonProperty("subject")]
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
            [JsonProperty("inmailProductType")]
            public string InmailProductType { get; set; }

            [JsonProperty("recruiterInmail")]
            public bool RecruiterInmail { get; set; }

            [JsonProperty("requestContactInfo")]
            public bool RequestContactInfo { get; set; }

            [JsonProperty("clickToReplyInmail")]
            public bool ClickToReplyInmail { get; set; }

            [JsonProperty("inmailType")]
            public string InmailType { get; set; }
        }

        public class From
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

            [JsonProperty("backgroundImage")]
            public BackgroundImage BackgroundImage { get; set; }

            [JsonProperty("publicIdentifier")]
            public string PublicIdentifier { get; set; }

            [JsonProperty("picture")]
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
    }

}