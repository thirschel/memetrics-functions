using System;
using MeMetrics.Updater.Application.Objects.Enums;

namespace MeMetrics.Updater.Application.Objects.MeMetrics
{
    public class RecruitmentMessage
    {
        public string RecruitmentMessageId { get; set; }

        public RecruitmentMessageSource MessageSource { get; set; }

        public string RecruiterId { get; set; }

        public string RecruiterName { get; set; }

        public string RecruiterCompany { get; set; }

        public string Subject { get; set; }

        public string Body { get; set; }

        public bool IsIncoming { get; set; }

        public DateTimeOffset OccurredDate { get; set; }
    }
}