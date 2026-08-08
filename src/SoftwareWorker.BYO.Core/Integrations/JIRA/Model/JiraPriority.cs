using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.JIRA.Model
{
    public class JiraPriority
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

    }
}