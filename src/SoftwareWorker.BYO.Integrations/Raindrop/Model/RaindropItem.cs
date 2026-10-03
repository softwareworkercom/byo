using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.Raindrop.Model
{
    public class RaindropItem
    {
        [JsonPropertyName("_id")]
        public long Id { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("excerpt")]
        public string? Excerpt { get; set; }

        [JsonPropertyName("link")]
        public string? Link { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("tags")]
        public List<string>? Tags { get; set; }

        [JsonPropertyName("collection")]
        public RaindropCollectionRef? Collection { get; set; }

        [JsonPropertyName("created")]
        public DateTime? Created { get; set; }

        [JsonPropertyName("lastUpdate")]
        public DateTime? LastUpdate { get; set; }

        [JsonPropertyName("important")]
        public bool Important { get; set; }

        [JsonPropertyName("domain")]
        public string? Domain { get; set; }
    }

    public class RaindropCollectionRef
    {
        [JsonPropertyName("$id")]
        public long Id { get; set; }
    }
}
