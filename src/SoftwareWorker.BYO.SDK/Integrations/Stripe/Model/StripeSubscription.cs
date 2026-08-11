using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.Stripe.Model
{
    public class StripeSubscription
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("object")]
        public string? Object { get; set; }

        [JsonPropertyName("customer")]
        public string? Customer { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("current_period_start")]
        public long? CurrentPeriodStart { get; set; }

        [JsonPropertyName("current_period_end")]
        public long? CurrentPeriodEnd { get; set; }

        [JsonPropertyName("cancel_at")]
        public long? CancelAt { get; set; }

        [JsonPropertyName("canceled_at")]
        public long? CanceledAt { get; set; }

        [JsonPropertyName("created")]
        public long? Created { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, string>? Metadata { get; set; }
    }
}
