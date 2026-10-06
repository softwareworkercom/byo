using SoftwareWorker.BYO.CLI.Helpers;
using SoftwareWorker.BYO.CLI.Integrations.NuGet;
using SoftwareWorker.BYO.CLI.Service;
using SoftwareWorker.BYO.SDK.Constants;
using SoftwareWorker.BYO.SDK.Services;
using System.IO.Compression;
using System.Net;
using System.Text;

namespace SoftwareWorker.BYO.Tests;

public class PluginSourceTests
{
    private const string ServiceIndexUrl = "https://feed.example.com/v3/index.json";

    #region Version selection

    [Fact]
    public void SelectLatest_ShouldPreferStableOverNewerPrereleaseAndFallBackToPrerelease()
    {
        Assert.Equal("1.5.0", NuGetVersionHelper.SelectLatest(["1.0.0", "2.0.0-beta.1", "1.5.0"]));
        Assert.Equal("2.0.0-beta.2", NuGetVersionHelper.SelectLatest(["2.0.0-beta.1", "2.0.0-beta.2"]));
        Assert.Null(NuGetVersionHelper.SelectLatest([]));
    }

    [Fact]
    public void FindMatch_ShouldIgnoreCaseAndBuildMetadata()
    {
        string[] versions = ["1.2.3+g7360a40ae3", "1.3.0-Beta"];

        Assert.Equal("1.2.3+g7360a40ae3", NuGetVersionHelper.FindMatch(versions, "1.2.3"));
        Assert.Equal("1.3.0-Beta", NuGetVersionHelper.FindMatch(versions, "1.3.0-beta"));
        Assert.Null(NuGetVersionHelper.FindMatch(versions, "9.9.9"));
    }

    #endregion

    #region Source definitions

    [Theory]
    [InlineData("corp=https://pkgs.dev.azure.com/org/_packaging/feed/nuget/v3/index.json", "corp", "https://pkgs.dev.azure.com/org/_packaging/feed/nuget/v3/index.json")]
    [InlineData("https://feed.example.com/v3/index.json?api-version=3", null, "https://feed.example.com/v3/index.json?api-version=3")]
    [InlineData("  local = C:\\feeds\\plugins ", "local", "C:\\feeds\\plugins")]
    [InlineData("/home/me/feeds", null, "/home/me/feeds")]
    public void TryParseDefinition_ShouldSplitNameFromLocation(string definition, string? expectedName, string expectedLocation)
    {
        Assert.True(PluginSourceService.TryParseDefinition(definition, out var name, out var location));
        Assert.Equal(expectedName, name);
        Assert.Equal(expectedLocation, location);
    }

    [Fact]
    public void CreateSource_ShouldPickFeedForUrlsAndFolderForPaths()
    {
        Assert.IsType<NuGetV3PackageSource>(PluginSourceService.CreateSource("https://api.nuget.org/v3/index.json"));
        Assert.IsType<LocalFolderPackageSource>(PluginSourceService.CreateSource(Path.GetTempPath()));
    }

    [Fact]
    public void GetSources_ShouldKeepConfiguredOrderAndAppendNuGetOrg()
    {
        using var settings = new TemporarySettings("""
            {
              "System:Plugins:Sources": ["corp=https://feed.example.com/v3/index.json", "C:\\feeds"]
            }
            """);

        var sources = PluginSourceService.GetSources();

        Assert.Equal(["corp", "C:\\feeds", "nuget.org"], sources.Select(source => source.Name));
    }

    [Fact]
    public void GetSources_ShouldNotDuplicateNuGetOrgWhenItIsConfiguredExplicitly()
    {
        using var settings = new TemporarySettings("""
            {
              "System:Plugins:Sources": ["mirror=https://api.nuget.org/v3/index.json/", "corp=https://feed.example.com/v3/index.json"]
            }
            """);

        var sources = PluginSourceService.GetSources();

        Assert.Equal(["mirror", "corp"], sources.Select(source => source.Name));
    }

