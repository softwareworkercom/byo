using SoftwareWorker.BYO.CLI.Engine;
using SoftwareWorker.BYO.SDK.Services;
using SoftwareWorker.BYO.Tests.Support;

namespace SoftwareWorker.BYO.Tests;

public sealed class CommandsRouterTests
{
    [Fact]
    public void BuildRootCommand_ShouldContainTheBuiltInTrunkCommands()
    {
        var root = CommandsRouter.BuildRootCommand();

        var names = root.Subcommands.Select(command => command.Name).ToList();
        Assert.All(["alias", "commands", "plugins", "run", "secrets", "settings", "workflows"], name => Assert.Contains(name, names));
    }

    [Fact]
    public void Route_ShouldRunABuiltInCommand_AndReturnZero()
    {
        using var storage = new TemporaryStorageScope();
        using var console = new ConsoleScope();
        SettingsService.Update("Routed:Key", "value");

        var exitCode = CommandsRouter.Route(["settings", "list"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Routed:Key", console.Output);
    }

    [Fact]
    public void Route_ShouldReturnOne_ForUnknownCommands()
    {
        using var storage = new TemporaryStorageScope();
        using var console = new ConsoleScope();

        var exitCode = CommandsRouter.Route(["no-such-command"]);

        Assert.Equal(1, exitCode);
    }
}
