using System;
using System.Text.RegularExpressions;

namespace MeMetrics.Updater.Application.Helpers
{
    public static class Utility
    {
        public static string Decode(string base64EncodedData)
        {
            if (string.IsNullOrEmpty(base64EncodedData))
            {
                return string.Empty;
            }

            var bytes = Base64Url.Decode(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }

        public static bool IsDateToday(DateTime input)
        {
            return input.ToString("YYYY-MM-DD") == DateTime.Now.ToString("YYYY-MM-DD");
        }

        public static string FormatStringToPhoneNumber(string str)
        {
            var phoneNumber = Regex.Replace(str, "[()+-]", "");
            phoneNumber = Regex.Replace(phoneNumber, " ", "");
            return phoneNumber.Length == 10 ? $"1{phoneNumber}" : phoneNumber;
        }

        public static class Base64Url
        {
            public static string Encode(byte[] arg)
            {
                if (arg == null)
                {
                    throw new ArgumentNullException("arg");
                }

                var s = Convert.ToBase64String(arg);
                return s
                    .Replace("=", "")
                    .Replace("/", "_")
                    .Replace("+", "-");
            }

            public static string ToBase64(string arg)
            {
                if (arg == null)
                {
                    throw new ArgumentNullException("arg");
                }

                var s = arg
                    .PadRight(arg.Length + (4 - arg.Length % 4) % 4, '=')
                    .Replace("_", "/")
                    .Replace("-", "+");

                return s;
            }

            public static byte[] Decode(string arg)
            {
                var decrypted = ToBase64(arg);

                return Convert.FromBase64String(decrypted);
            }
        }

        public static string AddPadding(int number)
        {
            return number.ToString().Length == 1 ? $"0{number}" : number.ToString();
        }
    }
}