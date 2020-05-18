using System;
using Newtonsoft.Json;
using Uber.API.Objects;

namespace MeMetrics.Updater.Application.Objects.Uber
{
    public  class TripsDetailResponse
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("data")]
        public TripsDetail Data { get; set; }
    }

    public class TripsDetail
    {
        [JsonProperty("data")]
        public TripData Data { get; set; }

        [JsonProperty("trip")]
        public Trip Trip { get; set; }

        [JsonProperty("driver")]
        public object Driver { get; set; }

        [JsonProperty("tripMap")]
        public TripMap TripMap { get; set; }

        [JsonProperty("receipt")]
        public Receipt Receipt { get; set; }
    }

    public class TripData
    {
        [JsonProperty("tripUUID")]
        public Guid TripUuid { get; set; }

        [JsonProperty("uuid")]
        public Guid Uuid { get; set; }
    }

    public class Receipt
    {
        [JsonProperty("car_label")]
        public string CarLabel { get; set; }

        [JsonProperty("distance")]
        public string Distance { get; set; }

        [JsonProperty("distance_label_short")]
        public string DistanceLabelShort { get; set; }

        [JsonProperty("trip_time_label")]
        public string TripTimeLabel { get; set; }

        [JsonProperty("distance_label")]
        public string DistanceLabel { get; set; }

        [JsonProperty("car_make")]
        public string CarMake { get; set; }

        [JsonProperty("duration")]
        public string Duration { get; set; }

        [JsonProperty("vehicle_type")]
        public string VehicleType { get; set; }

        [JsonProperty("car_make_label")]
        public string CarMakeLabel { get; set; }
    }

    public class TripDetail
    {
        [JsonProperty("uuid")]
        public Guid Uuid { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("clientUUID")]
        public Guid ClientUuid { get; set; }

        [JsonProperty("driverUUID")]
        public Guid DriverUuid { get; set; }

        [JsonProperty("paymentProfileUUID")]
        public Guid PaymentProfileUuid { get; set; }

        [JsonProperty("cityID")]
        public long CityId { get; set; }

        [JsonProperty("countryID")]
        public long? CountryId { get; set; }

        [JsonProperty("vehicleViewName")]
        public string VehicleViewName { get; set; }

        [JsonProperty("vehicleViewID")]
        public long VehicleViewId { get; set; }

        [JsonProperty("clientFare")]
        public double ClientFare { get; set; }

        [JsonProperty("currencyCode")]
        public string CurrencyCode { get; set; }

        [JsonProperty("isSurgeTrip")]
        public bool IsSurgeTrip { get; set; }

        [JsonProperty("begintripFormattedAddress")]
        public string BegintripFormattedAddress { get; set; }

        [JsonProperty("dropoffFormattedAddress")]
        public string DropoffFormattedAddress { get; set; }

        [JsonProperty("requestTime")]
        public DateTimeOffset RequestTime { get; set; }

        [JsonProperty("dropoffTime")]
        public DateTimeOffset DropoffTime { get; set; }
    }

    public class TripMap
    {
        [JsonProperty("url")]
        public Uri Url { get; set; }

        [JsonProperty("mapType")]
        public string MapType { get; set; }

        [JsonProperty("mapTypeCompatible")]
        public bool MapTypeCompatible { get; set; }
    }
}