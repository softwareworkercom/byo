using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.NuGet.Model
{
    public class NuGetPackage
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("authors")]
        public string[] Authors { get; set; } = Array.Empty<string>();

        [JsonPropertyName("owners")]
        public string[] Owners { get; set; } = Array.Empty<string>();

        [JsonPropertyName("iconUrl")]
        public string IconUrl { get; set; } = string.Empty;

        [JsonPropertyName("licenseUrl")]
        public string LicenseUrl { get; set; } = string.Empty;

        [JsonPropertyName("projectUrl")]
        public string ProjectUrl { get; set; } = string.Empty;

        [JsonPropertyName("registration")]
        public string Registration { get; set; } = string.Empty;

        [JsonPropertyName("summary")]
        public string Summary { get; set; } = string.Empty;

        [JsonPropertyName("tags")]
        public string[] Tags { get; set; } = Array.Empty<string>();

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("totalDownloads")]
        public long TotalDownloads { get; set; }

        [JsonPropertyName("verified")]
        public bool Verified { get; set; }

        [JsonPropertyName("versions")]
        public NuGetPackageVersion[] Versions { get; set; } = Array.Empty<NuGetPackageVersion>();
    }
}
