namespace SoftwareWorker.BYO.Integrations.AzureDevOps.Model
{
    public class AzureDevOpsRunBody
    {
        public bool previewRun { get; set; }
        public string[] stagesToSkip { get; set; }
        public AzureDevOpsResources resources { get; set; }
    }


}
