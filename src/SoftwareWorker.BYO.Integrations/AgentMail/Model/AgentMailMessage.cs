using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.AgentMail.Model
{
    public class AgentMailMessage
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("thread_id")]
        public string? ThreadId { get; set; }

        [JsonPropertyName("subject")]
        public string? Subject { get; set; }

        [JsonPropertyName("from")]
        public AgentMailParticipant? From { get; set; }

        [JsonPropertyName("to")]
        public List<AgentMailParticipant>? To { get; set; }

        [JsonPropertyName("cc")]
        public List<AgentMailParticipant>? Cc { get; set; }

        [JsonPropertyName("bcc")]
        public List<AgentMailParticipant>? Bcc { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("html")]
        public string? Html { get; set; }

        [JsonPropertyName("received_at")]
        public DateTimeOffset? ReceivedAt { get; set; }

        [JsonPropertyName("sent_at")]
        public DateTimeOffset? SentAt { get; set; }

        [JsonPropertyName("has_attachments")]
        public bool? HasAttachments { get; set; }
    }
}
