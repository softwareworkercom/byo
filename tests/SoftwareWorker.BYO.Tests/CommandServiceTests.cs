using SoftwareWorker.BYO.CLI.Core.Constants;
using SoftwareWorker.BYO.CLI.Core.Engine;
using SoftwareWorker.BYO.CLI.Core.Model;
using SoftwareWorker.BYO.CLI.Core.Service;
using SoftwareWorker.BYO.Core.Model.Enums;

namespace SoftwareWorker.BYO.Tests;

public sealed class CommandServiceTests : IDisposable
{
    private readonly string _originalCommandsFilePath;
    private readonly string _testStorageDirectory;

    public CommandServiceTests()
    {
        _originalCommandsFilePath = CommandService.CommandsFilePath;
        _testStorageDirectory = Path.Combine(Path.GetTempPath(), "byo-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testStorageDirectory);

        CommandService.CommandsFilePath = Path.Combine(_testStorageDirectory, "commands.json");
    }

    [Fact]
    public void Create_AndGetList_ShouldPersistCommandWithNormalizedBookmark()
    {
        var created = CommandService.Create(
            "Deploy",
            "dotnet run",
            workingDirectory: "C:/repo",
            shell: ShellTypeEnum.PowerShell,
            folderPath: " /DevOps/Deploy/ ");

        var commands = CommandService.GetList();

        Assert.Single(commands);
        Assert.Equal("Deploy", created.Name);
        Assert.Equal("dotnet run", created.Executable);
        Assert.Equal("C:/repo", created.Directory);
        Assert.Equal(ShellTypeEnum.PowerShell, created.Shell);
        Assert.Equal("DevOps/Deploy", created.Bookmark);
        Assert.Equal("Deploy", commands[0].Name);
    }

    [Fact]
    public void Update_ShouldChangeOnlyProvidedFields_AndSetUpdatedAt()
    {
        CommandService.Create("Build", "dotnet build", folderPath: "Build");

        var updated = CommandService.Update(
            "Build",
            newDescription: "Build.Api",
            executable: "dotnet build src/Api",
            shell: ShellTypeEnum.Cmd,
            folderPath: "/Pipelines/");

        Assert.NotNull(updated);
        Assert.Equal("Build.Api", updated.Name);
        Assert.Equal("dotnet build src/Api", updated.Executable);
        Assert.Equal(ShellTypeEnum.Cmd, updated.Shell);
        Assert.Equal("Pipelines", updated.Bookmark);
        Assert.NotNull(updated.UpdatedAt);
    }

    [Fact]
    public void Update_ShouldReturnNull_WhenCommandDoesNotExist()
    {
        var updated = CommandService.Update("Missing.Command", executable: "echo test");

        Assert.Null(updated);
    }

    [Fact]
    public void Delete_ShouldRemoveMatchingCommand()
    {
        CommandService.Create("CmdA", "dotnet test", workingDirectory: "C:/repo");
        var commandB = CommandService.Create("CmdB", "dotnet publish", workingDirectory: "C:/repo");

        var deleted = CommandService.Delete(commandB);
        var commands = CommandService.GetList();

        Assert.True(deleted);
        Assert.Single(commands);
        Assert.Equal("CmdA", commands[0].Name);
    }

    [Fact]
    public void Delete_ShouldReturnFalse_WhenCommandDoesNotExist()
    {
        CommandService.Create("CmdA", "dotnet test", workingDirectory: "C:/repo");

        var deleted = CommandService.Delete(new ShellCommand
        {
            Name = "CmdB",
            Executable = "dotnet publish",
            Directory = "C:/repo"
        });

        Assert.False(deleted);
        Assert.Single(CommandService.GetList());
    }

    public void Dispose()
    {
        CommandService.CommandsFilePath = _originalCommandsFilePath;

        if (Directory.Exists(_testStorageDirectory))
        {
            Directory.Delete(_testStorageDirectory, recursive: true);
        }
    }

}

public sealed class CommandCliTests : IDisposable
{
    private readonly string _originalCommandsFilePath;
    private readonly string _testStorageDirectory;

    public CommandCliTests()
    {
        _originalCommandsFilePath = CommandService.CommandsFilePath;
        _testStorageDirectory = Path.Combine(Path.GetTempPath(), "byo-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testStorageDirectory);

        CommandService.CommandsFilePath = Path.Combine(_testStorageDirectory, "commands.json");
    }

    [Fact]
    public void Route_Set_ShouldPersistCommand()
    {
        var commandName = $"cli-create-{Guid.NewGuid():N}";

        var exitCode = CommandsRouter.Route([
            "commands",
            "set",
            "--name", commandName,
            "--executable", "dotnet --version",
            "--bookmark", "/DevOps/Build/",
            "--directory", "C:/repo"
        ]);

        var commands = CommandService.GetList();
        var created = commands.SingleOrDefault(c => c.Name == commandName);

        Assert.Equal(0, exitCode);
        Assert.NotNull(created);
        Assert.Equal("dotnet --version", created.Executable);
        Assert.Equal("DevOps/Build", created.Bookmark);
        Assert.Equal("C:/repo", created.Directory);
    }

    [Fact]
    public void Route_Set_ShouldPersistCommandWithProvidedShell()
    {
        var commandName = $"cli-set-{Guid.NewGuid():N}";

        var exitCode = CommandsRouter.Route([
            "commands",
            "set",
            "--name", commandName,
            "--executable", "dotnet test",
            "--bookmark", "/Pipelines/CI/",
            "--shell", "Cmd"
        ]);

        var commands = CommandService.GetList();
        var created = commands.SingleOrDefault(c => c.Name == commandName);

        Assert.Equal(0, exitCode);
        Assert.NotNull(created);
        Assert.Equal("dotnet test", created.Executable);
        Assert.Equal("Pipelines/CI", created.Bookmark);
        Assert.Equal(ShellTypeEnum.Cmd, created.Shell);
    }

    [Fact]
    public void Route_Set_ShouldNotPersist_WhenRequiredParameterIsMissing()
    {
        var commandName = $"cli-missing-{Guid.NewGuid():N}";

        var exitCode = CommandsRouter.Route([
            "commands",
            "set",
            "--name", commandName
        ]);

        var commands = CommandService.GetList();

        Assert.Equal(0, exitCode);
        Assert.DoesNotContain(commands, c => c.Name == commandName);
    }

    public void Dispose()
    {
        CommandService.CommandsFilePath = _originalCommandsFilePath;

        if (Directory.Exists(_testStorageDirectory))
        {
            Directory.Delete(_testStorageDirectory, recursive: true);
        }
    }
}
