using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.Telegram.Model
{
    public class TelegramWebhookInfo
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("has_custom_certificate")]
        public bool HasCustomCertificate { get; set; }

        [JsonPropertyName("pending_update_count")]
        public int PendingUpdateCount { get; set; }

        [JsonPropertyName("last_error_date")]
        public long? LastErrorDate { get; set; }

        [JsonPropertyName("last_error_message")]
        public string? LastErrorMessage { get; set; }

        [JsonPropertyName("max_connections")]
        public int? MaxConnections { get; set; }

        [JsonPropertyName("allowed_updates")]
        public List<string>? AllowedUpdates { get; set; }
    }
}
