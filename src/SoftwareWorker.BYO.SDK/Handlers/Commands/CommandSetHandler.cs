using SoftwareWorker.BYO.CLI.Abstractions.Attributes;
using SoftwareWorker.BYO.CLI.Core.Service;
using SoftwareWorker.BYO.Core.Model.Enums;

namespace SoftwareWorker.BYO.CLI.Core.Handlers.Commands
{
    [TrunkCommand("commands", "Saved command management")]
    [BranchCommand("set", "Create a new saved command")]
    [Parameter("name", "Command name", true, null)]
    [Parameter("bookmark", "Bookmark hierarchy path (e.g. DevOps/Deploy)", false, null)]
    [Parameter("executable", "Command executable (use {{tokenName}} for tokens resolved from configuration)", true, null)]
    [Parameter("shell", "Shell type", false, "PowerShell|Cmd|Wsl")]
    [Parameter("directory", "Working directory", false, null)]
    internal class CommandSetHandler : BaseCommandHandler
    {
        public string? Name { get; set; }
        public string? Executable { get; set; }
        public string? Directory { get; set; }
        public string? Shell { get; set; }
        public string? Bookmark { get; set; }

        public override async Task ExecuteAsync()
        {
            if (string.IsNullOrEmpty(Name))
            {
                UserInterfaceService.ShowError("Command name is required");
                return;
            }

            if (string.IsNullOrEmpty(Executable))
            {
                UserInterfaceService.ShowError("Command executable is required");
                return;
            }

            // Parse shell string to enum
            ShellTypeEnum? shell = null;
            if (!string.IsNullOrEmpty(Shell) && Enum.TryParse<ShellTypeEnum>(Shell, true, out var parsedShell))
            {
                shell = parsedShell;
            }

            var existingCommand = CommandService.GetList().FirstOrDefault(c =>
                c.Name.Equals(Name, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(
                    FolderNavigationService.NormalizePath(c.Bookmark),
                    FolderNavigationService.NormalizePath(Bookmark),
                    StringComparison.OrdinalIgnoreCase));

            if (existingCommand != null)
            {
                if (!UserInterfaceService.Confirm($"A command named '{Name}' already exists in bookmark '{(string.IsNullOrWhiteSpace(Bookmark) ? "/" : Bookmark)}'. Do you want to update all other properties?"))
                {
                    UserInterfaceService.ShowWarning("Command update cancelled.");
                    return;
                }

                CommandService.UpdateByNameAndBookmark(Name, Bookmark, Executable, Directory, shell);
                UserInterfaceService.ShowGreen("Command updated successfully!");
                await Task.CompletedTask;
                return;
            }

            CommandService.Create(Name, Executable, Directory, shell, Bookmark);
            UserInterfaceService.ShowGreen("Command added successfully!");
            await Task.CompletedTask;
        }
    }
}
