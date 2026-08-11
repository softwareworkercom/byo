namespace SoftwareWorker.BYO.Integrations.AzureDevOps.Model
{
    public class AzureDevOpsYamlDetails
    {
        public AzureDevOpsIncludedTemplate[] includedTemplates { get; set; }
        public AzureDevOpsRootYamlFile rootYamlFile { get; set; }
        public string expandedYamlUrl { get; set; }
    }
}
