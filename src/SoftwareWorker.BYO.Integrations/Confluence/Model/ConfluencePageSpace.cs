using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.Confluence.Model
{
    public class ConfluencePageSpace
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;
    }
}