    [Fact]
    public void GetSources_ShouldHonourTheNuGetOrgSwitch()
    {
        using var settings = new TemporarySettings("""
            {
              "System:Plugins:Sources": ["corp=https://feed.example.com/v3/index.json"],
              "System:Plugins:UseNuGetOrg": "false"
            }
            """);

        var sources = PluginSourceService.GetSources();

        var source = Assert.Single(sources);
        Assert.Equal("corp", source.Name);
        Assert.Equal("https://feed.example.com/v3/index.json", source.Location);
    }

    #endregion

    #region NuGet V3 feeds

    [Fact]
    public async Task NuGetV3_ShouldReadVersionsFromTheFlatContainerAnnouncedByTheServiceIndex()
    {
        var source = new NuGetV3PackageSource("feed", ServiceIndexUrl, credential: null, CreateFeed());

        var result = await source.GetVersionsAsync("BYO.Plugin.Weather", TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal(["1.0.0", "1.0.1", "1.1.0-beta"], result.Versions);
        Assert.Equal("1.0.1", NuGetVersionHelper.SelectLatest(result.Versions));
    }

    [Fact]
    public async Task NuGetV3_ShouldDistinguishMissingPackagesFromUnreachableFeeds()
    {
        var reachable = new NuGetV3PackageSource("feed", ServiceIndexUrl, credential: null, CreateFeed());
        var unreachable = new NuGetV3PackageSource("down", "https://down.example.com/v3/index.json", credential: null, new FakeHttpHandler([]));

        var missing = await reachable.GetVersionsAsync("BYO.Plugin.Nope", TestContext.Current.CancellationToken);
        var failed = await unreachable.GetVersionsAsync("BYO.Plugin.Weather", TestContext.Current.CancellationToken);

        Assert.True(missing.Succeeded);
        Assert.Empty(missing.Versions);
        Assert.False(failed.Succeeded);
        Assert.False(string.IsNullOrWhiteSpace(failed.Error));
    }

    [Fact]
    public async Task NuGetV3_ShouldDownloadWithBasicCredentials()
    {
        var handler = CreateFeed();
        var source = new NuGetV3PackageSource("feed", ServiceIndexUrl, new NetworkCredential("user", "secret"), handler);
        var target = Path.Combine(Path.GetTempPath(), $"byo-nupkg-{Guid.NewGuid():N}.nupkg");

        try
        {
            Assert.True(await source.DownloadPackageAsync("BYO.Plugin.Weather", "1.0.1", target, TestContext.Current.CancellationToken));
            Assert.Equal("nupkg-bytes", await File.ReadAllTextAsync(target, TestContext.Current.CancellationToken));
            Assert.False(await source.DownloadPackageAsync("BYO.Plugin.Weather", "9.9.9", target, TestContext.Current.CancellationToken));

            var expectedHeader = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("user:secret"));
            Assert.NotEmpty(handler.AuthorizationHeaders);
            Assert.All(handler.AuthorizationHeaders, header => Assert.Equal(expectedHeader, header));
        }
        finally
        {
            File.Delete(target);
        }
    }

