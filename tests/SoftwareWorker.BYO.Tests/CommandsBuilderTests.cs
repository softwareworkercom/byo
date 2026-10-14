using SoftwareWorker.BYO.CLI.Engine;
using SoftwareWorker.BYO.CLI.Handlers.Settings;
using SoftwareWorker.BYO.SDK.Abstractions.Model.Command;
using SoftwareWorker.BYO.Tests.Support;
using System.CommandLine;
using System.CommandLine.Completions;

namespace SoftwareWorker.BYO.Tests;

public sealed class CommandsBuilderTests
{
    [Fact]
    public void LoadCommands_ShouldBuildTheCommandTree_WithBuiltInOptionsAndCompletions()
    {
        var root = BuildRoot();

        var sample = Assert.Single(root.Subcommands);
        Assert.Equal("sample", sample.Name);
        Assert.Equal(["--name", "--mode", "--schedule", "--async"], sample.Options.Select(option => option.Name));
        Assert.Equal(["throw", "dyn", "group", "missing", "plain"], sample.Subcommands.Select(command => command.Name));
        Assert.Equal("leaf", Assert.Single(sample.Subcommands.Single(command => command.Name == "group").Subcommands).Name);

        Assert.True(sample.TreatUnmatchedTokensAsErrors);
        Assert.False(sample.Subcommands.Single(command => command.Name == "dyn").TreatUnmatchedTokensAsErrors);

        var mode = sample.Options.Single(option => option.Name == "--mode");
        Assert.Equal(["fast", "slow"], mode.GetCompletions(CompletionContext.Empty).Select(item => item.Label));
        Assert.Empty(sample.Options.Single(option => option.Name == "--name").GetCompletions(CompletionContext.Empty));
    }

    [Fact]
    public async Task Invoke_ShouldBindParameters_AndReportCompletion()
    {
        using var console = new ConsoleScope();
        EchoHandler.LastInstance = null;

        var exitCode = await BuildRoot().Parse(["sample", "--name", "deploy", "--mode", "slow"]).InvokeAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(0, exitCode);
        Assert.Equal("deploy", EchoHandler.LastInstance?.Name);
        Assert.Equal("slow", EchoHandler.LastInstance?.Mode);
        Assert.Contains("echo deploy slow", console.Output);
        Assert.Contains("Completed in", console.Output);
    }

    [Fact]
    public async Task Invoke_ShouldRejectMissingRequiredParameters_WhenNotInteractive()
    {
        using var console = new ConsoleScope();
        EchoHandler.LastInstance = null;

        await BuildRoot().Parse(["sample", "--mode", "fast"]).InvokeAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Null(EchoHandler.LastInstance);
        Assert.Contains("Missing required parameter(s): --name.", console.Output);
    }

    [Fact]
    public async Task Invoke_ShouldRunLeafCommands_AndReportHandlerFailures()
    {
        using var console = new ConsoleScope();

        await BuildRoot().Parse(["sample", "group", "leaf", "--name", "leafy"]).InvokeAsync(cancellationToken: TestContext.Current.CancellationToken);
        await BuildRoot().Parse(["sample", "throw"]).InvokeAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("echo leafy", console.Output);
        Assert.Contains("InvalidOperationException: boom", console.Output);
        Assert.Contains("Failed after", console.Output);
    }

    [Fact]
    public async Task Invoke_ShouldRejectInvalidScheduleValues()
    {
        using var console = new ConsoleScope();
        EchoHandler.LastInstance = null;

        await BuildRoot().Parse(["sample", "--name", "x", "--schedule", "bogus"]).InvokeAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Null(EchoHandler.LastInstance);
        Assert.Contains("Invalid --schedule value", console.Output);
    }

    [Fact]
    public async Task Invoke_ShouldRunHandlersThatAcceptDynamicParameters()
    {
        using var console = new ConsoleScope();
        DynamicHandler.LastDynamicParameters = null;

        var exitCode = await BuildRoot().Parse(["sample", "dyn", "--extra", "value"]).InvokeAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(0, exitCode);
        Assert.NotNull(DynamicHandler.LastDynamicParameters);
        Assert.Contains("dynamic handler ran", console.Output);
    }

