using SoftwareWorker.BYO.CLI.Core.Handlers.Secrets;
using SoftwareWorker.BYO.CLI.Core.Service;
using SoftwareWorker.BYO.Core.Storage;

namespace SoftwareWorker.BYO.Tests;

public sealed class SecretsCommandTests : IDisposable
{
    private readonly string _originalSettingsFilePath;
    private readonly string _originalSettingsSecretsFilePath;
    private readonly string _originalSecretsFilePath;
    private readonly string _originalSecretsSettingsFilePath;
    private readonly string _originalRsaKeyFilePath;
    private readonly string _testStorageDirectory;

    public SecretsCommandTests()
    {
        _originalSettingsFilePath = SettingsService.SettingsFilePath;
        _originalSettingsSecretsFilePath = SettingsService.SecretsFilePath;
        _originalSecretsFilePath = SecretsService.SecretsFilePath;
        _originalSecretsSettingsFilePath = SecretsService.SettingsFilePath;
        _originalRsaKeyFilePath = KeyManagementService.RsaKeyFilePath;

        _testStorageDirectory = Path.Combine(Path.GetTempPath(), "byo-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testStorageDirectory);

        var settingsFilePath = Path.Combine(_testStorageDirectory, "settings.json");
        var secretsFilePath = Path.Combine(_testStorageDirectory, "secrets.json");
        var rsaKeyFilePath = Path.Combine(_testStorageDirectory, "rsa.key");

        SettingsService.SettingsFilePath = settingsFilePath;
        SettingsService.SecretsFilePath = secretsFilePath;
        SecretsService.SecretsFilePath = secretsFilePath;
        SecretsService.SettingsFilePath = settingsFilePath;
        KeyManagementService.RsaKeyFilePath = rsaKeyFilePath;
    }

    [Fact]
    public async Task SecretsUpdateCommand_ShouldStoreEncryptedValue()
    {
        var key = NewKey("update");
        const string value = "top-secret-value";

        var command = new SecretsUpdateCommand
        {
            Key = key,
            Value = value
        };

        await command.ExecuteAsync();

        var rawSecrets = StorageService.LoadDictionary(SecretsService.SecretsFilePath);
        Assert.True(rawSecrets.ContainsKey(key));
        Assert.NotEqual(value, rawSecrets[key]);
        Assert.Equal(value, SecretsService.Get(key));
    }

    [Fact]
    public async Task SecretsReadHandler_ShouldExecuteWhenSecretsExist()
    {
        var keyA = NewKey("read-a");
        var keyB = NewKey("read-b");

        SecretsService.Update(keyA, "value-a");
        SecretsService.Update(keyB, "value-b");

        var command = new SecretsReadHandler();
        await command.ExecuteAsync();

        var secrets = SecretsService.GetList();
        Assert.NotNull(secrets);
        Assert.Equal("value-a", secrets[keyA]);
        Assert.Equal("value-b", secrets[keyB]);
    }

    [Fact]
    public async Task SecretsDeleteCommand_ShouldRemoveSecretByKey()
    {
        var key = NewKey("delete");
        SecretsService.Update(key, "value");

        var command = new SecretsDeleteCommand
        {
            Key = key
        };

        await command.ExecuteAsync();

        Assert.Null(SecretsService.GetList(key));
    }

    [Fact]
    public async Task SecretsReencryptCommand_ShouldRotateKeyAndKeepValuesReadable()
    {
        var key = NewKey("reencrypt");
        const string value = "reencrypt-value";
        SecretsService.Update(key, value);

        var encryptedValueBefore = StorageService.LoadDictionary(SecretsService.SecretsFilePath)[key];
        var rsaKeyBefore = File.ReadAllText(KeyManagementService.RsaKeyFilePath);

        var command = new SecretsReencryptCommand();
        await command.ExecuteAsync();

        var encryptedValueAfter = StorageService.LoadDictionary(SecretsService.SecretsFilePath)[key];
        var rsaKeyAfter = File.ReadAllText(KeyManagementService.RsaKeyFilePath);

        Assert.NotEqual(rsaKeyBefore, rsaKeyAfter);
        Assert.NotEqual(encryptedValueBefore, encryptedValueAfter);
        Assert.Equal(value, SecretsService.Get(key));
    }

    public void Dispose()
    {
        SettingsService.SettingsFilePath = _originalSettingsFilePath;
        SettingsService.SecretsFilePath = _originalSettingsSecretsFilePath;
        SecretsService.SecretsFilePath = _originalSecretsFilePath;
        SecretsService.SettingsFilePath = _originalSecretsSettingsFilePath;
        KeyManagementService.RsaKeyFilePath = _originalRsaKeyFilePath;

        if (Directory.Exists(_testStorageDirectory))
        {
            Directory.Delete(_testStorageDirectory, recursive: true);
        }
    }

    private static string NewKey(string suffix)
    {
        return $"SecretsCommandTests:{suffix}:{Guid.NewGuid():N}";
    }
}
