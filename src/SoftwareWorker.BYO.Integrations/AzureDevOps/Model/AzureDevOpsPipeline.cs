namespace SoftwareWorker.BYO.Integrations.AzureDevOps.Model
{
    public class AzureDevOpsPipeline
    {
        public AzureDevOpsLinks _links { get; set; }
        public AzureDevOpsPipelineConfiguration configuration { get; set; }
        public string url { get; set; }
        public int id { get; set; }
        public int revision { get; set; }
        public string name { get; set; }
        public string folder { get; set; }
    }


}
