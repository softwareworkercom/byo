using SoftwareWorker.BYO.CLI.Abstractions.Attributes;
using SoftwareWorker.BYO.CLI.Core.Model;
using SoftwareWorker.BYO.CLI.Core.Service;
using SoftwareWorker.BYO.Core.Model.Enums;
using Spectre.Console;

namespace SoftwareWorker.BYO.CLI.Core.Handlers.Workflows
{
    [TrunkCommand("workflows", "Workflow management")]
    [BranchCommand("read", "List all workflows")]
    internal class WorkflowReadHandler : BaseCommandHandler
    {
        public override async Task ExecuteAsync()
        {
            var workflows = WorkflowService.GetList();

            if (workflows == null || workflows.Count == 0)
            {
                UserInterfaceService.ShowWarning("No workflows found. Use 'sw workflows create' to create a workflow.");
                return;
            }

            // Group workflows by folder path (null/empty = root)
            var grouped = workflows
                .OrderBy(w => w.FolderPath ?? string.Empty)
                .ThenBy(w => w.Name)
                .GroupBy(w => FolderNavigationService.NormalizePath(w.FolderPath))
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

            var description = string.IsNullOrEmpty(workflow.Description)
                ? "[grey]No description[/]"
                : Markup.Escape(workflow.Description);
            lines.Add($"[yellow]Description:[/] {description}");
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
                    var stepDescription = GetStepDescription(step);
                    lines.Add($"  [white]{i + 1}.[/] {stepDescription}");
                }
            }

            return string.Join(Environment.NewLine, lines);
        }

        private static string GetStepDescription(WorkflowStep step)
        {
            return step.StepType switch
            {
                WorkflowStepTypeEnum.Message => $"[blue]Message[/]: {Markup.Escape(step.Prompt ?? "(empty)")}",
                WorkflowStepTypeEnum.YesNoQuestion => $"[green]Yes/No Question[/]: {Markup.Escape(step.Prompt ?? "(empty)")}" +
                    (step.InterruptOnNo ? " [grey](interrupts on No)[/]" : ""),
                WorkflowStepTypeEnum.InputAsSetting => $"[magenta]Input as Setting[/]: {Markup.Escape(step.Prompt ?? "(empty)")} → [grey]{Markup.Escape(step.StorageKey ?? "(no key)")}[/]",
                WorkflowStepTypeEnum.InputAsSecret => $"[red]Input as Secret[/]: {Markup.Escape(step.Prompt ?? "(empty)")} → [grey]{Markup.Escape(step.StorageKey ?? "(no key)")}[/]",
                WorkflowStepTypeEnum.ExecuteCommand => $"[yellow]Execute Command[/]: [white]{Markup.Escape(step.CommandName ?? "(no command)")}[/]",
                _ => $"[grey]Unknown step type: {step.StepType}[/]"
            };
        }
    }
}
