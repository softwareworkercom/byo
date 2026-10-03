using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.Confluence.Model
{
    public class ConfluencePageSearchResults
    {
        public List<ConfluencePageSearchResult> Results { get; set; }
        [JsonPropertyName("_links")]
        public ConfluencePageLinks Links { get; set; }
    }
}