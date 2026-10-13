using SoftwareWorker.BYO.CLI.Handlers.Alias;
using SoftwareWorker.BYO.CLI.Handlers.Commands;
using SoftwareWorker.BYO.CLI.Handlers.Plugins;
using SoftwareWorker.BYO.CLI.Handlers.Run;
using SoftwareWorker.BYO.CLI.Handlers.Settings;
using SoftwareWorker.BYO.CLI.Handlers.Workflows;
using SoftwareWorker.BYO.SDK.Abstractions.Attributes;
using SoftwareWorker.BYO.SDK.Constants;
using SoftwareWorker.BYO.SDK.Handlers.Secrets;
using SoftwareWorker.BYO.SDK.Model;
using SoftwareWorker.BYO.SDK.Model.Enums;
using SoftwareWorker.BYO.SDK.Services;
using SoftwareWorker.BYO.Tests.Support;
using System.IO.Compression;
using System.Reflection;
using System.Text.Json;

namespace SoftwareWorker.BYO.Tests;

/// <summary>
/// Runs the built-in command handlers against temporary storage, driving their prompts through a test console.
/// </summary>
public sealed class CliHandlerTests : IDisposable
{
    private readonly TemporaryStorageScope _storage = new(isolateEncryptionKey: true);

    private static ShellTypeEnum LocalShell => OperatingSystem.IsWindows() ? ShellTypeEnum.Cmd : ShellTypeEnum.Sh;

    private static WorkflowStep MessageStep(string prompt) => new() { StepType = WorkflowStepTypeEnum.Message, Prompt = prompt };

    #region Settings

    [Fact]
    public async Task SettingsList_ShouldWarnWhenEmpty_AndRenderTheSettingsTable()
    {
        using var console = new ConsoleScope();

        await new SettingsListHandler().ExecuteAsync();
        Assert.Contains("No settings found.", console.Output);

        SettingsService.Update("Alpha", "1");
        SettingsService.Update("Beta", "plain [value]");
        await new SettingsListHandler().ExecuteAsync();

        Assert.Contains("Alpha", console.Output);
        Assert.Contains("plain [value]", console.Output);
    }

    [Fact]
    public async Task SettingsSet_ShouldStoreTheValue_AndAskBeforeReplacingIt()
    {
        using var console = new ConsoleScope(interactive: true);

        await new SettingsSetCommand { Key = "K", Value = "v1" }.ExecuteAsync();
        Assert.Equal("v1", SettingsService.Get("K"));
        Assert.Contains("Key=K, Value=v1", console.Output);

        console.Input.PushTextWithEnter("n");
        await new SettingsSetCommand { Key = "K", Value = "v2" }.ExecuteAsync();
        Assert.Equal("v1", SettingsService.Get("K"));
        Assert.Contains("Setting update cancelled.", console.Output);

        console.PushKeys(ConsoleKey.Enter);
        await new SettingsSetCommand { Key = "K", Value = "v3" }.ExecuteAsync();
        Assert.Equal("v3", SettingsService.Get("K"));
    }

    [Fact]
    public async Task SettingsSet_ShouldPromptForTheKeyAndValue_WhenTheyAreMissing()
    {
        using var console = new ConsoleScope(interactive: true);

        await new SettingsSetCommand().ExecuteAsync();
        Assert.Contains("No settings found.", console.Output);

        SettingsService.Update("A", "1");
        SettingsService.Update("B", "2");
        console.PushKeys(ConsoleKey.DownArrow, ConsoleKey.Enter);
        console.Input.PushTextWithEnter("new");
        console.PushKeys(ConsoleKey.Enter);

        await new SettingsSetCommand().ExecuteAsync();

        Assert.Equal("new", SettingsService.Get("B"));
        Assert.Equal("1", SettingsService.Get("A"));
    }

    [Fact]
    public async Task SettingsDelete_ShouldRequireAKey_AndDeleteTheSetting()
    {
        using var console = new ConsoleScope();
        SettingsService.Update("K", "v");

        await new SettingsDeleteCommand().ExecuteAsync();
        Assert.Contains("Key is required for delete.", console.Output);
        Assert.Equal("v", SettingsService.Get("K"));

        await new SettingsDeleteCommand { Key = "K" }.ExecuteAsync();
        Assert.Null(SettingsService.Get("K", showErrorIfNotFound: false));
        Assert.Contains("Deleted setting with Key=K", console.SystemOutput);
    }

