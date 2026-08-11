using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.AzureDevOps.Model
{
    public class AzureDevOpsCheckQueryResponse
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("value")]
        public AzureDevOpsCheck[] Value { get; set; } = Array.Empty<AzureDevOpsCheck>();
    }
}
