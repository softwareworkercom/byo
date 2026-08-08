using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.HashiCorpVault.Model
{
    public class VaultSecretMetadataResponse
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
        public VaultMetadataData? Data { get; set; }
    }

    public class VaultMetadataData
    {
        [JsonPropertyName("cas_required")]
        public bool CasRequired { get; set; }

        [JsonPropertyName("created_time")]
        public string? CreatedTime { get; set; }

        [JsonPropertyName("current_version")]
        public int CurrentVersion { get; set; }

        [JsonPropertyName("delete_version_after")]
        public string? DeleteVersionAfter { get; set; }

        [JsonPropertyName("max_versions")]
        public int MaxVersions { get; set; }

        [JsonPropertyName("oldest_version")]
        public int OldestVersion { get; set; }

        [JsonPropertyName("updated_time")]
        public string? UpdatedTime { get; set; }

        [JsonPropertyName("versions")]
        public Dictionary<string, VaultVersionInfo>? Versions { get; set; }
    }

    public class VaultVersionInfo
    {
        [JsonPropertyName("created_time")]
        public string? CreatedTime { get; set; }

        [JsonPropertyName("deletion_time")]
        public string? DeletionTime { get; set; }

        [JsonPropertyName("destroyed")]
        public bool Destroyed { get; set; }
    }
}
