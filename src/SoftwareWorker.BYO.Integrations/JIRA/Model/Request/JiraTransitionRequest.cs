using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.JIRA.Model.Request
{
    public class JiraTransitionRequest
    {
        [JsonPropertyName("transition")]
        public JiraTransitionInfo Transition { get; set; } = new JiraTransitionInfo();
    }
}
