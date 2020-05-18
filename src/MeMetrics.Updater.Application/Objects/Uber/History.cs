using System;
using Newtonsoft.Json;

namespace MeMetrics.Updater.Application.Objects.Uber
{
    public class HistoryResponse
    {
        [JsonProperty("count")]
        public long Count { get; set; }

        [JsonProperty("history")]
        public History[] History { get; set; }

        [JsonProperty("limit")]
        public long Limit { get; set; }

        [JsonProperty("offset")]
        public long Offset { get; set; }
    }

    public class History
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("distance")]
        public double Distance { get; set; }

        [JsonProperty("product_id")]
        public Guid ProductId { get; set; }

        [JsonProperty("start_time")]
        public long StartTime { get; set; }

        [JsonProperty("start_city")]
        public StartCity StartCity { get; set; }

        [JsonProperty("end_time")]
        public long EndTime { get; set; }

        [JsonProperty("request_id")]
        public string RequestId { get; set; }

        [JsonProperty("request_time")]
        public long RequestTime { get; set; }
    }

    public class StartCity
    {
        [JsonProperty("latitude")]
        public double Latitude { get; set; }

        [JsonProperty("display_name")]
        public string DisplayName { get; set; }

        [JsonProperty("longitude")]
        public double Longitude { get; set; }
    }
}