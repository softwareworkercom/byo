using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.MicrosoftGraph.Model
{
    public class MicrosoftGraphRecipient
    {
        [JsonPropertyName("emailAddress")]
        public MicrosoftGraphEmailAddress? EmailAddress { get; set; }

        [JsonPropertyName("user")]
        public MicrosoftGraphIdentity? User { get; set; }

        [JsonPropertyName("application")]
        public MicrosoftGraphIdentity? Application { get; set; }

        [JsonPropertyName("device")]
        public MicrosoftGraphIdentity? Device { get; set; }
    }

    public class MicrosoftGraphIdentity
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }
    }
}
