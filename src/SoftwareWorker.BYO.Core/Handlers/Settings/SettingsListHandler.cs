using SoftwareWorker.BYO.CLI.Abstractions.Attributes;
using SoftwareWorker.BYO.CLI.Core.Service;
using Spectre.Console;

namespace SoftwareWorker.BYO.CLI.Core.Handlers.Settings
{
    [TrunkCommand("settings", "Settings operations")]
    [BranchCommand("list", "Read settings")]
    public class SettingsListHandler : BaseCommandHandler
    {
        public override async Task ExecuteAsync()
        {
            var settingsTable = new Table();
            settingsTable.Title = new TableTitle("Settings");
            settingsTable.AddColumn("Name");
            settingsTable.AddColumn("Value");

            var settings = SettingsService.GetList() ?? new Dictionary<string, string>();

            if (settings.Count == 0)
            {
                UserInterfaceService.ShowWarning("No settings found. Use 'byo settings set --key <key> --value <value>' to add one.");
                return;
            }

            foreach (var item in settings.OrderBy(c => c.Key))
            {
                settingsTable.AddRow(
                    Markup.Escape(item.Key),
                    Markup.Escape(item.Value ?? string.Empty)
                );
            }

            UserInterfaceService.ShowTable(settingsTable);
        }
    }
}
