using SoftwareWorker.BYO.CLI.Service;
using System.Diagnostics;
using System.Xml.Linq;

namespace SoftwareWorker.BYO.Tests;

public sealed class UnixScheduledCommandServiceTests
{
    [Theory]
    [InlineData("", "\"\"")]
    [InlineData("a b", "\"a b\"")]
    [InlineData("a\"b\\c", "\"a\\\"b\\\\c\"")]
    [InlineData("%h ${HOME} $USER", "\"%%h $${HOME} $$USER\"")]
    [InlineData("a\nb\rc\td", "\"a\\nb\\rc\\td\"")]
    public void QuoteSystemdArgument_ShouldPreserveLiterals(string argument, string expected)
    {
        Assert.Equal(expected, UnixScheduledCommandService.QuoteSystemdArgument(argument));
    }

    [Fact]
    public void QuoteSystemdArgument_ShouldRejectNulAndPreserveDirectoryDollarSigns()
    {
        Assert.Throws<ArgumentException>(() => UnixScheduledCommandService.QuoteSystemdArgument("a\0b"));
        Assert.Equal("\"$HOME %%h\"", UnixScheduledCommandService.QuoteSystemdArgument("$HOME %h", escapeEnvironment: false));
    }

    [Fact]
    public void BuildSystemdUnit_ShouldConfigureUnlimitedRecoveryAndLiteralInvocation()
    {
        var startInfo = CreateStartInfo();
        var unit = UnixScheduledCommandService.BuildSystemdUnit(startInfo);
        Assert.Contains("StartLimitIntervalSec=0\n", unit);
        Assert.Contains("Type=simple\n", unit);
        Assert.Contains("Restart=always\nRestartSec=10s\n", unit);
        Assert.Contains("WantedBy=default.target\n", unit);
        Assert.Contains("ExecStart=\"/opt/BYO Tools/byo\" \"--byo-schedule-worker\" \"settings\" \"list\" \"--schedule\" \"2s\" \"a b\" \"%%h $${HOME}\"\n", unit);
        Assert.Contains("WorkingDirectory=/home/test/Jobs $literal %%h\n", unit);
        Assert.DoesNotContain('\r', unit);
        Assert.DoesNotContain("/bin/sh", unit);
    }

    [Theory]
    [InlineData("/tmp/a b$%h", "/tmp/a b$%%h")]
    [InlineData("/tmp/a\"b\\c", "/tmp/a\"b\\c")]
    [InlineData("/tmp/a ", "/tmp/a /.")]
    [InlineData("/tmp/a\\", "/tmp/a\\/.")]
    public void FormatSystemdDirectory_ShouldPreserveLiteralPaths(string path, string expected)
    {
        Assert.Equal(expected, UnixScheduledCommandService.FormatSystemdDirectory(path));
    }

    [Theory]
    [InlineData("relative")]
    [InlineData("/tmp/a\nb")]
    [InlineData("/tmp/a\rb")]
    [InlineData("/tmp/a\0b")]
    public void FormatSystemdDirectory_ShouldRejectUnrepresentablePaths(string path)
    {
        Assert.Throws<ArgumentException>(() => UnixScheduledCommandService.FormatSystemdDirectory(path));
    }

