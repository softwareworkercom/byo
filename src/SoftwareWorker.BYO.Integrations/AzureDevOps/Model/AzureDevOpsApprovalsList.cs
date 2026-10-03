using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.AzureDevOps.Model
{
    public class AzureDevOpsApprovalsList
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("value")]
        public AzureDevOpsApprovalItem[] Value { get; set; } = Array.Empty<AzureDevOpsApprovalItem>();
    }
}
