using SoftwareWorker.BYO.CLI.Abstractions.Attributes;
using SoftwareWorker.BYO.CLI.Core.Helpers;
using SoftwareWorker.BYO.CLI.Core.Model;
using SoftwareWorker.BYO.CLI.Core.Service;
using Spectre.Console;

namespace SoftwareWorker.BYO.CLI.Core.Handlers.Workflows
{
    [TrunkCommand("workflows", "Workflow management")]
    [BranchCommand("list", "List all workflows")]
    internal class WorkflowListHandler : BaseCommandHandler
    {
        public override async Task ExecuteAsync()
        {
            var workflows = WorkflowService.GetList();

            if (workflows == null || workflows.Count == 0)
            {
                UserInterfaceService.ShowWarning("No workflows found. Use 'byo workflows create' to create a workflow.");
                return;
            }

            // Group workflows by folder path (null/empty = root)
            var grouped = workflows
                .OrderBy(w => w.Bookmark ?? string.Empty)
                .ThenBy(w => w.Name)
                .GroupBy(w => FolderNavigationService.NormalizePath(w.Bookmark))
                .OrderBy(g => g.Key);

            foreach (var group in grouped)
            {
                var folderHeader = string.IsNullOrEmpty(group.Key) ? "[grey](root)[/]" : $"[cyan]{Markup.Escape(group.Key)}[/]";
                UserInterfaceService.ShowMarkup($"[bold]📁 {folderHeader}[/]");
                UserInterfaceService.WriteLine();

                foreach (var workflow in group.OrderBy(r => r.Name))
                {
                    var panel = new Panel(BuildWorkflowContent(workflow))
                        .Header($"[cyan bold]{Markup.Escape(workflow.Name)}[/]")
                        .Border(BoxBorder.Rounded)
                        .BorderColor(Color.Cyan);

                    UserInterfaceService.ShowPanel(panel);
                    UserInterfaceService.WriteLine();
                }
            }

            UserInterfaceService.ShowGrey($"Total workflows: {workflows.Count}");

            await Task.CompletedTask;
        }

        private static string BuildWorkflowContent(Workflow workflow)
        {
            var lines = new List<string>();

            lines.Add($"[yellow]Name:[/] {workflow.Name}");
            lines.Add($"[yellow]Bookmark:[/] {workflow.Bookmark}");
            lines.Add($"[yellow]Created:[/] {workflow.CreatedAt:yyyy-MM-dd HH:mm}");
            lines.Add(string.Empty);
            lines.Add($"[yellow]Steps ({workflow.Steps.Count}):[/]");

            if (workflow.Steps.Count == 0)
            {
                lines.Add("  [grey]No steps configured[/]");
            }
            else
            {
                for (var i = 0; i < workflow.Steps.Count; i++)
                {
                    var step = workflow.Steps[i];
                    var stepDescription = WorkflowStepDescriptionHelper.GetStepDescription(step);
                    lines.Add($"  [white]{i + 1}.[/] {stepDescription}");
                }
            }

            return string.Join(Environment.NewLine, lines);
        }
    }
}
