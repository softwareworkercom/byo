using SoftwareWorker.BYO.CLI.Abstractions.Model.Command;
using SoftwareWorker.BYO.CLI.Core.Constants;
using SoftwareWorker.BYO.CLI.Core.Engine;
using System.CommandLine;
using System.Reflection;

namespace SoftwareWorker.BYO.Tests;

public class CliTests
{
    [Fact]
    public void BuildFromReflection_ShouldDiscoverCliCommands()
    {
        var trunkCommands = CommandsScanner.BuildFromReflection();

        Assert.NotEmpty(trunkCommands);
        Assert.Contains(trunkCommands, command => command.Name == "commands");
        Assert.All(trunkCommands, command =>
        {
            Assert.False(string.IsNullOrWhiteSpace(command.Name));
            Assert.False(string.IsNullOrWhiteSpace(command.Description));
        });
    }

    [Fact]
    public void LoadCommands_ShouldParseAllExecutableCommandsWithBuiltInOptions()
    {
        var trunkCommands = CommandsScanner.BuildFromReflection();
        var rootCommand = BuildRootCommand(trunkCommands);
        var executableCommands = GetExecutableCommands(trunkCommands);

        Assert.NotEmpty(executableCommands);

        foreach (var executableCommand in executableCommands)
        {
            var args = new List<string>(executableCommand.Path);
            args.AddRange(BuildDeclaredParameterArguments(executableCommand.Parameters));
            args.Add("--schedule");
            args.Add("5m");
            args.Add("--interactive");
            args.Add("--export");
            args.Add("json");
            args.Add("--async");

            var parseResult = rootCommand.Parse(args.ToArray());
            Assert.Empty(parseResult.Errors);
        }
    }

    [Fact]
    public void Commands_ShouldRejectUnknownOptionByDefault()
    {
        var trunkCommands = CommandsScanner.BuildFromReflection();
        var rootCommand = BuildRootCommand(trunkCommands);
        var executable = GetExecutableCommands(trunkCommands).First();

        var parseResult = rootCommand.Parse([.. executable.Path, "--unknown-option", "value"]);

        Assert.NotEmpty(parseResult.Errors);
    }

    [Fact]
    public void BuildFromReflection_ShouldExposeRunTargetParameter()
    {
        var trunkCommands = CommandsScanner.BuildFromReflection();
        var runCommand = trunkCommands.SingleOrDefault(command => command.Name == "run");

        Assert.NotNull(runCommand);
        Assert.NotNull(runCommand!.Parameters);

        var targetParameter = runCommand.Parameters.SingleOrDefault(parameter => parameter.Name == "target");
        Assert.NotNull(targetParameter);
        Assert.Equal("command|workflow", targetParameter!.DefaultValue?.ToString());

        var nameParameter = runCommand.Parameters.SingleOrDefault(parameter => parameter.Name == "name");
        Assert.NotNull(nameParameter);

        var bookmarkParameter = runCommand.Parameters.SingleOrDefault(parameter => parameter.Name == "bookmark");
        Assert.NotNull(bookmarkParameter);
    }

    [Fact]
    public void BuildFromReflection_ShouldExposeExtensionsInstallCommand()
    {
        var trunkCommands = CommandsScanner.BuildFromReflection();
        var extensionsCommand = trunkCommands.SingleOrDefault(command => command.Name == "extensions");

        Assert.NotNull(extensionsCommand);
        Assert.NotNull(extensionsCommand!.BranchCommands);

        var installCommand = extensionsCommand.BranchCommands!.SingleOrDefault(command => command.Name == "install");
        Assert.NotNull(installCommand);
        Assert.NotNull(installCommand!.Parameters);

        var packageParameter = installCommand.Parameters!.SingleOrDefault(parameter => parameter.Name == "package");
        Assert.NotNull(packageParameter);
        Assert.True(packageParameter!.IsRequired);

        var versionParameter = installCommand.Parameters.SingleOrDefault(parameter => parameter.Name == "version");
        Assert.NotNull(versionParameter);
        Assert.False(versionParameter!.IsRequired);
    }

    [Fact]
    public void BuildFromReflection_ShouldExposeExtensionsListCommand()
    {
        var trunkCommands = CommandsScanner.BuildFromReflection();
        var extensionsCommand = trunkCommands.SingleOrDefault(command => command.Name == "extensions");

        Assert.NotNull(extensionsCommand);
        Assert.NotNull(extensionsCommand!.BranchCommands);

        var listCommand = extensionsCommand.BranchCommands!.SingleOrDefault(command => command.Name == "list");
        Assert.NotNull(listCommand);
        Assert.True(listCommand!.Parameters == null || !listCommand.Parameters.Any());
    }

