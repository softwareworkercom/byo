namespace SoftwareWorker.BYO.CLI.Abstractions.Model.Command
{
    public class TrunkCommand : CommandBase
    {
        public BranchCommand[]? BranchCommands { get; set; }
    }
}
