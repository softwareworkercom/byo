using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.AzureDevOps.Model
{
    public class AzureDevOpsPullRequests
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("value")]
        public AzureDevOpsPullRequest[] Value { get; set; } = Array.Empty<AzureDevOpsPullRequest>();
    }
}
