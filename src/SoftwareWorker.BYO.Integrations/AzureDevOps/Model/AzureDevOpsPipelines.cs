namespace SoftwareWorker.BYO.Integrations.AzureDevOps.Model
{
    public class AzureDevOpsPipelines
    {
        public int count { get; set; }
        public AzureDevOpsPipeline[] value { get; set; }
        public AzureDevOpsPipelineSelf self { get; set; }
    }
}
