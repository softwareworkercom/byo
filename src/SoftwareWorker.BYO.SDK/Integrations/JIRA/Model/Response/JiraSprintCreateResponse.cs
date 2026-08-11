using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.JIRA.Model.Response
{
    public class JiraSprintCreateResponse
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("self")]
        public string Self { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("startDate")]
        public DateTime StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public DateTime EndDate { get; set; }

        [JsonPropertyName("originBoardId")]
        public int OriginBoardId { get; set; }

        [JsonPropertyName("goal")]
        public string Goal { get; set; }
    }
}
