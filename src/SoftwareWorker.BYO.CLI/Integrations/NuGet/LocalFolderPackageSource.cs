using SoftwareWorker.BYO.CLI.Helpers;

namespace SoftwareWorker.BYO.CLI.Integrations.NuGet
{
    /// <summary>
    /// A folder of <c>.nupkg</c> files used as a package source, typically the output of <c>dotnet pack</c>.
    /// Package metadata is read from the <c>.nuspec</c> embedded in each file.
    /// </summary>
    internal sealed class LocalFolderPackageSource : IPluginPackageSource
    {
        private const string MissingFolderError = "the folder does not exist";

        public LocalFolderPackageSource(string? name, string directory)
        {
            Location = directory.Trim();
            Name = string.IsNullOrWhiteSpace(name) ? Location : name;
        }

        public string Name { get; }

        public string Location { get; }

        public Task<PackageVersionsResult> GetVersionsAsync(string packageId, CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(Location))
            {
                return Task.FromResult(PackageVersionsResult.Failed(MissingFolderError));
            }

            var versions = LocalPackageHelper.GetPackages(Location, packageId)
                .Select(package => package.Version)
                .OrderBy(version => version, NuGetVersionHelper.Comparer)
                .ToList();

            return Task.FromResult(new PackageVersionsResult(versions));
        }

        public Task<bool> DownloadPackageAsync(string packageId, string version, string targetPath, CancellationToken cancellationToken = default)
        {
            var package = LocalPackageHelper.GetPackages(Location, packageId)
                .FirstOrDefault(candidate => candidate.Version.Equals(version, StringComparison.OrdinalIgnoreCase));

            if (package == null)
            {
                return Task.FromResult(false);
            }

            try
            {
                File.Copy(package.FilePath, targetPath, overwrite: true);
                return Task.FromResult(true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return Task.FromResult(false);
            }
        }

        public Task<PackageSearchResult> SearchAsync(string packageIdPrefix, CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(Location))
            {
                return Task.FromResult(PackageSearchResult.Failed(MissingFolderError));
            }

            var packages = LocalPackageHelper.GetLatestPackages(Location)
                .Where(package => package.Id.StartsWith(packageIdPrefix, StringComparison.OrdinalIgnoreCase))
                .Select(package => new PluginPackageInfo(package.Id, package.Version, package.Description, []))
                .ToList();

            return Task.FromResult(new PackageSearchResult(packages));
        }
    }
}
