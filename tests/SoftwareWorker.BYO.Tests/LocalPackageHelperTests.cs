using SoftwareWorker.BYO.CLI.Helpers;
using SoftwareWorker.BYO.CLI.Integrations.NuGet;
using System.IO.Compression;

namespace SoftwareWorker.BYO.Tests;

public sealed class LocalPackageHelperTests : IDisposable
{
    private readonly string _feed = Directory.CreateTempSubdirectory("byo-feed-").FullName;

    [Fact]
    public void GetLatestPackages_ShouldPickTheHighestVersionPerId_AndSkipUnreadableFiles()
    {
        WriteNupkg("BYO.Plugin.A", "1.0.0");
        WriteNupkg("BYO.Plugin.A", "1.1.0-beta");
        WriteNupkg("byo.plugin.a", "1.0.1");
        WriteNupkg("Other", "2.0.0", description: "");
        File.WriteAllText(Path.Combine(_feed, "BYO.Plugin.A.9.9.9.symbols.nupkg"), "ignored");
        File.WriteAllText(Path.Combine(_feed, "corrupt.nupkg"), "not a zip");
        WriteZip("no-nuspec.nupkg", ("readme.txt", "no metadata"));
        WriteZip("no-metadata.nupkg", ("x.nuspec", "<package><other /></package>"));
        WriteZip("no-id.nupkg", ("x.nuspec", "<package><metadata><version>1.0.0</version></metadata></package>"));

        var latest = LocalPackageHelper.GetLatestPackages(_feed);

        Assert.Equal(["BYO.Plugin.A", "Other"], latest.Select(package => package.Id));
        Assert.Equal("1.1.0-beta", latest[0].Version);
        Assert.Equal("BYO.Plugin.A test package", latest[0].Description);
        Assert.Equal(string.Empty, latest[1].Description);
        Assert.True(File.Exists(latest[1].FilePath));
    }

    [Fact]
    public void GetPackages_ShouldReturnEveryVersionOfOneId_IgnoringCase()
    {
        WriteNupkg("BYO.Plugin.A", "1.0.0");
        WriteNupkg("byo.plugin.a", "1.0.1");
        WriteNupkg("Other", "2.0.0");

        var versions = LocalPackageHelper.GetPackages(_feed, "byo.plugin.A").Select(package => package.Version).OrderBy(version => version);

        Assert.Equal(["1.0.0", "1.0.1"], versions);
        Assert.Empty(LocalPackageHelper.GetPackages(Path.Combine(_feed, "missing"), "BYO.Plugin.A"));
        Assert.Empty(LocalPackageHelper.GetLatestPackages(" "));
    }

    [Fact]
    public async Task LocalFolderPackageSource_ShouldReportMissingFolders_AndCopyPackages()
    {
        WriteNupkg("BYO.Plugin.A", "1.0.0");
        var source = new LocalFolderPackageSource(null, _feed);
        var missing = new LocalFolderPackageSource("missing", Path.Combine(_feed, "nope"));
        var target = Path.Combine(_feed, "copied.nupkg");

        Assert.Equal(_feed, source.Name);
        Assert.Equal(["1.0.0"], (await source.GetVersionsAsync("byo.plugin.a", TestContext.Current.CancellationToken)).Versions);
        Assert.False((await missing.GetVersionsAsync("BYO.Plugin.A", TestContext.Current.CancellationToken)).Succeeded);
        Assert.False((await missing.SearchAsync("BYO.", TestContext.Current.CancellationToken)).Succeeded);
        Assert.True(await source.DownloadPackageAsync("BYO.Plugin.A", "1.0.0", target, TestContext.Current.CancellationToken));
        Assert.True(File.Exists(target));
        Assert.False(await source.DownloadPackageAsync("BYO.Plugin.A", "2.0.0", target, TestContext.Current.CancellationToken));
        Assert.False(await source.DownloadPackageAsync("BYO.Plugin.A", "1.0.0", Path.Combine(_feed, "no-dir", "x.nupkg"), TestContext.Current.CancellationToken));
    }

    private void WriteNupkg(string packageId, string version, string? description = null)
    {
        var nuspec =
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
            "<package xmlns=\"http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd\">\n" +
            "  <metadata>\n" +
            $"    <id>{packageId}</id>\n" +
            $"    <version>{version}</version>\n" +
            (description == null ? $"    <description>{packageId} test package</description>\n" : $"    <description>{description}</description>\n") +
            "  </metadata>\n" +
            "</package>\n";

        WriteZip($"{packageId}.{version}.nupkg", ($"{packageId}.nuspec", nuspec));
    }

    private void WriteZip(string fileName, params (string Name, string Content)[] entries)
    {
        using var stream = File.Create(Path.Combine(_feed, fileName));
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create);
        foreach (var (name, content) in entries)
        {
            using var writer = new StreamWriter(archive.CreateEntry(name).Open());
            writer.Write(content);
        }
    }

    public void Dispose()
    {
        Directory.Delete(_feed, recursive: true);
    }
}
