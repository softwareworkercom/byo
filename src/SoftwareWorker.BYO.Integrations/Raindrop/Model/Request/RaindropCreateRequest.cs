using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.Raindrop.Model.Request
{
    public class RaindropCreateRequest
    {
        [JsonPropertyName("link")]
        public string Link { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("excerpt")]
        public string? Excerpt { get; set; }

        [JsonPropertyName("tags")]
        public List<string>? Tags { get; set; }

        [JsonPropertyName("collection")]
        public RaindropCollectionRef? Collection { get; set; }

        [JsonPropertyName("important")]
        public bool? Important { get; set; }
    }
}
