using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.AzureDevOps.Model
{
    public class AzureDevOpsWorkItemCreateRequest
    {
        [JsonPropertyName("operations")]
        public List<AzureDevOpsWorkItemOperation> Operations { get; set; } = new List<AzureDevOpsWorkItemOperation>();
    }
}
