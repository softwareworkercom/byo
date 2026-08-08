namespace SoftwareWorker.BYO.Integrations.GitHub.Model
{
    public class GitHubPullRequests
    {
        public int total_count { get; set; }
        public bool incomplete_results { get; set; }
        public GitHubPullRequest[] items { get; set; }
    }
}
