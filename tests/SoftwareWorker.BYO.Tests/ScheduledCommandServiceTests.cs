using SoftwareWorker.BYO.CLI.Service;
using System.Diagnostics;
using System.Xml.Linq;

namespace SoftwareWorker.BYO.Tests;

public sealed class ScheduledCommandServiceTests
{
    [Theory]
    [InlineData("", "\"\"")]
    [InlineData("plain", "\"plain\"")]
    [InlineData("a b", "\"a b\"")]
    [InlineData("a\"b", "\"a\\\"b\"")]
    [InlineData("C:\\folder\\", "\"C:\\folder\\\\\"")]
    [InlineData("a\\\"b", "\"a\\\\\\\"b\"")]
    public void QuoteArgument_ShouldPreserveWindowsArgumentBoundaries(string argument, string expected)
    {
        Assert.Equal(expected, ScheduledCommandService.QuoteArgument(argument));
    }

    [Fact]
    public void BuildTaskXml_ShouldConfigureRecurringAndSignInRecoveryWithoutPrivilegeEscalation()
    {
        var startInfo = CreateStartInfo();
        var start = new DateTime(2026, 11, 9, 9, 30, 0);
        var task = XElement.Parse(ScheduledCommandService.BuildTaskXml(startInfo, "S-1-5-21-123", start));
        XNamespace ns = "http://schemas.microsoft.com/windows/2004/02/mit/task";
        var triggers = task.Element(ns + "Triggers")!;
        var timeTrigger = triggers.Element(ns + "TimeTrigger")!;
        Assert.Equal("2026-11-09T09:30:00", timeTrigger.Element(ns + "StartBoundary")!.Value);
        Assert.Equal("PT1M", timeTrigger.Element(ns + "Repetition")!.Element(ns + "Interval")!.Value);
        Assert.Null(timeTrigger.Element(ns + "Repetition")!.Element(ns + "Duration"));
        Assert.Equal("S-1-5-21-123", triggers.Element(ns + "LogonTrigger")!.Element(ns + "UserId")!.Value);
        var principal = task.Element(ns + "Principals")!.Element(ns + "Principal")!;
        Assert.Equal("InteractiveToken", principal.Element(ns + "LogonType")!.Value);
        Assert.Equal("LeastPrivilege", principal.Element(ns + "RunLevel")!.Value);
        Assert.Null(principal.Element(ns + "Password"));
        var settings = task.Element(ns + "Settings")!;
        Assert.Equal("IgnoreNew", settings.Element(ns + "MultipleInstancesPolicy")!.Value);
        Assert.Equal("PT0S", settings.Element(ns + "ExecutionTimeLimit")!.Value);
        Assert.Equal("true", settings.Element(ns + "StartWhenAvailable")!.Value);
        Assert.Equal("false", settings.Element(ns + "DisallowStartIfOnBatteries")!.Value);
        Assert.Equal("false", settings.Element(ns + "StopIfGoingOnBatteries")!.Value);
    }

    [Fact]
    public void BuildTaskXml_ShouldForwardExecutableArgumentsAndWorkingDirectoryWithoutShellExpansion()
    {
        var startInfo = CreateStartInfo();
        startInfo.ArgumentList.Add("a&b<\"c>\\");
        var task = XElement.Parse(ScheduledCommandService.BuildTaskXml(startInfo, "user", DateTime.Today));
        XNamespace ns = "http://schemas.microsoft.com/windows/2004/02/mit/task";
        var action = task.Element(ns + "Actions")!.Element(ns + "Exec")!;
        Assert.Equal(startInfo.FileName, action.Element(ns + "Command")!.Value);
        Assert.Equal(startInfo.WorkingDirectory, action.Element(ns + "WorkingDirectory")!.Value);
        Assert.Equal("\"--byo-schedule-worker\" \"settings\" \"list\" \"--schedule\" \"2s\" \"a&b<\\\"c>\\\\\"",
            action.Element(ns + "Arguments")!.Value);
    }

    [Fact]
    public void GetTaskName_ShouldBeStableAndDistinguishArgumentsExecutableAndDirectory()
    {
        var original = CreateStartInfo();
        var name = ScheduledCommandService.GetTaskName(original);
        Assert.StartsWith("BYO-Schedule-", name);
        Assert.Equal(name, ScheduledCommandService.GetTaskName(CreateStartInfo()));
        var changed = CreateStartInfo();
        changed.ArgumentList.Add("--filter=a b");
        Assert.NotEqual(name, ScheduledCommandService.GetTaskName(changed));
        changed = CreateStartInfo();
        changed.WorkingDirectory += "Other";
        Assert.NotEqual(name, ScheduledCommandService.GetTaskName(changed));
        changed = CreateStartInfo();
        changed.FileName += "Other";
        Assert.NotEqual(name, ScheduledCommandService.GetTaskName(changed));
        Assert.DoesNotContain("settings", name);
    }

    [Fact]
    public void AcquireLease_ShouldPreventDuplicatesAndAllowRecoveryAfterRelease()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var path = Path.Combine(Path.GetTempPath(), "byo-schedule-test-" + Guid.NewGuid().ToString("N"), "schedule.lock");
        try
        {
            using (var lease = ScheduledCommandService.AcquireLease(path))
            {
                Assert.NotNull(lease);
                Assert.Null(ScheduledCommandService.AcquireLease(path));
            }
            using var recoveredLease = ScheduledCommandService.AcquireLease(path);
            Assert.NotNull(recoveredLease);
        }
        finally
        {
            File.Delete(path);
            Directory.Delete(Path.GetDirectoryName(path)!);
        }
    }

    private static ProcessStartInfo CreateStartInfo()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = @"C:\BYO & Tools\byo.exe",
            WorkingDirectory = @"C:\Jobs & Projects"
        };
        foreach (var argument in new[] { ScheduledCommandService.WorkerArgument, "settings", "list", "--schedule", "2s" })
        {
            startInfo.ArgumentList.Add(argument);
        }
        return startInfo;
    }
}
