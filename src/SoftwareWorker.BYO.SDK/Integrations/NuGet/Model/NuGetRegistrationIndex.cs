using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.NuGet.Model
{
    public class NuGetRegistrationIndex
    {
        [JsonPropertyName("@id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("items")]
        public NuGetRegistrationPage[] Items { get; set; } = Array.Empty<NuGetRegistrationPage>();
    }
}
