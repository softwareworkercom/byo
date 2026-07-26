using SoftwareWorker.BYO.CLI.Core.Service;
using SoftwareWorker.BYO.Core.Storage;
using System.Text.Json;

namespace SoftwareWorker.BYO.Tests;

public sealed class SettingsServiceTests : IDisposable
{
    private readonly string _originalSettingsFilePath;
    private readonly string _originalSecretsFilePath;
    private readonly string _testStorageDirectory;

    public SettingsServiceTests()
    {
        _originalSettingsFilePath = SettingsService.SettingsFilePath;
        _originalSecretsFilePath = SettingsService.SecretsFilePath;

        _testStorageDirectory = Path.Combine(Path.GetTempPath(), "byo-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testStorageDirectory);

        SettingsService.SettingsFilePath = Path.Combine(_testStorageDirectory, "settings.json");
        SettingsService.SecretsFilePath = Path.Combine(_testStorageDirectory, "secrets.json");
    }

    [Fact]
    public void Update_AndGet_ShouldStoreAndReturnValue()
    {
        var key = NewKey("store-get");

        var updateResult = SettingsService.Update(key, "value-a");
        var getResult = SettingsService.Get(key, showErrorIfNotFound: false);

        Assert.Equal("value-a", updateResult);
        Assert.Equal("value-a", getResult);
    }

    [Fact]
    public void Update_ShouldOverwriteExistingValue()
    {
        var key = NewKey("overwrite");

        SettingsService.Update(key, "value-a");
        SettingsService.Update(key, "value-b");

        Assert.Equal("value-b", SettingsService.Get(key, showErrorIfNotFound: false));
    }

    [Fact]
    public void GetBoolean_ShouldParseAndUseDefaultValue()
    {
        var trueKey = NewKey("bool-true");
        var invalidKey = NewKey("bool-invalid");
        var missingKey = NewKey("bool-missing");

        SettingsService.Update(trueKey, "true");
        SettingsService.Update(invalidKey, "not-a-bool");

        Assert.True(SettingsService.GetBoolean(trueKey));
        Assert.False(SettingsService.GetBoolean(invalidKey));
        Assert.True(SettingsService.GetBoolean(missingKey, defaultValue: true));
    }

    [Fact]
    public void GetList_ShouldFilterByPrefix()
    {
        var prefix = NewKey("prefix");
        var keyA = $"{prefix}:a";
        var keyB = $"{prefix}:b";
        var otherKey = NewKey("other");

        SettingsService.Update(keyA, "1");
        SettingsService.Update(keyB, "2");
        SettingsService.Update(otherKey, "x");

        var list = SettingsService.GetList(prefix);

        Assert.NotNull(list);
        Assert.Equal(2, list.Count);
        Assert.Equal("1", list[keyA]);
        Assert.Equal("2", list[keyB]);
        Assert.Null(SettingsService.GetList(NewKey("not-found")));
    }

    [Fact]
    public void Delete_ShouldRemoveExistingKey()
    {
        var key = NewKey("delete");
        SettingsService.Update(key, "value");

        SettingsService.Delete(key);

        Assert.Null(SettingsService.Get(key, showErrorIfNotFound: false));
    }

    [Fact]
    public void Update_ShouldFailWhenKeyExistsInSecrets()
    {
        var key = NewKey("exists-in-secrets");
        StorageService.SaveDictionary(SettingsService.SecretsFilePath, new Dictionary<string, string> { [key] = "encrypted-value" });

        var result = SettingsService.Update(key, "value");

        Assert.Null(result);
        Assert.Null(SettingsService.Get(key, showErrorIfNotFound: false));
    }

    [Fact]
    public void Get_ShouldResolveSystemTokens()
    {
        var key = NewKey("guid-token");
        SettingsService.Update(key, "{{Guid}}");

        var result = SettingsService.Get(key, showErrorIfNotFound: false);

        Assert.NotNull(result);
        Assert.Matches("^[0-9a-fA-F-]{36}$", result);
    }

    [Fact]
    public void Update_ShouldPersistJsonArrayValues()
    {
        var key = NewKey("array");
        var value = "[\"one\",\"two\"]";

        var result = SettingsService.Update(key, value);
        var storedValue = SettingsService.Get(key, showErrorIfNotFound: false);

        var content = File.ReadAllText(SettingsService.SettingsFilePath);
        using var document = JsonDocument.Parse(content);
        var element = document.RootElement.GetProperty(key);

        Assert.Equal(JsonValueKind.Array, element.ValueKind);
        Assert.Equal("one", element[0].GetString());
        Assert.Equal("two", element[1].GetString());
    }

    [Fact]
    public void GetArray_ShouldReturnConfiguredArray()
    {
        var key = NewKey("array-get");
        SettingsService.Update(key, "[\"alpha\",\"beta\"]");

        var result = SettingsService.GetArray(key, showErrorIfNotFound: false);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("alpha", result[0]);
        Assert.Equal("beta", result[1]);
    }

    [Fact]
    public void GetArray_ShouldParseLooseBracketedArrayFormat()
    {
        var key = NewKey("array-loose");
        SettingsService.Update(key, "[alpha,beta]");

        var result = SettingsService.GetArray(key, showErrorIfNotFound: false);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("alpha", result[0]);
        Assert.Equal("beta", result[1]);
    }

    public void Dispose()
    {
        SettingsService.SettingsFilePath = _originalSettingsFilePath;
        SettingsService.SecretsFilePath = _originalSecretsFilePath;

        if (Directory.Exists(_testStorageDirectory))
        {
            Directory.Delete(_testStorageDirectory, recursive: true);
        }
    }

    private static string NewKey(string suffix)
    {
        return $"SettingsServiceTests:{suffix}:{Guid.NewGuid():N}";
    }

}
