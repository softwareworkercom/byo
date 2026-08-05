using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.MicrosoftGraph.Model
{
    public class MicrosoftGraphOnlineMeetingsResponse
    {
        [JsonPropertyName("@odata.context")]
        public string? ODataContext { get; set; }

        [JsonPropertyName("value")]
        public List<MicrosoftGraphOnlineMeeting>? Value { get; set; }
    }
}
