using SoftwareWorker.BYO.CLI.Core.Service;
using System.Text.Json;
using System.Text.RegularExpressions;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace SoftwareWorker.BYO.Tests;

public class TokenServiceTests
{
    [Fact]
    public void ResolveTokens_ShouldReplaceTokensFromOverrides_CaseInsensitive()
    {
        const string text = "Hello {{Tests.TokenA}} and {{tests.tokena}}";
        var overrides = new Dictionary<string, string>
        {
            ["{{TESTS.TOKENA}}"] = "world"
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

        Assert.Equal(text, result);
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

        Assert.Equal(text, result);
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
            ["{{context:tenant}}"] = "prod"
        };

        var result = TokenService.ResolveTokens(text, tokenOverrides: overrides);

        Assert.Equal("Tenant=prod", result);
    }

    [Fact]
    public void ResolveTokens_ShouldKeepUnresolvedTokensUnchanged()
    {
        const string text = "Value: {{TokenServiceTests_Unresolved_987654321}}";

        var result = TokenService.ResolveTokens(text);

        Assert.Equal(text, result);
    }

    [Fact]
    public void ResolveTokens_ShouldLeaveTextUnchangedWhenNoTokensExist()
    {
        const string text = "No placeholders here";

        var result = TokenService.ResolveTokens(text);

        Assert.Equal(text, result);
    }
}
