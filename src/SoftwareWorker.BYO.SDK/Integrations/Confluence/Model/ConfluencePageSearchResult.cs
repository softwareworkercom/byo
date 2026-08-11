using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.Confluence.Model
{
    public class ConfluencePageSearchResult
    {
        public ConfluencePageSearchResult Content { get; set; }
        public string Id { get; set; }
        public string Type { get; set; }
        public string Title { get; set; }

        [JsonPropertyName("_links")]
        public ConfluencePageLinks Links { get; set; }

        public ConfluencePageHistory History { get; set; }
        public DateTime? LastModified
        {
            get
            {
                return this.History?.LastUpdated.When;
            }
        }
    }
}
