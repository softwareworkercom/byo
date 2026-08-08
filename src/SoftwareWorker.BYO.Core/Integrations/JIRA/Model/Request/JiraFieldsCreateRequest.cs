using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.JIRA.Model.Request
{
    public class JiraFieldsCreateRequest
    {

        [JsonPropertyName("project")]
        public JiraProject Project { get; set; }

        [JsonPropertyName("summary")]
        public string Summary { get; set; }

        [JsonPropertyName("description")]
        public AtlassianDocument Description { get; set; }

        [JsonPropertyName("parent")]
        public JiraParent Parent { get; set; }

        [JsonPropertyName("components")]
        public List<JiraComponent> Components { get; set; }

        [JsonPropertyName("issuetype")]
        public JiraIssueType IssueType { get; set; }

        [JsonPropertyName("assignee")]
        public JiraUser Assignee { get; set; }

        [JsonPropertyName("priority")]
        public JiraPriority Priority { get; set; }

        [JsonPropertyName("labels")]
        public List<string> Labels { get; set; }

        //TODO: Make this customizable
        [JsonPropertyName("customfield_10007")]
        public long? SprintId { get; set; }


        [JsonPropertyName("fixVersions")]
        public List<JiraFixVersion> FixVersions { get; set; }

        //[JsonPropertyName("customfield_10045")]
        //public JiraEnvironment Environment { get; set; }

        //[JsonPropertyName("customfield_10015")]
        //public virtual DateOnly? StartDate { get; set; }
        //[JsonPropertyName("duedate")]
        //public virtual DateOnly? DueDate { get; set; }

        //[JsonPropertyName("timeestimate")]
        //public int? TimeEstimateInSeconds { get; set; }

    }
}
