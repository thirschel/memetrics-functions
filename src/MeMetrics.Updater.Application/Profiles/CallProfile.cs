using System;
using System.Linq;
using System.Text.RegularExpressions;
using AutoMapper;
using MeMetrics.Updater.Application.Helpers;
using MeMetrics.Updater.Application.Objects;

namespace MeMetrics.Updater.Application.Profiles
{
    public class CallProfile : Profile
    {
        private readonly Regex _callSnippetRegex = new Regex(@"(\d+)s \(\d+:\d+:\d+\) (\d+) \((incoming|outgoing) call\)", RegexOptions.IgnoreCase);
        public CallProfile()
        {
            CreateMap<Google.Apis.Gmail.v1.Data.Message, Objects.MeMetrics.Call>()
                .ForMember(dest => dest.CallId, source => source.MapFrom(x => x.Id))
                .ForMember(dest => dest.OccurredDate, source => source.MapFrom(x => DateTimeOffset.Parse(x.Payload.Headers.First(x => x.Name == Constants.EmailHeader.Date).Value)))
                .ForMember(dest => dest.PhoneNumber, source => source.MapFrom((src, dest, destMember, context) =>
                {
                    var emailMatch = _callSnippetRegex.Match(src.Snippet);
                    return Utility.FormatStringToPhoneNumber(emailMatch.Groups[2].Value);
                }))
                .ForMember(dest => dest.Duration, source => source.MapFrom((src, dest, destMember, context) =>
                {
                    var emailMatch = _callSnippetRegex.Match(src.Snippet);
                    return int.Parse(emailMatch.Groups[1].Value);
                }))
                .ForMember(dest => dest.IsIncoming, source => source.MapFrom((src, dest, destMember, context) =>
                {
                    var emailMatch = _callSnippetRegex.Match(src.Snippet);
                    return emailMatch.Groups[3].Value.ToLower() == "incoming";
                }));
        }
    }
}