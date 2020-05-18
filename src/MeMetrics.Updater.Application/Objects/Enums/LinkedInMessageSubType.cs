using System.ComponentModel;

namespace MeMetrics.Updater.Application.Objects.Enums
{
    public enum LinkedInMessageSubType
    {
        [Description("INMAIL")]
        INMAIL,
        [Description("INMAIL_REPLY")]
        INMAIL_REPLY,
        [Description("MEMBER_TO_MEMBER")]
        MEMBER_TO_MEMBER,
    }
}