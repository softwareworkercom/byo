using SoftwareWorker.BYO.SDK.Model;
using SoftwareWorker.BYO.SDK.Model.Enums;
using SoftwareWorker.BYO.SDK.Services;
using SoftwareWorker.BYO.Tests.Support;

namespace SoftwareWorker.BYO.Tests;

public sealed class WorkflowExecutionServiceTests
{
    private static ShellTypeEnum LocalShell => OperatingSystem.IsWindows() ? ShellTypeEnum.Cmd : ShellTypeEnum.Sh;

    [Fact]
    public void BuildDisplayString_ShouldReturnTheWorkflowName()
    {
        Assert.Equal("Deploy", WorkflowExecutionService.BuildDisplayString(new Workflow { Name = "Deploy" }));
    }

    [Fact]
    public async Task ExecuteWorkflowAsync_ShouldFail_WhenThereAreNoSteps()
    {
        using var console = new ConsoleScope();

        var result = await WorkflowExecutionService.ExecuteWorkflowAsync(new Workflow { Name = "Empty" });

        Assert.False(result);
        Assert.Contains("No steps found in this workflow.", console.Output);
    }

    [Fact]
    public async Task ExecuteWorkflowAsync_ShouldShowEveryMessageStepInItsColor()
    {
        using var console = new ConsoleScope();
        var colors = Enum.GetValues<MessageColorEnum>();
        var workflow = new Workflow
        {
            Name = "Messages",
            Steps = colors.Select(color => new WorkflowStep { StepType = WorkflowStepTypeEnum.Message, Prompt = $"text-{color}", Color = color }).ToList()
        };

        var result = await WorkflowExecutionService.ExecuteWorkflowAsync(workflow);

        Assert.True(result);
        Assert.Contains($"Executing workflow 'Messages' with {colors.Length} step(s)...", console.Output);
        Assert.All(colors, color => Assert.Contains($"text-{color}", console.Output));
    }

    [Fact]
    public async Task MessageStep_ShouldSkipEmptyPrompts_AndWaitForEnterWhenRequested()
    {
        using var console = new ConsoleScope(standardInput: Environment.NewLine);

        Assert.True(await WorkflowExecutionService.ExecuteStepAsync(new WorkflowStep { StepType = WorkflowStepTypeEnum.Message }));
        Assert.Equal(string.Empty, console.Output);

        Assert.True(await WorkflowExecutionService.ExecuteStepAsync(new WorkflowStep { StepType = WorkflowStepTypeEnum.Message, Prompt = "wait", WaitForEnter = true }));
        Assert.Contains("Press Enter to continue...", console.Output);
    }

    [Fact]
    public async Task YesNoStep_WithoutPrompt_ShouldReportTheErrorAndContinue()
    {
        using var console = new ConsoleScope();

        var result = await WorkflowExecutionService.ExecuteStepAsync(new WorkflowStep { StepType = WorkflowStepTypeEnum.YesNoQuestion });

        Assert.True(result);
        Assert.Contains("Yes/No question has no prompt configured.", console.Output);
    }

    [Fact]
    public async Task YesNoStep_ShouldInterruptOnlyWhenConfiguredAndTheAnswerIsNo()
    {
        using var console = new ConsoleScope(interactive: true);
        console.PushKeys(ConsoleKey.DownArrow, ConsoleKey.Enter);
        console.PushKeys(ConsoleKey.DownArrow, ConsoleKey.Enter);
        console.PushKeys(ConsoleKey.Enter);

        Assert.False(await WorkflowExecutionService.ExecuteStepAsync(new WorkflowStep { StepType = WorkflowStepTypeEnum.YesNoQuestion, Prompt = "Go?", InterruptOnNo = true }));
        Assert.True(await WorkflowExecutionService.ExecuteStepAsync(new WorkflowStep { StepType = WorkflowStepTypeEnum.YesNoQuestion, Prompt = "Go?" }));
        Assert.True(await WorkflowExecutionService.ExecuteStepAsync(new WorkflowStep { StepType = WorkflowStepTypeEnum.YesNoQuestion, Prompt = "Go?", InterruptOnNo = true }));
    }

