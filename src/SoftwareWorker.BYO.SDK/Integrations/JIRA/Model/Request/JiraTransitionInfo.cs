using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.JIRA.Model.Request
{
    public class JiraTransitionInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }
}
