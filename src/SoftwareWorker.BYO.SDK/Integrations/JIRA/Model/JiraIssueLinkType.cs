using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.JIRA.Model
{
    public class JiraIssueLinkType
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("inward")]
        public string Inward { get; set; }

        [JsonPropertyName("outward")]
        public string Outward { get; set; }
    }
}
