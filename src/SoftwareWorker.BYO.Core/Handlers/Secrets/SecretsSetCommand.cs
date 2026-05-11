using SoftwareWorker.BYO.CLI.Abstractions.Attributes;
using SoftwareWorker.BYO.CLI.Core.Service;
using Spectre.Console;

namespace SoftwareWorker.BYO.CLI.Core.Handlers.Secrets
{
    [TrunkCommand("secrets", "Encrypted secrets operations")]
    [BranchCommand("set", "Set a secret")]
    [Parameter("key", "The key to update", true, null)]
    [Parameter("value", "The new value", true, null)]
    public class SecretsSetCommand : BaseCommandHandler
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

            var secrets = SecretsService.GetList() ?? new Dictionary<string, string>();
            if (secrets.TryGetValue(Key, out var currentValue))
            {
                var shouldReplace = UserInterfaceService.Confirm(
                    $"Secret '[cyan]{Markup.Escape(Key)}[/]' already exists with current value '[grey]{Markup.Escape(currentValue)}[/]'. Do you want to replace it?");

                if (!shouldReplace)
                {
                    UserInterfaceService.ShowWarning("Secret update cancelled.");
                    return;
                }
            }

            var result = SecretsService.Update(Key, Value);

            if (result is not null)
            {
                UserInterfaceService.ShowGreen($"Key={Key}, Value={result}");
            }
        }
    }
}
