using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.HashiCorpVault.Model
{
    public class VaultTokenResponse
    {
        [JsonPropertyName("request_id")]
        public string? RequestId { get; set; }

        [JsonPropertyName("lease_id")]
        public string? LeaseId { get; set; }

        [JsonPropertyName("renewable")]
        public bool Renewable { get; set; }

        [JsonPropertyName("lease_duration")]
        public int LeaseDuration { get; set; }

        [JsonPropertyName("auth")]
        public VaultAuthInfo? Auth { get; set; }
    }

    public class VaultAuthInfo
    {
        [JsonPropertyName("client_token")]
        public string? ClientToken { get; set; }

        [JsonPropertyName("accessor")]
        public string? Accessor { get; set; }

        [JsonPropertyName("policies")]
        public List<string>? Policies { get; set; }

        [JsonPropertyName("token_policies")]
        public List<string>? TokenPolicies { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, string>? Metadata { get; set; }

        [JsonPropertyName("lease_duration")]
        public int LeaseDuration { get; set; }

        [JsonPropertyName("renewable")]
        public bool Renewable { get; set; }

        [JsonPropertyName("entity_id")]
        public string? EntityId { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }

        [JsonPropertyName("orphan")]
        public bool Orphan { get; set; }
    }

    public class VaultTokenCreateRequest
    {
        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("ttl")]
        public int? Ttl { get; set; }

        [JsonPropertyName("policies")]
        public List<string>? Policies { get; set; }

        [JsonPropertyName("meta")]
        public Dictionary<string, string>? Meta { get; set; }

        [JsonPropertyName("renewable")]
        public bool? Renewable { get; set; }
    }

    public class VaultTokenRenewRequest
    {
        [JsonPropertyName("token")]
        public string? Token { get; set; }

        [JsonPropertyName("increment")]
        public int? Increment { get; set; }
    }

    public class VaultTokenRevokeRequest
    {
        [JsonPropertyName("token")]
        public string? Token { get; set; }
    }

    public class VaultTokenLookupResponse
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
        public VaultTokenData? Data { get; set; }
    }

    public class VaultTokenData
    {
        [JsonPropertyName("accessor")]
        public string? Accessor { get; set; }

        [JsonPropertyName("creation_time")]
        public long CreationTime { get; set; }

        [JsonPropertyName("creation_ttl")]
        public int CreationTtl { get; set; }

        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("entity_id")]
        public string? EntityId { get; set; }

        [JsonPropertyName("expire_time")]
        public string? ExpireTime { get; set; }

        [JsonPropertyName("explicit_max_ttl")]
        public int ExplicitMaxTtl { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("issue_time")]
        public string? IssueTime { get; set; }

        [JsonPropertyName("meta")]
        public Dictionary<string, string>? Meta { get; set; }

        [JsonPropertyName("num_uses")]
        public int NumUses { get; set; }

        [JsonPropertyName("orphan")]
        public bool Orphan { get; set; }

        [JsonPropertyName("path")]
        public string? Path { get; set; }

        [JsonPropertyName("policies")]
        public List<string>? Policies { get; set; }

        [JsonPropertyName("renewable")]
        public bool Renewable { get; set; }

        [JsonPropertyName("ttl")]
        public int Ttl { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }
    }
}
