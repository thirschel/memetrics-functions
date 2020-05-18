using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MeMetrics.Updater.Application.Objects.Lyft
{
    public class RideHistoryResponse
    {
        [JsonProperty("limit")]
        public long Limit { get; set; }

        [JsonProperty("ride_history")]
        public List<RideHistory> RideHistory { get; set; }
    }

    public class RideHistory
    {
        [JsonProperty("origin")]
        public Destination Origin { get; set; }

        [JsonProperty("passenger")]
        public Passenger Passenger { get; set; }

        [JsonProperty("line_items")]
        public LineItem[] LineItems { get; set; }

        [JsonProperty("distance_miles")]
        public double DistanceMiles { get; set; }

        [JsonProperty("duration_seconds")]
        public long DurationSeconds { get; set; }

        [JsonProperty("dropoff")]
        public Destination Dropoff { get; set; }

        [JsonProperty("charges")]
        public Charge[] Charges { get; set; }

        [JsonProperty("requested_at")]
        public DateTimeOffset RequestedAt { get; set; }

        [JsonProperty("price")]
        public Price Price { get; set; }

        [JsonProperty("destination")]
        public Destination Destination { get; set; }

        [JsonProperty("driver")]
        public Driver Driver { get; set; }

        [JsonProperty("primetime_percentage", NullValueHandling = NullValueHandling.Ignore)]
        public string PrimetimePercentage { get; set; }

        [JsonProperty("pickup")]
        public Destination Pickup { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("ride_id")]
        public string RideId { get; set; }

        [JsonProperty("vehicle")]
        public Vehicle Vehicle { get; set; }

        [JsonProperty("ride_type")]
        public string RideType { get; set; }

        [JsonProperty("pricing_details_url")]
        public Uri PricingDetailsUrl { get; set; }

        [JsonProperty("ride_profile")]
        public string RideProfile { get; set; }

        [JsonProperty("rating", NullValueHandling = NullValueHandling.Ignore)]
        public long? Rating { get; set; }
    }

    public class Charge
    {
        [JsonProperty("payment_method")]
        public string PaymentMethod { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("amount")]
        public long Amount { get; set; }
    }

    public class Destination
    {
        [JsonProperty("lat")]
        public double Lat { get; set; }

        [JsonProperty("lng")]
        public double Lng { get; set; }

        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("time", NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? Time { get; set; }
    }

    public class Driver
    {
        [JsonProperty("rating")]
        public string Rating { get; set; }

        [JsonProperty("first_name")]
        public string FirstName { get; set; }

        [JsonProperty("image_url")]
        public Uri ImageUrl { get; set; }
    }

    public class LineItem
    {
        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("amount")]
        public long Amount { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }

    public class Passenger
    {
        [JsonProperty("rating")]
        public string Rating { get; set; }

        [JsonProperty("first_name")]
        public string FirstName { get; set; }

        [JsonProperty("last_name")]
        public string LastName { get; set; }

        [JsonProperty("image_url")]
        public Uri ImageUrl { get; set; }

        [JsonProperty("user_id")]
        public string UserId { get; set; }
    }

    public class Price
    {
        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("amount")]
        public long Amount { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }

    public class Vehicle
    {
        [JsonProperty("color")]
        public string Color { get; set; }

        [JsonProperty("make")]
        public string Make { get; set; }

        [JsonProperty("license_plate")]
        public string LicensePlate { get; set; }

        [JsonProperty("image_url")]
        public Uri ImageUrl { get; set; }

        [JsonProperty("year")]
        public long Year { get; set; }

        [JsonProperty("license_plate_state")]
        public string LicensePlateState { get; set; }

        [JsonProperty("model")]
        public string Model { get; set; }
    }
}