    [Fact]
    public void BuildFromReflection_ShouldExposeExtensionsUninstallCommand()
    {
        var trunkCommands = CommandsScanner.BuildFromReflection();
        var extensionsCommand = trunkCommands.SingleOrDefault(command => command.Name == "extensions");

        Assert.NotNull(extensionsCommand);
        Assert.NotNull(extensionsCommand!.BranchCommands);

        var uninstallCommand = extensionsCommand.BranchCommands!.SingleOrDefault(command => command.Name == "uninstall");
        Assert.NotNull(uninstallCommand);
        Assert.NotNull(uninstallCommand!.Parameters);

        var packageParameter = uninstallCommand.Parameters!.SingleOrDefault(parameter => parameter.Name == "package");
        Assert.NotNull(packageParameter);
        Assert.True(packageParameter!.IsRequired);

        var versionParameter = uninstallCommand.Parameters.SingleOrDefault(parameter => parameter.Name == "version");
        Assert.NotNull(versionParameter);
        Assert.False(versionParameter!.IsRequired);
    }

    [Fact]
    public void BuildFromReflection_ShouldIncludeHandlersFromExtensionsDirectory()
    {
        var originalExtensionsDirectory = SystemConstants.EXTENSIONS_BINARIES_DIRECTORY;
        var tempRoot = Path.Combine(Path.GetTempPath(), "byo-tests", Guid.NewGuid().ToString("N"));
        var extensionDirectory = Path.Combine(tempRoot, "extensions");

        Directory.CreateDirectory(extensionDirectory);

        try
        {
            SystemConstants.EXTENSIONS_BINARIES_DIRECTORY = extensionDirectory;

            var sourceAssemblyPath = typeof(CommandsScanner).Assembly.Location;
            var destinationAssemblyPath = Path.Combine(extensionDirectory, Path.GetFileName(sourceAssemblyPath));
            File.Copy(sourceAssemblyPath, destinationAssemblyPath, overwrite: true);

            var trunkCommands = CommandsScanner.BuildFromReflection();

            Assert.Contains(trunkCommands, command => command.Name == "extensions");
        }
        finally
        {
            SystemConstants.EXTENSIONS_BINARIES_DIRECTORY = originalExtensionsDirectory;

            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, true);
            }
        }
    }

    [Theory]
    [InlineData("command")]
    [InlineData("workflow")]
    public void RunCommand_ShouldParseTargetValues(string target)
    {
        var trunkCommands = CommandsScanner.BuildFromReflection();
        var rootCommand = BuildRootCommand(trunkCommands);

        var parseResult = rootCommand.Parse(["run", "--target", target]);

        Assert.Empty(parseResult.Errors);
    }

    [Theory]
    [InlineData("command")]
    [InlineData("workflow")]
    public void RunCommand_ShouldParseNameAndBookmarkValues(string target)
    {
        var trunkCommands = CommandsScanner.BuildFromReflection();
        var rootCommand = BuildRootCommand(trunkCommands);

        var parseResult = rootCommand.Parse(["run", "--target", target, "--name", "deploy", "--bookmark", "DevOps/Prod"]);

        Assert.Empty(parseResult.Errors);
    }

    [Theory]
    [InlineData("00:00:05", true, 5)]
    [InlineData("30s", true, 30)]
    [InlineData("5m", true, 300)]
    [InlineData("1h", true, 3600)]
    [InlineData("1d", true, 86400)]
    [InlineData("1w", true, 604800)]
    [InlineData("1mo", true, 2592000)]
    [InlineData("0s", false, 0)]
    [InlineData("abc", false, 0)]
    [InlineData("", false, 0)]
    public void TryParseScheduleInterval_ShouldValidateAndParseValues(string input, bool expectedSuccess, int expectedSeconds)
    {
        var result = InvokeTryParseScheduleInterval(input);

        Assert.Equal(expectedSuccess, result.Success);
        if (expectedSuccess)
        {
            Assert.Equal(expectedSeconds, (int)result.Interval.TotalSeconds);
        }
    }

    [Fact]
    public void ExtractDynamicParameters_ShouldIgnoreKnownOptionsAndCaptureUnknownOnes()
    {
        var optionsMap = new Dictionary<string, Option<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = new Option<string>("--name")
        };

        var rawTokens = new[]
        {
            "command",
            "--name", "known-value",
            "--interactive",
            "--schedule", "5m",
            "--export", "json",
            "--async",
            "--custom", "abc",
            "--context:tenant", "prod"
        };

        var dynamicParameters = InvokeExtractDynamicParameters(rawTokens, optionsMap);

        Assert.Equal("abc", dynamicParameters["custom"]);
        Assert.Equal("prod", dynamicParameters["context:tenant"]);
        Assert.DoesNotContain("name", dynamicParameters.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("interactive", dynamicParameters.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("schedule", dynamicParameters.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("export", dynamicParameters.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("async", dynamicParameters.Keys, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void InferContextValue_ShouldBuildContextFromCommandSegments()
    {
        var context = InvokeInferContextValue(["command", "sub", "run", "--name", "value"]);

        Assert.Equal("byo-command-sub-run", context);
    }

    [Fact]
    public void BuildReplayCommand_ShouldRebuildCommandWithKnownOptions()
    {
        var rawTokens = new[] { "commands", "run", "--unknown", "x" };
        var options = new Dictionary<string, object>
        {
            ["name"] = "demo",
            ["schedule"] = "5m"
        };

        var replay = InvokeBuildReplayCommand(rawTokens, options);

        Assert.StartsWith("byo commands run", replay, StringComparison.Ordinal);
        Assert.Contains("--name 'demo'", replay, StringComparison.Ordinal);
        Assert.Contains("--schedule '5m'", replay, StringComparison.Ordinal);
        Assert.DoesNotContain("--unknown", replay, StringComparison.Ordinal);
    }

    private static RootCommand BuildRootCommand(List<TrunkCommand> trunkCommands)
    {
        var rootCommand = new RootCommand();
        CommandsBuilder.LoadCommands(rootCommand, trunkCommands);
        return rootCommand;
    }

    private static List<ExecutableCommand> GetExecutableCommands(List<TrunkCommand> trunkCommands)
    {
        var commands = new List<ExecutableCommand>();

        foreach (var trunk in trunkCommands)
        {
            if (!string.IsNullOrWhiteSpace(trunk.Handler))
            {
                commands.Add(new ExecutableCommand([trunk.Name], trunk.Parameters));
            }

            if (trunk.BranchCommands == null)
            {
                continue;
            }

            foreach (var branch in trunk.BranchCommands)
            {
                if (branch.LeafCommands is { Length: > 0 })
                {
                    foreach (var leaf in branch.LeafCommands)
                    {
                        commands.Add(new ExecutableCommand([trunk.Name, branch.Name, leaf.Name], leaf.Parameters));
                    }
                }
                else if (!string.IsNullOrWhiteSpace(branch.Handler))
                {
                    commands.Add(new ExecutableCommand([trunk.Name, branch.Name], branch.Parameters));
                }
            }
        }

        return commands;
    }

    private static IEnumerable<string> BuildDeclaredParameterArguments(IEnumerable<Parameter>? parameters)
    {
        if (parameters == null)
        {
            yield break;
        }

        foreach (var parameter in parameters)
        {
            yield return $"--{parameter.Name}";
            yield return parameter.DefaultValue?.ToString() ?? "value";
        }
    }

    private static (bool Success, TimeSpan Interval) InvokeTryParseScheduleInterval(string input)
    {
        var method = GetPrivateMethod("TryParseScheduleInterval");
        object[] args = [input, TimeSpan.Zero];
        var success = (bool)method.Invoke(null, args)!;
        return (success, (TimeSpan)args[1]);
    }

    private static Dictionary<string, string> InvokeExtractDynamicParameters(
        IEnumerable<string> rawTokens,
        Dictionary<string, Option<string>> optionsMap)
    {
        var method = GetPrivateMethod("ExtractDynamicParameters");
        var result = method.Invoke(null, [rawTokens, optionsMap]);
        return Assert.IsType<Dictionary<string, string>>(result);
    }

    private static string? InvokeInferContextValue(IReadOnlyList<string> rawTokens)
    {
        var method = GetPrivateMethod("InferContextValue");
        return (string?)method.Invoke(null, [rawTokens]);
    }

    private static string InvokeBuildReplayCommand(
        IEnumerable<string> rawTokens,
        IReadOnlyDictionary<string, object> options)
    {
        var method = GetPrivateMethod("BuildReplayCommand");
        var result = method.Invoke(null, [rawTokens, options]);
        return Assert.IsType<string>(result);
    }

    private static MethodInfo GetPrivateMethod(string name)
    {
        return typeof(CommandsBuilder).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)
               ?? throw new InvalidOperationException($"Method {name} not found.");
    }

    private sealed record ExecutableCommand(IReadOnlyList<string> Path, IEnumerable<Parameter>? Parameters);
}
