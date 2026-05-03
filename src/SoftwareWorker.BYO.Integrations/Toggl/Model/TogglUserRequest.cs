using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.Toggl.Model
{
    public class TogglUserRequest
    {
        [JsonPropertyName("fullname")]
        public string? Fullname { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("timezone")]
        public string? Timezone { get; set; }
    }
}
