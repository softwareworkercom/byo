namespace SoftwareWorker.BYO.Integrations.AzureDevOps.Model
{
    public class AzureDevOpsTemplates
    {
        public AzureDevOpsRepository repository { get; set; }
        public string refName { get; set; }
        public string version { get; set; }
    }
}
