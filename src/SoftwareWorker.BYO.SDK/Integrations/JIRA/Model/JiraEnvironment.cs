using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.JIRA.Model
{
    public class JiraEnvironment
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; }
    }
}