    #endregion

    #region Secrets

    [Fact]
    public async Task SecretsSet_ShouldRequireKeyAndValue_AndAskBeforeReplacing()
    {
        using var console = new ConsoleScope(interactive: true);

        await new SecretsSetCommand { Key = "K" }.ExecuteAsync();
        Assert.Contains("Key and Value are required parameters.", console.Output);

        await new SecretsSetCommand { Key = "K", Value = "v1" }.ExecuteAsync();
        Assert.Equal("v1", SecretsService.Get("K"));

        console.Input.PushTextWithEnter("n");
        await new SecretsSetCommand { Key = "K", Value = "v2" }.ExecuteAsync();
        Assert.Equal("v1", SecretsService.Get("K"));
        Assert.Contains("Secret update cancelled.", console.Output);

        console.PushKeys(ConsoleKey.Enter);
        await new SecretsSetCommand { Key = "K", Value = "v3" }.ExecuteAsync();
        Assert.Equal("v3", SecretsService.Get("K"));
    }

    [Fact]
    public async Task SecretsReencrypt_ShouldRotateTheKey_AndKeepValuesReadable()
    {
        using var console = new ConsoleScope();

        await new SecretsReencryptCommand().ExecuteAsync();

        SecretsService.Update("A", "1");
        var rawBefore = StorageService.LoadDictionary(_storage.SecretsFilePath)["A"];
        var keyBefore = KeyManagementService.Get();

        await new SecretsReencryptCommand().ExecuteAsync();

        Assert.NotEqual(keyBefore, KeyManagementService.Get());
        Assert.NotEqual(rawBefore, StorageService.LoadDictionary(_storage.SecretsFilePath)["A"]);
        Assert.Equal("1", SecretsService.Get("A"));
        Assert.Contains("Secrets re-encrypted with new key.", console.Output);
    }

    #endregion

    #region Commands

    [Fact]
    public async Task CommandList_ShouldWarnWhenEmpty_AndGroupCommandsByBookmark()
    {
        using var console = new ConsoleScope();

        await new CommandListHandler().ExecuteAsync();
        Assert.Contains("No commands found.", console.Output);

        CommandService.Create("Build", "dotnet build", "C:/repo");
        CommandService.Create("Deploy", "dotnet deploy", folderPath: "Ops/Deploy");
        await new CommandListHandler().ExecuteAsync();

        Assert.Contains("(root)", console.Output);
        Assert.Contains("Ops/Deploy", console.Output);
        Assert.Contains("dotnet build", console.Output);
        Assert.Contains("C:/repo", console.Output);
    }

    [Fact]
    public async Task CommandSet_ShouldValidateInput_CreateCommands_AndUpdateExistingOnes()
    {
        using var console = new ConsoleScope(interactive: true);

        await new CommandSetHandler().ExecuteAsync();
        Assert.Contains("Command name is required", console.Output);

        await new CommandSetHandler { Name = "Build" }.ExecuteAsync();
        Assert.Contains("Command executable is required", console.Output);

        await new CommandSetHandler { Name = "Build", Executable = "dotnet build", Shell = "cmd", Bookmark = "/Ops/" }.ExecuteAsync();
        var created = Assert.Single(CommandService.GetList());
        Assert.Equal(ShellTypeEnum.Cmd, created.Shell);
        Assert.Equal("Ops", created.Bookmark);
        Assert.Contains("Command added successfully!", console.Output);

        console.Input.PushTextWithEnter("n");
        await new CommandSetHandler { Name = "build", Executable = "other", Bookmark = "Ops" }.ExecuteAsync();
        Assert.Equal("dotnet build", Assert.Single(CommandService.GetList()).Executable);
        Assert.Contains("Command update cancelled.", console.Output);

        console.PushKeys(ConsoleKey.Enter);
        await new CommandSetHandler { Name = "build", Executable = "dotnet build -c Release", Bookmark = "ops", Directory = "C:/repo", Shell = "Wsl" }.ExecuteAsync();
        var updated = Assert.Single(CommandService.GetList());
        Assert.Equal("dotnet build -c Release", updated.Executable);
        Assert.Equal("C:/repo", updated.Directory);
        Assert.Equal(ShellTypeEnum.Wsl, updated.Shell);
        Assert.Contains("Command updated successfully!", console.Output);

        await new CommandSetHandler { Name = "Other", Executable = "echo", Shell = "bogus" }.ExecuteAsync();
        Assert.Null(CommandService.GetList().Single(command => command.Name == "Other").Shell);
    }

