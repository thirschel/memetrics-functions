using System;
using Newtonsoft.Json;

namespace Uber.API.Objects
{
    public class TripsResponse
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("data")]
        public TripsResponseData Data { get; set; }
    }

    public class TripsResponseData
    {
        [JsonProperty("data")]
        public DataData Data { get; set; }

        [JsonProperty("paymentProfiles")]
        public PaymentProfile[] PaymentProfiles { get; set; }

        [JsonProperty("trips")]
        public Trips Trips { get; set; }

        [JsonProperty("cities")]
        public City[] Cities { get; set; }

        [JsonProperty("drivers")]
        public Driver[] Drivers { get; set; }
    }

    public class City
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class DataData
    {
        [JsonProperty("limit")]
        public long Limit { get; set; }

        [JsonProperty("offset")]
        public long Offset { get; set; }

        [JsonProperty("range")]
        public Range Range { get; set; }

        [JsonProperty("uuid")]
        public Guid Uuid { get; set; }
    }

    public class Range
    {
        [JsonProperty("fromTime")]
        public object FromTime { get; set; }

        [JsonProperty("toTime")]
        public object ToTime { get; set; }
    }

    public class Driver
    {
        [JsonProperty("uuid")]
        public Guid Uuid { get; set; }

        [JsonProperty("firstname")]
        public string Firstname { get; set; }

        [JsonProperty("lastname")]
        public string Lastname { get; set; }
    }

    public class PaymentProfile
    {
        [JsonProperty("uuid")]
        public Guid Uuid { get; set; }

        [JsonProperty("randomUuid")]
        public Guid RandomUuid { get; set; }

        [JsonProperty("tokenType")]
        public string TokenType { get; set; }

        [JsonProperty("cardType")]
        public string CardType { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("expiry")]
        public string Expiry { get; set; }
    }

    public class Trips
    {
        [JsonProperty("count")]
        public long Count { get; set; }

        [JsonProperty("pagingResult")]
        public PagingResult PagingResult { get; set; }

        [JsonProperty("trips")]
        public Trip[] TripsTrips { get; set; }
    }

    public class PagingResult
    {
        [JsonProperty("hasMore")]
        public bool HasMore { get; set; }

        [JsonProperty("nextCursor")]
        public long NextCursor { get; set; }
    }

    public class Trip
    {
        [JsonProperty("uuid")]
        public Guid Uuid { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("clientUUID")]
        public Guid ClientUuid { get; set; }

        [JsonProperty("driverUUID")]
        public Guid? DriverUuid { get; set; }

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
        public DateTimeOffset? DropoffTime { get; set; }
    }

}