namespace SoftwareWorker.BYO.Integrations.AzureDevOps.Model
{
    public class AzureDevOpsRun
    {
        public AzureDevOpsYamlDetails yamlDetails { get; set; }
        public AzureDevOpsLinks _links { get; set; }
        public AzureDevOpsTemplateParameters templateParameters { get; set; }
        public AzureDevOpsPipeline pipeline { get; set; }
        public string state { get; set; }
        public string result { get; set; }
        public DateTime createdDate { get; set; }
        public string url { get; set; }
        public AzureDevOpsResources resources { get; set; }
        public int id { get; set; }
        public string name { get; set; }
    }
}