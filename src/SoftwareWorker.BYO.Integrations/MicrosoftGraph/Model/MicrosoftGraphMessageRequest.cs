using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.MicrosoftGraph.Model
{
    public class MicrosoftGraphMessageRequest
    {
        [JsonPropertyName("body")]
        public MicrosoftGraphMessageBody? Body { get; set; }
    }
}
