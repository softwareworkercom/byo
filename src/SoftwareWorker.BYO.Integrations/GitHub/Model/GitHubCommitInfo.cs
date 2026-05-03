using SoftwareWorker.BYO.Integrations.GitHub.Model;

public class GitHubCommitInfo
{
    public GitHubAuthor author { get; set; }
    public GitHubCommitter committer { get; set; }
    public string message { get; set; }
    public GitHubTree tree { get; set; }
    public string url { get; set; }
    public int comment_count { get; set; }
    public GitHubVerification verification { get; set; }
}
