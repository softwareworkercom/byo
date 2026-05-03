using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.HashiCorpVault.Model
{
    public class VaultHealthResponse
    {
        [JsonPropertyName("initialized")]
        public bool Initialized { get; set; }

        [JsonPropertyName("sealed")]
        public bool Sealed { get; set; }

        [JsonPropertyName("standby")]
        public bool Standby { get; set; }

        [JsonPropertyName("performance_standby")]
        public bool PerformanceStandby { get; set; }

        [JsonPropertyName("replication_performance_mode")]
        public string? ReplicationPerformanceMode { get; set; }

        [JsonPropertyName("replication_dr_mode")]
        public string? ReplicationDrMode { get; set; }

        [JsonPropertyName("server_time_utc")]
        public long ServerTimeUtc { get; set; }

        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("cluster_name")]
        public string? ClusterName { get; set; }

        [JsonPropertyName("cluster_id")]
        public string? ClusterId { get; set; }
    }
}
