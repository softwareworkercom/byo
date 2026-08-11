using SoftwareWorker.BYO.CLI.Abstractions.Attributes;
using SoftwareWorker.BYO.CLI.Core.Service;

namespace SoftwareWorker.BYO.CLI.Core.Handlers.Commands
{
    [TrunkCommand("commands", "Saved command management")]
    [BranchCommand("delete", "Delete a saved command")]
    internal class CommandDeleteHandler : BaseCommandHandler
    {
        public override async Task ExecuteAsync()
        {
            var commands = CommandService.GetList().ToList();

            if (commands.Count == 0)
            {
                UserInterfaceService.ShowWarning("No saved commands found. Use 'byo commands create' to add a command.");
                return;
            }

            var selectedCommand = FolderNavigationService.NavigateAndSelect(
                commands,
                c => c.Bookmark,
                c => string.IsNullOrWhiteSpace(c.Name) ? c.Executable : c.Name,
                "command to delete");

            if (selectedCommand == null)
            {
                UserInterfaceService.ShowWarning("No command selected.");
                return;
            }

            // Confirm deletion
            var selectedCommandName = string.IsNullOrWhiteSpace(selectedCommand.Name)
                ? selectedCommand.Executable
                : selectedCommand.Name;

            if (!UserInterfaceService.Confirm($"Are you sure you want to delete '{selectedCommandName}'?"))
            {
                UserInterfaceService.ShowWarning("Deletion cancelled.");
                return;
            }

            // Remove the command
            CommandService.Delete(selectedCommand);

            UserInterfaceService.ShowGreen($"Command '{selectedCommandName}' deleted successfully.");

            await Task.CompletedTask;
        }
    }
}
