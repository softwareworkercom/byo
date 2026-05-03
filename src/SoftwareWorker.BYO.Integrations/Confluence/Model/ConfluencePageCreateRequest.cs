using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.Confluence.Model
{
    public class ConfluencePageCreateRequest
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "page";

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("space")]
        public ConfluencePageSpace Space { get; set; } = new ConfluencePageSpace();

        [JsonPropertyName("body")]
        public ConfluencePageBody Body { get; set; } = new ConfluencePageBody();
    }
}
