using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.HashiCorpVault.Model
{
    public class VaultMountsResponse
    {
        [JsonPropertyName("request_id")]
        public string? RequestId { get; set; }

        [JsonPropertyName("lease_id")]
        public string? LeaseId { get; set; }

        [JsonPropertyName("renewable")]
        public bool Renewable { get; set; }

        [JsonPropertyName("lease_duration")]
        public int LeaseDuration { get; set; }

        [JsonPropertyName("data")]
        public Dictionary<string, VaultMount>? Data { get; set; }
    }

    public class VaultMount
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("accessor")]
        public string? Accessor { get; set; }

        [JsonPropertyName("config")]
        public VaultMountConfig? Config { get; set; }

        [JsonPropertyName("options")]
        public Dictionary<string, string>? Options { get; set; }

        [JsonPropertyName("local")]
        public bool Local { get; set; }

        [JsonPropertyName("seal_wrap")]
        public bool SealWrap { get; set; }

        [JsonPropertyName("external_entropy_access")]
        public bool ExternalEntropyAccess { get; set; }

        [JsonPropertyName("plugin_version")]
        public string? PluginVersion { get; set; }
    }

    public class VaultMountConfig
    {
        [JsonPropertyName("default_lease_ttl")]
        public int DefaultLeaseTtl { get; set; }

        [JsonPropertyName("max_lease_ttl")]
        public int MaxLeaseTtl { get; set; }

        [JsonPropertyName("force_no_cache")]
        public bool ForceNoCache { get; set; }
    }

    public class VaultMountRequest
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("config")]
        public VaultMountConfig? Config { get; set; }

        [JsonPropertyName("options")]
        public Dictionary<string, string>? Options { get; set; }
    }
}
