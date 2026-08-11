using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.JIRA.Model.Response
{

    public class JiraGroupMembersResponse
    {
        public string self { get; set; }
        public int maxResults { get; set; }
        public int startAt { get; set; }
        public int total { get; set; }
        public bool isLast { get; set; }
        [JsonPropertyName("values")]
        public JiraUser[] Users { get; set; }
    }
}
