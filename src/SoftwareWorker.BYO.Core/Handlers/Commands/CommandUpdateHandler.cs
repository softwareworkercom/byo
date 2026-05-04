using SoftwareWorker.BYO.CLI.Abstractions.Attributes;
using SoftwareWorker.BYO.CLI.Core.Service;
using SoftwareWorker.BYO.Core.Model.Enums;

namespace SoftwareWorker.BYO.CLI.Core.Handlers.Commands
{
    [TrunkCommand("commands", "Saved command management")]
    [BranchCommand("update", "Update an existing saved command")]
    [Parameter("name", "Command name to update", false, null)]
    [Parameter("newname", "New command name", false, null)]
    [Parameter("executable", "New command executable", false, null)]
    [Parameter("directory", "New working directory", false, null)]
    [Parameter("shell", "New shell type", false, "PowerShell|Cmd|Wsl")]
    [Parameter("bookmark", "New bookmark hierarchy path (e.g. DevOps/Deploy)", false, null)]
    internal class CommandUpdateHandler : BaseCommandHandler
    {
        public string? Name { get; set; }
        public string? NewName { get; set; }
        public string? Executable { get; set; }
        public string? Directory { get; set; }
        public string? Shell { get; set; }
        public string? Bookmark { get; set; }

        public override async Task ExecuteAsync()
        {
            var targetName = Name;

            // If name is not provided, use folder navigation for interactive selection
            if (string.IsNullOrEmpty(targetName))
            {
                var allCommands = CommandService.GetList().ToList();

                if (allCommands.Count == 0)
                {
                    UserInterfaceService.ShowWarning("No commands found. Use 'byo commands create' to create a command.");
                    return;
                }

                var selectedCommand = FolderNavigationService.NavigateAndSelect(
                    allCommands,
                    c => c.Bookmark,
                    c => c.Name,
                    "command to update");

                if (selectedCommand == null)
                {
                    UserInterfaceService.ShowWarning("No command selected.");
                    return;
                }

                targetName = selectedCommand.Name;
            }

            // Check if at least one update field is provided
            if (string.IsNullOrEmpty(NewName) &&
                string.IsNullOrEmpty(Executable) &&
                string.IsNullOrEmpty(Directory) &&
                string.IsNullOrEmpty(Shell) &&
                string.IsNullOrEmpty(Bookmark))
            {
                UserInterfaceService.ShowWarning("No update fields provided. Please specify at least one field to update.");
                return;
            }

            // Parse shell string to enum if provided
            ShellTypeEnum? shell = null;
            if (!string.IsNullOrEmpty(Shell))
            {
                if (Enum.TryParse<ShellTypeEnum>(Shell, true, out var parsedShell))
                {
                    shell = parsedShell;
                }
                else
                {
                    UserInterfaceService.ShowError($"Error: Invalid shell type '{Shell}'. Valid options: PowerShell, Cmd, Wsl");
                    return;
                }
            }

            var updatedCommand = CommandService.Update(targetName, NewName, Executable, Directory, shell, Bookmark);

            if (updatedCommand == null)
            {
                UserInterfaceService.ShowError($"Error: Command with name '{Name}' not found.");
                return;
            }

            UserInterfaceService.ShowGreen($"Command '{targetName}' updated successfully.");
            await Task.CompletedTask;
        }
    }
}
