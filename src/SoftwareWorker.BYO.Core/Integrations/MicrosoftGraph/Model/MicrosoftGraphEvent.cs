using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.MicrosoftGraph.Model
{
    public class MicrosoftGraphEvent
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("subject")]
        public string? Subject { get; set; }

        [JsonPropertyName("body")]
        public MicrosoftGraphMessageBody? Body { get; set; }

        [JsonPropertyName("start")]
        public MicrosoftGraphDateTimeTimeZone? Start { get; set; }

        [JsonPropertyName("end")]
        public MicrosoftGraphDateTimeTimeZone? End { get; set; }

        [JsonPropertyName("location")]
        public MicrosoftGraphLocation? Location { get; set; }

        [JsonPropertyName("attendees")]
        public List<MicrosoftGraphAttendee>? Attendees { get; set; }

        [JsonPropertyName("organizer")]
        public MicrosoftGraphRecipient? Organizer { get; set; }

        [JsonPropertyName("isAllDay")]
        public bool? IsAllDay { get; set; }
    }
}
