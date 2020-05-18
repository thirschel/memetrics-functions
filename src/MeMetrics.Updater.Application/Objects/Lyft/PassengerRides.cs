using System;
using Newtonsoft.Json;

namespace MeMetrics.Updater.Application.Objects.Lyft
{
    public class PassengerRidesResponse
    {
        [JsonProperty("data")]
        public PassengerRides[] Data { get; set; }

        [JsonProperty("hasMore")]
        public bool HasMore { get; set; }

        [JsonProperty("limit")]
        public long Limit { get; set; }

        [JsonProperty("skip")]
        public long Skip { get; set; }
    }

    public class PassengerRides
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
        public RideState RideState { get; set; }

        [JsonProperty("rideType")]
        public RideType RideType { get; set; }

        [JsonProperty("rideTypeLabel")]
        public string RideTypeLabel { get; set; }

        [JsonProperty("timeZone")]
        public TimeZone TimeZone { get; set; }

        [JsonProperty("totalMoney")]
        public TotalMoney TotalMoney { get; set; }
    }

    public class TotalMoney
    {
        [JsonProperty("amount")]
        public long Amount { get; set; }

        [JsonProperty("amountCurrency")]
        public AmountCurrency AmountCurrency { get; set; }
    }

    public enum RideState { Processed };

    public enum RideType { Standard };

    public enum TimeZone { Utc0500, Utc0600 };

    public enum AmountCurrency { Usd };

}
