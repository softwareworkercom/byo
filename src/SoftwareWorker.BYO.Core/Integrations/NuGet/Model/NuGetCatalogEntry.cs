using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.NuGet.Model
{
    public class NuGetCatalogEntry
    {
        [JsonPropertyName("@id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        public string PackageId { get; set; } = string.Empty;

        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("authors")]
        public string Authors { get; set; } = string.Empty;

        [JsonPropertyName("iconUrl")]
        public string IconUrl { get; set; } = string.Empty;

        [JsonPropertyName("licenseUrl")]
        public string LicenseUrl { get; set; } = string.Empty;

        [JsonPropertyName("projectUrl")]
        public string ProjectUrl { get; set; } = string.Empty;

        [JsonPropertyName("published")]
        public DateTime Published { get; set; }

        [JsonPropertyName("requireLicenseAcceptance")]
        public bool RequireLicenseAcceptance { get; set; }

        [JsonPropertyName("summary")]
        public string Summary { get; set; } = string.Empty;

        [JsonPropertyName("tags")]
        public string[] Tags { get; set; } = Array.Empty<string>();

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("dependencyGroups")]
        public NuGetDependencyGroup[] DependencyGroups { get; set; } = Array.Empty<NuGetDependencyGroup>();
    }
}
