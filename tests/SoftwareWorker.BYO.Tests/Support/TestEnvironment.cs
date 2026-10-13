using SoftwareWorker.BYO.SDK.Constants;
using System.Runtime.CompilerServices;

namespace SoftwareWorker.BYO.Tests.Support;

/// <summary>
/// Keeps the test run independent from the plugins installed in the developer's profile.
/// The command scanner loads every assembly under the plugin binaries folder, so a plugin
/// built against another SDK version would otherwise break every test that builds the command tree.
/// </summary>
internal static class TestEnvironment
{
    public static string PluginsRoot { get; } = Path.Combine(Path.GetTempPath(), "byo-tests", $"plugins-{Guid.NewGuid():N}");

    [ModuleInitializer]
    internal static void IsolatePluginDirectories()
    {
        Directory.CreateDirectory(PluginsRoot);
        SystemConstants.PLUGINS_DIRECTORY = PluginsRoot;
        SystemConstants.PLUGINS_PACKAGES_DIRECTORY = Path.Combine(PluginsRoot, "packages");
        SystemConstants.PLUGINS_BINARIES_DIRECTORY = Path.Combine(PluginsRoot, "bin");
    }
}