    [Fact]
    public async Task ExecuteWorkflowAsync_ShouldStop_WhenAStepInterruptsTheFlow()
    {
        using var console = new ConsoleScope(interactive: true);
        console.PushKeys(ConsoleKey.DownArrow, ConsoleKey.Enter);
        var workflow = new Workflow
        {
            Name = "Guarded",
            Steps =
            [
                new WorkflowStep { StepType = WorkflowStepTypeEnum.YesNoQuestion, Prompt = "Continue?", InterruptOnNo = true },
                new WorkflowStep { StepType = WorkflowStepTypeEnum.Message, Prompt = "never shown" }
            ]
        };

        var result = await WorkflowExecutionService.ExecuteWorkflowAsync(workflow);

        Assert.False(result);
        Assert.Contains("Workflow execution interrupted by user.", console.Output);
        Assert.DoesNotContain("never shown", console.Output);
    }

    [Fact]
    public async Task InputAsSettingStep_ShouldStoreTheInput_OrReportMissingConfiguration()
    {
        using var storage = new TemporaryStorageScope();
        using var console = new ConsoleScope(interactive: true);
        console.Input.PushTextWithEnter("stored-value");

        Assert.True(await WorkflowExecutionService.ExecuteStepAsync(new WorkflowStep { StepType = WorkflowStepTypeEnum.InputAsSetting, Prompt = "Value" }));
        Assert.Contains("Input step has no prompt or storage key configured.", console.Output);

        Assert.True(await WorkflowExecutionService.ExecuteStepAsync(new WorkflowStep { StepType = WorkflowStepTypeEnum.InputAsSetting, Prompt = "Value", StorageKey = "Workflow:Setting" }));
        Assert.Equal("stored-value", SettingsService.Get("Workflow:Setting"));
        Assert.Contains("Saved to settings as 'Workflow:Setting'", console.Output);
    }

    [Fact]
    public async Task InputAsSecretStep_ShouldStoreTheInputEncrypted_OrReportMissingConfiguration()
    {
        using var storage = new TemporaryStorageScope(isolateEncryptionKey: true);
        using var console = new ConsoleScope(interactive: true);
        console.Input.PushTextWithEnter("secret-value");

        Assert.True(await WorkflowExecutionService.ExecuteStepAsync(new WorkflowStep { StepType = WorkflowStepTypeEnum.InputAsSecret, StorageKey = "Workflow:Secret" }));
        Assert.Contains("Secret input step has no prompt or storage key configured.", console.Output);

        Assert.True(await WorkflowExecutionService.ExecuteStepAsync(new WorkflowStep { StepType = WorkflowStepTypeEnum.InputAsSecret, Prompt = "Token", StorageKey = "Workflow:Secret" }));
        Assert.Equal("secret-value", SecretsService.Get("Workflow:Secret"));
        Assert.NotEqual("secret-value", StorageService.LoadDictionary(storage.SecretsFilePath)["Workflow:Secret"]);
        Assert.Contains("Saved to secrets as 'Workflow:Secret'", console.Output);
    }

    [Fact]
    public async Task ExecuteCommandStep_ShouldReportUnknownCommands_AndMissingExecutables()
    {
        using var storage = new TemporaryStorageScope();
        using var console = new ConsoleScope();

        Assert.True(await WorkflowExecutionService.ExecuteStepAsync(new WorkflowStep { StepType = WorkflowStepTypeEnum.ExecuteCommand, CommandName = "nope" }));
        Assert.True(await WorkflowExecutionService.ExecuteStepAsync(new WorkflowStep { StepType = WorkflowStepTypeEnum.ExecuteCommand }));

        Assert.Contains("Command 'nope' not found.", console.Output);
        Assert.Contains("Execute command step has no command configured.", console.Output);
    }

    [Fact]
    public async Task ExecuteCommandStep_ShouldRunInlineAndSavedCommands()
    {
        using var storage = new TemporaryStorageScope();
        using var console = new ConsoleScope();
        CommandService.Create("Say", "echo saved-step", shell: LocalShell);

        Assert.True(await WorkflowExecutionService.ExecuteStepAsync(new WorkflowStep
        {
            StepType = WorkflowStepTypeEnum.ExecuteCommand,
            CommandExecutable = "echo inline-step",
            CommandShell = LocalShell
        }));
        Assert.True(await WorkflowExecutionService.ExecuteStepAsync(new WorkflowStep { StepType = WorkflowStepTypeEnum.ExecuteCommand, CommandName = "say" }));

        Assert.Contains("inline-step", console.Output);
        Assert.Contains("saved-step", console.Output);
    }

    [Fact]
    public async Task ExecuteStepAsync_ShouldReportUnknownStepTypes()
    {
        using var console = new ConsoleScope();

        var result = await WorkflowExecutionService.ExecuteStepAsync(new WorkflowStep { StepType = (WorkflowStepTypeEnum)99 });

        Assert.True(result);
        Assert.Contains("Unknown step type: 99", console.Output);
    }
}