    [Fact]
    public async Task CommandDelete_ShouldWarnWhenEmpty_ThenSelectAndConfirm()
    {
        using var console = new ConsoleScope(interactive: true);

        await new CommandDeleteHandler().ExecuteAsync();
        Assert.Contains("No saved commands found.", console.Output);

        CommandService.Create("Alpha", "echo a");
        CommandService.Create("Beta", "echo b");

        console.PushKeys(ConsoleKey.Enter);
        console.Input.PushTextWithEnter("n");
        await new CommandDeleteHandler().ExecuteAsync();
        Assert.Equal(2, CommandService.GetList().Count);
        Assert.Contains("Deletion cancelled.", console.Output);

        console.PushKeys(ConsoleKey.Enter, ConsoleKey.Enter);
        await new CommandDeleteHandler().ExecuteAsync();
        Assert.Equal("Beta", Assert.Single(CommandService.GetList()).Name);
        Assert.Contains("Command 'Alpha' deleted successfully.", console.Output);
    }

    #endregion

    #region Workflows

    [Fact]
    public async Task WorkflowList_ShouldWarnWhenEmpty_AndRenderAPanelPerWorkflow()
    {
        using var console = new ConsoleScope();

        await new WorkflowListHandler().ExecuteAsync();
        Assert.Contains("No workflows found.", console.Output);

        WorkflowService.Create("Empty", [], "Ops");
        WorkflowService.Create("Steps", [MessageStep("hi"), new WorkflowStep { StepType = WorkflowStepTypeEnum.ExecuteCommand, CommandExecutable = "echo" }]);
        await new WorkflowListHandler().ExecuteAsync();

        Assert.Contains("No steps configured", console.Output);
        Assert.Contains("Execute Command", console.Output);
        Assert.Contains("Total workflows: 2", console.Output);
    }

    [Fact]
    public async Task WorkflowDelete_ShouldWarnWhenEmpty_ThenSelectAndConfirm()
    {
        using var console = new ConsoleScope(interactive: true);

        await new WorkflowDeleteHandler().ExecuteAsync();
        Assert.Contains("No workflows found.", console.Output);

        WorkflowService.Create("Alpha", [MessageStep("a")]);
        WorkflowService.Create("Beta", [MessageStep("b")]);

        console.PushKeys(ConsoleKey.Enter, ConsoleKey.DownArrow, ConsoleKey.Enter);
        await new WorkflowDeleteHandler().ExecuteAsync();
        Assert.Equal(2, WorkflowService.GetList().Count);
        Assert.Contains("Deletion cancelled.", console.Output);

        console.PushKeys(ConsoleKey.Enter, ConsoleKey.Enter);
        await new WorkflowDeleteHandler().ExecuteAsync();
        Assert.Equal("Beta", Assert.Single(WorkflowService.GetList()).Name);
        Assert.Contains("Workflow 'Alpha' deleted successfully.", console.Output);
    }

    [Fact]
    public async Task WorkflowSet_ShouldCancel_WhenNoStepsAreAdded()
    {
        using var console = new ConsoleScope(interactive: true);
        PushDone(console);

        await new WorkflowSetHandler { Name = "W", Bookmark = "Ops" }.ExecuteAsync();

        Assert.Empty(WorkflowService.GetList());
        Assert.Contains("No steps entered. Workflow creation cancelled.", console.Output);
    }

