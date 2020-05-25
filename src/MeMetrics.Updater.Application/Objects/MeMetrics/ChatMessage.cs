using System;

namespace MeMetrics.Updater.Application.Objects.MeMetrics
{
    public class ChatMessage
    {
        public string ChatMessageId { get; set; }

        public string GroupId { get; set; }

        public string GroupName { get; set; }

        public string SenderName { get; set; }

        public string SenderId { get; set; }

        public DateTimeOffset OccurredDate { get; set; }

        public bool IsIncoming { get; set; }

        public string Text { get; set; }

        public bool IsMedia { get; set; }

        public int TextLength => Text.Length;

    }
}