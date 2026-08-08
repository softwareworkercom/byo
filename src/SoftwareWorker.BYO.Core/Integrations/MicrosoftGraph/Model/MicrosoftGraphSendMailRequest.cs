using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.MicrosoftGraph.Model
{
    public class MicrosoftGraphSendMailRequest
    {
        [JsonPropertyName("message")]
        public MicrosoftGraphMailMessageRequest? Message { get; set; }

        [JsonPropertyName("saveToSentItems")]
        public bool? SaveToSentItems { get; set; }
    }
}
