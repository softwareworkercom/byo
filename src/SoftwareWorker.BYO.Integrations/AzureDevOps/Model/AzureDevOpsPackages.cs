using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.AzureDevOps.Model
{
    public class AzureDevOpsPackages
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("value")]
        public AzureDevOpsPackage[] Value { get; set; } = Array.Empty<AzureDevOpsPackage>();
    }
}
