using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.Stripe.Model
{
    public class StripeListResponse<T>
    {
        [JsonPropertyName("object")]
        public string? Object { get; set; }

        [JsonPropertyName("data")]
        public List<T>? Data { get; set; }

        [JsonPropertyName("has_more")]
        public bool HasMore { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }
}
