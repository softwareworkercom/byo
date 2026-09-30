using SoftwareWorker.BYO.CLI.Core.Helpers;

namespace SoftwareWorker.BYO.Tests;

public class PluginCompatibilityHelperTests
{
    [Fact]
    public void BuildErrorMessage_WithPluginAssembly_ShouldIncludeCliAndPluginSdkVersions()
    {
        var pluginAssembly = typeof(PluginCompatibilityHelperTests).Assembly;

        var message = PluginCompatibilityHelper.BuildErrorMessage(new MissingMethodException("Missing Foo.Bar()"), pluginAssembly);

        Assert.Contains("Error: MissingMethodException: Missing Foo.Bar()", message);
        Assert.Contains("CLI BYO.SDK version: ", message);
        Assert.Contains($"Plugin: {pluginAssembly.GetName().Name} ", message);
        Assert.Contains("Plugin built against BYO.SDK version: ", message);
        Assert.DoesNotContain("version: unknown", message);
    }

    [Fact]
    public void BuildErrorMessage_WithoutKnownPlugin_ShouldStillIncludeCliSdkVersion()
    {
        var message = PluginCompatibilityHelper.BuildErrorMessage(new TypeLoadException());

        Assert.Contains("CLI BYO.SDK version: ", message);
        Assert.DoesNotContain("Plugin built against", message);
    }
}
