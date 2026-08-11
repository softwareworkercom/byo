namespace SoftwareWorker.BYO.CLI.Abstractions.Model.Command
{
    public class BranchCommand : CommandBase
    {
        public LeafCommand[]? LeafCommands { get; set; }
    }
}
