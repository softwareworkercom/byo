namespace SoftwareWorker.BYO.Integrations.GitHub.Model
{
    public class GitHubCommit
    {
        public string sha { get; set; }
        public string node_id { get; set; }
        public GitHubCommitInfo commit { get; set; }
        public string url { get; set; }
        public string html_url { get; set; }
        public string comments_url { get; set; }
        public GitHubAuthor author { get; set; }
        public GitHubCommitter committer { get; set; }
        public GitHubParent[] parents { get; set; }
    }

}
