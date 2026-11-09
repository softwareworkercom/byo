using SoftwareWorker.BYO.CLI.Engine;
using SoftwareWorker.BYO.SDK.Services;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace SoftwareWorker.BYO.CLI.Service;

internal static class ScheduledCommandService
{
    internal const string WorkerArgument = "--byo-schedule-worker";
    internal static bool IsWorker { get; private set; }

    internal static string[] GetCommandArguments(string[] arguments)
    {
        IsWorker = arguments.Length > 0 && arguments[0] == WorkerArgument;
        return IsWorker ? arguments[1..] : arguments;
    }

    internal static string GetTaskName(ProcessStartInfo startInfo)
    {
        var identity = JsonSerializer.Serialize(new
        {
            startInfo.FileName,
            startInfo.WorkingDirectory,
            Arguments = startInfo.ArgumentList.ToArray()
        });
        return "BYO-Schedule-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)))[..24];
    }

    internal static FileStream? AcquireLease(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        try
        {
            return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException ex) when ((ex.HResult & 0xffff) is 32 or 33)
        {
            return null;
        }
    }

    internal static async Task<Registration?> RegisterAsync(IReadOnlyList<string> arguments)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Schedule recovery requires Windows Task Scheduler.");
        }

        var startInfo = CommandsBuilder.CreateSelfProcessStartInfo(arguments);
        var taskName = GetTaskName(startInfo);
        var leasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BYO", "Schedules", taskName + ".lock");
        var lease = AcquireLease(leasePath);
        if (lease == null)
        {
            UserInterfaceService.ShowGrey($"This schedule is already running ({taskName}).");
            return null;
        }

        try
        {
            if (!IsWorker)
            {
                var workerStartInfo = CommandsBuilder.CreateSelfProcessStartInfo([WorkerArgument, .. arguments]);
                using var identity = WindowsIdentity.GetCurrent();
                var xml = BuildTaskXml(workerStartInfo, identity.User!.Value, DateTime.Now.AddMinutes(1));
                var xmlPath = Path.GetTempFileName();
                try
                {
                    await File.WriteAllTextAsync(xmlPath, xml);
                    await RunSchedulerAsync(["/Create", "/TN", taskName, "/XML", xmlPath, "/F"]);
                }
                finally
                {
                    File.Delete(xmlPath);
                }
            }

            UserInterfaceService.ShowGrey($"Recovery enabled: {taskName}. Restarts within about a minute after a crash and after Windows sign-in.");
            return new Registration(taskName, lease);
        }
        catch
        {
            lease.Dispose();
            throw;
        }
    }

    internal static string BuildTaskXml(ProcessStartInfo startInfo, string userId, DateTime startBoundary)
    {
        XNamespace ns = "http://schemas.microsoft.com/windows/2004/02/mit/task";
        return new XElement(ns + "Task", new XAttribute("version", "1.2"),
            new XElement(ns + "RegistrationInfo",
                new XElement(ns + "Description", "BYO scheduled command recovery. Delete this task to disable recovery.")),
            new XElement(ns + "Triggers",
                new XElement(ns + "TimeTrigger",
                    new XElement(ns + "Repetition", new XElement(ns + "Interval", "PT1M"),
                        new XElement(ns + "StopAtDurationEnd", false)),
                    new XElement(ns + "StartBoundary", startBoundary.ToString("yyyy-MM-dd'T'HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)),
                    new XElement(ns + "Enabled", true)),
                new XElement(ns + "LogonTrigger", new XElement(ns + "Enabled", true),
                    new XElement(ns + "UserId", userId))),
            new XElement(ns + "Principals",
                new XElement(ns + "Principal", new XAttribute("id", "Author"),
                    new XElement(ns + "UserId", userId),
                    new XElement(ns + "LogonType", "InteractiveToken"),
                    new XElement(ns + "RunLevel", "LeastPrivilege"))),
            new XElement(ns + "Settings",
                new XElement(ns + "MultipleInstancesPolicy", "IgnoreNew"),
                new XElement(ns + "DisallowStartIfOnBatteries", false),
                new XElement(ns + "StopIfGoingOnBatteries", false),
                new XElement(ns + "StartWhenAvailable", true),
                new XElement(ns + "ExecutionTimeLimit", "PT0S")),
            new XElement(ns + "Actions", new XAttribute("Context", "Author"),
                new XElement(ns + "Exec", new XElement(ns + "Command", startInfo.FileName),
                    new XElement(ns + "Arguments", string.Join(" ", startInfo.ArgumentList.Select(QuoteArgument))),
                    new XElement(ns + "WorkingDirectory", startInfo.WorkingDirectory))))
            .ToString();
    }

    internal static string QuoteArgument(string argument)
    {
        var result = new StringBuilder("\"");
        var backslashes = 0;
        foreach (var character in argument)
        {
            if (character == '\\')
            {
                backslashes++;
                continue;
            }

            result.Append('\\', character == '"' ? backslashes * 2 + 1 : backslashes);
            result.Append(character);
            backslashes = 0;
        }

        result.Append('\\', backslashes * 2);
        return result.Append('"').ToString();
    }

    private static async Task RunSchedulerAsync(IEnumerable<string> arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "schtasks.exe"),
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Unable to start Windows Task Scheduler.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Windows Task Scheduler failed ({process.ExitCode}): {await error} {await output}");
        }
    }

    internal sealed class Registration(string taskName, FileStream lease) : IDisposable
    {
        internal Task StopAsync() => RunSchedulerAsync(["/Delete", "/TN", taskName, "/F"]);
        public void Dispose() => lease.Dispose();
    }
}
