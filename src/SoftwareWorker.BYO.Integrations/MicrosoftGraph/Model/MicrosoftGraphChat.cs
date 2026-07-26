using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.MicrosoftGraph.Model
{
    public class MicrosoftGraphChat
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("topic")]
        public string? Topic { get; set; }

        [JsonPropertyName("chatType")]
        public string? ChatType { get; set; }
    }
}