    [Fact]
    public async Task WorkflowSet_ShouldBuildEveryStepTypeInteractively()
    {
        using var console = new ConsoleScope(interactive: true);
        CommandService.Create("Saved", "echo saved");

        // 1. Message: prompt, color (Red, Yellow, Blue...), wait for enter
        console.PushKeys(ConsoleKey.Enter);
        console.Input.PushTextWithEnter("Hello");
        console.PushKeys(ConsoleKey.DownArrow, ConsoleKey.DownArrow, ConsoleKey.Enter);
        console.PushKeys(ConsoleKey.Enter);

        // 2. Yes/No question: prompt, interrupt on No
        console.PushKeys(ConsoleKey.DownArrow, ConsoleKey.Enter);
        console.Input.PushTextWithEnter("Continue?");
        console.PushKeys(ConsoleKey.DownArrow, ConsoleKey.Enter);

        // 3. Input as setting: prompt, key
        console.PushKeys(ConsoleKey.DownArrow, ConsoleKey.DownArrow, ConsoleKey.Enter);
        console.Input.PushTextWithEnter("Setting prompt");
        console.Input.PushTextWithEnter("Setting:Key");

        // 4. Input as secret: prompt, key
        console.PushKeys(ConsoleKey.DownArrow, ConsoleKey.DownArrow, ConsoleKey.DownArrow, ConsoleKey.Enter);
        console.Input.PushTextWithEnter("Secret prompt");
        console.Input.PushTextWithEnter("Secret:Key");

        // 5. Execute a custom command: executable, no directory, pick a shell (Cmd), not async
        console.PushKeys(ConsoleKey.DownArrow, ConsoleKey.DownArrow, ConsoleKey.DownArrow, ConsoleKey.DownArrow, ConsoleKey.Enter);
        console.PushKeys(ConsoleKey.Enter);
        console.Input.PushTextWithEnter("echo custom");
        console.PushKeys(ConsoleKey.Enter);
        console.PushKeys(ConsoleKey.Enter);
        console.PushKeys(ConsoleKey.DownArrow, ConsoleKey.Enter);
        console.PushKeys(ConsoleKey.DownArrow, ConsoleKey.Enter);

        // 6. Execute the saved command asynchronously
        console.PushKeys(ConsoleKey.DownArrow, ConsoleKey.DownArrow, ConsoleKey.DownArrow, ConsoleKey.DownArrow, ConsoleKey.Enter);
        console.PushKeys(ConsoleKey.DownArrow, ConsoleKey.Enter);
        console.PushKeys(ConsoleKey.Enter);

        PushDone(console);

        await new WorkflowSetHandler { Name = "W", Bookmark = "/Ops/" }.ExecuteAsync();

        var workflow = Assert.Single(WorkflowService.GetList());
        Assert.Equal("Ops", workflow.Bookmark);
        Assert.Equal(6, workflow.Steps.Count);

        Assert.Equal((WorkflowStepTypeEnum.Message, "Hello", MessageColorEnum.Blue, true), (workflow.Steps[0].StepType, workflow.Steps[0].Prompt, workflow.Steps[0].Color, workflow.Steps[0].WaitForEnter));
        Assert.Equal((WorkflowStepTypeEnum.YesNoQuestion, "Continue?", false), (workflow.Steps[1].StepType, workflow.Steps[1].Prompt, workflow.Steps[1].InterruptOnNo));
        Assert.Equal((WorkflowStepTypeEnum.InputAsSetting, "Setting prompt", "Setting:Key"), (workflow.Steps[2].StepType, workflow.Steps[2].Prompt, workflow.Steps[2].StorageKey));
        Assert.Equal((WorkflowStepTypeEnum.InputAsSecret, "Secret prompt", "Secret:Key"), (workflow.Steps[3].StepType, workflow.Steps[3].Prompt, workflow.Steps[3].StorageKey));
        Assert.Equal((WorkflowStepTypeEnum.ExecuteCommand, "echo custom", (string?)null, ShellTypeEnum.Cmd, false), (workflow.Steps[4].StepType, workflow.Steps[4].CommandExecutable, workflow.Steps[4].CommandDirectory, workflow.Steps[4].CommandShell, workflow.Steps[4].RunAsync));
        Assert.Equal((WorkflowStepTypeEnum.ExecuteCommand, "Saved", true), (workflow.Steps[5].StepType, workflow.Steps[5].CommandName, workflow.Steps[5].RunAsync));
        Assert.Contains("Workflow 'W' created successfully with 6 step(s).", console.Output);
    }

