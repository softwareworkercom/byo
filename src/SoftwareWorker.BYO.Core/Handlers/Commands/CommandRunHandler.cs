using SoftwareWorker.BYO.CLI.Abstractions.Attributes;
using SoftwareWorker.BYO.CLI.Core.Service;

namespace SoftwareWorker.BYO.CLI.Core.Handlers.Commands
{
    [TrunkCommand("commands", "Saved command management")]
    [BranchCommand("run", "Run a saved command")]
    internal class CommandRunHandler : BaseCommandHandler
    {
        public override async Task ExecuteAsync()
        {
            var allCommands = CommandService.GetList().ToList();

            if (allCommands.Count == 0)
            {
                UserInterfaceService.ShowWarning("No saved commands found. Use 'sw commands create' to add a command.");
                return;
            }

            var selectedCommand = FolderNavigationService.NavigateAndSelect(
                allCommands,
                c => c.FolderPath,
                c => string.IsNullOrWhiteSpace(c.Description) ? c.Executable : c.Description,
                "command to run");

            if (selectedCommand == null)
            {
                UserInterfaceService.ShowWarning("No command selected.");
                return;
            }

            var resolvedExecutable = TokenService.ResolveTokens(selectedCommand.Executable);

            UserInterfaceService.ShowGreen("Executing command...");

            TerminalService.Run(
                resolvedExecutable,
                selectedCommand.WorkingDirectory,
                shell: selectedCommand.Shell);

            await Task.CompletedTask;
        }
    }
}

