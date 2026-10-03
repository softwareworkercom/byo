using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.Toggl.Model
{
    public class TogglUser
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("api_token")]
        public string? ApiToken { get; set; }

        [JsonPropertyName("default_workspace_id")]
        public long DefaultWorkspaceId { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("fullname")]
        public string? Fullname { get; set; }

        [JsonPropertyName("timezone")]
        public string? Timezone { get; set; }

        [JsonPropertyName("toggl_accounts_id")]
        public string? TogglAccountsId { get; set; }

        [JsonPropertyName("beginning_of_week")]
        public int BeginningOfWeek { get; set; }

        [JsonPropertyName("image_url")]
        public string? ImageUrl { get; set; }

        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public string? UpdatedAt { get; set; }

        [JsonPropertyName("openid_email")]
        public string? OpenidEmail { get; set; }

        [JsonPropertyName("openid_enabled")]
        public bool OpenidEnabled { get; set; }

        [JsonPropertyName("country_id")]
        public long? CountryId { get; set; }

        [JsonPropertyName("at")]
        public string? At { get; set; }

        [JsonPropertyName("intercom_hash")]
        public string? IntercomHash { get; set; }

        [JsonPropertyName("has_password")]
        public bool HasPassword { get; set; }
    }
}
