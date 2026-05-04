using SoftwareWorker.BYO.CLI.Abstractions.Attributes;
using SoftwareWorker.BYO.CLI.Core.Service;
using Spectre.Console;

namespace SoftwareWorker.BYO.CLI.Core.Handlers.Commands
{
    [TrunkCommand("commands", "Saved command management")]
    [BranchCommand("read", "List all saved commands")]
    internal class CommandReadHandler : BaseCommandHandler
    {
        public override async Task ExecuteAsync()
        {
            var commands = CommandService.GetList();

            if (commands == null || commands.Count == 0)
            {
                UserInterfaceService.ShowWarning("No commands found. Use 'sw command create' to create a command.");
                return;
            }

            // Group commands by folder path (null/empty = root)
            var grouped = commands
                .OrderBy(c => c.FolderPath ?? string.Empty)
                .ThenBy(c => c.Description)
                .GroupBy(c => FolderNavigationService.NormalizePath(c.FolderPath))
                .OrderBy(g => g.Key);

            foreach (var group in grouped)
            {
                var folderHeader = string.IsNullOrEmpty(group.Key) ? "[grey](root)[/]" : $"[cyan]{Markup.Escape(group.Key)}[/]";
                UserInterfaceService.ShowMarkup($"[bold]📁 {folderHeader}[/]");

                var table = new Table()
                    .Border(TableBorder.Rounded)
                    .BorderColor(Color.Cyan)
                    .AddColumn("[bold]Description[/]")
                    .AddColumn("[bold]Executable[/]")
                    .AddColumn("[bold]Working Directory[/]")
                    .AddColumn("[bold]Created[/]");

                foreach (var command in group)
                {
                    table.AddRow(
                        command.Description,
                        Markup.Escape(command.Executable),
                        command.WorkingDirectory ?? "[grey]-[/]",
                        command.CreatedAt.ToString("yyyy-MM-dd HH:mm")
                    );
                }

                UserInterfaceService.ShowTable(table);
                UserInterfaceService.WriteLine();
            }

            await Task.CompletedTask;
        }
    }
}
