namespace SoftwareWorker.BYO.Integrations.GitHub.Model
{
    public class GitHubVerification
    {
        public bool verified { get; set; }
        public string reason { get; set; }
        public object signature { get; set; }
        public object payload { get; set; }
    }

}
