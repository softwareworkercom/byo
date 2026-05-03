using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.JIRA.Model
{
    public class JiraFields
    {
        [JsonPropertyName("project")]
        public JiraProject Project { get; set; }

        [JsonPropertyName("summary")]
        public string Summary { get; set; }

        [JsonPropertyName("description")]
        public object? Description { get; set; }

        [JsonPropertyName("parent")]
        public JiraParent Parent { get; set; }

        [JsonPropertyName("components")]
        public List<JiraComponent> Components { get; set; }

        [JsonPropertyName("status")]
        public virtual JiraStatus Status { get; set; }

        [JsonPropertyName("duedate")]
        public virtual DateOnly? DueDate { get; set; }

        [JsonPropertyName("issuetype")]
        public JiraIssueType IssueType { get; set; }

        [JsonPropertyName("assignee")]
        public JiraUser Assignee { get; set; }

        [JsonPropertyName("creator")]
        public JiraUser Creator { get; set; }

        [JsonPropertyName("reporter")]
        public JiraUser Reporter { get; set; }

        [JsonPropertyName("priority")]
        public JiraPriority Priority { get; set; }

        //TODO: Make this customizable in Refit Settings
        [JsonPropertyName("customfield_10007")]
        public List<JiraSprint> Sprints { get; set; }

        //TODO: Make this customizable in Refit Settings
        [JsonPropertyName("customfield_10005")]
        public double? Points { get; set; }

        //TODO: Make this customizable in Refit Settings
        //[JsonPropertyName("customfield_10001")]
        public JiraTeam? Team { get; set; }

        [JsonPropertyName("fixVersions")]
        public List<JiraFixVersion> FixVersions { get; set; }

        [JsonPropertyName("created")]
        public DateTime Created { get; set; }

        [JsonPropertyName("updated")]
        public DateTime? Updated { get; set; }

        [JsonPropertyName("subtasks")]
        public List<JiraIssue> Subtasks { get; set; }

        [JsonPropertyName("labels")]
        public List<string> Labels { get; set; }

        [JsonPropertyName("comment")]
        public JiraCommentContainer Comment { get; set; }

        [JsonPropertyName("timeestimate")]
        public int? TimeEstimateInSeconds { get; set; }

        [JsonPropertyName("issuelinks")]
        public List<JiraIssueLink> IssueLinks { get; set; }
    }

    public class JiraCommentContainer
    {
        [JsonPropertyName("comments")]
        public List<JiraComment> Comments { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }
    }
}
