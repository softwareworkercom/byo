using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.JIRA.Model
{
    public class JiraIssueType
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}