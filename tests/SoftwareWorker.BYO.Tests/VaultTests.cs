using SoftwareWorker.BYO.SDK.Vault;
using System.ComponentModel;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace SoftwareWorker.BYO.Tests;

[SupportedOSPlatform("windows")]
public sealed class VaultTests
{
    public static bool IsWindows => OperatingSystem.IsWindows();

    [Fact(Skip = "Uses the Windows Credential Manager.", SkipUnless = nameof(IsWindows))]
    public void VaultProvider_ShouldUseTheWindowsVault_OnWindows()
    {
        Assert.IsType<WindowsVault>(VaultProvider.Instance);
        Assert.Same(VaultProvider.Instance, VaultProvider.Instance);
    }

    [Fact(Skip = "Uses the Windows Credential Manager.", SkipUnless = nameof(IsWindows))]
    public void WindowsVault_ShouldRoundTripTextSecrets_AndDeleteThem()
    {
        var vault = new WindowsVault();
        var key = NewKey();

        try
        {
            SetSecretOrSkip(vault, key, "plain text secret");

            Assert.Equal("plain text secret", vault.GetSecret(key));
        }
        finally
        {
            vault.DeleteSecret(key);
        }

        Assert.Null(vault.GetSecret(key));
    }

    [Fact(Skip = "Uses the Windows Credential Manager.", SkipUnless = nameof(IsWindows))]
    public void WindowsVault_ShouldStorePkcs8KeysInBinaryForm()
    {
        var vault = new WindowsVault();
        var key = NewKey();
        var secret = "pkcs8:" + Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        try
        {
            SetSecretOrSkip(vault, key, secret);

            Assert.Equal(secret, vault.GetSecret(key));
        }
        finally
        {
            vault.DeleteSecret(key);
        }
    }

    [Fact(Skip = "Uses the Windows Credential Manager.", SkipUnless = nameof(IsWindows))]
    public void WindowsVault_ShouldReadLegacyDpapiProtectedSecrets()
    {
        var vault = new WindowsVault();
        var key = NewKey();
        var legacy = Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes("legacy secret"), null, DataProtectionScope.CurrentUser));

        try
        {
            SetSecretOrSkip(vault, key, legacy);

            Assert.Equal("legacy secret", vault.GetSecret(key));
        }
        finally
        {
            vault.DeleteSecret(key);
        }
    }

    [Fact(Skip = "Uses the Windows Credential Manager.", SkipUnless = nameof(IsWindows))]
    public void WindowsVault_ShouldRejectPayloadsLargerThanTheCredentialLimit()
    {
        var vault = new WindowsVault();

        var exception = Assert.Throws<InvalidOperationException>(() => vault.SetSecret(NewKey(), new string('x', 3000)));

        Assert.Contains("too large", exception.Message);
    }

    [Fact(Skip = "Uses the Windows Credential Manager.", SkipUnless = nameof(IsWindows))]
    public void WindowsVault_ShouldReturnNullForUnknownKeys_AndIgnoreDeletingThem()
    {
        var vault = new WindowsVault();
        var key = NewKey();

        Assert.Null(vault.GetSecret(key));
        vault.DeleteSecret(key);
    }

    private static string NewKey() => $"byo-tests-vault-{Guid.NewGuid():N}";

    /// <summary>
    /// The Credential Manager is not reachable from every logon session (a service or a tool-hosted shell
    /// gets ERROR_NOT_ENOUGH_MEMORY from CredWrite); those runs skip instead of failing.
    /// </summary>
    private static void SetSecretOrSkip(WindowsVault vault, string key, string secret)
    {
        try
        {
            vault.SetSecret(key, secret);
        }
        catch (Win32Exception exception)
        {
            Assert.Skip($"The Windows Credential Manager is unavailable in this session: {exception.Message}");
        }
    }
}
