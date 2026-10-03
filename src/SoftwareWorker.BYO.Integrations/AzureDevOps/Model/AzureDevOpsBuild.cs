namespace SoftwareWorker.BYO.Integrations.AzureDevOps.Model
{
    public class AzureDevOpsBuild
    {
        public AzureDevOpsLinks _links { get; set; }
        public AzureDevOpsProperties properties { get; set; }
        public object[] tags { get; set; }
        public object[] validationResults { get; set; }
        public AzureDevOpsPlan[] plans { get; set; }
        public AzureDevOpsTriggerInfo triggerInfo { get; set; }
        public int id { get; set; }
        public string buildNumber { get; set; }
        public string status { get; set; }
        public DateTime queueTime { get; set; }
        public DateTime startTime { get; set; }
        public string url { get; set; }
        public AzureDevOpsDefinition definition { get; set; }
        public int buildNumberRevision { get; set; }
        public AzureDevOpsProject project { get; set; }
        public string uri { get; set; }
        public string sourceBranch { get; set; }
        public string sourceVersion { get; set; }
        public AzureDevOpsQueue queue { get; set; }
        public string priority { get; set; }
        public string reason { get; set; }
        public AzureDevOpsRequestedFor requestedFor { get; set; }
        public AzureDevOpsRequestedBy requestedBy { get; set; }
        public DateTime lastChangedDate { get; set; }
        public AzureDevOpsLastChangedBy lastChangedBy { get; set; }
        public AzureDevOpsOrchestrationPlan orchestrationPlan { get; set; }
        public AzureDevOpsLogs logs { get; set; }
        public AzureDevOpsRepository repository { get; set; }
        public bool retainedByRelease { get; set; }
        public object triggeredByBuild { get; set; }
        public bool appendCommitMessageToRunName { get; set; }
        public string result { get; set; }
        public DateTime finishTime { get; set; }
    }
}
