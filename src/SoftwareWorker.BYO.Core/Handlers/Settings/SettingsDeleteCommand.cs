using SoftwareWorker.BYO.CLI.Abstractions.Attributes;
using SoftwareWorker.BYO.CLI.Core.Service;

namespace SoftwareWorker.BYO.CLI.Core.Handlers.Settings
{
    [TrunkCommand("settings", "Settings operations")]
    [BranchCommand("delete", "Delete a setting")]
    [Parameter("key", "The key to delete", true, null)]
    public class SettingsDeleteCommand : BaseCommandHandler
    {
        public string? Key { get; set; }

        public override async Task ExecuteAsync()
        {
            if (string.IsNullOrWhiteSpace(Key))
            {
                UserInterfaceService.ShowError("Key is required for delete.");
                return;
            }

            SettingsService.Delete(Key);
            Console.WriteLine($"Deleted setting with Key={Key}");
        }
    }
}
