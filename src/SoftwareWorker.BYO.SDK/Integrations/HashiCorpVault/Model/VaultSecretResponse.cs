using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.HashiCorpVault.Model
{
    public class VaultSecretResponse
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
        public VaultSecretData? Data { get; set; }
    }

    public class VaultSecretData
    {
        [JsonPropertyName("data")]
        public Dictionary<string, object>? Data { get; set; }

        [JsonPropertyName("metadata")]
        public VaultSecretMetadata? Metadata { get; set; }
    }

    public class VaultSecretMetadata
    {
        [JsonPropertyName("created_time")]
        public string? CreatedTime { get; set; }

        [JsonPropertyName("custom_metadata")]
        public Dictionary<string, string>? CustomMetadata { get; set; }

        [JsonPropertyName("deletion_time")]
        public string? DeletionTime { get; set; }

        [JsonPropertyName("destroyed")]
        public bool Destroyed { get; set; }

        [JsonPropertyName("version")]
        public int Version { get; set; }
    }
}
