using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.AgentMail.Model
{
    public class AgentMailListMessagesResponse
    {
        [JsonPropertyName("data")]
        public List<AgentMailMessage>? Data { get; set; }

        [JsonPropertyName("items")]
        public List<AgentMailMessage>? Items { get; set; }

        [JsonPropertyName("messages")]
        public List<AgentMailMessage>? Messages { get; set; }

        [JsonPropertyName("next_page_token")]
        public string? NextPageToken { get; set; }

        public List<AgentMailMessage> GetMessages()
        {
            if (Data != null)
            {
                return Data;
            }

            if (Items != null)
            {
                return Items;
            }

            return Messages ?? [];
        }
    }
}
