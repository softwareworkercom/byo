using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.Toggl.Model
{
    public class TogglWorkspace
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("organization_id")]
        public long OrganizationId { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("profile")]
        public int Profile { get; set; }

        [JsonPropertyName("premium")]
        public bool Premium { get; set; }

        [JsonPropertyName("business_ws")]
        public bool BusinessWs { get; set; }

        [JsonPropertyName("admin")]
        public bool Admin { get; set; }

        [JsonPropertyName("suspended_at")]
        public string? SuspendedAt { get; set; }

        [JsonPropertyName("server_deleted_at")]
        public string? ServerDeletedAt { get; set; }

        [JsonPropertyName("default_hourly_rate")]
        public decimal? DefaultHourlyRate { get; set; }

        [JsonPropertyName("rate_last_updated")]
        public string? RateLastUpdated { get; set; }

        [JsonPropertyName("default_currency")]
        public string? DefaultCurrency { get; set; }

        [JsonPropertyName("only_admins_may_create_projects")]
        public bool OnlyAdminsMayCreateProjects { get; set; }

        [JsonPropertyName("only_admins_may_create_tags")]
        public bool OnlyAdminsMayCreateTags { get; set; }

        [JsonPropertyName("only_admins_see_billable_rates")]
        public bool OnlyAdminsSeeBillableRates { get; set; }

        [JsonPropertyName("only_admins_see_team_dashboard")]
        public bool OnlyAdminsSeeTeamDashboard { get; set; }

        [JsonPropertyName("projects_billable_by_default")]
        public bool ProjectsBillableByDefault { get; set; }

        [JsonPropertyName("rounding")]
        public int Rounding { get; set; }

        [JsonPropertyName("rounding_minutes")]
        public int RoundingMinutes { get; set; }

        [JsonPropertyName("api_token")]
        public string? ApiToken { get; set; }

        [JsonPropertyName("at")]
        public string? At { get; set; }

        [JsonPropertyName("logo_url")]
        public string? LogoUrl { get; set; }

        [JsonPropertyName("ical_url")]
        public string? IcalUrl { get; set; }

        [JsonPropertyName("ical_enabled")]
        public bool IcalEnabled { get; set; }

        [JsonPropertyName("csv_upload")]
        public object? CsvUpload { get; set; }

        [JsonPropertyName("subscription")]
        public object? Subscription { get; set; }

        [JsonPropertyName("hide_start_end_times")]
        public bool HideStartEndTimes { get; set; }
    }
}
