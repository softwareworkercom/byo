using SoftwareWorker.BYO.CLI.Abstractions.Attributes;
using SoftwareWorker.BYO.CLI.Core.Helpers;
using SoftwareWorker.BYO.CLI.Core.Model;
using SoftwareWorker.BYO.CLI.Core.Service;
using SoftwareWorker.BYO.Core.Model.Enums;
using Spectre.Console;

namespace SoftwareWorker.BYO.CLI.Core.Handlers.Workflows
{
    [TrunkCommand("workflows", "Workflow management")]
    [BranchCommand("set", "Create a new workflow with interactive steps")]
    [Parameter("name", "Workflow name", true, null)]
    [Parameter("bookmark", "Bookmark hierarchy path (e.g. DevOps/Deploy)", true, null)]
    public class WorkflowSetHandler : BaseCommandHandler
    {
        public string? Name { get; set; }
        public string? Bookmark { get; set; }
        private const string CustomCommandOption = "Custom command (not saved)";
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
                        var commandOptions = new List<string> { CustomCommandOption };
                        commandOptions.AddRange(commands.Select(c => c.Name));

                        var selectedCommandOption = UserInterfaceService.SelectSingleItem("command", commandOptions);

                        if (selectedCommandOption == CustomCommandOption)
                        {
                            step.CommandExecutable = UserInterfaceService.AskInput("Enter command executable (use {{tokenName}} for tokens resolved from configuration)");

                            var customDirectory = UserInterfaceService.AskInput("Enter working directory (leave empty for default)", allowEmpty: true);
                            step.CommandDirectory = string.IsNullOrWhiteSpace(customDirectory) ? null : customDirectory;

                            if (UserInterfaceService.AskYesNo("Specify a shell type? (defaults to PowerShell)"))
                            {
                                step.CommandShell = UserInterfaceService.SelectEnum<ShellTypeEnum>("Select shell type");
                            }
                        }
                        else
                        {
                            step.CommandName = selectedCommandOption;
                        }

                        step.RunAsync = UserInterfaceService.AskYesNo("Run this command asynchronously in background?");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                workflowSteps.Add(step);
                UserInterfaceService.ShowMarkup($"  [grey]✓ Added:[/] {WorkflowStepDescriptionHelper.GetStepDescription(step)}");
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
                var wantsOverride = UserInterfaceService.AskYesNo($"{ex.Message} Do you want to override it?");

                if (!wantsOverride)
                {
                    UserInterfaceService.ShowWarning("Workflow creation cancelled.");
                    return;
                }

                WorkflowService.Create(Name, workflowSteps, Bookmark, overrideExisting: true);
                UserInterfaceService.ShowGreen($"Workflow '{Name}' overridden successfully with {workflowSteps.Count} step(s).");
            }

            await Task.CompletedTask;
        }

        private static string GetStepTypeLabel(WorkflowStepTypeEnum stepType)
        {
            return stepType switch
            {
                WorkflowStepTypeEnum.Message => "Display a message",
                WorkflowStepTypeEnum.YesNoQuestion => "Ask for confirmation",
                WorkflowStepTypeEnum.InputAsSetting => "Input as Setting",
                WorkflowStepTypeEnum.InputAsSecret => "Input as Secret",
                WorkflowStepTypeEnum.ExecuteCommand => "Execute Command",
                _ => stepType.ToString()
            };
        }
    }
}
