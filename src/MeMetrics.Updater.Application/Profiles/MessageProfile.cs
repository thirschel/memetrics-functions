using System;
using System.Linq;
using System.Text.RegularExpressions;
using AutoMapper;
using MeMetrics.Updater.Application.Helpers;
using MeMetrics.Updater.Application.Objects;
using MeMetrics.Updater.Application.Objects.Enums;

namespace MeMetrics.Updater.Application.Profiles
{
    public class MessageProfile : Profile
    {
        public MessageProfile()
        {
            CreateMap<Google.Apis.Gmail.v1.Data.Message, Objects.MeMetrics.Message>()
                .ForMember(dest => dest.MessageId, source => source.MapFrom(x => x.Id))
                .ForMember(dest => dest.PhoneNumber, source => source.MapFrom(x => Utility.FormatStringToPhoneNumber(x.Payload.Headers.First(x => x.Name == Constants.EmailHeader.PhoneNumber).Value)))
                .ForMember(dest => dest.Text, source => source.MapFrom(x => EmailHelper.GetBody(x)))
                .ForMember(dest => dest.ThreadId, source => source.MapFrom(x => x.Payload.Headers.First(x => x.Name == Constants.EmailHeader.ThreadId).Value))
                .ForMember(dest => dest.OccurredDate, source => source.MapFrom((src, dest, destMember, context) =>
                {
                    var dateValue = src.Payload.Headers.First(x => x.Name == Constants.EmailHeader.Date).Value;
                    dateValue = Regex.Replace(dateValue, "\\(\\w{3,4}\\)", string.Empty);
                    return DateTimeOffset.Parse(dateValue);
                }))
                .ForMember(dest => dest.Name, source => source.MapFrom((src, dest, destMember, context) =>
                {
                    var email = context.Items["Email"];
                    var to = src.Payload.Headers.First(x => x.Name == Constants.EmailHeader.To).Value;
                    var from = src.Payload.Headers.First(x => x.Name == Constants.EmailHeader.From).Value;
                    var sender = from.ToLower() != email ? from : to;
                    var nameRegex = new Regex(@"(.*) <.*>", RegexOptions.IgnoreCase);
                    return nameRegex.Match(sender).Groups[1].Value.Trim();
                }))
                .ForMember(dest => dest.IsIncoming, source => source.MapFrom((src, dest, destMember, context) =>
                {
                    var email = context.Items["Email"];
                    var from = src.Payload.Headers.First(x => x.Name == Constants.EmailHeader.From).Value;
                    return from.ToLower() != email;
                }))
                .ForMember(dest => dest.IsMedia, source => source.MapFrom((x, dest) =>
                {
                    return x.Payload.Parts != null && x.Payload.Parts.Any(p => p.Body?.AttachmentId != null);
                }));
        }
    }
}