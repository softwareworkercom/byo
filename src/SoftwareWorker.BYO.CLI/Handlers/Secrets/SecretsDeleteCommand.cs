using SoftwareWorker.BYO.SDK.Abstractions.Attributes;
using SoftwareWorker.BYO.SDK.Service;

namespace SoftwareWorker.BYO.SDK.Handlers.Secrets
{
    [TrunkCommand("secrets", "Encrypted secrets operations")]
    [BranchCommand("delete", "Delete a secret")]
    [Parameter("key", "The key to delete", true, null)]
    public class SecretsDeleteCommand : BaseCommandHandler
    {
        public string? Key { get; set; }

        public override async Task ExecuteAsync()
        {
            if (string.IsNullOrWhiteSpace(Key))
            {
                UserInterfaceService.ShowError("Key is required for delete.");
                return;
            }

            SecretsService.Delete(Key);
            Console.WriteLine($"Deleted secret with Key={Key}");
        }
    }
}
