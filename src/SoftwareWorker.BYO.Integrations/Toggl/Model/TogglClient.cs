using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.Toggl.Model
{
    public class TogglClient
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("wid")]
        public long Wid { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("at")]
        public string? At { get; set; }

        [JsonPropertyName("archived")]
        public bool Archived { get; set; }

        [JsonPropertyName("creator_id")]
        public long? CreatorId { get; set; }
    }
}
