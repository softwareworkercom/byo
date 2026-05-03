using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.NuGet.Model
{
    public class NuGetPackageVersion
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        [JsonPropertyName("downloads")]
        public long Downloads { get; set; }

        [JsonPropertyName("@id")]
        public string Id { get; set; } = string.Empty;
    }
}
