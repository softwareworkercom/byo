using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.HashiCorpVault.Model
{
    public class VaultPoliciesResponse
    {
        [JsonPropertyName("policies")]
        public string[]? Policies { get; set; }
    }

    public class VaultPolicyResponse
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("policy")]
        public string? Policy { get; set; }
    }

    public class VaultPolicyRequest
    {
        [JsonPropertyName("policy")]
        public string? Policy { get; set; }
    }
}
