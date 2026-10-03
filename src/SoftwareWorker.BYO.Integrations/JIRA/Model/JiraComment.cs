using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.JIRA.Model
{
    public class JiraComment
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("body")]
        public object Body { get; set; }

        [JsonPropertyName("author")]
        public JiraUser Author { get; set; }

        [JsonPropertyName("created")]
        public DateTime Created { get; set; }

        [JsonPropertyName("updated")]
        public DateTime Updated { get; set; }
    }
}
