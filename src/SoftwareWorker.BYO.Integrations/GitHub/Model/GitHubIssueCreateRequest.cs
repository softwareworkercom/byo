using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.GitHub.Model
{
    public class GitHubIssueCreateRequest
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("body")]
        public string Body { get; set; } = string.Empty;

        [JsonPropertyName("assignees")]
        public string[]? Assignees { get; set; }

        [JsonPropertyName("labels")]
        public string[]? Labels { get; set; }
    }
}
