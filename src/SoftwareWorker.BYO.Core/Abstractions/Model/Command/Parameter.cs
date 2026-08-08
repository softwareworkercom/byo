namespace SoftwareWorker.BYO.CLI.Abstractions.Model.Command
{
    public class Parameter
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public object? DefaultValue { get; set; }
        public bool IsRequired { get; set; }
    }
}
