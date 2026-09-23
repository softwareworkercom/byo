using SoftwareWorker.BYO.CLI.Core.Service;
using System.Globalization;
using System.Text.Json;

namespace SoftwareWorker.BYO.Tests;

public sealed class TokenServiceTests : IDisposable
{
    private readonly string _originalSettingsFilePath;
    private readonly string _originalSettingsSecretsFilePath;
    private readonly string _originalSecretsFilePath;
    private readonly string _originalSecretsSettingsFilePath;
    private readonly string _testStorageDirectory;

    public TokenServiceTests()
    {
        _originalSettingsFilePath = SettingsService.SettingsFilePath;
        _originalSettingsSecretsFilePath = SettingsService.SecretsFilePath;
        _originalSecretsFilePath = SecretsService.SecretsFilePath;
        _originalSecretsSettingsFilePath = SecretsService.SettingsFilePath;

        _testStorageDirectory = Path.Combine(Path.GetTempPath(), "byo-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testStorageDirectory);

        var settingsFilePath = Path.Combine(_testStorageDirectory, "settings.json");
        var secretsFilePath = Path.Combine(_testStorageDirectory, "secrets.json");

        SettingsService.SettingsFilePath = settingsFilePath;
        SettingsService.SecretsFilePath = secretsFilePath;
        SecretsService.SettingsFilePath = settingsFilePath;
        SecretsService.SecretsFilePath = secretsFilePath;
    }

    [Fact]
    public void ResolveTokens_ShouldReplaceTokensFromOverrides_CaseInsensitive()
    {
        const string text = "Hello {{Tests.TokenA}} and {{tests.tokena}}";
        var overrides = new Dictionary<string, string>
        {
            ["TESTS.TOKENA"] = "world"
        };

        var result = TokenService.ResolveTokens(text, tokenOverrides: overrides);

        Assert.Equal("Hello world and world", result);
    }

    [Fact]
    public void ResolveTokens_ShouldReplaceNestedObjectPropertyTokens()
    {
        const string text = "Repository: {{Project.Repository.Name}}";
        var model = new
        {
            Project = new
            {
                Repository = new
                {
                    Name = "byo"
                }
            }
        };

        var result = TokenService.ResolveTokens(text, model);

        Assert.Equal("Repository: byo", result);
    }

    [Fact]
    public void ResolveTokens_ShouldPrioritizeOverridesOverObjectValues()
    {
        const string text = "Repository: {{Project.Repository.Name}}";
        var model = new
        {
            Project = new
            {
                Repository = new
                {
                    Name = "from-object"
                }
            }
        };
        var overrides = new Dictionary<string, string>
        {
            ["Project.Repository.Name"] = "from-override"
        };

        var result = TokenService.ResolveTokens(text, model, overrides);

        Assert.Equal("Repository: from-override", result);
    }

    [Fact]
    public void ResolveTokens_ShouldReplaceTokenWithClassPrefixUsingObjectTraversal()
    {
        const string text = "Repository: {{Model.Project.Repository.Name}}";
        var model = new
        {
            Project = new
            {
                Repository = new
                {
                    Name = "byo"
                }
            }
        };

        var result = TokenService.ResolveTokens(text, model);

        Assert.Equal("Repository: byo", result);
    }

    [Fact]
    public void ResolveTokens_ShouldNotResolveSingleSegmentTokenFromObject()
    {
        const string text = "Repository: {{Name}}";
        var model = new { Name = "byo" };

        var result = TokenService.ResolveTokens(text, model);

        Assert.Equal("Repository: ", result);
    }

    [Fact]
    public void ResolveTokens_ShouldResolveNestedJsonElementProperties()
    {
        const string text = "Name={{project.repository.name}}, Id={{project.id}}, Enabled={{project.enabled}}";
        using var json = JsonDocument.Parse("""
            {
              "project": {
                "repository": { "name": "byo" },
                "id": 42,
                "enabled": true
              }
            }
            """);

        var result = TokenService.ResolveTokens(text, json.RootElement);

        Assert.Equal("Name=byo, Id=42, Enabled=true", result);
    }

    [Fact]
    public void ResolveTokens_ShouldResolveSingleSegmentJsonElementProperty()
    {
        const string text = "Name={{Name}}";
        using var json = JsonDocument.Parse("""
            {
              "Name": "byo"
            }
            """);

        var result = TokenService.ResolveTokens(text, json.RootElement);

        Assert.Equal("Name=byo", result);
    }

    [Fact]
    public void ResolveTokens_ShouldReplaceJsonElementNullValueWithEmptyString()
    {
        const string text = "Description={{project.description}}";
        using var json = JsonDocument.Parse("""
            {
              "project": {
                "description": null
              }
            }
            """);

        var result = TokenService.ResolveTokens(text, json.RootElement);

        Assert.Equal("Description=", result);
    }

    [Fact]
    public void ResolveTokens_ShouldReplaceGuidToken()
    {
        const string text = "Correlation={{Guid}}";

        var result = TokenService.ResolveTokens(text);

        Assert.StartsWith("Correlation=", result, StringComparison.Ordinal);
        Assert.DoesNotContain("{{Guid}}", result, StringComparison.Ordinal);
        var guidValue = result["Correlation=".Length..];
        Assert.Matches("^[0-9a-fA-F-]{36}$", guidValue);
    }

    [Fact]
    public void ResolveTokens_ShouldReplaceCurrentDateAndTimeSystemTokens()
    {
        var beforeLocal = DateTime.Now;
        var beforeUtc = DateTime.UtcNow;

        var result = TokenService.ResolveTokens("LocalDate={{DateNow}}; LocalDateTime={{DateTimeNow}}; UtcDate={{UtcDateNow}}; UtcDateTime={{UtcDateTimeNow}}");

        var afterLocal = DateTime.Now;
        var afterUtc = DateTime.UtcNow;
        var parts = result.Split("; ", StringSplitOptions.None)
            .Select(part => part.Split('=', 2))
            .ToDictionary(part => part[0], part => part[1], StringComparer.Ordinal);

        var localDate = DateTime.ParseExact(parts["LocalDate"], "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var localDateTime = DateTime.ParseExact(parts["LocalDateTime"], "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        var utcDate = DateTime.ParseExact(parts["UtcDate"], "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var utcDateTime = DateTime.ParseExact(parts["UtcDateTime"], "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        Assert.Contains(localDate.Date, new[] { beforeLocal.Date, afterLocal.Date });
        Assert.InRange(localDateTime, beforeLocal.AddMinutes(-1), afterLocal.AddMinutes(1));
        Assert.Contains(utcDate.Date, new[] { beforeUtc.Date, afterUtc.Date });
        Assert.InRange(utcDateTime, beforeUtc.AddMinutes(-1), afterUtc.AddMinutes(1));
    }

    [Fact]
    public void ResolveTokens_ShouldSupportColonTokensFromOverrides()
    {
        const string text = "Tenant={{context:tenant}}";
        var overrides = new Dictionary<string, string>
        {
            ["context:tenant"] = "prod"
        };

        var result = TokenService.ResolveTokens(text, tokenOverrides: overrides);

        Assert.Equal("Tenant=prod", result);
    }

    [Fact]
    public void ParseOverridesFromCommandLine_ShouldSupportSpaceSeparatedValues()
    {
        var overrides = TokenService.ParseOverridesFromCommandLine(new[]
        {
            "byo.dll",
            "--Tenant", "prod",
            "--context:region", "westus",
            "--async"
        });

        Assert.Equal("prod", overrides["Tenant"]);
        Assert.Equal("westus", overrides["context:region"]);
        Assert.False(overrides.ContainsKey("connection"));
        Assert.False(overrides.ContainsKey("async"));
    }

    [Fact]
    public void ParseOverridesFromCommandLine_ShouldSupportEqualsSeparatedValues()
    {
        var overrides = TokenService.ParseOverridesFromCommandLine(new[]
        {
            "byo.dll",
            "--Tenant=prod",
            "--context:region=westus"
        });

        Assert.Equal("prod", overrides["Tenant"]);
        Assert.Equal("westus", overrides["context:region"]);
    }

    [Fact]
    public void ParseOverridesFromCommandLine_ShouldIgnoreFlagsAndMissingValues()
    {
        var overrides = TokenService.ParseOverridesFromCommandLine(new[]
        {
            "byo.dll",
            "--flag-only",
            "--missing-value",
            "--next-option",
            "--Tenant",
            "prod"
        });

        Assert.Single(overrides);
        Assert.Equal("prod", overrides["Tenant"]);
    }

    [Fact]
    public void ResolveTokens_ShouldReplaceUnresolvedTokensWithEmptyString()
    {
        const string text = "Value: {{TokenServiceTests_Unresolved_987654321}}";

        var result = TokenService.ResolveTokens(text);

        Assert.Equal("Value: ", result);
    }

    [Fact]
    public void ResolveTokens_ShouldLeaveTextUnchangedWhenNoTokensExist()
    {
        const string text = "No placeholders here";

        var result = TokenService.ResolveTokens(text);

        Assert.Equal(text, result);
    }

    [Fact]
    public void ResolveTokens_ShouldConvertDateTimeWindowOverridesToAbsoluteDateTime()
    {
        var before = DateTime.Now.AddDays(-7);

        var result = TokenService.ResolveTokens("From={{DateTimeWindow}}", tokenOverrides: new Dictionary<string, string>
        {
            ["DateTimeWindow"] = "7d"
        });

        var after = DateTime.Now.AddDays(-7);
        var value = result["From=".Length..];
        var parsed = DateTime.ParseExact(value, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        Assert.InRange(parsed, before.AddMinutes(-1), after.AddMinutes(1));
    }

    [Fact]
    public void ResolveTokens_ShouldConvertDateTimeWindowOverridesCaseInsensitively()
    {
        var before = DateTime.Now.AddDays(-7);

        var result = TokenService.ResolveTokens("From={{datetimewindow}}", tokenOverrides: new Dictionary<string, string>
        {
            ["DATETIMEWINDOW"] = " 7 D "
        });

        var after = DateTime.Now.AddDays(-7);
        var value = result["From=".Length..];
        var parsed = DateTime.ParseExact(value, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        Assert.InRange(parsed, before.AddMinutes(-1), after.AddMinutes(1));
    }

    [Fact]
    public void ResolveTokens_ShouldKeepInvalidDateTimeWindowOverrideValue()
    {
        var result = TokenService.ResolveTokens("From={{DateTimeWindow}}", tokenOverrides: new Dictionary<string, string>
        {
            ["DateTimeWindow"] = "not-a-window"
        });

        Assert.Equal("From=not-a-window", result);
    }

    [Fact]
    public void ResolveTokens_ShouldResolveTokensFromSettings()
    {
        var key = NewKey("setting");
        SettingsService.Update(key, "value-a");

        var result = TokenService.ResolveTokens($"Value={{{{{key}}}}}");

        Assert.Equal("Value=value-a", result);
    }

    [Fact]
    public void ResolveTokens_ShouldResolveTokensFromSecrets()
    {
        var key = NewKey("secret");
        SecretsService.Update(key, "super-secret");

        var result = TokenService.ResolveTokens($"Value={{{{{key}}}}}");

        Assert.Equal("Value=super-secret", result);
    }

    [Fact]
    public void ResolveTokens_ShouldResolveMultipleTokensFromMixedSources()
    {
        var settingKey = NewKey("apiBaseUrl");
        SettingsService.Update(settingKey, "https://example.test");
        var model = new
        {
            Project = new
            {
                Repository = new
                {
                    Name = "byo"
                }
            }
        };
        var overrides = new Dictionary<string, string>
        {
            ["Tenant"] = "prod"
        };

        var text = "Tenant={{Tenant}}, Url={{" + settingKey + "}}, Repo={{Project.Repository.Name}}, Correlation={{Guid}}";

        var result = TokenService.ResolveTokens(
            text,
            model,
            overrides);

        Assert.StartsWith("Tenant=prod, Url=https://example.test, Repo=byo, Correlation=", result, StringComparison.Ordinal);
        var guidValue = result["Tenant=prod, Url=https://example.test, Repo=byo, Correlation=".Length..];
        Assert.Matches("^[0-9a-fA-F-]{36}$", guidValue);
    }

    public void Dispose()
    {
        SettingsService.SettingsFilePath = _originalSettingsFilePath;
        SettingsService.SecretsFilePath = _originalSettingsSecretsFilePath;
        SecretsService.SecretsFilePath = _originalSecretsFilePath;
        SecretsService.SettingsFilePath = _originalSecretsSettingsFilePath;

        if (Directory.Exists(_testStorageDirectory))
        {
            Directory.Delete(_testStorageDirectory, recursive: true);
        }
    }

    private static string NewKey(string suffix)
    {
        return $"TokenServiceTests:{suffix}:{Guid.NewGuid():N}";
    }
}
