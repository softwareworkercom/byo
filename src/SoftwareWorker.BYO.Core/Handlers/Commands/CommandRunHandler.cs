using SoftwareWorker.BYO.CLI.Abstractions.Attributes;
using SoftwareWorker.BYO.CLI.Core.Service;

namespace SoftwareWorker.BYO.CLI.Core.Handlers.Commands
{
    [TrunkCommand("commands", "Saved command management")]
    [BranchCommand("run", "Run a saved command")]
    [Parameter("name", "Command name to run.", true, null)]
    [Parameter("bookmark", "Bookmark hierarchy path (e.g. DevOps/Deploy)", true, null)]
    internal class CommandRunHandler : BaseCommandHandler
    {
        public string? Name { get; set; }
        public string? Bookmark { get; set; }

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

            if (string.IsNullOrWhiteSpace(Bookmark))
            {
                UserInterfaceService.ShowError("Bookmark is required.");
                return;
            }

            var normalizedBookmark = FolderNavigationService.NormalizePath(Bookmark);

            var selectedCommand = allCommands.FirstOrDefault(c =>
                FolderNavigationService.NormalizePath(c.Bookmark).Equals(normalizedBookmark, StringComparison.OrdinalIgnoreCase) &&
                (!string.IsNullOrWhiteSpace(c.Name) && c.Name.Equals(Name, StringComparison.OrdinalIgnoreCase)) ||
                (FolderNavigationService.NormalizePath(c.Bookmark).Equals(normalizedBookmark, StringComparison.OrdinalIgnoreCase) &&
                 string.IsNullOrWhiteSpace(c.Name) &&
                 c.Executable.Equals(Name, StringComparison.OrdinalIgnoreCase)));

            if (selectedCommand == null)
            {
                UserInterfaceService.ShowError($"Command '{Name}' not found in bookmark '{normalizedBookmark}'.");
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

