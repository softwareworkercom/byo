using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.Confluence.Model
{
    public class ConfluencePage
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("version")]
        public ConfluencePageVersion Version { get; set; } = new ConfluencePageVersion();

        [JsonPropertyName("space")]
        public ConfluencePageSpace? Space { get; set; }

        [JsonPropertyName("_links")]
        public ConfluencePageLinks? Links { get; set; }
    }
}
