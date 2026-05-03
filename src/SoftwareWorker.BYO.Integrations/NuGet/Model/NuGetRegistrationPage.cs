using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.NuGet.Model
{
    public class NuGetRegistrationPage
    {
        [JsonPropertyName("@id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("lower")]
        public string Lower { get; set; } = string.Empty;

        [JsonPropertyName("upper")]
        public string Upper { get; set; } = string.Empty;

        [JsonPropertyName("items")]
        public NuGetPackageMetadata[] Items { get; set; } = Array.Empty<NuGetPackageMetadata>();
    }
}