    [Fact]
    public async Task Invoke_ShouldFail_WhenTheHandlerCannotBeResolved()
    {
        using var console = new ConsoleScope();
        var configuration = new InvocationConfiguration { Error = TextWriter.Null, Output = TextWriter.Null };

        var missing = await BuildRoot().Parse(["sample", "missing"]).InvokeAsync(configuration, TestContext.Current.CancellationToken);
        var notAHandler = await BuildRoot().Parse(["sample", "plain"]).InvokeAsync(configuration, TestContext.Current.CancellationToken);

        Assert.NotEqual(0, missing);
        Assert.NotEqual(0, notAHandler);
    }

    [Fact]
    public void StaticHelpers_ShouldExposeVersionToolNameAndHandlers()
    {
        Assert.False(string.IsNullOrWhiteSpace(CommandsBuilder.GetCurrentVersion()));
        _ = CommandsBuilder.GetToolCommandName();

        var handlers = CommandsBuilder.GetAllCommandHandlers();

        Assert.Contains(typeof(SettingsListHandler), handlers);
        Assert.DoesNotContain(typeof(EchoHandler), handlers);
    }

    [Fact]
    public void CreateSelfProcessStartInfo_ShouldTargetTheCurrentProcess_AndForwardArguments()
    {
        var startInfo = CommandsBuilder.CreateSelfProcessStartInfo(["settings", "list", "--filter", "a b"]);

        Assert.Equal(Environment.ProcessPath, startInfo.FileName);
        Assert.False(startInfo.UseShellExecute);
        Assert.Equal(Environment.CurrentDirectory, startInfo.WorkingDirectory);

        // When tests run under the dotnet muxer ("dotnet SoftwareWorker.BYO.Tests.dll"), the entry
        // assembly is expected as the first argument so the relaunched process loads the right code.
        var entryPoint = Environment.GetCommandLineArgs().FirstOrDefault() ?? string.Empty;
        var expectedArguments = new List<string>();
        if (string.Equals(Path.GetFileNameWithoutExtension(Environment.ProcessPath), "dotnet", StringComparison.OrdinalIgnoreCase)
            && entryPoint.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            expectedArguments.Add(entryPoint);
        }

        expectedArguments.AddRange(["settings", "list", "--filter", "a b"]);
        Assert.Equal(expectedArguments, startInfo.ArgumentList);
    }

    private static RootCommand BuildRoot()
    {
        var trunk = new TrunkCommand
        {
            Name = "sample",
            Description = "Sample trunk command",
            Handler = typeof(EchoHandler).FullName,
            Parameters =
            [
                new Parameter { Name = "name", Description = "Name", IsRequired = true },
                new Parameter { Name = "mode", Description = "Mode", DefaultValue = "fast|slow" }
            ],
            BranchCommands =
            [
                new BranchCommand { Name = "throw", Description = "Throws", Handler = typeof(ThrowingHandler).FullName, Parameters = [] },
                new BranchCommand { Name = "dyn", Description = "Dynamic", Handler = typeof(DynamicHandler).FullName },
                new BranchCommand
                {
                    Name = "group",
                    Description = "Group",
                    LeafCommands =
                    [
                        new LeafCommand
                        {
                            Name = "leaf",
                            Description = "Leaf",
                            Handler = typeof(EchoHandler).FullName,
                            Parameters = [new Parameter { Name = "name", Description = "Name" }]
                        }
                    ]
                },
                new BranchCommand { Name = "missing", Description = "Missing handler", Handler = "No.Such.Handler" },
                new BranchCommand { Name = "plain", Description = "Not a handler", Handler = typeof(NotAHandler).FullName }
            ]
        };

        var root = new RootCommand();
        CommandsBuilder.LoadCommands(root, [trunk]);
        return root;
    }
}
