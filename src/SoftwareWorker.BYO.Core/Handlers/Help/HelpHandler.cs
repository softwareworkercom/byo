using SoftwareWorker.BYO.CLI.Abstractions.Attributes;
using SoftwareWorker.BYO.CLI.Abstractions.Model.Command;
using SoftwareWorker.BYO.CLI.Core.Engine;
using SoftwareWorker.BYO.CLI.Core.Service;
using Spectre.Console;

namespace SoftwareWorker.BYO.CLI.Core.Handlers.Help
{
    [TrunkCommand("help", "Lists all the commands available in the CLI")]
    internal class HelpHandler : BaseCommandHandler
    {
        public override async Task ExecuteAsync()
        {
            ShowCliCommandsTable();
            ShowSavedCommandsTable();
            ShowWorkflowsTable();

            await Task.CompletedTask;
        }

        private static void ShowCliCommandsTable()
        {
            var trunkCommands = CommandsScanner.BuildFromReflection();

            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Cyan)
                .Title("[bold cyan]CLI Commands[/]")
                .AddColumn("[bold]Command[/]")
                .AddColumn("[bold]Description[/]")
                .AddColumn("[bold]Parameters[/]");

            foreach (var trunk in trunkCommands)
            {
                if (trunk.BranchCommands == null || trunk.BranchCommands.Length == 0)
                {
                    AddCommandRow(table, $"sw {trunk.Name}", trunk.Description, trunk.Parameters);
                }
                else
                {
                    foreach (var branch in trunk.BranchCommands)
                    {
                        if (branch.LeafCommands != null && branch.LeafCommands.Length > 0)
                        {
                            foreach (var leaf in branch.LeafCommands)
                            {
                                AddCommandRow(table, $"sw {trunk.Name} {branch.Name} {leaf.Name}", leaf.Description, leaf.Parameters);
                            }
                        }
                        else
                        {
                            AddCommandRow(table, $"sw {trunk.Name} {branch.Name}", branch.Description, branch.Parameters);
                        }
                    }
                }
            }

            UserInterfaceService.ShowTable(table);
        }

        private static void ShowSavedCommandsTable()
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

            foreach (var command in commands.OrderBy(c => c.Description))
            {
                table.AddRow(
                    Markup.Escape(command.Description ?? string.Empty),
                    Markup.Escape(command.Executable ?? string.Empty),
                    command.WorkingDirectory != null ? Markup.Escape(command.WorkingDirectory) : "[grey]-[/]"
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

        private static void AddCommandRow(Table table, string command, string description, Parameter[]? parameters)
        {
            var paramList = FormatParameters(parameters);

            table.AddRow(
                $"[cyan]{Markup.Escape(command)}[/]",
                Markup.Escape(description ?? string.Empty),
                paramList
            );
        }

        private static string FormatParameters(Parameter[]? parameters)
        {
            if (parameters == null || parameters.Length == 0)
                return "[grey]-[/]";

            return string.Join("\n", parameters.Select(p =>
                p.IsRequired
                    ? $"[white]--{Markup.Escape(p.Name)}[/] [red](required)[/] {Markup.Escape(p.Description ?? string.Empty)}"
                    : $"[grey]--{Markup.Escape(p.Name)}[/] {Markup.Escape(p.Description ?? string.Empty)}"));
        }
    }
}
