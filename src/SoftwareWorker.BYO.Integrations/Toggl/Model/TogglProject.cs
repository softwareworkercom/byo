using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.Toggl.Model
{
    public class TogglProject
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("workspace_id")]
        public long WorkspaceId { get; set; }

        [JsonPropertyName("client_id")]
        public long? ClientId { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("is_private")]
        public bool IsPrivate { get; set; }

        [JsonPropertyName("active")]
        public bool Active { get; set; }

        [JsonPropertyName("at")]
        public string? At { get; set; }

        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }

        [JsonPropertyName("server_deleted_at")]
        public string? ServerDeletedAt { get; set; }

        [JsonPropertyName("color")]
        public string? Color { get; set; }

        [JsonPropertyName("billable")]
        public bool? Billable { get; set; }

        [JsonPropertyName("template")]
        public bool? Template { get; set; }

        [JsonPropertyName("auto_estimates")]
        public bool? AutoEstimates { get; set; }

        [JsonPropertyName("estimated_hours")]
        public int? EstimatedHours { get; set; }

        [JsonPropertyName("rate")]
        public decimal? Rate { get; set; }

        [JsonPropertyName("rate_last_updated")]
        public string? RateLastUpdated { get; set; }

        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        [JsonPropertyName("recurring")]
        public bool Recurring { get; set; }

        [JsonPropertyName("recurring_parameters")]
        public object? RecurringParameters { get; set; }

        [JsonPropertyName("current_period")]
        public object? CurrentPeriod { get; set; }

        [JsonPropertyName("fixed_fee")]
        public decimal? FixedFee { get; set; }

        [JsonPropertyName("actual_hours")]
        public int? ActualHours { get; set; }

        [JsonPropertyName("wid")]
        public long Wid { get; set; }

        [JsonPropertyName("cid")]
        public long? Cid { get; set; }
    }
}
