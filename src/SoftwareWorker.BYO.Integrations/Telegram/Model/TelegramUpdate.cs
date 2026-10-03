using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.Telegram.Model
{
    public class TelegramUpdate
    {
        [JsonPropertyName("update_id")]
        public long UpdateId { get; set; }

        [JsonPropertyName("message")]
        public TelegramMessage? Message { get; set; }
    }
}
