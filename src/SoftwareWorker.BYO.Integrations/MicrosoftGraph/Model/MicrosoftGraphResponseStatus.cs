using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.MicrosoftGraph.Model
{
    public class MicrosoftGraphResponseStatus
    {
        [JsonPropertyName("response")]
        public string? Response { get; set; }

        [JsonPropertyName("time")]
        public DateTime? Time { get; set; }
    }
}
