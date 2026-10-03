using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.AzureDevOps.Model
{
    public class AzureDevOpsCheckQueryRequest
    {
        [JsonPropertyName("resources")]
        public AzureDevOpsCheckResource[] Resources { get; set; } = Array.Empty<AzureDevOpsCheckResource>();
    }
}
