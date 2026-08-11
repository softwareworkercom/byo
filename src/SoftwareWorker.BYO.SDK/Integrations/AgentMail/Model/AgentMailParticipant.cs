using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.AgentMail.Model
{
    public class AgentMailParticipant
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }
    }
}