    [Fact]
    public async Task WorkflowSet_ShouldOfferToOverrideAnExistingWorkflow()
    {
        using var console = new ConsoleScope(interactive: true);
        WorkflowService.Create("W", [MessageStep("old")], "Ops");

        PushMessageStep(console, "new");
        PushDone(console);
        console.PushKeys(ConsoleKey.DownArrow, ConsoleKey.Enter);
        await new WorkflowSetHandler { Name = "W", Bookmark = "Ops" }.ExecuteAsync();
        Assert.Equal("old", Assert.Single(WorkflowService.GetList()).Steps[0].Prompt);
        Assert.Contains("Workflow creation cancelled.", console.Output);

        PushMessageStep(console, "new");
        PushDone(console);
        console.PushKeys(ConsoleKey.Enter);
        await new WorkflowSetHandler { Name = "W", Bookmark = "Ops" }.ExecuteAsync();
        Assert.Equal("new", Assert.Single(WorkflowService.GetList()).Steps[0].Prompt);
        Assert.Contains("Workflow 'W' overridden successfully with 1 step(s).", console.Output);
    }

    private static void PushMessageStep(ConsoleScope console, string prompt)
    {
        console.PushKeys(ConsoleKey.Enter);
        console.Input.PushTextWithEnter(prompt);
        console.PushKeys(ConsoleKey.Enter);
        console.PushKeys(ConsoleKey.DownArrow, ConsoleKey.Enter);
    }

    private static void PushDone(ConsoleScope console)
    {
        var stepTypes = Enum.GetValues<WorkflowStepTypeEnum>().Length;
        for (var index = 0; index < stepTypes; index++)
        {
            console.PushKeys(ConsoleKey.DownArrow);
        }

        console.PushKeys(ConsoleKey.Enter);
    }

    #endregion

    #region Plugins

    [Fact]
    public async Task PluginUninstall_ShouldRequireAPackage_AndRemoveInstalledVersions()
    {
        using var console = new ConsoleScope();
        var packages = SystemConstants.PLUGINS_PACKAGES_DIRECTORY;
        var binaries = SystemConstants.PLUGINS_BINARIES_DIRECTORY;

        await new PluginUninstallHandler().ExecuteAsync();
        Assert.Contains("Package id is required.", console.Output);

        await new PluginUninstallHandler { Package = "BYO.Plugin.X" }.ExecuteAsync();
        Assert.Contains("No installed plugin files were found for 'BYO.Plugin.X'.", console.Output);

        File.WriteAllText(CreateFile(packages, "byo.plugin.x", "1.0.0", "x.nupkg"), "package");
        File.WriteAllText(CreateFile(binaries, "byo.plugin.x", "1.0.0", "x.dll"), "binary");
        File.WriteAllText(CreateFile(binaries, "byo.plugin.x", "2.0.0", "x.dll"), "binary");

        await new PluginUninstallHandler { Package = "BYO.Plugin.X", Version = "3.0.0" }.ExecuteAsync();
        Assert.Contains("Plugin 'BYO.Plugin.X' version '3.0.0' was not found.", console.Output);

        await new PluginUninstallHandler { Package = " BYO.Plugin.X ", Version = "1.0.0" }.ExecuteAsync();
        Assert.Contains("Uninstalled plugin 'BYO.Plugin.X' version '1.0.0'.", console.Output);
        Assert.False(Directory.Exists(Path.Combine(packages, "byo.plugin.x")));
        Assert.True(Directory.Exists(Path.Combine(binaries, "byo.plugin.x", "2.0.0")));

        await new PluginUninstallHandler { Package = "BYO.Plugin.X" }.ExecuteAsync();
        Assert.Contains("Uninstalled plugin 'BYO.Plugin.X' (all versions).", console.Output);
        Assert.False(Directory.Exists(Path.Combine(binaries, "byo.plugin.x")));
    }

