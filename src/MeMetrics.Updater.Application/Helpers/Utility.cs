using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
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

        public static string GetDescription(this Enum GenericEnum)
        {
            Type genericEnumType = GenericEnum.GetType();
            MemberInfo[] memberInfo = genericEnumType.GetMember(GenericEnum.ToString());
            if ((memberInfo != null && memberInfo.Length > 0))
            {
                var _Attribs = memberInfo[0].GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false);
                if ((_Attribs != null && _Attribs.Count() > 0))
                {
                    return ((System.ComponentModel.DescriptionAttribute)_Attribs.ElementAt(0)).Description;
                }
            }
            return GenericEnum.ToString();
        }
    }
}