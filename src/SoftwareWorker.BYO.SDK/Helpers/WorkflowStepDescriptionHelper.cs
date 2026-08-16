using SoftwareWorker.BYO.CLI.Core.Model;
using SoftwareWorker.BYO.Core.Model.Enums;
using Spectre.Console;

namespace SoftwareWorker.BYO.CLI.Core.Helpers
{
    /// <summary>
    /// Builds a human-readable, markup-formatted description of a workflow step's configured selections.
    /// Shared between workflow creation (immediate recap) and workflow listing.
    /// </summary>
    public static class WorkflowStepDescriptionHelper
    {
        public static string GetStepDescription(WorkflowStep step)
        {
            return step.StepType switch
            {
                WorkflowStepTypeEnum.Message => $"[blue]Message[/]: {Markup.Escape(step.Prompt ?? "(empty)")}" +
                    $" [grey](color: {step.Color}, wait for enter: {(step.WaitForEnter ? "Yes" : "No")})[/]",
                WorkflowStepTypeEnum.YesNoQuestion => $"[green]Yes/No Question[/]: {Markup.Escape(step.Prompt ?? "(empty)")}" +
                    $" [grey](interrupts on No: {(step.InterruptOnNo ? "Yes" : "No")})[/]",
                WorkflowStepTypeEnum.InputAsSetting => $"[magenta]Input as Setting[/]: {Markup.Escape(step.Prompt ?? "(empty)")} → [grey]{Markup.Escape(step.StorageKey ?? "(no key)")}[/]",
                WorkflowStepTypeEnum.InputAsSecret => $"[red]Input as Secret[/]: {Markup.Escape(step.Prompt ?? "(empty)")} → [grey]{Markup.Escape(step.StorageKey ?? "(no key)")}[/]",
                WorkflowStepTypeEnum.ExecuteCommand => GetExecuteCommandDescription(step),
                _ => $"[grey]Unknown step type: {step.StepType}[/]"
            };
        }

        private static string GetExecuteCommandDescription(WorkflowStep step)
        {
            var runAsyncSuffix = $" [grey](async: {(step.RunAsync ? "Yes" : "No")})[/]";

            if (!string.IsNullOrEmpty(step.CommandName))
            {
                return $"[yellow]Execute Command[/]: [white]{Markup.Escape(step.CommandName)}[/]{runAsyncSuffix}";
            }

            var directorySuffix = string.IsNullOrEmpty(step.CommandDirectory)
                ? string.Empty
                : $" [grey](dir: {Markup.Escape(step.CommandDirectory)})[/]";
            var shellSuffix = step.CommandShell is null
                ? string.Empty
                : $" [grey](shell: {step.CommandShell})[/]";

            return $"[yellow]Execute Command[/]: [white]{Markup.Escape(step.CommandExecutable ?? "(no command)")}[/] [grey](custom)[/]{directorySuffix}{shellSuffix}{runAsyncSuffix}";
        }
    }
}
