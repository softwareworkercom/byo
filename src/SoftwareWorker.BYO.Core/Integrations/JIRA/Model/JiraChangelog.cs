using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.JIRA.Model
{
    public class JiraChangelog
    {
        [JsonPropertyName("startat")]
        public int StartAt { get; set; }

        [JsonPropertyName("maxresults")]
        public int MaxResults { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("histories")]
        public JiraHistory[] Histories { get; set; }
    }
}