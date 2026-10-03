using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.Auth0.Model
{
    public class Auth0Connection
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("strategy")]
        public string? Strategy { get; set; }

        [JsonPropertyName("enabled_clients")]
        public List<string>? EnabledClients { get; set; }

        [JsonPropertyName("realms")]
        public List<string>? Realms { get; set; }

        [JsonPropertyName("options")]
        public Dictionary<string, object>? Options { get; set; }

        [JsonPropertyName("is_domain_connection")]
        public bool IsDomainConnection { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, string>? Metadata { get; set; }
    }
}
