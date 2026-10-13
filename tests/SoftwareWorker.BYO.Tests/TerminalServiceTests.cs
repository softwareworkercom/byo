using SoftwareWorker.BYO.SDK.Model.Enums;
using SoftwareWorker.BYO.SDK.Services;
using SoftwareWorker.BYO.Tests.Support;
using System.Diagnostics;
using System.Reflection;

namespace SoftwareWorker.BYO.Tests;

public sealed class TerminalServiceTests
{
    public static bool IsWindows => OperatingSystem.IsWindows();

    private static ShellTypeEnum LocalShell => OperatingSystem.IsWindows() ? ShellTypeEnum.Cmd : ShellTypeEnum.Sh;

    [Fact]
    public void Run_ShouldCaptureStandardOutput_AndReportTheCommandDetails()
    {
        using var console = new ConsoleScope();

        var exitCode = TerminalService.Run("echo terminal-output", shell: LocalShell);

        Assert.NotNull(exitCode);
        Assert.Contains("terminal-output", console.Output);
        Assert.Contains("Command: echo terminal-output", console.Output);
        Assert.Contains($"Directory: {Environment.CurrentDirectory}", console.Output);
    }

    [Fact]
    public void Run_ShouldShowStandardError_AndReturnANonZeroExitCode()
    {
        using var console = new ConsoleScope();
        var command = OperatingSystem.IsWindows() ? "echo oops 1>&2 & exit 3" : "echo oops 1>&2; exit 3";

        var exitCode = TerminalService.Run(command, shell: LocalShell);

        Assert.Equal(3, exitCode);
        Assert.Contains("oops", console.Output);
    }

    [Fact]
    public void Run_ShouldUseTheProvidedWorkingDirectory()
    {
        using var console = new ConsoleScope();
        var directory = Directory.CreateTempSubdirectory("byo-terminal-").FullName;
        try
        {
            var command = OperatingSystem.IsWindows() ? "cd" : "pwd";

            var exitCode = TerminalService.Run(command, directory, shell: LocalShell);

            Assert.NotNull(exitCode);
            Assert.Contains($"Directory: {directory}", console.Output);
            Assert.Contains(Path.GetFileName(directory), console.Output);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Run_ShouldReturnNull_WhenTheShellCannotBeStarted()
    {
        using var console = new ConsoleScope();
        var missingShell = OperatingSystem.IsWindows() ? ShellTypeEnum.Zsh : ShellTypeEnum.Cmd;

        var exitCode = TerminalService.Run("echo never", shell: missingShell);

        Assert.Null(exitCode);
        Assert.NotEmpty(console.Output);
    }

    [Fact]
    public void ConfigureProcessStartInfo_ShouldRedirectOutput_WhenConsoleIsNotInteractive()
    {
        using var console = new ConsoleScope();

        var startInfo = CreateProcessStartInfo();

        Assert.True(startInfo.RedirectStandardOutput);
        Assert.True(startInfo.RedirectStandardError);
        Assert.False(startInfo.UseShellExecute);
    }

    [Fact]
    public void ConfigureProcessStartInfo_ShouldPreserveConsoleHandles_WhenConsoleIsInteractive()
    {
        using var console = new ConsoleScope(interactive: true);

        var startInfo = CreateProcessStartInfo();

        Assert.False(startInfo.RedirectStandardOutput);
        Assert.False(startInfo.RedirectStandardError);
        Assert.False(startInfo.UseShellExecute);
    }

    [Fact(Skip = "PowerShell is only available on Windows.", SkipUnless = nameof(IsWindows))]
    public void Run_ShouldDefaultToPowerShellOnWindows()
    {
        using var console = new ConsoleScope();

        var exitCode = TerminalService.Run("Write-Output default-shell");

        Assert.Equal(0, exitCode);
        Assert.Contains("default-shell", console.Output);
    }

    [Fact(Skip = "PowerShell is only available on Windows.", SkipUnless = nameof(IsWindows))]
    public void Run_ShouldEncodeTheCommand_ForUnknownShellValues()
    {
        using var console = new ConsoleScope();

        var exitCode = TerminalService.Run("Write-Output 'encoded shell'", shell: (ShellTypeEnum)99);

        Assert.Equal(0, exitCode);
        Assert.Contains("encoded shell", console.Output);
    }

    private static ProcessStartInfo CreateProcessStartInfo(bool isMultipleExecution = false, bool runAsync = false)
    {
        var method = typeof(TerminalService).GetMethod("ConfigureProcessStartInfo", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        object?[] arguments = [null, "shell", "args", isMultipleExecution, runAsync];
        var result = method!.Invoke(null, arguments);

        return Assert.IsType<ProcessStartInfo>(result);
    }
}
