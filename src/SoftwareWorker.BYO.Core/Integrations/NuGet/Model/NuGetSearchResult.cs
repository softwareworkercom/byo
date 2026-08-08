using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.NuGet.Model
{
    public class NuGetSearchResult
    {
        [JsonPropertyName("totalHits")]
        public int TotalHits { get; set; }

        [JsonPropertyName("data")]
        public NuGetPackage[] Data { get; set; } = Array.Empty<NuGetPackage>();
    }
}
