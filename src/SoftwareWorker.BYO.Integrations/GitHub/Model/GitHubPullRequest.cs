namespace SoftwareWorker.BYO.Integrations.GitHub.Model
{
    public class GitHubPullRequest
    {
        public string url { get; set; }
        public string repository_url { get; set; }
        public string labels_url { get; set; }
        public string comments_url { get; set; }
        public string events_url { get; set; }
        public string html_url { get; set; }
        public long id { get; set; }
        public string node_id { get; set; }
        public int number { get; set; }
        public string title { get; set; }
        public GitHubUser user { get; set; }
        public object[] labels { get; set; }
        public string state { get; set; }
        public bool locked { get; set; }
        public GitHubAssignee assignee { get; set; }
        public GitHubAssignee[] assignees { get; set; }
        public object milestone { get; set; }
        public int comments { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public object closed_at { get; set; }
        public string author_association { get; set; }
        public object active_lock_reason { get; set; }
        public bool draft { get; set; }
        public GitHubPullRequest pull_request { get; set; }
        public string body { get; set; }
        public GitHubReactions reactions { get; set; }
        public string timeline_url { get; set; }
        public object performed_via_github_app { get; set; }
        public object state_reason { get; set; }
        public float score { get; set; }
    }
}
