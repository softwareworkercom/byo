using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.JIRA.Model.Response
{
    public class JiraIssueQueryResponse
    {

        [JsonPropertyName("issues")]
        public List<JiraIssue> Issues { get; set; }

        [JsonPropertyName("nextPageToken")]
        public string NextPageToken { get; set; }

        [JsonPropertyName("isLast")]
        public bool IsLast { get; set; }

    }
}