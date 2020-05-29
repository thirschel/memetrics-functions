using System;
using System.Text.RegularExpressions;
using AutoMapper;
using MeMetrics.Updater.Application.Helpers;
using MeMetrics.Updater.Application.Objects.Enums;
using MeMetrics.Updater.Application.Objects.Uber;

namespace MeMetrics.Updater.Application.Profiles
{
    public class RideProfile : Profile
    {
        private const string _placeHolderCoordinate = "0.00000000";
        public RideProfile()
        {
            CreateMap<TripsDetail, Objects.MeMetrics.Ride>()
                .ForMember(dest => dest.RideId, source => source.MapFrom(x => x.Trip.Uuid.ToString()))
                .ForMember(dest => dest.RideType, source => source.MapFrom(x => RideType.Uber))
                .ForMember(dest => dest.Distance, source => source.MapFrom(x => decimal.Parse(x.Receipt.Distance)))
                .ForMember(dest => dest.RequestDate, source => source.MapFrom(x => x.Trip.RequestTime))
                .ForMember(dest => dest.DropoffDate, source => source.MapFrom(x => x.Trip.DropoffTime))
                .ForMember(dest => dest.Price, source => source.MapFrom(x => (decimal) x.Trip.ClientFare))
                .ForMember(dest => dest.OriginLat, source => source.MapFrom((src, dest) => src.TripMap?.Url != null ? Utility.GetCoordinatesFromGoogleMapsUrl(src.TripMap.Url.ToString()).OriginLat : _placeHolderCoordinate))
                .ForMember(dest => dest.OriginLong, source => source.MapFrom((src, dest) => src.TripMap?.Url != null ? Utility.GetCoordinatesFromGoogleMapsUrl(src.TripMap.Url.ToString()).OriginLong : _placeHolderCoordinate))
                .ForMember(dest => dest.DestinationLat, source => source.MapFrom((src, dest) => src.TripMap?.Url != null ? Utility.GetCoordinatesFromGoogleMapsUrl(src.TripMap.Url.ToString()).DestinationLat : _placeHolderCoordinate))
                .ForMember(dest => dest.DestinationLong, source => source.MapFrom((src, dest) => src.TripMap?.Url != null ? Utility.GetCoordinatesFromGoogleMapsUrl(src.TripMap.Url.ToString()).DestinationLong : _placeHolderCoordinate))
                .ForMember(dest => dest.PickupDate, source => source.MapFrom((src, dest, destMember, context) =>
                {
                    var tripDurations = Regex.Match(src.Receipt.Duration, @"(\d+):(\d+):(\d+)");
                    var timespan = new TimeSpan(0, int.Parse(tripDurations.Groups[1].Value), int.Parse(tripDurations.Groups[2].Value), int.Parse(tripDurations.Groups[3].Value));
                    var pickup = src.Trip.DropoffTime.Value.Subtract(timespan);
                    return new DateTimeOffset(pickup.Year, pickup.Month, pickup.Day, pickup.Hour, pickup.Minute, pickup.Second, pickup.Offset);
                }));

            CreateMap<Objects.Lyft.Trip, Objects.MeMetrics.Ride>()
                .ForMember(dest => dest.RideId, source => source.MapFrom(x => x.RideId))
                .ForMember(dest => dest.RideType, source => source.MapFrom(x => RideType.Lyft))
                .ForMember(dest => dest.Distance, source => source.MapFrom(x => x.Distance))
                .ForMember(dest => dest.RequestDate, source => source.MapFrom(x => DateTimeOffset.FromUnixTimeSeconds(x.RequestTimestamp)))
                .ForMember(dest => dest.DropoffDate, source => source.MapFrom(x => DateTimeOffset.FromUnixTimeSeconds(x.DropoffTimestamp)))
                .ForMember(dest => dest.PickupDate, source => source.MapFrom(x => DateTimeOffset.FromUnixTimeSeconds(x.PickupTimestamp)))
                .ForMember(dest => dest.Price, source => source.MapFrom(x => (decimal)x.TotalMoney.Amount / 100));
        }
    }
}