    [Fact]
    public async Task PluginList_ShouldWarnWithoutSources_ReportUnsearchableSources_AndListPackages()
    {
        using var console = new ConsoleScope();
        SettingsService.Update(SystemConstants.SYSTEM_PluginSourcesUseNuGetOrg, "false");

        await new PluginListHandler().ExecuteAsync();
        Assert.Contains("No package sources are configured.", console.Output);

        var missing = Path.Combine(_storage.Root, "missing-feed");
        SettingsService.Update(SystemConstants.SYSTEM_PluginSources, JsonSerializer.Serialize(new[] { $"missing={missing}" }));
        await new PluginListHandler().ExecuteAsync();
        Assert.Contains("Source 'missing' could not be searched", console.Output);
        Assert.Contains("No plugins found in missing.", console.Output);

        var feed = Path.Combine(_storage.Root, "feed");
        WriteNupkg(feed, "BYO.Plugin.Weather", "1.0.0");
        WriteNupkg(feed, "BYO.Plugin.Weather", "1.1.0");
        WriteNupkg(feed, "Other.Package", "1.0.0");
        SettingsService.Update(SystemConstants.SYSTEM_PluginSources, JsonSerializer.Serialize(new[] { $"local={feed}" }));
        await new PluginListHandler().ExecuteAsync();

        Assert.Contains("BYO.Plugin.Weather", console.Output);
        Assert.Contains("1.1.0", console.Output);
        Assert.DoesNotContain("Other.Package", console.Output);
        Assert.Contains("Total plugins: 1", console.Output);
    }

    [Fact]
    public async Task PluginInstall_ShouldRequireAPackage_AndReportPackagesThatCannotBeFound()
    {
        using var console = new ConsoleScope();
        SettingsService.Update(SystemConstants.SYSTEM_PluginSourcesUseNuGetOrg, "false");
        var feed = Path.Combine(_storage.Root, "feed");
        Directory.CreateDirectory(feed);
        SettingsService.Update(SystemConstants.SYSTEM_PluginSources, JsonSerializer.Serialize(new[] { $"local={feed}" }));

        await new PluginInstallHandler().ExecuteAsync();
        Assert.Contains("Package id is required.", console.Output);

        await new PluginInstallHandler { Package = " BYO.Plugin.Missing " }.ExecuteAsync();

        Assert.Contains("Installing BYO.Plugin.Missing...", console.Output);
        Assert.Contains("Package 'BYO.Plugin.Missing' was not found in local.", console.Output);
    }

    private static string CreateFile(string root, params string[] segments)
    {
        var path = Path.Combine([root, .. segments]);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        return path;
    }

