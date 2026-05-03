using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.NuGet.Model
{
    public class NuGetPackageMetadata
    {
        [JsonPropertyName("@id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("@type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("registration")]
        public string Registration { get; set; } = string.Empty;

        [JsonPropertyName("catalogEntry")]
        public NuGetCatalogEntry CatalogEntry { get; set; } = new NuGetCatalogEntry();

        [JsonPropertyName("packageContent")]
        public string PackageContent { get; set; } = string.Empty;
    }
}
