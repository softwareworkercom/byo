using SoftwareWorker.BYO.CLI.Core.Model;
using SoftwareWorker.BYO.Core.Model.Enums;
using Spectre.Console;

namespace SoftwareWorker.BYO.CLI.Core.Service
{
    public static class WorkflowManagementService
    {
        /// <summary>
        /// Builds a display string for a workflow suitable for selection prompts.
        /// </summary>
        /// <param name="workflow">The workflow to build the display string for.</param>
        /// <returns>A formatted display string.</returns>
        public static string BuildDisplayString(Workflow workflow)
        {
            return workflow.Name;
        }

        /// <summary>
        /// Executes all steps of a workflow in sequence.
        /// </summary>
        /// <param name="workflow">The workflow to execute.</param>
        /// <returns>True if the workflow completed successfully, false if interrupted.</returns>
        public static async Task<bool> ExecuteWorkflowAsync(Workflow workflow)
        {
            if (workflow.Steps.Count == 0)
            {
                UserInterfaceService.ShowError("No steps found in this workflow.");
                return false;
            }

            UserInterfaceService.ShowGreen($"Executing workflow '{workflow.Name}' with {workflow.Steps.Count} step(s)...");
            UserInterfaceService.WriteLine();

            foreach (var step in workflow.Steps)
            {
                var shouldContinue = await ExecuteStepAsync(step);

                if (!shouldContinue)
                {
                    UserInterfaceService.ShowWarning("Workflow execution interrupted by user.");
                    return false;
                }
            }

            UserInterfaceService.ShowGreen($"Workflow '{workflow.Name}' completed successfully.");
            return true;
        }

        /// <summary>
        /// Executes a single workflow step based on its type.
        /// </summary>
        /// <param name="step">The step to execute.</param>
        /// <returns>True if execution should continue, false to interrupt.</returns>
        public static async Task<bool> ExecuteStepAsync(WorkflowStep step)
        {
            switch (step.StepType)
            {
                case WorkflowStepTypeEnum.Message:
                    ExecuteMessageStep(step);
                    return true;

                case WorkflowStepTypeEnum.YesNoQuestion:
                    return ExecuteYesNoQuestion(step);

                case WorkflowStepTypeEnum.InputAsSetting:
                    ExecuteInputAsSetting(step);
                    return true;

                case WorkflowStepTypeEnum.InputAsSecret:
                    ExecuteInputAsSecret(step);
                    return true;

                case WorkflowStepTypeEnum.ExecuteCommand:
                    await ExecuteCommandStep(step);
                    return true;

                default:
                    UserInterfaceService.ShowError($"Unknown step type: {step.StepType}");
                    return true;
            }
        }

        private static void ExecuteMessageStep(WorkflowStep step)
        {
            if (string.IsNullOrEmpty(step.Prompt))
            {
                return;
            }

            var message = step.Prompt;

            switch (step.Color)
            {
                case MessageColorEnum.Red:
                    UserInterfaceService.ShowRed(message);
                    break;
                case MessageColorEnum.Yellow:
                    UserInterfaceService.ShowYellow(message);
                    break;
                case MessageColorEnum.Blue:
                    UserInterfaceService.ShowBlue(message);
                    break;
                case MessageColorEnum.Green:
                    UserInterfaceService.ShowGreen(message);
                    break;
                case MessageColorEnum.Cyan:
                    UserInterfaceService.ShowCyan(message);
                    break;
                case MessageColorEnum.Grey:
                    UserInterfaceService.ShowGrey(message);
                    break;
                case MessageColorEnum.Default:
                default:
                    UserInterfaceService.WriteLine(message);
                    break;
            }

            if (step.WaitForEnter)
            {
                UserInterfaceService.WaitForEnter();
            }
        }

        private static bool ExecuteYesNoQuestion(WorkflowStep step)
        {
            if (string.IsNullOrEmpty(step.Prompt))
            {
                UserInterfaceService.ShowError("Yes/No question has no prompt configured.");
                return true;
            }

            var answer = UserInterfaceService.AskYesNo(step.Prompt);

            if (!answer && step.InterruptOnNo)
            {
                return false; // Interrupt the flow
            }

            return true;
        }

        private static void ExecuteInputAsSetting(WorkflowStep step)
        {
            if (string.IsNullOrEmpty(step.Prompt) || string.IsNullOrEmpty(step.StorageKey))
            {
                UserInterfaceService.ShowError("Input step has no prompt or storage key configured.");
                return;
            }

            var input = UserInterfaceService.AskInput(step.Prompt);
            SettingsService.Update(step.StorageKey, input);
            UserInterfaceService.ShowSuccess($"Saved to settings as '{step.StorageKey}'");
        }

        private static void ExecuteInputAsSecret(WorkflowStep step)
        {
            if (string.IsNullOrEmpty(step.Prompt) || string.IsNullOrEmpty(step.StorageKey))
            {
                UserInterfaceService.ShowError("Secret input step has no prompt or storage key configured.");
                return;
            }

            var input = UserInterfaceService.AskSecret(step.Prompt);
            SecretsService.Update(step.StorageKey, input);
            UserInterfaceService.ShowSuccess($"Saved to secrets as '{step.StorageKey}'");
        }

        private static async Task ExecuteCommandStep(WorkflowStep step)
        {
            if (string.IsNullOrEmpty(step.CommandName))
            {
                UserInterfaceService.ShowError("Execute command step has no command name configured.");
                return;
            }

            var allCommands = CommandService.GetList();
            var command = allCommands.FirstOrDefault(c =>
                c.Description.Equals(step.CommandName, StringComparison.OrdinalIgnoreCase));

            if (command == null)
            {
                UserInterfaceService.ShowError($"Command '{step.CommandName}' not found.");
                return;
            }

            var resolvedExecutable = TokenService.ResolveTokens(command.Executable);

            UserInterfaceService.ShowMarkup($"[cyan]Executing command:[/] [yellow]{Markup.Escape(command.Description)}[/]");
            UserInterfaceService.ShowMarkup($"[grey]Command: {Markup.Escape(resolvedExecutable)}[/]");
            UserInterfaceService.ShowInformation("Running...");

            TerminalService.Run(
                resolvedExecutable,
                command.WorkingDirectory,
                shell: command.Shell,
                runAsync: step.RunAsync);

            UserInterfaceService.ShowSuccess($"Command '{command.Description}' completed.");

            await Task.CompletedTask;
        }
    }
}
