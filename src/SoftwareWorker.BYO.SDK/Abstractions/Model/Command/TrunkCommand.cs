namespace SoftwareWorker.BYO.SDK.Abstractions.Model.Command
{
    public class TrunkCommand : CommandBase
    {
        public BranchCommand[]? BranchCommands { get; set; }
    }
}
