namespace SoftwareWorker.BYO.CLI.Integrations.NuGet
{
    /// <summary>
    /// A place plugin packages are fetched from: a folder of <c>.nupkg</c> files or a NuGet V3 feed.
    /// Sources are tried in order by the installer, see <c>PluginSourceService</c>.
    /// </summary>
    internal interface IPluginPackageSource
    {
        /// <summary>
        /// Name shown to the user: <c>nuget.org</c>, the name a configured source was given, or its location.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Where the source lives: a service index URL or a folder path.
        /// </summary>
        string Location { get; }

        /// <summary>
        /// Lists every version of a package the source has, lowest first. An empty list means the
        /// source does not have the package; a failed result means the source could not be queried.
        /// </summary>
        Task<PackageVersionsResult> GetVersionsAsync(string packageId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Copies the <c>.nupkg</c> of one exact version to <paramref name="targetPath"/>.
        /// Returns false when the source does not have that version or the transfer failed.
        /// </summary>
        Task<bool> DownloadPackageAsync(string packageId, string version, string targetPath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Finds the latest version of each package whose id starts with <paramref name="packageIdPrefix"/>.
        /// </summary>
        Task<PackageSearchResult> SearchAsync(string packageIdPrefix, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// The versions of a package at one source. <see cref="Error"/> is set when the source could not be
    /// queried (unreachable, unauthorized, not a NuGet feed), which is different from the package not being there.
    /// </summary>
    internal sealed record PackageVersionsResult(IReadOnlyList<string> Versions, string? Error = null)
    {
        public bool Succeeded => Error == null;

        public static PackageVersionsResult NotFound { get; } = new([]);

        public static PackageVersionsResult Failed(string error) => new([], error);
    }

    /// <summary>
    /// The packages matching a search at one source, or the reason the search failed.
    /// </summary>
    internal sealed record PackageSearchResult(IReadOnlyList<PluginPackageInfo> Packages, string? Error = null)
    {
        public bool Succeeded => Error == null;

        public static PackageSearchResult Failed(string error) => new([], error);
    }

    /// <summary>
    /// A package as listed by a source. <see cref="Owners"/> is empty when the source does not track owners.
    /// </summary>
    internal sealed record PluginPackageInfo(string Id, string Version, string Description, IReadOnlyList<string> Owners);
}
