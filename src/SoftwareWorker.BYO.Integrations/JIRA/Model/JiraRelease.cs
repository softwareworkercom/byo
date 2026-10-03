namespace SoftwareWorker.BYO.Integrations.JIRA.Model
{
    public class JiraRelease
    {
        public string self { get; set; }
        public string id { get; set; }
        public string description { get; set; }
        public string name { get; set; }
        public bool archived { get; set; }
        public bool released { get; set; }
        public string startDate { get; set; }
        public string releaseDate { get; set; }
        public string userStartDate { get; set; }
        public string userReleaseDate { get; set; }
        public int projectId { get; set; }
    }
}
