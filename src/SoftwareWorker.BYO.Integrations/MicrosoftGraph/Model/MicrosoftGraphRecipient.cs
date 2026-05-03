using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.MicrosoftGraph.Model
{
    public class MicrosoftGraphRecipient
    {
        [JsonPropertyName("emailAddress")]
        public MicrosoftGraphEmailAddress? EmailAddress { get; set; }
    }
}
