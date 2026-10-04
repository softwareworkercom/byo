using SoftwareWorker.BYO.SDK.Abstractions.Attributes;
using SoftwareWorker.BYO.SDK;
using SoftwareWorker.BYO.SDK.Services;
using SoftwareWorker.BYO.SDK.Constants;

namespace SoftwareWorker.BYO.CLI.Handlers.Alias
{
    [TrunkCommand("alias", "Shell alias management")]
    [BranchCommand("delete", "Remove the shell alias for byo")]
    internal class AliasDeleteHandler : BaseCommandHandler
    {
        public override async Task ExecuteAsync()
        {
            var currentAlias = SettingsService.Get(SystemConstants.SYSTEM_Alias, showErrorIfNotFound: false);

            if (string.IsNullOrWhiteSpace(currentAlias))
            {
                UserInterfaceService.ShowWarning("No alias is currently set.");
                return;
            }

            UserInterfaceService.ShowGrey($"Current alias: '{currentAlias}'");

            if (!UserInterfaceService.Confirm($"Are you sure you want to remove the alias '{currentAlias}'?"))
            {
                UserInterfaceService.ShowWarning("Alias removal cancelled.");
                return;
            }

            // Remove from settings
            SettingsService.Delete(SystemConstants.SYSTEM_Alias);

            UserInterfaceService.ShowGreen($"Alias '{currentAlias}' removed from settings.");
            UserInterfaceService.ShowGrey("Note: You may need to manually remove the alias from your shell profile files:");
            UserInterfaceService.ShowGrey("  - PowerShell: $PROFILE");
            UserInterfaceService.ShowGrey("  - bash: ~/.bashrc");
            UserInterfaceService.ShowGrey("  - zsh: ~/.zshrc");
            UserInterfaceService.ShowGrey("  - fish: ~/.config/fish/config.fish");
            UserInterfaceService.ShowGrey("  - cmd (Windows): %USERPROFILE%\\.byo_aliases.doskey");

            await Task.CompletedTask;
        }
    }
}
