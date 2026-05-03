using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.MicrosoftGraph.Model
{
    public class MicrosoftGraphMailMessageRequest
    {
        [JsonPropertyName("subject")]
        public string? Subject { get; set; }

        [JsonPropertyName("body")]
        public MicrosoftGraphMessageBody? Body { get; set; }

        [JsonPropertyName("toRecipients")]
        public List<MicrosoftGraphRecipient>? ToRecipients { get; set; }

        [JsonPropertyName("ccRecipients")]
        public List<MicrosoftGraphRecipient>? CcRecipients { get; set; }
    }
}
