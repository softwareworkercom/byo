using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.MicrosoftGraph.Model
{
    public class MicrosoftGraphMailMessage
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("subject")]
        public string? Subject { get; set; }

        [JsonPropertyName("body")]
        public MicrosoftGraphMessageBody? Body { get; set; }

        [JsonPropertyName("from")]
        public MicrosoftGraphRecipient? From { get; set; }

        [JsonPropertyName("toRecipients")]
        public List<MicrosoftGraphRecipient>? ToRecipients { get; set; }

        [JsonPropertyName("ccRecipients")]
        public List<MicrosoftGraphRecipient>? CcRecipients { get; set; }

        [JsonPropertyName("receivedDateTime")]
        public DateTime? ReceivedDateTime { get; set; }

        [JsonPropertyName("sentDateTime")]
        public DateTime? SentDateTime { get; set; }

        [JsonPropertyName("hasAttachments")]
        public bool? HasAttachments { get; set; }

        [JsonPropertyName("isRead")]
        public bool? IsRead { get; set; }
    }
}
