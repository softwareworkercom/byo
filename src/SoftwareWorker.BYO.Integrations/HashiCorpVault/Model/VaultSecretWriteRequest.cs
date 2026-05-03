using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.HashiCorpVault.Model
{
    public class VaultSecretWriteRequest
    {
        [JsonPropertyName("data")]
        public Dictionary<string, object>? Data { get; set; }

        [JsonPropertyName("options")]
        public Dictionary<string, object>? Options { get; set; }
    }

    public class VaultSecretWriteResponse
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
        public VaultSecretWriteData? Data { get; set; }
    }

    public class VaultSecretWriteData
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
