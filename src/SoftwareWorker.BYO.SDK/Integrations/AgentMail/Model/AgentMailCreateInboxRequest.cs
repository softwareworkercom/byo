using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.AgentMail.Model
{
    public class AgentMailCreateInboxRequest
    {
        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("domain")]
        public string? Domain { get; set; }

        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }
    }
}
