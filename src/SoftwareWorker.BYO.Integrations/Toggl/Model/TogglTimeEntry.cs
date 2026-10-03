using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.Toggl.Model
{
    public class TogglTimeEntry
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("workspace_id")]
        public long WorkspaceId { get; set; }

        [JsonPropertyName("project_id")]
        public long? ProjectId { get; set; }

        [JsonPropertyName("task_id")]
        public long? TaskId { get; set; }

        [JsonPropertyName("billable")]
        public bool Billable { get; set; }

        [JsonPropertyName("start")]
        public string? Start { get; set; }

        [JsonPropertyName("stop")]
        public string? Stop { get; set; }

        [JsonPropertyName("duration")]
        public int Duration { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("tags")]
        public List<string>? Tags { get; set; }

        [JsonPropertyName("tag_ids")]
        public List<long>? TagIds { get; set; }

        [JsonPropertyName("duronly")]
        public bool DurOnly { get; set; }

        [JsonPropertyName("at")]
        public string? At { get; set; }

        [JsonPropertyName("server_deleted_at")]
        public string? ServerDeletedAt { get; set; }

        [JsonPropertyName("user_id")]
        public long UserId { get; set; }

        [JsonPropertyName("uid")]
        public long Uid { get; set; }

        [JsonPropertyName("wid")]
        public long Wid { get; set; }

        [JsonPropertyName("pid")]
        public long? Pid { get; set; }
    }
}