    [Fact]
    public async Task NuGetV3_ShouldSearchByPrefixAndReadOwnersAsStringOrArray()
    {
        var source = new NuGetV3PackageSource("feed", ServiceIndexUrl, credential: null, CreateFeed());

        var result = await source.SearchAsync("BYO.Plugin.", TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal(["BYO.Plugin.Jira", "BYO.Plugin.Weather"], result.Packages.Select(package => package.Id).OrderBy(id => id));
        Assert.Equal(["softwareworkercom", "other"], result.Packages.Single(package => package.Id == "BYO.Plugin.Jira").Owners);
        Assert.Equal(["softwareworkercom"], result.Packages.Single(package => package.Id == "BYO.Plugin.Weather").Owners);
    }

    #endregion

    #region Resolution across sources

    [Fact]
    public async Task ResolveAndDownload_ShouldUseTheFirstSourceThatHasAMatchingVersion()
    {
        var tempRoot = CreateTempRoot();
        var originalPackagesDirectory = SystemConstants.PLUGINS_PACKAGES_DIRECTORY;
        SystemConstants.PLUGINS_PACKAGES_DIRECTORY = Path.Combine(tempRoot, "packages");

        try
        {
            var first = Path.Combine(tempRoot, "first");
            var second = Path.Combine(tempRoot, "second");
            WriteNupkg(first, "BYO.Plugin.Weather", "1.0.0");
            WriteNupkg(second, "BYO.Plugin.Weather", "1.1.0");
            WriteNupkg(second, "BYO.Plugin.Jira", "2.0.0");

            IPluginPackageSource[] sources = [new LocalFolderPackageSource("first", first), new LocalFolderPackageSource("second", second)];
            var warnings = new List<string>();

            var latest = await PluginInstallationService.ResolveAndDownloadAsync("BYO.Plugin.Weather", null, sources, warnings);
            var specific = await PluginInstallationService.ResolveAndDownloadAsync("BYO.Plugin.Weather", "1.1.0", sources, warnings);
            var elsewhere = await PluginInstallationService.ResolveAndDownloadAsync("BYO.Plugin.Jira", null, sources, warnings);
            var missing = await PluginInstallationService.ResolveAndDownloadAsync("BYO.Plugin.Missing", null, sources, warnings);

            // The first source wins even though the second has a newer version.
            Assert.Equal(("first", "1.0.0"), (latest!.Source.Name, latest.Version));
            Assert.Equal(("second", "1.1.0"), (specific!.Source.Name, specific.Version));
            Assert.Equal("second", elsewhere!.Source.Name);
            Assert.True(File.Exists(elsewhere.PackageFilePath));
            Assert.Null(missing);
            Assert.Empty(warnings);
        }
        finally
        {
            SystemConstants.PLUGINS_PACKAGES_DIRECTORY = originalPackagesDirectory;
            Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public async Task ResolveAndDownload_ShouldWarnAboutSourcesItCannotQueryAndContinue()
    {
        var tempRoot = CreateTempRoot();
        var originalPackagesDirectory = SystemConstants.PLUGINS_PACKAGES_DIRECTORY;
        SystemConstants.PLUGINS_PACKAGES_DIRECTORY = Path.Combine(tempRoot, "packages");

        try
        {
            var feed = Path.Combine(tempRoot, "feed");
            WriteNupkg(feed, "BYO.Plugin.Weather", "1.0.0");

            IPluginPackageSource[] sources =
            [
                new LocalFolderPackageSource("missing", Path.Combine(tempRoot, "does-not-exist")),
                new LocalFolderPackageSource("feed", feed)
            ];
            var warnings = new List<string>();

            var resolved = await PluginInstallationService.ResolveAndDownloadAsync("BYO.Plugin.Weather", null, sources, warnings);

            Assert.Equal("feed", resolved!.Source.Name);
            var warning = Assert.Single(warnings);
            Assert.Contains("'missing'", warning);
        }
        finally
        {
            SystemConstants.PLUGINS_PACKAGES_DIRECTORY = originalPackagesDirectory;
            Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public async Task EnsureDependencyPackageExtracted_ShouldFetchDependenciesFromAnySource()
    {
        var tempRoot = CreateTempRoot();
        var originalPackagesDirectory = SystemConstants.PLUGINS_PACKAGES_DIRECTORY;
        SystemConstants.PLUGINS_PACKAGES_DIRECTORY = Path.Combine(tempRoot, "packages");

        try
        {
            var empty = Path.Combine(tempRoot, "empty");
            var feed = Path.Combine(tempRoot, "feed");
            Directory.CreateDirectory(empty);
            WriteNupkg(feed, "Some.Dependency", "3.0.0");

            IPluginPackageSource[] sources = [new LocalFolderPackageSource("empty", empty), new LocalFolderPackageSource("feed", feed)];

            var extracted = await PluginInstallationService.EnsureDependencyPackageExtractedAsync("Some.Dependency", "3.0.0", sources);

            Assert.NotNull(extracted);
            Assert.Single(Directory.GetFiles(extracted!, "*.nuspec"));
            Assert.Null(await PluginInstallationService.EnsureDependencyPackageExtractedAsync("Some.Other", "1.0.0", sources));
        }
        finally
        {
            SystemConstants.PLUGINS_PACKAGES_DIRECTORY = originalPackagesDirectory;
            Directory.Delete(tempRoot, true);
        }
    }

    #endregion

    #region Helpers

    private static FakeHttpHandler CreateFeed() => new(new Dictionary<string, string>
    {
        [ServiceIndexUrl] = """
            {
              "version": "3.0.0",
              "resources": [
                { "@id": "https://feed.example.com/v3/search", "@type": "SearchQueryService/3.5.0" },
                { "@id": "https://feed.example.com/v3/flat", "@type": "PackageBaseAddress/3.0.0" }
              ]
            }
            """,
        ["https://feed.example.com/v3/flat/byo.plugin.weather/index.json"] = """{ "versions": ["1.0.0", "1.1.0-beta", "1.0.1"] }""",
        ["https://feed.example.com/v3/flat/byo.plugin.weather/1.0.1/byo.plugin.weather.1.0.1.nupkg"] = "nupkg-bytes",
        ["https://feed.example.com/v3/search?q=BYO.Plugin.&prerelease=true&skip=0&take=100"] = """
            {
              "totalHits": 3,
              "data": [
                { "id": "BYO.Plugin.Weather", "version": "1.0.1", "description": "Weather", "authors": "Someone", "owners": ["softwareworkercom"] },
                { "id": "BYO.Plugin.Jira", "version": "2.0.0", "owners": "softwareworkercom, other" },
                { "id": "Unrelated.Package", "version": "2.0.0" }
              ]
            }
            """
    });

    private static string CreateTempRoot()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "byo-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        return tempRoot;
    }

    private static void WriteNupkg(string directory, string packageId, string version)
    {
        Directory.CreateDirectory(directory);

        using var stream = File.Create(Path.Combine(directory, $"{packageId}.{version}.nupkg"));
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create);
        using var writer = new StreamWriter(archive.CreateEntry($"{packageId}.nuspec").Open());

        writer.Write(
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
            "<package>\n" +
            "  <metadata>\n" +
            $"    <id>{packageId}</id>\n" +
            $"    <version>{version}</version>\n" +
            $"    <description>{packageId} test package</description>\n" +
            "  </metadata>\n" +
            "</package>\n");
    }

    private sealed class FakeHttpHandler(Dictionary<string, string> responses) : HttpMessageHandler
    {
        public List<string?> AuthorizationHeaders { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            AuthorizationHeaders.Add(request.Headers.Authorization?.ToString());

            var response = responses.TryGetValue(request.RequestUri!.OriginalString, out var body)
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) }
                : new HttpResponseMessage(HttpStatusCode.NotFound);

            return Task.FromResult(response);
        }
    }

    /// <summary>
    /// Points the settings and secrets services at temporary files for the lifetime of a test.
    /// </summary>
    private sealed class TemporarySettings : IDisposable
    {
        private readonly string _root;
        private readonly string _originalSettingsPath = SettingsService.SettingsFilePath;
        private readonly string _originalSettingsSecretsPath = SettingsService.SecretsFilePath;
        private readonly string _originalSecretsPath = SecretsService.SecretsFilePath;
        private readonly string _originalSecretsSettingsPath = SecretsService.SettingsFilePath;

        public TemporarySettings(string settingsJson)
        {
            _root = CreateTempRoot();
            var settingsPath = Path.Combine(_root, "settings.json");
            var secretsPath = Path.Combine(_root, "secrets.json");
            File.WriteAllText(settingsPath, settingsJson);

            SettingsService.SettingsFilePath = settingsPath;
            SettingsService.SecretsFilePath = secretsPath;
            SecretsService.SecretsFilePath = secretsPath;
            SecretsService.SettingsFilePath = settingsPath;
        }

        public void Dispose()
        {
            SettingsService.SettingsFilePath = _originalSettingsPath;
            SettingsService.SecretsFilePath = _originalSettingsSecretsPath;
            SecretsService.SecretsFilePath = _originalSecretsPath;
            SecretsService.SettingsFilePath = _originalSecretsSettingsPath;
            Directory.Delete(_root, true);
        }
    }

    #endregion
}
