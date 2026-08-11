using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.MicrosoftGraph.Model
{
    public class MicrosoftGraphOnlineMeeting
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("joinWebUrl")]
        public string? JoinWebUrl { get; set; }

        [JsonPropertyName("startDateTime")]
        public DateTimeOffset? StartDateTime { get; set; }

        [JsonPropertyName("createdDateTime")]
        public DateTimeOffset? CreatedDateTime { get; set; }
    }
}
