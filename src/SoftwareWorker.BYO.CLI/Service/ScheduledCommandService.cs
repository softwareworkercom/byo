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
        catch (IOException ex) when ((ex.HResult & 0xffff) is 32 or 33
            || OperatingSystem.IsLinux() && (ex.HResult & 0xffff) == 11
            || OperatingSystem.IsMacOS() && (ex.HResult & 0xffff) == 35)
        {
            return null;
        }
    }

    internal static async Task<Registration?> RegisterAsync(IReadOnlyList<string> arguments)
    {
        if (!OperatingSystem.IsWindows() && !OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
        {
            throw new PlatformNotSupportedException("Schedule recovery requires Windows, Linux with systemd, or macOS with launchd.");
        }

        var startInfo = CommandsBuilder.CreateSelfProcessStartInfo(arguments);
        var taskName = GetTaskName(startInfo);
        var leaseDirectory = OperatingSystem.IsWindows()
            ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");
        var leasePath = Path.Combine(leaseDirectory, "BYO", "Schedules", taskName + ".lock");
        var lease = AcquireLease(leasePath);
        // Native Unix managers start a worker immediately. It waits while the foreground process owns the schedule.
        while (lease == null && IsWorker && !OperatingSystem.IsWindows())
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            lease = AcquireLease(leasePath);
        }
        if (lease == null)
        {
            UserInterfaceService.ShowGrey($"This schedule is already running ({taskName}).");
            return null;
        }

        try
        {
            Func<Task> stop = () => RunSchedulerAsync(["/Delete", "/TN", taskName, "/F"]);
            if (!OperatingSystem.IsWindows())
            {
                var unix = new UnixScheduledCommandService(OperatingSystem.IsMacOS(),
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    Environment.GetEnvironmentVariable("XDG_CONFIG_HOME"), RunManagerAsync);
                // Workers also resolve their stop action, but must never register or restart their own supervisor.
                stop = IsWorker
                    ? await unix.GetStopActionAsync(taskName)
                    : await unix.RegisterAsync(taskName, CommandsBuilder.CreateSelfProcessStartInfo([WorkerArgument, .. arguments]));
            }
            else if (!IsWorker)
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

            var recoveryDescription = OperatingSystem.IsWindows()
                ? "Restarts within about a minute after a crash and after Windows sign-in."
                : OperatingSystem.IsMacOS()
                    ? "launchd recovery enabled; resumes after macOS sign-in."
                    : "systemd user recovery enabled; resumes at sign-in (at boot if user lingering is enabled).";
            UserInterfaceService.ShowGrey($"Recovery enabled: {taskName}. {recoveryDescription}");
            return new Registration(stop, lease);
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

    private static Task<string> RunSchedulerAsync(IEnumerable<string> arguments) => RunManagerAsync(
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "schtasks.exe"), arguments.ToArray());

    private static async Task<string> RunManagerAsync(string executable, string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
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
            ?? throw new InvalidOperationException($"Unable to start recovery manager '{executable}'.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        var outputText = await output;
        var errorText = await error;
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Recovery manager '{executable}' failed ({process.ExitCode}): {errorText} {outputText}");
        }
        return outputText;
    }

    internal sealed class Registration(Func<Task> stop, FileStream lease) : IDisposable
    {
        internal Task StopAsync() => stop();
        public void Dispose() => lease.Dispose();
    }
}
