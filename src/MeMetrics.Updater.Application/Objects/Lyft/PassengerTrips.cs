using System;
using Newtonsoft.Json;

namespace MeMetrics.Updater.Application.Objects.Lyft
{
    public class PassengerTrips
    {
        [JsonProperty("data")]
        public Trip[] Data { get; set; }

        [JsonProperty("hasMore")]
        public bool HasMore { get; set; }

        [JsonProperty("limit")]
        public long Limit { get; set; }

        [JsonProperty("skip")]
        public long Skip { get; set; }
    }

    public partial class Trip
    {
        [JsonProperty("distance")]
        public long Distance { get; set; }

        [JsonProperty("driverName")]
        public string DriverName { get; set; }

        [JsonProperty("driverPhotoUrl")]
        public Uri DriverPhotoUrl { get; set; }

        [JsonProperty("dropoffTimestamp")]
        public long DropoffTimestamp { get; set; }

        [JsonProperty("isBusinessRide")]
        public bool IsBusinessRide { get; set; }

        [JsonProperty("pickupTimestamp")]
        public long PickupTimestamp { get; set; }

        [JsonProperty("requestTimestamp")]
        public long RequestTimestamp { get; set; }

        [JsonProperty("rideId")]
        public string RideId { get; set; }

        [JsonProperty("rideState")]
        public string RideState { get; set; }

        [JsonProperty("rideType")]
        public Enums.RideType RideType { get; set; }

        [JsonProperty("rideTypeLabel")]
        public string RideTypeLabel { get; set; }

        [JsonProperty("timeZone")]
        public string TimeZone { get; set; }

        [JsonProperty("totalMoney")]
        public TotalMoney TotalMoney { get; set; }
    }

    public class TotalMoney
    {
        [JsonProperty("amount")]
        public long Amount { get; set; }

        [JsonProperty("amountCurrency")]
        public string AmountCurrency { get; set; }
    }
}