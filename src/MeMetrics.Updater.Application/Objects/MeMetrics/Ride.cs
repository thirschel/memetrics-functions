using System;
using MeMetrics.Updater.Application.Objects.Enums;

namespace MeMetrics.Updater.Application.Objects.MeMetrics
{
    public class Ride
    {
        public string RideId { get; set; }

        public RideType RideType { get; set; }

        public string OriginLat { get; set; }

        public string OriginLong { get; set; }

        public string DestinationLat { get; set; }

        public string DestinationLong { get; set; }

        public DateTimeOffset RequestDate { get; set; }

        public DateTimeOffset? PickupDate { get; set; }

        public DateTimeOffset? DropoffDate { get; set; }

        public decimal? Price { get; set; }

        public decimal? Distance { get; set; }
  }
}