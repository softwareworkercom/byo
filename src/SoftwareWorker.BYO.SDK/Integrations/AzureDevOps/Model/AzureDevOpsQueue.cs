namespace SoftwareWorker.BYO.Integrations.AzureDevOps.Model
{
    public class AzureDevOpsQueue
    {
        public int id { get; set; }
        public string name { get; set; }
        public AzureDevOpsPool pool { get; set; }
    }
}
