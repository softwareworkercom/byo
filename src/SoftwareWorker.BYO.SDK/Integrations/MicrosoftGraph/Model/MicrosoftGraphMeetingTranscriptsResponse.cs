using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.MicrosoftGraph.Model
{
    public class MicrosoftGraphMeetingTranscriptsResponse
    {
        [JsonPropertyName("@odata.context")]
        public string? ODataContext { get; set; }

        [JsonPropertyName("value")]
        public List<MicrosoftGraphMeetingTranscript>? Value { get; set; }
    }
}
