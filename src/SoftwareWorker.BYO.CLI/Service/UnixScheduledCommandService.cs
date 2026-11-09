using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace SoftwareWorker.BYO.CLI.Service;

internal sealed class UnixScheduledCommandService(
    bool isMacOS,
    string homeDirectory,
    string? configDirectory,
    Func<string, string[], Task<string>> runManager)
{
    internal async Task<Func<Task>> RegisterAsync(string taskName, ProcessStartInfo startInfo)
    {
        if (isMacOS)
        {
            var uid = await GetUserIdAsync();
            var label = "com.softwareworker.byo." + taskName.ToLowerInvariant();
            var domain = "gui/" + uid;
            var target = domain + "/" + label;
            var path = Path.Combine(homeDirectory, "Library", "LaunchAgents", label + ".plist");
            var logDirectory = Path.Combine(homeDirectory, "Library", "Logs", "BYO");
            await runManager("/bin/launchctl", ["print", domain]);
            Directory.CreateDirectory(logDirectory);
            var jobs = await runManager("/bin/launchctl", ["list"]);
            if (jobs.Split('\n').Any(line => line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).LastOrDefault() == label))
            {
                await runManager("/bin/launchctl", ["bootout", target]);
            }

            await WriteDefinitionAsync(path, BuildLaunchAgent(startInfo, label, logDirectory));
            try
            {
                await runManager("/bin/launchctl", ["enable", target]);
                await runManager("/bin/launchctl", ["bootstrap", domain, path]);
            }
            catch
            {
                File.Delete(path);
                throw;
            }
            return CreateMacStopAction(path, target);
        }

        var unit = taskName.ToLowerInvariant() + ".service";
        var unitPath = GetUnitPath(unit);
        await runManager("systemctl", ["--user", "daemon-reload"]);
        await WriteDefinitionAsync(unitPath, BuildSystemdUnit(startInfo, GetConfigHome()));
        try
        {
            await runManager("systemctl", ["--user", "daemon-reload"]);
            await runManager("systemctl", ["--user", "enable", "--now", unitPath]);
        }
        catch (Exception registrationError)
        {
            try
            {
                await CreateLinuxStopAction(unitPath, unit)();
            }
            catch (Exception cleanupError)
            {
                throw new AggregateException("Schedule registration and cleanup failed.", registrationError, cleanupError);
            }
            throw;
        }
        return CreateLinuxStopAction(unitPath, unit);
    }

    internal async Task<Func<Task>> GetStopActionAsync(string taskName)
    {
        if (isMacOS)
        {
            var label = "com.softwareworker.byo." + taskName.ToLowerInvariant();
            var path = Path.Combine(homeDirectory, "Library", "LaunchAgents", label + ".plist");
            return CreateMacStopAction(path, "gui/" + await GetUserIdAsync() + "/" + label);
        }
        var unit = taskName.ToLowerInvariant() + ".service";
        return CreateLinuxStopAction(GetUnitPath(unit), unit);
    }

    private async Task<string> GetUserIdAsync()
    {
        var uid = (await runManager("/usr/bin/id", ["-u"])).Trim();
        if (!uint.TryParse(uid, NumberStyles.None, CultureInfo.InvariantCulture, out _))
        {
            throw new InvalidOperationException("Unable to determine the current user ID for launchd.");
        }
        return uid;
    }

    private string GetUnitPath(string unit) => Path.Combine(GetConfigHome(), "systemd", "user", unit);

    private string GetConfigHome()
    {
        var configHome = string.IsNullOrWhiteSpace(configDirectory)
            ? Path.Combine(homeDirectory, ".config")
            : configDirectory;
        if (!Path.IsPathFullyQualified(configHome))
        {
            throw new InvalidOperationException("XDG_CONFIG_HOME must be an absolute path for scheduled recovery.");
        }
        return configHome;
    }

    private Func<Task> CreateMacStopAction(string path, string target) => async () =>
    {
        // Remove persistence before bootout, which can terminate the calling worker itself.
        File.Delete(path);
        await runManager("/bin/launchctl", ["bootout", target]);
    };

    private Func<Task> CreateLinuxStopAction(string path, string unit) => async () =>
    {
        // Disable and remove persistence before stop, which can terminate the calling worker itself.
        await runManager("systemctl", ["--user", "disable", unit]);
        File.Delete(path);
        await runManager("systemctl", ["--user", "daemon-reload"]);
        await runManager("systemctl", ["--user", "stop", unit]);
    };

    private static async Task WriteDefinitionAsync(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllTextAsync(temporaryPath, content, new UTF8Encoding(false));
            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(temporaryPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }

    internal static string BuildLaunchAgent(ProcessStartInfo startInfo, string label, string logDirectory)
    {
        var document = new XDocument(new XDeclaration("1.0", "UTF-8", null),
            new XDocumentType("plist", "-//Apple//DTD PLIST 1.0//EN", "http://www.apple.com/DTDs/PropertyList-1.0.dtd", null),
            new XElement("plist", new XAttribute("version", "1.0"), new XElement("dict",
                new XElement("key", "Label"), new XElement("string", label),
                new XElement("key", "ProgramArguments"), new XElement("array",
                    new[] { startInfo.FileName }.Concat(startInfo.ArgumentList).Select(value => new XElement("string", value))),
                new XElement("key", "WorkingDirectory"), new XElement("string", startInfo.WorkingDirectory),
                new XElement("key", "RunAtLoad"), new XElement("true"),
                new XElement("key", "KeepAlive"), new XElement("true"),
                new XElement("key", "ThrottleInterval"), new XElement("integer", 10),
                new XElement("key", "StandardOutPath"), new XElement("string", Path.Combine(logDirectory, label + ".out.log")),
                new XElement("key", "StandardErrorPath"), new XElement("string", Path.Combine(logDirectory, label + ".err.log")))));
        return document.ToString();
    }

    internal static string BuildSystemdUnit(ProcessStartInfo startInfo, string? configHome = null)
    {
        var command = QuoteSystemdArgument(startInfo.FileName, escapeEnvironment: false) + " "
            + string.Join(" ", startInfo.ArgumentList.Select(value => QuoteSystemdArgument(value)));
        return "[Unit]\nDescription=BYO scheduled command recovery\nStartLimitIntervalSec=0\n\n"
            + "[Service]\nType=simple\n"
            + "ExecStart=" + command + "\n"
            + "WorkingDirectory=" + FormatSystemdDirectory(startInfo.WorkingDirectory) + "\n"
            + (configHome == null ? string.Empty : "Environment=" + QuoteSystemdArgument("XDG_CONFIG_HOME=" + configHome, escapeEnvironment: false) + "\n")
            + "Restart=always\nRestartSec=10s\nTimeoutStopSec=15s\n\n"
            + "[Install]\nWantedBy=default.target\n";
    }

    internal static string FormatSystemdDirectory(string directory)
    {
        if (!directory.StartsWith('/') || directory.IndexOfAny(['\0', '\n', '\r']) >= 0)
        {
            throw new ArgumentException("systemd recovery requires an absolute working directory without NUL or newline characters.", nameof(directory));
        }
        // WorkingDirectory is a literal path, not a quoted command-line token.
        var value = directory.Replace("%", "%%", StringComparison.Ordinal);
        return char.IsWhiteSpace(value[^1]) || value[^1] == '\\' ? value + "/." : value;
    }

    internal static string QuoteSystemdArgument(string argument, bool escapeEnvironment = true)
    {
        var quoted = new StringBuilder("\"");
        foreach (var character in argument)
        {
            quoted.Append(character switch
            {
                '\0' => throw new ArgumentException("Service arguments cannot contain NUL characters.", nameof(argument)),
                '\\' => "\\\\",
                '"' => "\\\"",
                '\n' => "\\n",
                '\r' => "\\r",
                '\t' => "\\t",
                '%' => "%%",
                '$' when escapeEnvironment => "$$",
                _ => character.ToString()
            });
        }
        return quoted.Append('"').ToString();
    }
}
