using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.AzureDevOps.Model
{
    public class AzureDevOpsPackage
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("protocolType")]
        public string ProtocolType { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("versions")]
        public AzureDevOpsPackageVersion[] Versions { get; set; } = Array.Empty<AzureDevOpsPackageVersion>();

        [JsonPropertyName("_links")]
        public AzureDevOpsLinks? Links { get; set; }
    }
}
