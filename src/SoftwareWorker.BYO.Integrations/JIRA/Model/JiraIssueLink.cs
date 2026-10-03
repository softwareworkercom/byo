using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.JIRA.Model
{
    public class JiraIssueLink
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("type")]
        public JiraIssueLinkType Type { get; set; }

        [JsonPropertyName("inwardIssue")]
        public JiraLinkedIssue InwardIssue { get; set; }

        [JsonPropertyName("outwardIssue")]
        public JiraLinkedIssue OutwardIssue { get; set; }
    }
}
