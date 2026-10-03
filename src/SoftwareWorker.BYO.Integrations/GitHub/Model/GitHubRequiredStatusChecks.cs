namespace SoftwareWorker.BYO.Integrations.GitHub.Model
{

    public class GitHubRequiredStatusChecks
    {
        public string enforcement_level { get; set; }
        public object[] contexts { get; set; }
        public object[] checks { get; set; }
    }

}
