using SoftwareWorker.BYO.CLI.Abstractions.Attributes;
using SoftwareWorker.BYO.CLI.Core;
using SoftwareWorker.BYO.CLI.Core.Model;
using SoftwareWorker.BYO.CLI.Core.Service;
using SoftwareWorker.BYO.Core.Model.Enums;

namespace SoftwareWorker.BYO.Extensions.GoogleCalendar;

[TrunkCommand("google", "Google operations")]
[BranchCommand("init", "Guided setup for Google Calendar public holidays command")]
public class GoogleCalendarInitHandler : BaseCommandHandler
{
    private const string ApiKeySecretKey = "GoogleCalendar:ApiKey";
    private const string CalendarIdSettingKey = "GoogleCalendar:CalendarId";
    private const string MaxItemsSettingKey = "GoogleCalendar:MaxItems";

    public override async Task ExecuteAsync()
    {
        var setupWorkflow = new Workflow
        {
            Name = "Google Calendar setup",
            Steps =
            [
                new WorkflowStep
                {
                    StepType = WorkflowStepTypeEnum.Message,
                    Prompt = "This setup will configure the required keys for 'google calendar'.",
                    Color = MessageColorEnum.Cyan,
                    WaitForEnter = true
                },
                new WorkflowStep
                {
                    StepType = WorkflowStepTypeEnum.InputAsSecret,
                    Prompt = "Enter your Google Calendar API key",
                    StorageKey = ApiKeySecretKey
                },
                new WorkflowStep
                {
                    StepType = WorkflowStepTypeEnum.InputAsSetting,
                    Prompt = "Enter the public holidays calendar id (example: en.usa#holiday@group.v.calendar.google.com)",
                    StorageKey = CalendarIdSettingKey
                },
                new WorkflowStep
                {
                    StepType = WorkflowStepTypeEnum.InputAsSetting,
                    Prompt = "Enter maximum number of holidays to fetch (positive integer)",
                    StorageKey = MaxItemsSettingKey
                },
                new WorkflowStep
                {
                    StepType = WorkflowStepTypeEnum.Message,
                    Prompt = "Setup complete. Run 'byo google calendar' to fetch public holidays.",
                    Color = MessageColorEnum.Green,
                    WaitForEnter = false
                }
            ]
        };

        var completed = await WorkflowExecutionService.ExecuteWorkflowAsync(setupWorkflow);

        if (!completed)
        {
            UserInterfaceService.ShowWarning("Setup was interrupted. You can run 'byo google init' again anytime.");
            return;
        }

        var maxItemsRaw = SettingsService.Get(MaxItemsSettingKey)?.Trim();
        if (!int.TryParse(maxItemsRaw, out var maxItems) || maxItems <= 0)
        {
            UserInterfaceService.ShowWarning($"Stored setting '{MaxItemsSettingKey}' is not a positive integer. Update it with 'byo settings set --key {MaxItemsSettingKey} --value 50'.");
        }
    }
}
