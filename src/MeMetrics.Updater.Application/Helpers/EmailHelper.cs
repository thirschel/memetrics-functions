using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MeMetrics.Updater.Application.Objects;

namespace MeMetrics.Updater.Application.Helpers
{
    public static class EmailHelper
    {
        public static string GetBody(Google.Apis.Gmail.v1.Data.Message email)
        {
            var body = string.Empty;
            if (email.Payload.Parts != null)
            {
                body = GetBodyFromMessagePart(email.Payload.Parts.ToList());
            }
            else
            {
                body = email.Payload.Body.Data ?? string.Empty;
            }

            return string.IsNullOrEmpty(body) ? string.Empty : Utility.Decode(body);
        }

        public static string GetBodyFromMessagePart(List<Google.Apis.Gmail.v1.Data.MessagePart> parts)
        {
            for (var i = 0; i < parts.Count; i++)
            {
                if (parts[i].MimeType.Contains(Constants.EmailHeader.MimeType_Text))
                {
                    return parts[i].Body.Data;
                }
                if (parts[i].Parts != null)
                {
                    return GetBodyFromMessagePart(parts[i].Parts.ToList());
                }
            }

            return string.Empty;
        }
    }
}