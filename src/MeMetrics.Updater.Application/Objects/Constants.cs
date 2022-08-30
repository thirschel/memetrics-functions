using System.Collections.Generic;

namespace MeMetrics.Updater.Application.Objects
{
    public class Constants
    {
        public static class EmailHeader
        {
            public static string From = "From";
            public static string ReplyTo = "Reply-To";
            public static string To = "To";
            public static string PhoneNumber = "X-smssync-address";
            public static string ThreadId = "X-smssync-thread";
            public static string Date = "Date";
            public static string Subject = "Subject";
            public static string MimeType_Text = "text/plain";
        }

        public static class HttpClients
        {
            public static string DisabledAutomaticCookieHandling = "configured-disable-automatic-cookies";
        }

        public static class MeMetrics
        {
            public static string ApiKeyHeaderName = "X-Api-Key";
        }

        public static class UberStatuses
        {
            public static string Completed = "COMPLETED";
            public static string Canceled = "CANCELED";
            public static string UberEats = "UberEATS";
        }

        public static class LyftStatuses
        {
            public static string Canceled = "cancelled";
        }

        public static List<string> PhoneNumberBlacklist = new List<string>()
        {
            "14157893023",
            "13126675815",
            "800050001020",
            // This is how verizon messages get uploaded to gmail
            "VerizonWireless"

        };
    }
}