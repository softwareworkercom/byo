using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.JIRA.Model.Response
{
    public class JiraComponentResponse
    {
        [JsonPropertyName("self")]
        public string Self { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("assigneeType")]
        public string AssigneeType { get; set; }

        [JsonPropertyName("realAssigneeType")]
        public string RealAssigneeType { get; set; }

        [JsonPropertyName("isAssigneeTypeValid")]
        public bool IsAssigneeTypeValid { get; set; }

        [JsonPropertyName("project")]
        public string Project { get; set; }

        [JsonPropertyName("projectId")]
        public int ProjectId { get; set; }
    }
}
