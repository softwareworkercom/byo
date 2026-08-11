using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.JIRA.Model
{
    public class JiraSprint
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("state")]
        public string State { get; set; }
        [JsonPropertyName("startdate")]
        public DateTime StartDate { get; set; }
        [JsonPropertyName("enddate")]
        public DateTime EndDate { get; set; }
        [JsonPropertyName("goal")]
        public string Goal { get; set; }
    }
}