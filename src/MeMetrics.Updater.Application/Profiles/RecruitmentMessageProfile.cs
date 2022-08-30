using System;
using System.Linq;
using System.Text.RegularExpressions;
using AutoMapper;
using MeMetrics.Updater.Application.Helpers;
using MeMetrics.Updater.Application.Objects;
using MeMetrics.Updater.Application.Objects.Enums;

namespace MeMetrics.Updater.Application.Profiles
{
    public class RecruitmentMessageProfile : Profile
    {
        private readonly Regex _senderRegex = new Regex(@"(.*) <(.*)>", RegexOptions.IgnoreCase);
        public RecruitmentMessageProfile()
        {
            CreateMap<Objects.LinkedIn.ConversationEvents.Element, Objects.MeMetrics.RecruitmentMessage>()
                .ForMember(dest => dest.MessageSource, source => source.MapFrom(x => RecruitmentMessageSource.LinkedIn))
                .ForMember(dest => dest.Subject, source => source.MapFrom(x => x.EventContent.MessageEvent.Subject))
                .ForMember(dest => dest.OccurredDate, source => source.MapFrom(x => DateTimeOffset.FromUnixTimeMilliseconds(x.CreatedAt)))
                .ForMember(dest => dest.Body, source => source.MapFrom((src, dest, destMember, context) =>
                {
                    return string.IsNullOrEmpty(src.EventContent.MessageEvent.Body)
                        ? src.EventContent.MessageEvent.AttributedBody.Text
                        : src.EventContent.MessageEvent.Body;
                }))
                .ForMember(dest => dest.RecruitmentMessageId, source => source.MapFrom((src, dest, destMember, context) =>
                {
                    /*
                     * Linkedin changed their event ids to be base64 encoded. The eventId now comes as 2 strings that comma-separated.
                     * These strings seem to have a number (2 seems to be constant, which is maybe the version number?) and a dash (-) before the base64 encoded string
                     * Once decoded the first string contains a guid with a number appended by an underscore (_) at the end. This is the threadId
                     * The second string contains the createdAt epoch time followed by the letter c followed by a 9 digit number that is hyphenated (-) after 5 digits.
                     * All of which is suffixed by an ampersand and the threadId again. The createdAt time plus the 9 digit number is the messageId.
                     */
                    var legacyLinkedInEventRegex = new Regex("urn:li:fs_event:\\(\\d+,(\\w+|\\d+|_)");
                    var linkedInEventRegex = new Regex("urn:li:fs_event:\\(\\d+-.*,\\d+-(.*)\\)");
                    var eventIdMatches = linkedInEventRegex.Match(src.EntityUrn);
                    if (legacyLinkedInEventRegex.IsMatch(src.EntityUrn))
                    {
                        return legacyLinkedInEventRegex.Match(src.EntityUrn).Groups[1].ToString();
                    }
                    var messageIdRegex = new Regex("(\\d+\\w.*)&");
                    var decodedEventId = Utility.Decode(eventIdMatches.Groups[1].Value);
                    return messageIdRegex.Match(decodedEventId).Groups[1].ToString();
                }));

            CreateMap<Objects.LinkedIn.MiniProfile, Objects.MeMetrics.RecruitmentMessage>()
                .ForMember(dest => dest.RecruiterId, source => source.MapFrom((src, dest, destMember, context) =>
                {
                    var recruiterIdRegex = new Regex("urn:li:member:(\\d+)");
                    return recruiterIdRegex.Match(src.ObjectUrn).Groups[1].ToString();
                }))
                .ForMember(dest => dest.RecruiterName, source => source.MapFrom(x => $"{x.FirstName} {x.LastName}"))
                .ForMember(dest => dest.RecruiterCompany, source => source.MapFrom(x => x.Occupation));

            CreateMap<Google.Apis.Gmail.v1.Data.Message, Objects.MeMetrics.RecruitmentMessage>()
                .ForMember(dest => dest.RecruitmentMessageId, source => source.MapFrom(x => x.Id))
                .ForMember(dest => dest.RecruiterCompany, source => source.MapFrom(x => string.Empty))
                .ForMember(dest => dest.MessageSource, source => source.MapFrom(x => RecruitmentMessageSource.DirectEmail))
                .ForMember(dest => dest.Subject, source => source.MapFrom(x => x.Payload.Headers.First(x => x.Name == Constants.EmailHeader.Subject).Value))
                .ForMember(dest => dest.OccurredDate, (source) => source.MapFrom((src, dest, destMember, context) =>
                {
                    var dateValue = src.Payload.Headers.First(x => x.Name == Constants.EmailHeader.Date).Value;
                    dateValue = Regex.Replace(dateValue, "\\(\\w{3,4}\\)", string.Empty);
                    return DateTimeOffset.Parse(dateValue);
                }))
                .ForMember(dest => dest.Body, source => source.MapFrom(x => EmailHelper.GetBody(x)))
                .ForMember(dest => dest.RecruiterName, source => source.MapFrom((src, dest, destMember, context) =>
                {
                    var email = context.Items["Email"];
                    var to = src.Payload.Headers.First(x => x.Name == Constants.EmailHeader.To).Value;
                    var fromHeaderToCheck = Constants.EmailHeader.From;
                    if (!src.Payload.Headers.Any(x => string.Equals(x.Name, fromHeaderToCheck, StringComparison.OrdinalIgnoreCase)))
                    {
                        fromHeaderToCheck = Constants.EmailHeader.ReplyTo;
                    }
                    var from = src.Payload.Headers.First(x => string.Equals(x.Name, fromHeaderToCheck, StringComparison.OrdinalIgnoreCase)).Value;
                    var withHeader = !from.Contains(email.ToString()) ? from : to;
                    withHeader = Regex.Replace(withHeader, "[\",]", "");
                    var match = _senderRegex.Match(withHeader);
                    return match.Groups[1].Value;
                }))
                .ForMember(dest => dest.RecruiterId, source => source.MapFrom((src, dest, destMember, context) =>
                {
                    var email = context.Items["Email"];
                    var to = src.Payload.Headers.First(x => x.Name == Constants.EmailHeader.To).Value;
                    var fromHeaderToCheck = Constants.EmailHeader.From;
                    if (!src.Payload.Headers.Any(x => string.Equals(x.Name, fromHeaderToCheck, StringComparison.OrdinalIgnoreCase)))
                    {
                        fromHeaderToCheck = Constants.EmailHeader.ReplyTo;
                    }
                    var from = src.Payload.Headers.First(x => string.Equals(x.Name, fromHeaderToCheck, StringComparison.OrdinalIgnoreCase)).Value;
                    var withHeader = !from.Contains(email.ToString()) ? from : to;
                    withHeader = Regex.Replace(withHeader, "[\",]", "");
                    var match = _senderRegex.Match(withHeader);
                    return match.Groups[2].Value;
                }))
                .ForMember(dest => dest.IsIncoming, source => source.MapFrom((src, dest, destMember, context) =>
                {
                    var email = (string)context.Items["Email"];
                    var fromHeaderToCheck = Constants.EmailHeader.From;
                    if (!src.Payload.Headers.Any(x => string.Equals(x.Name, fromHeaderToCheck, StringComparison.OrdinalIgnoreCase)))
                    {
                        fromHeaderToCheck = Constants.EmailHeader.ReplyTo;
                    }
                    var from = src.Payload.Headers.First(x => string.Equals(x.Name, fromHeaderToCheck, StringComparison.OrdinalIgnoreCase)).Value;
                    return !from.Equals(email, StringComparison.OrdinalIgnoreCase);
                }));
        }
    }
}