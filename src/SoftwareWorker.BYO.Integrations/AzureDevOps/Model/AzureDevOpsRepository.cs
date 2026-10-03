namespace SoftwareWorker.BYO.Integrations.AzureDevOps.Model
{
    public class AzureDevOpsRepository
    {
        public string fullName { get; set; }
        public AzureDevOpsConnection connection { get; set; }
        public string type { get; set; }
        public string id { get; set; }
        public object clean { get; set; }
        public bool checkoutSubmodules { get; set; }
    }
}
