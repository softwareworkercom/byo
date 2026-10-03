namespace SoftwareWorker.BYO.SDK.Abstractions.Model.Command
{
    public class BranchCommand : CommandBase
    {
        public LeafCommand[]? LeafCommands { get; set; }
    }
}
