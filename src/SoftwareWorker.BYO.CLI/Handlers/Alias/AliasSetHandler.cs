using SoftwareWorker.BYO.SDK.Abstractions.Attributes;
using SoftwareWorker.BYO.SDK;
using SoftwareWorker.BYO.SDK.Services;
using SoftwareWorker.BYO.SDK.Constants;
using SoftwareWorker.BYO.CLI.Service;

namespace SoftwareWorker.BYO.CLI.Handlers.Alias
{
    [TrunkCommand("alias", "Shell alias management")]
    [BranchCommand("set", "Set or change the shell alias for byo")]
    internal class AliasSetHandler : BaseCommandHandler
    {
        public override async Task ExecuteAsync()
        {
            var currentAlias = SettingsService.Get(SystemConstants.SYSTEM_Alias, showErrorIfNotFound: false);

            if (!string.IsNullOrWhiteSpace(currentAlias))
            {
                UserInterfaceService.ShowGrey($"Current alias: '{currentAlias}'");
            }

            var alias = AliasService.AskAliasByUser();

            if (string.IsNullOrEmpty(alias))
            {
                UserInterfaceService.ShowWarning("Alias setting cancelled.");
                return;
            }

            // Check if trying to set the same alias
            if (!string.IsNullOrWhiteSpace(currentAlias) && 
                currentAlias.Equals(alias, StringComparison.OrdinalIgnoreCase))
            {
                UserInterfaceService.ShowGrey($"Alias '{alias}' is already set.");
                return;
            }

            // Update settings
            if (SettingsService.Update(SystemConstants.SYSTEM_Alias, alias) == null)
            {
                UserInterfaceService.ShowError("Failed to save alias to settings.");
                return;
            }

            // Deploy to shells
            AliasService.DeployAlias(alias);

            await Task.CompletedTask;
        }
    }
}
