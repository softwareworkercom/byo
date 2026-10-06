using SoftwareWorker.BYO.CLI.Helpers;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SoftwareWorker.BYO.CLI.Integrations.NuGet
{
    /// <summary>
    /// A NuGet V3 feed addressed by its service index, for example <c>https://api.nuget.org/v3/index.json</c>.
    /// The package base address (flat container) and search resources are read from the index, so any V3
    /// server works the same way: NuGet.org, Azure Artifacts, GitHub Packages, Artifactory, Nexus, BaGet
    /// and others. Private feeds are accessed with HTTP basic authentication when a credential is given.
    /// https://learn.microsoft.com/en-us/nuget/api/overview
    /// </summary>
    internal sealed class NuGetV3PackageSource : IPluginPackageSource
    {
        private const string PackageBaseAddressType = "PackageBaseAddress/3.0.0";

        private static readonly string[] PreferredSearchServiceTypes =
        [
            "SearchQueryService/3.5.0",
            "SearchQueryService/3.0.0-rc",
            "SearchQueryService/3.0.0-beta",
            "SearchQueryService"
        ];

        private static readonly TimeSpan RequestTimeout = TimeSpan.FromMinutes(2);

        private static readonly HttpClient SharedHttpClient = new() { Timeout = RequestTimeout };

        private readonly HttpClient _httpClient;
        private readonly NetworkCredential? _credential;
        private Task<ServiceIndex>? _serviceIndex;

        /// <param name="name">Name shown to the user.</param>
        /// <param name="serviceIndexUrl">The feed's V3 service index URL.</param>
        /// <param name="credential">Optional credential sent as HTTP basic authentication, e.g. a user name and personal access token.</param>
        /// <param name="handler">Optional message handler, used by tests to fake the feed.</param>
        public NuGetV3PackageSource(string name, string serviceIndexUrl, NetworkCredential? credential = null, HttpMessageHandler? handler = null)
        {
            Name = name;
            Location = serviceIndexUrl.Trim();
            _credential = credential;
            _httpClient = handler == null ? SharedHttpClient : new HttpClient(handler) { Timeout = RequestTimeout };
        }

        public string Name { get; }

        public string Location { get; }

        public async Task<PackageVersionsResult> GetVersionsAsync(string packageId, CancellationToken cancellationToken = default)
        {
            try
            {
                var index = await GetServiceIndexAsync(cancellationToken);
                var url = $"{index.PackageBaseAddress}{packageId.ToLowerInvariant()}/index.json";

                using var response = await SendAsync(url, HttpCompletionOption.ResponseContentRead, cancellationToken);
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return PackageVersionsResult.NotFound;
                }

                response.EnsureSuccessStatusCode();

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

                if (!document.RootElement.TryGetProperty("versions", out var versionsElement) ||
                    versionsElement.ValueKind != JsonValueKind.Array)
                {
                    return PackageVersionsResult.NotFound;
                }

                var versions = versionsElement
                    .EnumerateArray()
                    .Select(element => element.GetString())
                    .Where(version => !string.IsNullOrWhiteSpace(version))
                    .Select(version => version!)
                    .OrderBy(version => version, NuGetVersionHelper.Comparer)
                    .ToList();

                return new PackageVersionsResult(versions);
            }
            catch (Exception ex) when (IsFeedError(ex))
            {
                return PackageVersionsResult.Failed(ex.Message);
            }
        }

        public async Task<bool> DownloadPackageAsync(string packageId, string version, string targetPath, CancellationToken cancellationToken = default)
        {
            try
            {
                var index = await GetServiceIndexAsync(cancellationToken);
                var packageIdLower = packageId.ToLowerInvariant();
                var normalizedVersion = NuGetVersionHelper.Normalize(version);
                var url = $"{index.PackageBaseAddress}{packageIdLower}/{normalizedVersion}/{packageIdLower}.{normalizedVersion}.nupkg";

                using var response = await SendAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                await using var sourceStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                await using var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await sourceStream.CopyToAsync(fileStream, cancellationToken);
                return true;
            }
            catch (Exception ex) when (IsFeedError(ex) || ex is IOException or UnauthorizedAccessException)
            {
                return false;
            }
        }

        public async Task<PackageSearchResult> SearchAsync(string packageIdPrefix, CancellationToken cancellationToken = default)
        {
            try
            {
                var index = await GetServiceIndexAsync(cancellationToken);
                if (index.SearchQueryService == null)
                {
                    return PackageSearchResult.Failed("the feed has no search service");
                }

                var separator = index.SearchQueryService.Contains('?') ? '&' : '?';
                var url = $"{index.SearchQueryService}{separator}q={Uri.EscapeDataString(packageIdPrefix)}&prerelease=true&skip=0&take=100";

                using var response = await SendAsync(url, HttpCompletionOption.ResponseContentRead, cancellationToken);
                response.EnsureSuccessStatusCode();

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

                var packages = new List<PluginPackageInfo>();

                if (document.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in data.EnumerateArray())
                    {
                        var id = GetString(item, "id");
                        var version = GetString(item, "version");

                        // Search is full text, so results that merely mention the prefix are dropped here.
                        if (string.IsNullOrWhiteSpace(id) ||
                            string.IsNullOrWhiteSpace(version) ||
                            !id.StartsWith(packageIdPrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        packages.Add(new PluginPackageInfo(id, version, GetString(item, "description") ?? string.Empty, GetStrings(item, "owners")));
                    }
                }

                return new PackageSearchResult(packages);
            }
            catch (Exception ex) when (IsFeedError(ex))
            {
                return PackageSearchResult.Failed(ex.Message);
            }
        }

        private Task<ServiceIndex> GetServiceIndexAsync(CancellationToken cancellationToken)
        {
            // The index is read once per source and shared by every lookup. A failure is not kept,
            // so a later call can succeed once the feed is reachable again.
            var pending = _serviceIndex;
            if (pending == null || pending.IsFaulted || pending.IsCanceled)
            {
                pending = LoadServiceIndexAsync(cancellationToken);
                _serviceIndex = pending;
            }

            return pending;
        }

        private async Task<ServiceIndex> LoadServiceIndexAsync(CancellationToken cancellationToken)
        {
            using var response = await SendAsync(Location, HttpCompletionOption.ResponseContentRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (!document.RootElement.TryGetProperty("resources", out var resources) || resources.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException($"'{Location}' is not a NuGet V3 service index.");
            }

            string? packageBaseAddress = null;
            var searchServices = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var resource in resources.EnumerateArray())
            {
                var type = GetString(resource, "@type");
                var id = GetString(resource, "@id");

                if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                if (type.Equals(PackageBaseAddressType, StringComparison.OrdinalIgnoreCase))
                {
                    packageBaseAddress ??= id;
                }
                else if (type.StartsWith("SearchQueryService", StringComparison.OrdinalIgnoreCase))
                {
                    searchServices.TryAdd(type, id);
                }
            }

            if (packageBaseAddress == null)
            {
                throw new InvalidOperationException($"'{Location}' does not offer the {PackageBaseAddressType} resource needed to download packages.");
            }

            var searchService = PreferredSearchServiceTypes
                .Select(type => searchServices.GetValueOrDefault(type))
                .FirstOrDefault(id => id != null)
                ?? searchServices.Values.FirstOrDefault();

            return new ServiceIndex(EnsureTrailingSlash(packageBaseAddress), searchService?.TrimEnd('/'));
        }

        private async Task<HttpResponseMessage> SendAsync(string url, HttpCompletionOption completionOption, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);

            if (_credential != null)
            {
                var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_credential.UserName}:{_credential.Password}"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", token);
            }

            return await _httpClient.SendAsync(request, completionOption, cancellationToken);
        }

        private static bool IsFeedError(Exception ex)
        {
            return ex is HttpRequestException
                or TaskCanceledException
                or JsonException
                or InvalidOperationException
                or UriFormatException;
        }

        private static string? GetString(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }

        /// <summary>
        /// Reads a property that feeds serialize either as a string ("a, b") or as an array (["a", "b"]).
        /// </summary>
        private static IReadOnlyList<string> GetStrings(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var value))
            {
                return [];
            }

            return value.ValueKind switch
            {
                JsonValueKind.String => SplitList(value.GetString()),
                JsonValueKind.Array => value.EnumerateArray()
                    .Where(item => item.ValueKind == JsonValueKind.String)
                    .SelectMany(item => SplitList(item.GetString()))
                    .ToList(),
                _ => []
            };
        }

        private static List<string> SplitList(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? []
                : value.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }

        private static string EnsureTrailingSlash(string url)
        {
            return url.EndsWith('/') ? url : url + "/";
        }

        private sealed record ServiceIndex(string PackageBaseAddress, string? SearchQueryService);
    }
}
