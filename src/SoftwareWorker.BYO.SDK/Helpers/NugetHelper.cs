using System.Text.Json;

namespace SoftwareWorker.BYO.CLI.Core.Helpers
{
    /// <summary>
    /// Centralizes direct HTTP interactions with NuGet.org (version resolution and package downloads)
    /// used by plugin installation.
    /// </summary>
    public static class NugetHelper
    {
        private static readonly HttpClient HttpClient = new()
        {
            Timeout = TimeSpan.FromMinutes(2)
        };

        /// <summary>
        /// Resolves the latest stable version of a package from NuGet.org, falling back to the
        /// latest prerelease version when no stable version exists.
        /// </summary>
        public static async Task<string?> ResolveLatestVersionAsync(string packageIdLower)
        {
            var indexUrl = $"https://api.nuget.org/v3-flatcontainer/{packageIdLower}/index.json";

            try
            {
                using var stream = await HttpClient.GetStreamAsync(indexUrl);
                using var document = await JsonDocument.ParseAsync(stream);

                if (!document.RootElement.TryGetProperty("versions", out var versionsElement) ||
                    versionsElement.ValueKind != JsonValueKind.Array)
                {
                    return null;
                }

                var versions = versionsElement
                    .EnumerateArray()
                    .Select(v => v.GetString())
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Select(v => v!)
                    .ToList();

                if (versions.Count == 0)
                {
                    return null;
                }

                var latestStable = versions.LastOrDefault(v => !v.Contains('-', StringComparison.Ordinal));
                return latestStable ?? versions.Last();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Downloads a package's .nupkg from NuGet.org's flat container to the given target path.
        /// </summary>
        public static async Task<bool> DownloadPackageAsync(string packageIdLower, string version, string targetPath)
        {
            var packageUrl = $"https://api.nuget.org/v3-flatcontainer/{packageIdLower}/{version}/{packageIdLower}.{version}.nupkg";

            try
            {
                using var response = await HttpClient.GetAsync(packageUrl);
                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                await using var sourceStream = await response.Content.ReadAsStreamAsync();
                await using var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await sourceStream.CopyToAsync(fileStream);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
