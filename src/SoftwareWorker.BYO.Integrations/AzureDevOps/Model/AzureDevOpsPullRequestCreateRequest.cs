using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.AzureDevOps.Model
{
    public class AzureDevOpsPullRequestCreateRequest
    {
        [JsonPropertyName("sourceRefName")]
        public string SourceRefName { get; set; } = string.Empty;

        [JsonPropertyName("targetRefName")]
        public string TargetRefName { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
    }
}