    [Fact]
    public async Task NativeParser_ShouldAcceptTheRecoveryDefinitionOnUnix()
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS()) return;
        var tool = OperatingSystem.IsMacOS() ? "/usr/bin/plutil" : "/usr/bin/systemd-analyze";
        if (!File.Exists(tool)) return;
        var home = CreateHome();
        Directory.CreateDirectory(home);
        var path = Path.Combine(home, OperatingSystem.IsMacOS() ? "test.plist" : "byo-test.service");
        try
        {
            var startInfo = CreateStartInfo();
            startInfo.FileName = "/bin/echo";
            var definition = OperatingSystem.IsMacOS()
                ? UnixScheduledCommandService.BuildLaunchAgent(startInfo, "com.test.byo", home)
                : UnixScheduledCommandService.BuildSystemdUnit(startInfo);
            await File.WriteAllTextAsync(path, definition);
            var native = new ProcessStartInfo(tool) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
            native.ArgumentList.Add(OperatingSystem.IsMacOS() ? "-lint" : "verify");
            native.ArgumentList.Add(path);
            using var process = Process.Start(native)!;
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync(TestContext.Current.CancellationToken);
            var details = await output + await error;
            Assert.True(process.ExitCode == 0, details);
        }
        finally
        {
            DeleteHomeFiles(home);
        }
    }

    [Fact]
    public async Task AcquireLease_ShouldRecognizeLinuxCrossProcessContention()
    {
        if (!OperatingSystem.IsLinux() || !File.Exists("/usr/bin/flock")) return;
        var home = CreateHome();
        Directory.CreateDirectory(home);
        var path = Path.Combine(home, "schedule.lock");
        var startInfo = new ProcessStartInfo("/usr/bin/flock") { UseShellExecute = false, RedirectStandardOutput = true };
        foreach (var argument in new[] { "-x", "--close", path, "/bin/sh", "-c", "printf 'locked\\n'; sleep 30" }) startInfo.ArgumentList.Add(argument);
        using var process = Process.Start(startInfo)!;
        try
        {
            Assert.Equal("locked", await process.StandardOutput.ReadLineAsync(TestContext.Current.CancellationToken));
            Assert.Null(ScheduledCommandService.AcquireLease(path));
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(TestContext.Current.CancellationToken);
            using var lease = ScheduledCommandService.AcquireLease(path);
            Assert.NotNull(lease);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(TestContext.Current.CancellationToken);
            }
            DeleteHomeFiles(home);
        }
    }

    [Fact]
    public void BuildLaunchAgent_ShouldPreserveArgumentsAndConfigureRecoveryAndLogs()
    {
        var startInfo = CreateStartInfo();
        startInfo.ArgumentList.Add("a&b<\"c>\\");
        var plist = XDocument.Parse(UnixScheduledCommandService.BuildLaunchAgent(startInfo, "com.test.byo", "logs"));
        var elements = plist.Root!.Element("dict")!.Elements().ToArray();
        var values = Enumerable.Range(0, elements.Length / 2).ToDictionary(index => elements[index * 2].Value, index => elements[index * 2 + 1]);
        Assert.Equal("com.test.byo", values["Label"].Value);
        Assert.Equal(new[] { startInfo.FileName }.Concat(startInfo.ArgumentList), values["ProgramArguments"].Elements("string").Select(element => element.Value));
        Assert.Equal(startInfo.WorkingDirectory, values["WorkingDirectory"].Value);
        Assert.Equal("true", values["RunAtLoad"].Name.LocalName);
        Assert.Equal("true", values["KeepAlive"].Name.LocalName);
        Assert.Equal("10", values["ThrottleInterval"].Value);
        Assert.Equal(Path.Combine("logs", "com.test.byo.out.log"), values["StandardOutPath"].Value);
        Assert.Equal(Path.Combine("logs", "com.test.byo.err.log"), values["StandardErrorPath"].Value);
        Assert.Equal("plist", plist.DocumentType!.Name);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RegisterAndStopLinux_ShouldPersistEnableThenRemoveBeforeStopping(bool customConfig)
    {
        var home = CreateHome();
        var config = customConfig ? Path.Combine(home, "custom-config") : null;
        var unit = "byo-schedule-test.service";
        var path = Path.Combine(config ?? Path.Combine(home, ".config"), "systemd", "user", unit);
        var calls = new List<string>();
        var service = new UnixScheduledCommandService(false, home, config, (executable, arguments) =>
        {
            Assert.Equal("systemctl", executable);
            calls.Add(string.Join(" ", arguments));
            if (arguments.Contains("enable"))
            {
                Assert.True(File.Exists(path));
                Assert.Equal(UnixScheduledCommandService.BuildSystemdUnit(CreateStartInfo(), config ?? Path.Combine(home, ".config")), File.ReadAllText(path));
            }
            if (arguments.Contains("stop"))
            {
                Assert.False(File.Exists(path));
            }
            return Task.FromResult(string.Empty);
        });
        try
        {
            var stop = await service.RegisterAsync("BYO-Schedule-TEST", CreateStartInfo());
            await stop();
            Assert.Equal(new[] { "--user daemon-reload", "--user daemon-reload", "--user enable --now " + path, "--user disable " + unit, "--user daemon-reload", "--user stop " + unit }, calls);
            Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(path)!));
        }
        finally
        {
            DeleteHomeFiles(home);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RegisterAndStopMac_ShouldBootstrapAndRemoveBeforeBootout(bool alreadyLoaded)
    {
        var home = CreateHome();
        var label = "com.softwareworker.byo.byo-schedule-test";
        var target = "gui/501/" + label;
        var path = Path.Combine(home, "Library", "LaunchAgents", label + ".plist");
        var calls = new List<string>();
        var bootouts = 0;
        var service = new UnixScheduledCommandService(true, home, null, (executable, arguments) =>
        {
            calls.Add(executable + " " + string.Join(" ", arguments));
            if (executable == "/usr/bin/id") return Task.FromResult("501\n");
            if (arguments[0] == "list") return Task.FromResult(alreadyLoaded ? "PID Status Label\n- 0 " + label + "\n" : "PID Status Label\n");
            if (arguments[0] == "bootstrap")
            {
                Assert.True(File.Exists(path));
                Assert.Equal(new[] { "bootstrap", "gui/501", path }, arguments);
                XDocument.Parse(File.ReadAllText(path));
            }
            if (arguments[0] == "bootout")
            {
                bootouts++;
                Assert.Equal(target, arguments[1]);
                Assert.False(File.Exists(path));
            }
            return Task.FromResult(string.Empty);
        });
        try
        {
            var stop = await service.RegisterAsync("BYO-Schedule-TEST", CreateStartInfo());
            await stop();
            Assert.Equal(alreadyLoaded ? 2 : 1, bootouts);
            Assert.Contains("/bin/launchctl enable " + target, calls);
            Assert.Equal("/bin/launchctl bootout " + target, calls[^1]);
            Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(path)!));
        }
        finally
        {
            DeleteHomeFiles(home);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WorkerStop_ShouldNotRegisterOrEnableSupervisor(bool isMacOS)
    {
        var home = CreateHome();
        var calls = new List<string>();
        var service = new UnixScheduledCommandService(isMacOS, home, null, (executable, arguments) =>
        {
            calls.Add(string.Join(" ", arguments));
            return Task.FromResult(executable == "/usr/bin/id" ? "501" : string.Empty);
        });
        try
        {
            var path = isMacOS
                ? Path.Combine(home, "Library", "LaunchAgents", "com.softwareworker.byo.byo-schedule-test.plist")
                : Path.Combine(home, ".config", "systemd", "user", "byo-schedule-test.service");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, "existing recovery definition");
            var stop = await service.GetStopActionAsync("BYO-Schedule-TEST");
            Assert.True(File.Exists(path));
            await stop();
            Assert.False(File.Exists(path));
            Assert.DoesNotContain(calls, call => call.Contains("enable", StringComparison.Ordinal) || call.Contains("bootstrap", StringComparison.Ordinal));
            Assert.Contains(calls, call => call.Contains(isMacOS ? "bootout" : "stop", StringComparison.Ordinal));
        }
        finally
        {
            DeleteHomeFiles(home);
        }
    }

    [Fact]
    public async Task Registration_ShouldSurfaceManagerFailureAndRejectInvalidUserIdOrRelativeConfig()
    {
        var home = CreateHome();
        try
        {
            var linux = new UnixScheduledCommandService(false, home, null, (_, _) => throw new InvalidOperationException("user bus unavailable"));
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => linux.RegisterAsync("BYO-Schedule-TEST", CreateStartInfo()));
            Assert.Equal("user bus unavailable", error.Message);
            var relative = new UnixScheduledCommandService(false, home, "relative", (_, _) => throw new InvalidOperationException("must not run"));
            Assert.Contains("absolute path", (await Assert.ThrowsAsync<InvalidOperationException>(() => relative.RegisterAsync("BYO-Schedule-TEST", CreateStartInfo()))).Message);
            var mac = new UnixScheduledCommandService(true, home, null, (_, _) => Task.FromResult("not-a-uid"));
            Assert.Contains("user ID", (await Assert.ThrowsAsync<InvalidOperationException>(() => mac.RegisterAsync("BYO-Schedule-TEST", CreateStartInfo()))).Message);
        }
        finally
        {
            DeleteHomeFiles(home);
        }
    }

    private static ProcessStartInfo CreateStartInfo()
    {
        var startInfo = new ProcessStartInfo { FileName = "/opt/BYO Tools/byo", WorkingDirectory = "/home/test/Jobs $literal %h" };
        foreach (var argument in new[] { ScheduledCommandService.WorkerArgument, "settings", "list", "--schedule", "2s", "a b", "%h ${HOME}" }) startInfo.ArgumentList.Add(argument);
        return startInfo;
    }

    private static string CreateHome() => Path.Combine(Path.GetTempPath(), "byo-unix-schedule-tests-" + Guid.NewGuid().ToString("N"));

    private static void DeleteHomeFiles(string home)
    {
        if (!Directory.Exists(home)) return;
        foreach (var file in Directory.GetFiles(home, "*", SearchOption.AllDirectories)) File.Delete(file);
        foreach (var directory in Directory.GetDirectories(home, "*", SearchOption.AllDirectories).OrderByDescending(path => path.Length)) Directory.Delete(directory);
        Directory.Delete(home);
    }
}
