using SoftwareWorker.BYO.CLI.Abstractions.Attributes;
using SoftwareWorker.BYO.CLI.Core.Service;

namespace SoftwareWorker.BYO.CLI.Core.Handlers.Commands
{
    [TrunkCommand("commands", "Saved command management")]
    [BranchCommand("run", "Run a saved command")]
    [Parameter("name", "Command name to run.", true, null)]
    internal class CommandRunHandler : BaseCommandHandler
    {
        public string? Name { get; set; }

        public override async Task ExecuteAsync()
        {
            var allCommands = CommandService.GetList().ToList();

            if (allCommands.Count == 0)
            {
                UserInterfaceService.ShowWarning("No saved commands found. Use 'byo commands create' to add a command.");
                return;
            }

            if (string.IsNullOrWhiteSpace(Name))
            {
                UserInterfaceService.ShowError("Command name is required.");
                return;
            }

            var selectedCommand = allCommands.FirstOrDefault(c =>
                (!string.IsNullOrWhiteSpace(c.Name) && c.Name.Equals(Name, StringComparison.OrdinalIgnoreCase)) ||
                (string.IsNullOrWhiteSpace(c.Name) && c.Executable.Equals(Name, StringComparison.OrdinalIgnoreCase)));

            if (selectedCommand == null)
            {
                UserInterfaceService.ShowError($"Command '{Name}' not found.");
                return;
            }

            var resolvedExecutable = TokenService.ResolveTokens(selectedCommand.Executable);

            UserInterfaceService.ShowGreen("Executing command...");

            TerminalService.Run(
                resolvedExecutable,
                selectedCommand.Directory,
                shell: selectedCommand.Shell);

            await Task.CompletedTask;
        }
    }
}

