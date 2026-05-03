using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.JIRA.Model
{
    public class JiraStatus
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}