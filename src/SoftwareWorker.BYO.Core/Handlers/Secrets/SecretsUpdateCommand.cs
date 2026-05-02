using SoftwareWorker.BYO.CLI.Abstractions.Attributes;
using SoftwareWorker.BYO.CLI.Core.Service;

namespace SoftwareWorker.BYO.CLI.Core.Handlers.Secrets
{
    [TrunkCommand("secrets", "Encrypted secrets operations")]
    [BranchCommand("update", "Update an encrypted secret")]
    [Parameter("key", "The key to update", true, null)]
    [Parameter("value", "The new value", true, null)]
    public class SecretsUpdateCommand : BaseCommandHandler
    {
        public string? Key { get; set; }
        public string? Value { get; set; }

        public override async Task ExecuteAsync()
        {
            if (string.IsNullOrEmpty(Key) || string.IsNullOrEmpty(Value))
            {
                UserInterfaceService.ShowError("Key and Value are required parameters.");
                return;
            }

            var result = SecretsService.Update(Key, Value);

            if (result is not null)
            {
                Console.WriteLine($"Secret Saved: Key={Key}, Value={result}");
            }
        }
    }
}
