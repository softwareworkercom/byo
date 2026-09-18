using SoftwareWorker.BYO.CLI.Core.Service;
using System.Text.Json;

namespace SoftwareWorker.BYO.Tests;

public class TokenServiceTests
{
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
    public void ResolveTokens_ShouldKeepTokenWhenJsonElementValueIsNull()
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
        var overrides = TokenService.ParseOverridesFromCommandLine(new string[]
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
    public void ResolveTokens_ShouldUseOverrideForSystemToken_DateTimeRangeFromNow()
    {
        const string text = "Date range: {{DateTimeRangeFromNow}}";
        var overrides = new Dictionary<string, string>
        {
            ["DateTimeRangeFromNow"] = "2024-01-15 10:30"
        };

        var result = TokenService.ResolveTokens(text, tokenOverrides: overrides);

        Assert.Equal("Date range: 2024-01-15 10:30", result);
    }

    [Fact]
    public void ResolveTokens_ShouldUseOverrideForSystemToken_DateTimeRangeFromNow_CaseInsensitiveOverrideKey()
    {
        const string text = "Date range: {{DateTimeRangeFromNow}}";
        var overrides = new Dictionary<string, string>
        {
            ["datetimerangefromnow"] = "2024-01-15 10:30"
        };

        var result = TokenService.ResolveTokens(text, tokenOverrides: overrides);

        Assert.Equal("Date range: 2024-01-15 10:30", result);
    }

    [Fact]
    public void ResolveTokens_ShouldUseOverrideForSystemToken_DateTimeRangeFromNow_CaseInsensitive()
    {
        const string text = "Date range: {{datetimerangefromnow}}";
        var overrides = new Dictionary<string, string>
        {
            ["DateTimeRangeFromNow"] = "2024-01-15 10:30"
        };

        var result = TokenService.ResolveTokens(text, tokenOverrides: overrides);

        Assert.Equal("Date range: 2024-01-15 10:30", result);
    }

    [Fact]
    public void ResolveTokens_ShouldReplaceTokenWithDateTimeWindowStartOverride()
    {
        const string text = "Report starting from {{DateTimeWindowStart}}";
        var rangeStartDate = "2024-01-01 00:00";
        var tokenOverrides = new Dictionary<string, string>
        {
            ["DateTimeWindowStart"] = rangeStartDate
        };

        var result = TokenService.ResolveTokens(text, tokenOverrides: tokenOverrides);

        Assert.Equal("Report starting from 2024-01-01 00:00", result);
    }

    [Fact]
    public void ResolveTokens_ShouldReplaceTokenWithDateTimeWindowStartOverride_CaseInsensitive()
    {
        const string text = "Report starting from {{datetimewindowstart}}";
        var rangeStartDate = "2024-01-01 00:00";
        var tokenOverrides = new Dictionary<string, string>
        {
            ["DateTimeWindowStart"] = rangeStartDate
        };

        var result = TokenService.ResolveTokens(text, tokenOverrides: tokenOverrides);

        Assert.Equal("Report starting from 2024-01-01 00:00", result);
    }

    [Fact]
    public void ResolveTokens_ShouldMultipleTokensWithDateTimeWindowStartAndOtherOverrides()
    {
        const string text = "Report period: {{DateTimeWindowStart}} to {{DateTimeWindowEnd}} for {{TenantId}}";
        var tokenOverrides = new Dictionary<string, string>
        {
            ["DateTimeWindowStart"] = "2024-01-01 00:00",
            ["DateTimeWindowEnd"] = "2024-12-31 23:59",
            ["TenantId"] = "tenant-123"
        };

        var result = TokenService.ResolveTokens(text, tokenOverrides: tokenOverrides);

        Assert.Equal("Report period: 2024-01-01 00:00 to 2024-12-31 23:59 for tenant-123", result);
    }
}
