namespace SoftwareWorker.BYO.Integrations.GitHub.Model
{
    public class GitHubBranch
    {
        public string name { get; set; }
        public GitHubCommit commit { get; set; }
        public GitHubLinks _links { get; set; }
        public bool _protected { get; set; }
        public GitHubProtection protection { get; set; }
        public string protection_url { get; set; }
    }

}
