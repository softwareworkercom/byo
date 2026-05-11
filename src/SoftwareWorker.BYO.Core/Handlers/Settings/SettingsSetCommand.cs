using SoftwareWorker.BYO.CLI.Abstractions.Attributes;
using SoftwareWorker.BYO.CLI.Core.Service;
using Spectre.Console;

namespace SoftwareWorker.BYO.CLI.Core.Handlers.Settings
{
    [TrunkCommand("settings", "Settings operations")]
    [BranchCommand("set", "Set a setting")]
    [Parameter("key", "The key to update", true, null)]
    [Parameter("value", "The new value", true, null)]
    public class SettingsSetCommand : BaseCommandHandler
    {
        public string? Key { get; set; }
        public string? Value { get; set; }

        public override async Task ExecuteAsync()
        {
            var settings = SettingsService.GetList() ?? new Dictionary<string, string>();

            if (string.IsNullOrWhiteSpace(Key))
            {
                if (settings.Count == 0)
                {
                    UserInterfaceService.ShowWarning("No settings found. Use 'byo settings update --key <key> --value <value>' to add one.");
                    return;
                }

                Key = UserInterfaceService.SelectSingleItem(
                    "setting to update",
                    settings.Keys.OrderBy(k => k).ToList(),
                    key => $"{key} [grey]= {Markup.Escape(settings[key])}[/]");
            }

            if (string.IsNullOrEmpty(Value))
            {
                Value = UserInterfaceService.Ask<string>($"New value for [cyan]{Key}[/]:");
            }

            if (settings.TryGetValue(Key!, out var currentValue))
            {
                var shouldReplace = UserInterfaceService.Confirm(
                    $"Setting '[cyan]{Markup.Escape(Key!)}[/]' already exists with current value '[grey]{Markup.Escape(currentValue)}[/]'. Do you want to replace it?");

                if (!shouldReplace)
                {
                    UserInterfaceService.ShowWarning("Setting update cancelled.");
                    return;
                }
            }

            var result = SettingsService.Update(Key, Value);

            if (result is not null)
            {
                UserInterfaceService.ShowGreen($"Key={Key}, Value={result}");
            }

            await Task.CompletedTask;
        }
    }
}
