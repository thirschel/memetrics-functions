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
                    var messageIdRegex = new Regex("urn:li:fs_event:\\(\\d+,(\\w+|\\d+|_)");
                    return messageIdRegex.Match(src.EntityUrn).Groups[1].ToString();
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
                    var from = src.Payload.Headers.First(x => x.Name == Constants.EmailHeader.From).Value;
                    var withHeader = !from.Contains(email.ToString()) ? from : to;
                    withHeader = Regex.Replace(withHeader, "[\",]", "");
                    var match = _senderRegex.Match(withHeader);
                    return match.Groups[1].Value;
                }))
                .ForMember(dest => dest.RecruiterId, source => source.MapFrom((src, dest, destMember, context) =>
                {
                    var email = context.Items["Email"];
                    var to = src.Payload.Headers.First(x => x.Name == Constants.EmailHeader.To).Value;
                    var from = src.Payload.Headers.First(x => x.Name == Constants.EmailHeader.From).Value;
                    var withHeader = !from.Contains(email.ToString()) ? from : to;
                    withHeader = Regex.Replace(withHeader, "[\",]", "");
                    var match = _senderRegex.Match(withHeader);
                    return match.Groups[2].Value;
                }))
                .ForMember(dest => dest.IsIncoming, source => source.MapFrom((src, dest, destMember, context) =>
                {
                    var email = context.Items["Email"];
                    var from = src.Payload.Headers.First(x => x.Name == Constants.EmailHeader.From).Value;
                    return !from.Contains(email.ToString());
                }));
        }
    }
}