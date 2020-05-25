using System;
using System.Linq;
using AutoMapper;

namespace MeMetrics.Updater.Application.Profiles
{
    public class ChatMessageProfile : Profile
    {
        public ChatMessageProfile()
        {
            CreateMap<Objects.GroupMe.Message, Objects.MeMetrics.ChatMessage>()
                .ForMember(dest => dest.ChatMessageId, source => source.MapFrom(x => x.Id))
                .ForMember(dest => dest.IsIncoming, source => source.MapFrom(x => !string.IsNullOrEmpty(x.UserId)))
                .ForMember(dest => dest.IsMedia, source => source.MapFrom(x => x.Attachments.Any()))
                .ForMember(dest => dest.OccurredDate, source => source.MapFrom(x => DateTimeOffset.FromUnixTimeMilliseconds(x.CreatedAt)))
                .ForMember(dest => dest.SenderId, source => source.MapFrom(x => x.SenderId))
                .ForMember(dest => dest.SenderName, source => source.MapFrom(x => x.Name))
                .ForMember(dest => dest.Text, source => source.MapFrom(x => x.Text));
        }
    }
}