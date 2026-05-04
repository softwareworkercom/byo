using SoftwareWorker.BYO.CLI.Abstractions.Attributes;
using SoftwareWorker.BYO.CLI.Abstractions.Model.Command;
using SoftwareWorker.BYO.CLI.Core.Engine;
using SoftwareWorker.BYO.CLI.Core.Service;
using Spectre.Console;

namespace SoftwareWorker.BYO.CLI.Core.Handlers.Help
{
    [TrunkCommand("list", "Lists all the commands available in the CLI")]
    internal class HelpHandler : BaseCommandHandler
    {
        public override async Task ExecuteAsync()
        {
            ShowCommandsTable();
            ShowWorkflowsTable();

            await Task.CompletedTask;
        }

        private static void ShowCommandsTable()
        {
            var commands = CommandService.GetList();

            if (commands == null || commands.Count == 0)
                return;

            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Yellow)
                .Title("[bold yellow]Saved Commands[/]")
                .AddColumn("[bold]Description[/]")
                .AddColumn("[bold]Executable[/]")
                .AddColumn("[bold]Working Directory[/]");

            foreach (var command in commands.OrderBy(c => c.Name))
            {
                table.AddRow(
                    Markup.Escape(command.Name ?? string.Empty),
                    Markup.Escape(command.Executable ?? string.Empty),
                    command.Directory != null ? Markup.Escape(command.Directory) : "[grey]-[/]"
                );
            }

            UserInterfaceService.ShowTable(table);
        }

        private static void ShowWorkflowsTable()
        {
            var workflows = WorkflowService.GetList();

            if (workflows == null || workflows.Count == 0)
                return;

            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Green)
                .Title("[bold green]Workflows[/]")
                .AddColumn("[bold]Name[/]")
                .AddColumn("[bold]Description[/]")
                .AddColumn("[bold]Steps[/]");

            foreach (var workflow in workflows.OrderBy(w => w.Name))
            {
                table.AddRow(
                    Markup.Escape(workflow.Name),
                    workflow.Description != null ? Markup.Escape(workflow.Description) : "[grey]-[/]",
                    workflow.Steps.Count.ToString()
                );
            }

            UserInterfaceService.ShowTable(table);
        }
    }
}
