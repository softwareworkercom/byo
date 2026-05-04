using SoftwareWorker.BYO.CLI.Abstractions.Attributes;
using SoftwareWorker.BYO.CLI.Core.Model;
using SoftwareWorker.BYO.CLI.Core.Service;
using SoftwareWorker.BYO.Core.Model.Enums;
using Spectre.Console;

namespace SoftwareWorker.BYO.CLI.Core.Handlers.Workflows
{
    [TrunkCommand("workflows", "Workflow management")]
    [BranchCommand("create", "Create a new workflow with interactive steps")]
    [Parameter("name", "Workflow name", true, null)]
    [Parameter("bookmark", "Bookmark hierarchy path (e.g. DevOps/Deploy)", true, null)]
    public class WorkflowCreateHandler : BaseCommandHandler
    {
        public string? Name { get; set; }
        public string? Bookmark { get; set; }
        private sealed record StepTypeOption(WorkflowStepTypeEnum? StepType, string Label);

        public override async Task ExecuteAsync()
        {
            var workflowSteps = new List<WorkflowStep>();

            UserInterfaceService.ShowCyan("Create workflow steps (in execution order).");
            UserInterfaceService.WriteLine();

            var stepIndex = 1;
            while (true)
            {
                UserInterfaceService.ShowMarkup($"[bold cyan]Step {stepIndex}:[/]");

                var stepTypeOptions = Enum.GetValues<WorkflowStepTypeEnum>()
                    .Select(stepType => new StepTypeOption(stepType, GetStepTypeLabel(stepType)))
                    .Append(new StepTypeOption(null, "[grey]Done - Finish adding steps[/]"))
                    .ToList();

                var selectedStepTypeOption = UserInterfaceService.Prompt(
                    new SelectionPrompt<StepTypeOption>()
                        .Title("[cyan]Select step type:[/]")
                        .UseConverter(option => option.Label)
                        .AddChoices(stepTypeOptions)
                );

                if (selectedStepTypeOption.StepType is null)
                {
                    break;
                }

                var step = new WorkflowStep
                {
                    StepType = selectedStepTypeOption.StepType.Value
                };

                switch (step.StepType)
                {
                    case WorkflowStepTypeEnum.Message:
                        step.Prompt = UserInterfaceService.AskInput("Enter message to display");
                        step.Color = UserInterfaceService.SelectEnum<MessageColorEnum>("Select message color");
                        step.WaitForEnter = UserInterfaceService.AskYesNo("Wait for user to press Enter after displaying message?");
                        break;
                    case WorkflowStepTypeEnum.YesNoQuestion:
                        step.Prompt = UserInterfaceService.AskInput("Enter question to ask");
                        step.InterruptOnNo = UserInterfaceService.AskYesNo("Interrupt workflow if answer is No?");
                        break;
                    case WorkflowStepTypeEnum.InputAsSetting:
                        step.Prompt = UserInterfaceService.AskInput("Enter prompt message");
                        step.StorageKey = UserInterfaceService.AskInput("Enter setting key name");
                        break;
                    case WorkflowStepTypeEnum.InputAsSecret:
                        step.Prompt = UserInterfaceService.AskInput("Enter prompt message");
                        step.StorageKey = UserInterfaceService.AskInput("Enter secret key name");
                        break;
                    case WorkflowStepTypeEnum.ExecuteCommand:
                        var commands = CommandService.GetList();
                        if (commands.Count == 0)
                        {
                            UserInterfaceService.ShowError("No saved commands available. Create a command first.");
                            continue;
                        }

                        var commandNames = commands.Select(c => c.Name).ToList();
                        step.CommandName = UserInterfaceService.SelectSingleItem("command", commandNames);
                        step.RunAsync = UserInterfaceService.AskYesNo("Run this command asynchronously in background?");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                workflowSteps.Add(step);
                stepIndex++;
                UserInterfaceService.WriteLine();
            }

            if (workflowSteps.Count == 0)
            {
                UserInterfaceService.ShowWarning("No steps entered. Workflow creation cancelled.");
                return;
            }

            try
            {
                WorkflowService.Create(Name, workflowSteps, Bookmark);
                UserInterfaceService.ShowGreen($"Workflow '{Name}' created successfully with {workflowSteps.Count} step(s).");
            }
            catch (InvalidOperationException ex)
            {
                UserInterfaceService.ShowError(ex.Message);
            }

            await Task.CompletedTask;
        }

        private static string GetStepTypeLabel(WorkflowStepTypeEnum stepType)
        {
            return stepType switch
            {
                WorkflowStepTypeEnum.Message => "Message - Display a message",
                WorkflowStepTypeEnum.YesNoQuestion => "Yes/No Question - Ask for confirmation",
                WorkflowStepTypeEnum.InputAsSetting => "Input as Setting - Prompt for input and save as setting",
                WorkflowStepTypeEnum.InputAsSecret => "Input as Secret - Prompt for input and save as secret",
                WorkflowStepTypeEnum.ExecuteCommand => "Execute Command - Run a saved command",
                _ => stepType.ToString()
            };
        }
    }
}
