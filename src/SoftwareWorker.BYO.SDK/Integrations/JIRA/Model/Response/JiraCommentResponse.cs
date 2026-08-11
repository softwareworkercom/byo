using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.JIRA.Model.Response
{
    public class JiraCommentResponse
    {
        public int startAt { get; set; }
        public int maxResults { get; set; }
        public int total { get; set; }
        [JsonPropertyName("comments")]
        public List<JiraComment> Comments { get; set; }
    }
}
