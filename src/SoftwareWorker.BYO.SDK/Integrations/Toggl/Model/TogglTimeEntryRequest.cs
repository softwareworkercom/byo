using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.Toggl.Model
{
    public class TogglTimeEntryRequest
    {
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("start")]
        public string? Start { get; set; }

        [JsonPropertyName("duration")]
        public int? Duration { get; set; }

        [JsonPropertyName("project_id")]
        public long? ProjectId { get; set; }

        [JsonPropertyName("tag_ids")]
        public List<long>? TagIds { get; set; }

        [JsonPropertyName("billable")]
        public bool? Billable { get; set; }

        [JsonPropertyName("created_with")]
        public string? CreatedWith { get; set; }
    }
}
