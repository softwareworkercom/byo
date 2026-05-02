using SoftwareWorker.BYO.CLI.Abstractions.Attributes;
using SoftwareWorker.BYO.CLI.Core.Service;
using Spectre.Console;

namespace SoftwareWorker.BYO.CLI.Core.Handlers.Settings
{
    [TrunkCommand("settings", "Settings operations")]
    [BranchCommand("update", "Update a setting")]
    [Parameter("key", "The key to update", true, null)]
    [Parameter("value", "The new value", true, null)]
    public class SettingsUpdateCommand : BaseCommandHandler
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
                    UserInterfaceService.ShowWarning("No settings found.");
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

            var result = SettingsService.Update(Key, Value);

            if (result is not null)
            {
                UserInterfaceService.ShowGreen($"Setting Saved: Key={Key}, Value={result}");
            }

            await Task.CompletedTask;
        }
    }
}
