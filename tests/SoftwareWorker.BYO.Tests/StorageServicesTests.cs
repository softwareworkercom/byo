using SoftwareWorker.BYO.SDK.Model;
using SoftwareWorker.BYO.SDK.Model.Enums;
using SoftwareWorker.BYO.SDK.Services;
using SoftwareWorker.BYO.Tests.Support;
using System.Text.Json;

namespace SoftwareWorker.BYO.Tests;

/// <summary>
/// Edge cases of the settings, secrets, command and workflow services not covered by their main test classes.
/// </summary>
public sealed class StorageServicesTests : IDisposable
{
    private readonly TemporaryStorageScope _storage = new(isolateEncryptionKey: true);

    #region SettingsService

    [Fact]
    public void SettingsGet_ShouldReportMissingKeys_AndReturnRawJsonForNonStringValues()
    {
        using var console = new ConsoleScope();

        Assert.Null(SettingsService.Get("missing"));
        Assert.Contains("Key 'missing' was not found in Settings.", console.Output);

        SettingsService.Update("array", """["a","b"]""");

        // Arrays are stored as JSON and come back as their (re-indented) raw JSON text.
        Assert.Equal(["a", "b"], JsonSerializer.Deserialize<string[]>(SettingsService.Get("array")!));
        Assert.Equal(["a", "b"], JsonSerializer.Deserialize<string[]>(SettingsService.GetList("arr")!["array"]));
    }

    [Fact]
    public void SettingsGetArray_ShouldHandleEveryStoredShape()
    {
        using var console = new ConsoleScope();
        SettingsService.Update("json", """["a", "b"]""");
        SettingsService.Update("loose", "[x, \"y\"]");
        SettingsService.Update("plain", "single");
        SettingsService.Update("empty", "");
        SettingsService.Update("object", """{"a":1}""");
        SettingsService.Update("unterminated", "[a, b");

        Assert.Equal(["a", "b"], SettingsService.GetArray("json"));
        Assert.Equal(["x", "y"], SettingsService.GetArray("loose"));
        Assert.Equal(["single"], SettingsService.GetArray("plain"));
        Assert.Empty(SettingsService.GetArray("empty")!);
        Assert.Equal(["""{"a":1}"""], SettingsService.GetArray("object"));
        Assert.Equal(["[a, b"], SettingsService.GetArray("unterminated"));
        Assert.Null(SettingsService.GetArray("missing"));
        Assert.Contains("Key 'missing' was not found in Settings.", console.Output);
    }

    [Fact]
    public void SettingsGetArray_ShouldRejectValuesThatAreNeitherStringsNorArrays()
    {
        using var console = new ConsoleScope();
        File.WriteAllText(_storage.SettingsFilePath, """{ "number": 5, "flag": true }""");

        Assert.Null(SettingsService.GetArray("number"));
        Assert.Contains("Key 'number' is not a string array in Settings.", console.Output);
        Assert.Null(SettingsService.GetArray("flag", showErrorIfNotFound: false));
        Assert.Equal("5", SettingsService.Get("number"));
        Assert.True(SettingsService.GetBoolean("flag"));
    }

    [Fact]
    public void SettingsDelete_ShouldReportMissingKeys()
    {
        using var console = new ConsoleScope();

        SettingsService.Delete("missing");

        Assert.Contains("Key 'missing' was not found in Settings.", console.Output);
    }

    #endregion

    #region SecretsService

    [Fact]
    public void SecretsService_ShouldReportMissingKeys_AndRejectKeysUsedBySettings()
    {
        using var console = new ConsoleScope();

        Assert.Null(SecretsService.Get("missing"));
        Assert.Null(SecretsService.GetList());

        SecretsService.Update("present", "value");
        Assert.Null(SecretsService.Get("missing"));
        Assert.Equal("value", SecretsService.Get("present"));

        SecretsService.Delete("missing");

        SettingsService.Update("shared", "setting");
        Assert.Null(SecretsService.Update("shared", "secret"));
        Assert.Null(SettingsService.Update("present", "setting"));

        Assert.Contains("Key 'missing' was not found in Secrets.", console.Output);
        Assert.Contains("Cannot add key 'shared' to Secrets", console.Output);
        Assert.Contains("Cannot add key 'present' to Settings", console.Output);
    }

    #endregion

    #region CommandService

    [Fact]
    public void CommandService_UpdateByNameAndBookmark_ShouldMatchNameAndBookmarkIgnoringCase()
    {
        CommandService.Create("Build", "dotnet build", folderPath: "Ops");
        CommandService.Create("Build", "dotnet build", folderPath: null);

        var updated = CommandService.UpdateByNameAndBookmark("build", "/ops/", "dotnet test", "C:/x", ShellTypeEnum.Cmd);

        Assert.NotNull(updated);
        Assert.Equal("dotnet test", updated.Executable);
        Assert.Equal("C:/x", updated.Directory);
        Assert.Equal(ShellTypeEnum.Cmd, updated.Shell);
        Assert.NotNull(updated.UpdatedAt);
        Assert.Equal("dotnet build", CommandService.GetList().Single(command => command.Bookmark == null).Executable);
        Assert.Null(CommandService.UpdateByNameAndBookmark("build", "Other", "x"));
        Assert.Null(CommandService.UpdateByNameAndBookmark("nope", "Ops", "x"));
    }

    [Fact]
    public void CommandService_NormalizeFolderPath_ShouldReturnNullForBlankPaths()
    {
        Assert.Null(CommandService.NormalizeFolderPath(" / "));
        Assert.Null(CommandService.NormalizeFolderPath(null));
        Assert.Equal("a/b", CommandService.NormalizeFolderPath("/a/b/"));
    }

    #endregion

    #region WorkflowService

    [Fact]
    public void WorkflowService_GetByName_AndDeleteByWorkflow_ShouldIgnoreCase()
    {
        var workflow = WorkflowService.Create("Deploy", [new WorkflowStep { StepType = WorkflowStepTypeEnum.Message, Prompt = "hi" }]);

        Assert.NotNull(WorkflowService.GetByName("deploy"));
        Assert.Null(WorkflowService.GetByName("missing"));
        Assert.True(WorkflowService.Delete(workflow));
        Assert.False(WorkflowService.Delete(workflow));
        Assert.Null(WorkflowService.NormalizeFolderPath("  "));
    }

    #endregion

    public void Dispose()
    {
        _storage.Dispose();
    }
}
