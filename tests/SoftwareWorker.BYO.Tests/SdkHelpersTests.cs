using SoftwareWorker.BYO.SDK.Helpers;
using SoftwareWorker.BYO.SDK.Model;
using SoftwareWorker.BYO.SDK.Model.Enums;
using SoftwareWorker.BYO.Tests.Support;
using System.Globalization;

namespace SoftwareWorker.BYO.Tests;

public sealed class SdkHelpersTests
{
    #region DateTimeHelper

    [Theory]
    [InlineData("1d", true)]
    [InlineData("2 H", true)]
    [InlineData("3mo", true)]
    [InlineData("1w", true)]
    [InlineData("30m", true)]
    [InlineData("abc", false)]
    [InlineData("0d", false)]
    [InlineData("-1d", false)]
    [InlineData("h", false)]
    [InlineData("mo", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("5x", false)]
    public void TryResolveRangeStart_ShouldAcceptOnlyPositiveWindowsWithKnownUnits(string window, bool expected)
    {
        var resolved = DateTimeHelper.TryResolveRangeStart(window, out var startDate);

        Assert.Equal(expected, resolved);
        Assert.Equal(expected, startDate.Length > 0);
    }

    [Fact]
    public void TryResolveRangeStart_ShouldSubtractTheWindowFromNow()
    {
        Assert.True(DateTimeHelper.TryResolveRangeStart("2d", out var startDate));

        var parsed = DateTime.ParseExact(startDate, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        Assert.InRange(parsed, DateTime.Now.AddDays(-2).AddMinutes(-2), DateTime.Now.AddDays(-2).AddMinutes(1));
    }

    #endregion

    #region WorkflowStepDescriptionHelper

    [Fact]
    public void GetStepDescription_ShouldDescribePromptSteps_AndFallBackForMissingValues()
    {
        var message = WorkflowStepDescriptionHelper.GetStepDescription(new WorkflowStep { StepType = WorkflowStepTypeEnum.Message, Color = MessageColorEnum.Cyan, WaitForEnter = true });
        var question = WorkflowStepDescriptionHelper.GetStepDescription(new WorkflowStep { StepType = WorkflowStepTypeEnum.YesNoQuestion, Prompt = "Sure [y]?", InterruptOnNo = true });
        var setting = WorkflowStepDescriptionHelper.GetStepDescription(new WorkflowStep { StepType = WorkflowStepTypeEnum.InputAsSetting, Prompt = "Name" });
        var secret = WorkflowStepDescriptionHelper.GetStepDescription(new WorkflowStep { StepType = WorkflowStepTypeEnum.InputAsSecret, Prompt = "Token", StorageKey = "Api:Token" });

        Assert.Equal("[blue]Message[/]: (empty) [grey](color: Cyan, wait for enter: Yes)[/]", message);
        Assert.Equal("[green]Yes/No Question[/]: Sure [[y]]? [grey](interrupts on No: Yes)[/]", question);
        Assert.Equal("[magenta]Input as Setting[/]: Name → [grey](no key)[/]", setting);
        Assert.Equal("[red]Input as Secret[/]: Token → [grey]Api:Token[/]", secret);
    }

    [Fact]
    public void GetStepDescription_ShouldDescribeCustomCommands_WithOptionalDirectoryAndShell()
    {
        var full = WorkflowStepDescriptionHelper.GetStepDescription(new WorkflowStep
        {
            StepType = WorkflowStepTypeEnum.ExecuteCommand,
            CommandExecutable = "dotnet build",
            CommandDirectory = "C:/repo",
            CommandShell = ShellTypeEnum.Cmd,
            RunAsync = true
        });
        var minimal = WorkflowStepDescriptionHelper.GetStepDescription(new WorkflowStep { StepType = WorkflowStepTypeEnum.ExecuteCommand });

        Assert.Equal("[yellow]Execute Command[/]: [white]dotnet build[/] [grey](custom)[/] [grey](dir: C:/repo)[/] [grey](shell: Cmd)[/] [grey](async: Yes)[/]", full);
        Assert.Equal("[yellow]Execute Command[/]: [white](no command)[/] [grey](custom)[/] [grey](async: No)[/]", minimal);
    }

    [Fact]
    public void GetStepDescription_ShouldReportUnknownStepTypes()
    {
        var description = WorkflowStepDescriptionHelper.GetStepDescription(new WorkflowStep { StepType = (WorkflowStepTypeEnum)42 });

        Assert.Equal("[grey]Unknown step type: 42[/]", description);
    }

    #endregion

    #region MachineIdentifierHelper

    [Fact]
    public void GetMachineIdentifier_ShouldCombineStableMachineProperties()
    {
        var identifier = MachineIdentifierHelper.GetMachineIdentifier();

        Assert.Contains(Environment.MachineName, identifier);
        Assert.Contains(Environment.UserName, identifier);
        Assert.True(identifier.Split('|').Length >= 6);
        Assert.Equal(identifier, MachineIdentifierHelper.GetMachineIdentifier());
    }

    #endregion

    #region ProcessHelper

    [Fact]
    public void IsRunningAsAdmin_ShouldNotThrow()
    {
        _ = ProcessHelper.IsRunningAsAdmin();
    }

    [Fact]
    public void KillProcesses_ShouldReportEachProcessName_EvenWhenNoneIsRunning()
    {
        using var console = new ConsoleScope();
        var first = $"byo-tests-missing-{Guid.NewGuid():N}";
        var second = $"byo-tests-missing-{Guid.NewGuid():N}";

        ProcessHelper.KillProcesses([first, second]);

        Assert.Contains($"Process {first} has been terminated.", console.Output);
        Assert.Contains($"Process {second} has been terminated.", console.Output);
    }

    [Fact]
    public void FindProcessId_ShouldReturnTheIdOfARunningProcess()
    {
        using var console = new ConsoleScope();
        // Use the OS-visible process name: on Linux it is truncated to 15 characters,
        // so deriving it from Environment.ProcessPath would not match GetProcessesByName.
        var processName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;

        var processId = ProcessHelper.FindProcessId(processName);

        Assert.NotNull(processId);
        Assert.True(processId > 0);
        Assert.Contains($"Process ID for {processName}.exe", console.Output);
    }

    #endregion
}
