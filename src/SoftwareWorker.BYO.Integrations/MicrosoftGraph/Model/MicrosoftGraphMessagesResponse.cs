using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.MicrosoftGraph.Model
{
    public class MicrosoftGraphMessagesResponse
    {
        [JsonPropertyName("@odata.context")]
        public string? ODataContext { get; set; }

        [JsonPropertyName("@odata.nextLink")]
        public string? ODataNextLink { get; set; }

        [JsonPropertyName("value")]
        public List<MicrosoftGraphMessage>? Value { get; set; }
    }
}
