using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.Telegram.Model
{
    public class TelegramSetWebhookRequest
    {
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("max_connections")]
        public int? MaxConnections { get; set; }

        [JsonPropertyName("allowed_updates")]
        public List<string>? AllowedUpdates { get; set; }

        [JsonPropertyName("drop_pending_updates")]
        public bool? DropPendingUpdates { get; set; }
    }
}
