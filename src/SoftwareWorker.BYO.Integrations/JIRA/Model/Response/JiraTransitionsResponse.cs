using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.JIRA.Model.Response
{
    public class JiraTransitionsResponse
    {
        [JsonPropertyName("transitions")]
        public JiraTransition[] Transitions { get; set; } = Array.Empty<JiraTransition>();
    }
}
