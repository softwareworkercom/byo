using SoftwareWorker.BYO.CLI.Abstractions.Attributes;
using SoftwareWorker.BYO.CLI.Core.Service;
using SoftwareWorker.BYO.Core.Model.Enums;

namespace SoftwareWorker.BYO.CLI.Core.Handlers.Commands
{
    [TrunkCommand("commands", "Saved Commands")]
    [BranchCommand("create", "Create Command")]
    [Parameter("description", "Command Description", true, null)]
    [Parameter("executable", "Command Executable (use {{tokenName}} for tokens resolved from configuration)", true, null)]
    [Parameter("folder", "Working Directory", false, null)]
    [Parameter("shell", "Shell Type", false, "PowerShell|Cmd|Wsl")]
    [Parameter("path", "Folder hierarchy path (e.g. DevOps/Deploy)", false, null)]
    internal class CommandCreateHandler : BaseCommandHandler
    {
        public string? Description { get; set; }
        public string? Executable { get; set; }
        public string? Folder { get; set; }
        public string? Shell { get; set; }
        public string? Path { get; set; }

        public override async Task ExecuteAsync()
        {
            if (string.IsNullOrEmpty(Description))
            {
                UserInterfaceService.ShowError("Command description is required");
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

            CommandService.Create(Description, Executable, Folder, shell, Path);

            UserInterfaceService.ShowGreen("Command added successfully!");
            await Task.CompletedTask;
        }
    }
}
