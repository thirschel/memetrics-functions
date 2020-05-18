using Newtonsoft.Json;

namespace MeMetrics.Updater.Application.Objects.PersonalCapital
{
    public class SpHeader
    {
        [JsonProperty("SP_HEADER_VERSION")]
        public long SpHeaderVersion { get; set; }

        [JsonProperty("userStage")]
        public string UserStage { get; set; }

        [JsonProperty("betaTester")]
        public bool BetaTester { get; set; }

        [JsonProperty("accountsMetaData")]
        public string[] AccountsMetaData { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("qualifiedLead")]
        public bool QualifiedLead { get; set; }

        [JsonProperty("developer")]
        public bool Developer { get; set; }

        [JsonProperty("userGuid")]
        public string UserGuid { get; set; }

        [JsonProperty("authLevel")]
        public string AuthLevel { get; set; }

        [JsonProperty("deviceName")]
        public string DeviceName { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }
    }
}