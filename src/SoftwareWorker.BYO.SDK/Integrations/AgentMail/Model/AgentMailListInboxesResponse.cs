using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.AgentMail.Model
{
    public class AgentMailListInboxesResponse
    {
        [JsonPropertyName("data")]
        public List<AgentMailInbox>? Data { get; set; }

        [JsonPropertyName("items")]
        public List<AgentMailInbox>? Items { get; set; }

        [JsonPropertyName("inboxes")]
        public List<AgentMailInbox>? Inboxes { get; set; }

        [JsonPropertyName("next_page_token")]
        public string? NextPageToken { get; set; }

        public List<AgentMailInbox> GetInboxes()
        {
            if (Data != null)
            {
                return Data;
            }

            if (Items != null)
            {
                return Items;
            }

            return Inboxes ?? [];
        }
    }
}
