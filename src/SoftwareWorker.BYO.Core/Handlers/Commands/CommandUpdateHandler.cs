using SoftwareWorker.BYO.CLI.Abstractions.Attributes;
using SoftwareWorker.BYO.CLI.Core.Service;
using SoftwareWorker.BYO.Core.Model.Enums;

namespace SoftwareWorker.BYO.CLI.Core.Handlers.Commands
{
    [TrunkCommand("commands", "Saved Commands")]
    [BranchCommand("update", "Update an existing command")]
    [Parameter("description", "Command description to update", false, null)]
    [Parameter("newdescription", "New command description", false, null)]
    [Parameter("executable", "New command executable", false, null)]
    [Parameter("folder", "New working directory", false, null)]
    [Parameter("shell", "New shell type", false, "PowerShell|Cmd|Wsl")]
    [Parameter("path", "New folder hierarchy path (e.g. DevOps/Deploy)", false, null)]
    internal class CommandUpdateHandler : BaseCommandHandler
    {
        public string? Description { get; set; }
        public string? NewDescription { get; set; }
        public string? Executable { get; set; }
        public string? Folder { get; set; }
        public string? Shell { get; set; }
        public string? Path { get; set; }

        public override async Task ExecuteAsync()
        {
            var targetDescription = Description;

            // If description is not provided, use folder navigation for interactive selection
            if (string.IsNullOrEmpty(targetDescription))
            {
                var allCommands = CommandService.GetList().ToList();

                if (allCommands.Count == 0)
                {
                    UserInterfaceService.ShowWarning("No commands found. Use 'sw command add' to create a command.");
                    return;
                }

                var selectedCommand = FolderNavigationService.NavigateAndSelect(
                    allCommands,
                    c => c.FolderPath,
                    c => c.Description,
                    "command to update");

                if (selectedCommand == null)
                {
                    UserInterfaceService.ShowWarning("No command selected.");
                    return;
                }

                targetDescription = selectedCommand.Description;
            }

            // Check if at least one update field is provided
            if (string.IsNullOrEmpty(NewDescription) &&
                string.IsNullOrEmpty(Executable) &&
                string.IsNullOrEmpty(Folder) &&
                string.IsNullOrEmpty(Shell) &&
                string.IsNullOrEmpty(Path))
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

            var updatedCommand = CommandService.Update(targetDescription, NewDescription, Executable, Folder, shell, Path);

            if (updatedCommand == null)
            {
                UserInterfaceService.ShowError($"Error: Command with description '{Description}' not found.");
                return;
            }

            UserInterfaceService.ShowGreen($"Command '{targetDescription}' updated successfully.");
            await Task.CompletedTask;
        }
    }
}
