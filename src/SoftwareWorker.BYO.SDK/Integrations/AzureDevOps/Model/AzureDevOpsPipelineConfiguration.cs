using SoftwareWorker.BYO.Integrations.AzureDevOps.Model;

public class AzureDevOpsPipelineConfiguration
{
    public string path { get; set; }
    public AzureDevOpsRepository repository { get; set; }
    public string type { get; set; }
}
