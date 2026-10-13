using SoftwareWorker.BYO.CLI.Service;
using SoftwareWorker.BYO.SDK.Constants;
using SoftwareWorker.BYO.SDK.Services;
using SoftwareWorker.BYO.Tests.Support;

namespace SoftwareWorker.BYO.Tests;

public sealed class AliasServiceTests : IDisposable
{
    private readonly TemporaryStorageScope _storage = new();

    [Fact]
    public void GetPromptName_ShouldFallBackToByo_AndTrimTheConfiguredAlias()
    {
        Assert.Equal("byo", AliasService.GetPromptName());

        SettingsService.Update(SystemConstants.SYSTEM_Alias, "  sw  ");

        Assert.Equal("sw", AliasService.GetPromptName());
    }

    [Fact]
    public void AskAliasByUser_ShouldRejectInvalidAliases_UntilAValidOneIsEntered()
    {
        using var console = new ConsoleScope(interactive: true);
        console.Input.PushTextWithEnter("9lives");
        console.Input.PushTextWithEnter("has space");
        console.Input.PushTextWithEnter(" ok_alias-1 ");

        var alias = AliasService.AskAliasByUser();

        Assert.Equal("ok_alias-1", alias);
        Assert.Contains("Alias must start with a letter", console.Output);
    }

    [Fact]
    public void EnsureAliasConfigured_ShouldDoNothing_WhenTheConsoleIsNotInteractive()
    {
        using var console = new ConsoleScope();

        AliasService.EnsureAliasConfigured();

        Assert.Equal(string.Empty, console.Output);
        Assert.Null(SettingsService.Get(SystemConstants.SYSTEM_Alias, showErrorIfNotFound: false));
        Assert.False(SettingsService.GetBoolean(SystemConstants.SYSTEM_SkipAliasPrompt));
    }

    public void Dispose()
    {
        _storage.Dispose();
    }
}