    private static void WriteNupkg(string directory, string packageId, string version)
    {
        Directory.CreateDirectory(directory);

        using var stream = File.Create(Path.Combine(directory, $"{packageId}.{version}.nupkg"));
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create);
        using var writer = new StreamWriter(archive.CreateEntry($"{packageId}.nuspec").Open());
        writer.Write($"<package><metadata><id>{packageId}</id><version>{version}</version><description>{packageId}</description></metadata></package>");
    }

    #endregion

    #region Alias

    [Fact]
    public async Task AliasDelete_ShouldWarnWithoutAnAlias_ThenConfirmBeforeRemovingIt()
    {
        using var console = new ConsoleScope(interactive: true);

        await new AliasDeleteHandler().ExecuteAsync();
        Assert.Contains("No alias is currently set.", console.Output);

        SettingsService.Update(SystemConstants.SYSTEM_Alias, "sw");
        console.Input.PushTextWithEnter("n");
        await new AliasDeleteHandler().ExecuteAsync();
        Assert.Equal("sw", SettingsService.Get(SystemConstants.SYSTEM_Alias));
        Assert.Contains("Alias removal cancelled.", console.Output);

        console.PushKeys(ConsoleKey.Enter);
        await new AliasDeleteHandler().ExecuteAsync();
        Assert.Null(SettingsService.Get(SystemConstants.SYSTEM_Alias, showErrorIfNotFound: false));
        Assert.Contains("Alias 'sw' removed from settings.", console.Output);
    }

    [Fact]
    public async Task AliasSet_ShouldNotDeploy_WhenTheSameAliasIsAlreadyConfigured()
    {
        using var console = new ConsoleScope(interactive: true);
        SettingsService.Update(SystemConstants.SYSTEM_Alias, "sw");
        console.Input.PushTextWithEnter("1bad");
        console.Input.PushTextWithEnter("SW");

        await new AliasSetHandler().ExecuteAsync();

        Assert.Contains("Current alias: 'sw'", console.Output);
        Assert.Contains("Alias must start with a letter", console.Output);
        Assert.Contains("Alias 'SW' is already set.", console.Output);
        Assert.Equal("sw", SettingsService.Get(SystemConstants.SYSTEM_Alias));
    }

    #endregion

    #region Run

    [Fact]
    public async Task Run_ShouldWarn_WhenThereIsNothingToRun()
    {
        using var console = new ConsoleScope();

        await new RunHandler { Target = RunTargetEnum.Command }.ExecuteAsync();
        await new RunHandler { Target = RunTargetEnum.Workflow }.ExecuteAsync();

        Assert.Contains("No commands found. Use 'byo commands create' first.", console.Output);
        Assert.Contains("No workflows found. Use 'byo workflows create' first.", console.Output);
    }

    [Fact]
    public async Task Run_ShouldPromptForTheTarget_WhenItIsNotProvided()
    {
        using var console = new ConsoleScope(interactive: true);
        console.PushKeys(ConsoleKey.DownArrow, ConsoleKey.Enter);

        await new RunHandler().ExecuteAsync();

        Assert.Contains("Select what to run:", console.Output);
        Assert.Contains("No workflows found.", console.Output);
    }

    [Fact]
    public async Task Run_ShouldWarn_WhenTheNamedItemDoesNotExist()
    {
        using var console = new ConsoleScope();
        CommandService.Create("A", "echo a");
        WorkflowService.Create("W", [MessageStep("w")]);

        await new RunHandler { Target = RunTargetEnum.Command, Name = "Nope" }.ExecuteAsync();
        await new RunHandler { Target = RunTargetEnum.Workflow, Name = "Nope", Bookmark = "Ops" }.ExecuteAsync();

        Assert.Contains("Command 'Nope' was not found in bookmark '/'.", console.Output);
        Assert.Contains("Workflow 'Nope' was not found in bookmark 'Ops'.", console.Output);
    }

    [Fact]
    public void Run_ShouldAllowDynamicParameters_ForCommandLineOverrides()
    {
        Assert.NotNull(typeof(RunHandler).GetCustomAttributes(typeof(AllowDynamicParametersAttribute), inherit: true).SingleOrDefault());
    }

    [Fact]
    public async Task Run_ShouldExecuteTheNamedCommandAndWorkflow()
    {
        using var console = new ConsoleScope();
        CommandService.Create("Say", "echo run-handler-command", shell: LocalShell);
        WorkflowService.Create("W", [MessageStep("run-handler-workflow")], "Ops");

        await new RunHandler { Target = RunTargetEnum.Command, Name = "say" }.ExecuteAsync();
        await new RunHandler { Target = RunTargetEnum.Workflow, Name = "w", Bookmark = "/Ops/" }.ExecuteAsync();

        Assert.Contains("run-handler-command", console.Output);
        Assert.Contains("run-handler-workflow", console.Output);
    }

    [Fact]
    public async Task Run_ShouldLetTheUserPickACommandOrWorkflow_WhenNoNameIsGiven()
    {
        using var console = new ConsoleScope(interactive: true);
        CommandService.Create("Say", "echo picked-command", shell: LocalShell);
        WorkflowService.Create("W", [MessageStep("picked-workflow")]);

        console.PushKeys(ConsoleKey.Enter);
        await new RunHandler { Target = RunTargetEnum.Command }.ExecuteAsync();
        console.PushKeys(ConsoleKey.Enter);
        await new RunHandler { Target = RunTargetEnum.Workflow }.ExecuteAsync();

        Assert.Contains("picked-command", console.Output);
        Assert.Contains("picked-workflow", console.Output);
    }

    #endregion

    public void Dispose()
    {
        _storage.Dispose();
    }
}
