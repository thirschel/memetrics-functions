namespace MeMetrics.Updater.Application.Objects
{
    public class Constants
    {
        public static class EmailHeader
        {
            public static string From = "From";
            public static string To = "To";
            public static string PhoneNumber = "X-smssync-address";
            public static string ThreadId = "X-smssync-thread";
            public static string Date = "Date";
            public static string Subject = "Subject";
        }

        public static class HttpClients
        {
            public static string DisabledAutomaticCookieHandling = "configured-disable-automatic-cookies";
        }

        public static class MeMetrics
        {
            public static string ApiKeyHeaderName = "X-Api-Key";
        }
    }
}