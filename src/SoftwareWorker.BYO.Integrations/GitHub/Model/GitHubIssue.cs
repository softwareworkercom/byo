using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.GitHub.Model
{
    public class GitHubIssue
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("number")]
        public int Number { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("body")]
        public string Body { get; set; } = string.Empty;

        [JsonPropertyName("state")]
        public string State { get; set; } = string.Empty;

        [JsonPropertyName("user")]
        public GitHubUser? User { get; set; }

        [JsonPropertyName("assignees")]
        public GitHubUser[] Assignees { get; set; } = Array.Empty<GitHubUser>();

        [JsonPropertyName("labels")]
        public GitHubLabel[] Labels { get; set; } = Array.Empty<GitHubLabel>();

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}
