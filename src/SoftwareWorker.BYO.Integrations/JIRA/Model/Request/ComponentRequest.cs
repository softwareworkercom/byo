using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.JIRA.Model.Request
{

    public class ComponentRequest
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }
        [JsonPropertyName("leadAccountId")]
        public object LeadAccountId { get; set; }

        [JsonPropertyName("assigneeType")]
        public string AssigneeType { get; set; }

        [JsonPropertyName("project")]
        public string Project { get; set; }
    }

}
