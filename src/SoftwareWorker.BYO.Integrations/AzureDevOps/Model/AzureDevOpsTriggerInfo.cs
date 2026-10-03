using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.AzureDevOps.Model
{
    public class AzureDevOpsTriggerInfo
    {
        [JsonPropertyName("ci.sourceBranch")]
        public string cisourceBranch { get; set; }

        [JsonPropertyName("ci.sourceSha")]
        public string cisourceSha { get; set; }

        [JsonPropertyName("ci.message")]
        public string cimessage { get; set; }

        [JsonPropertyName("ci.triggerRepository")]
        public string citriggerRepository { get; set; }
    }
}
