namespace SoftwareWorker.BYO.CLI.Abstractions.Model.Command
{
    public abstract class CommandBase
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string? Handler { get; set; }
        public Parameter[]? Parameters { get; set; }
    }
}
