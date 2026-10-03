using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.AzureDevOps.Model
{
    public class AzureDevOpsRootYamlFile
    {
        [JsonPropertyName("ref")]
        public string _ref { get; set; }
        public string yamlFile { get; set; }
        public string repoAlias { get; set; }
    }
}
