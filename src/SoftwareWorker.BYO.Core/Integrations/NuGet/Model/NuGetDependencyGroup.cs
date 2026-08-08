using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.NuGet.Model
{
    public class NuGetDependencyGroup
    {
        [JsonPropertyName("@id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("targetFramework")]
        public string TargetFramework { get; set; } = string.Empty;

        [JsonPropertyName("dependencies")]
        public NuGetDependency[] Dependencies { get; set; } = Array.Empty<NuGetDependency>();
    }
}
