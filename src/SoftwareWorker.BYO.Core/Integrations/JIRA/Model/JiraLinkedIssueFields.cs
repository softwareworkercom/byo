using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.JIRA.Model
{
    public class JiraLinkedIssueFields
    {
        [JsonPropertyName("summary")]
        public string Summary { get; set; }

        [JsonPropertyName("status")]
        public JiraStatus Status { get; set; }

        [JsonPropertyName("issuetype")]
        public JiraIssueType IssueType { get; set; }
    }
}
