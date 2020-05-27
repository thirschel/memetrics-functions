using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text.RegularExpressions;
using System.Web;
using MeMetrics.Updater.Application.Objects;
using MeMetrics.Updater.Application.Objects.LinkedIn;

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
            base64EncodedData = base64EncodedData.Replace('-', '+');
            base64EncodedData = base64EncodedData.Replace('_', '/');
            var bytes = Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }

        public static string FormatStringToPhoneNumber(string str)
        {
            var phoneNumber = Regex.Replace(str, "[()+-]", "");
            phoneNumber = Regex.Replace(phoneNumber, " ", "");
            return phoneNumber.Length == 10 ? $"1{phoneNumber}" : phoneNumber;
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
                var _Attribs = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
                if ((_Attribs != null && _Attribs.Count() > 0))
                {
                    return ((DescriptionAttribute)_Attribs.ElementAt(0)).Description;
                }
            }
            return GenericEnum.ToString();
        }

        public static RideCoordinates GetCoordinatesFromGoogleMapsUrl(string str)
        {
            var mapString = HttpUtility.UrlDecode(str);
            var coordinateRegex = new Regex("(-?\\d{1,3}\\.\\d+,-?\\d{1,3}\\.\\d+)");
            var coordinateGroups = coordinateRegex.Matches(mapString);
            var origin = coordinateGroups[0].ToString().Split(',');
            var destination = coordinateGroups[1].ToString().Split(',');

            return new RideCoordinates()
            {
                OriginLat = origin[0].Substring(0, Math.Min(origin[0].Length, 10)),
                OriginLong = origin[1].Substring(0, Math.Min(origin[1].Length, 10)),
                DestinationLat = destination[0].Substring(0, Math.Min(destination[0].Length, 10)),
                DestinationLong = destination[1].Substring(0, Math.Min(destination[1].Length, 10)),
            };
        }
    }
}