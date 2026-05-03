namespace SoftwareWorker.BYO.Integrations.AzureDevOps.Model
{
    public class AzureDevOpsLinks
    {
        public AzureDevOpsLinksSelf self { get; set; }
        public AzureDevOpsWeb web { get; set; }
        public AzureDevOpsPipelineWeb pipelineweb { get; set; }
        public AzureDevOpsPipeline pipeline { get; set; }
        public AzureDevOpsAvatar avatar { get; set; }
    }
}
