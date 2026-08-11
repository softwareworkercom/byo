namespace SoftwareWorker.BYO.Integrations.Confluence.Model
{
    public class ConfluencePageUpdateRequest
    {
        public string Type { get; set; } = "page";
        public string Title { get; set; }
        public ConfluencePageBody Body { get; set; }
        public ConfluencePageVersion Version { get; set; }
    }
}
