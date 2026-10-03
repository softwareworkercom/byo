using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.MicrosoftGraph.Model
{
    public class MicrosoftGraphAttendee
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("status")]
        public MicrosoftGraphResponseStatus? Status { get; set; }

        [JsonPropertyName("emailAddress")]
        public MicrosoftGraphEmailAddress? EmailAddress { get; set; }
    }
}
