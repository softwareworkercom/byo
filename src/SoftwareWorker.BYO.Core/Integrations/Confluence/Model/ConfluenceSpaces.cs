using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.Confluence.Model
{
    public class ConfluenceSpaces
    {
        [JsonPropertyName("results")]
        public ConfluenceSpace[] Results { get; set; } = Array.Empty<ConfluenceSpace>();

        [JsonPropertyName("size")]
        public int Size { get; set; }
    }
}
