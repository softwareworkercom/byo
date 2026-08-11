using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.GitHub.Model
{
    public class GitHubWorkflowRuns
    {
        [JsonPropertyName("total_count")]
        public int TotalCount { get; set; }

        [JsonPropertyName("workflow_runs")]
        public GitHubWorkflowRun[] WorkflowRuns { get; set; } = Array.Empty<GitHubWorkflowRun>();
    }
}
