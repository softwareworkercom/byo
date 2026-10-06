using System.IO.Compression;
using System.Xml.Linq;

namespace SoftwareWorker.BYO.CLI.Helpers
{
    /// <summary>
    /// Reads locally published NuGet packages (<c>.nupkg</c> files) from a folder that
    /// acts as a local feed, resolving package metadata directly from the embedded
    /// <c>.nuspec</c> so callers can list and install packages without contacting a server.
    /// </summary>
    public static class LocalPackageHelper
    {
        /// <summary>
        /// Metadata describing a locally published package discovered in a feed folder.
        /// </summary>
        public sealed record LocalPackageInfo(string Id, string Version, string Description, string FilePath);

        /// <summary>
        /// Enumerates the local feed folder and returns the latest version of each package id.
        /// </summary>
        public static List<LocalPackageInfo> GetLatestPackages(string sourceDirectory)
        {
            return GetAllPackages(sourceDirectory)
                .GroupBy(package => package.Id, StringComparer.OrdinalIgnoreCase)
                .Select(group => group
                    .OrderBy(package => package.Version, NuGetVersionHelper.Comparer)
                    .Last())
                .OrderBy(package => package.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Returns every version of one package id found in the local feed folder.
        /// </summary>
        public static List<LocalPackageInfo> GetPackages(string sourceDirectory, string packageId)
        {
            return GetAllPackages(sourceDirectory)
                .Where(package => package.Id.Equals(packageId, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        private static List<LocalPackageInfo> GetAllPackages(string sourceDirectory)
        {
            var packages = new List<LocalPackageInfo>();

            if (string.IsNullOrWhiteSpace(sourceDirectory) || !Directory.Exists(sourceDirectory))
            {
                return packages;
            }

            foreach (var file in Directory.GetFiles(sourceDirectory, "*.nupkg", SearchOption.TopDirectoryOnly))
            {
                if (file.EndsWith(".symbols.nupkg", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var metadata = ReadMetadata(file);
                if (metadata != null)
                {
                    packages.Add(metadata);
                }
            }

            return packages;
        }

        private static LocalPackageInfo? ReadMetadata(string nupkgPath)
        {
            try
            {
                using var archive = ZipFile.OpenRead(nupkgPath);

                var nuspecEntry = archive.Entries.FirstOrDefault(entry =>
                        entry.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase) &&
                        !entry.FullName.Contains('/'))
                    ?? archive.Entries.FirstOrDefault(entry =>
                        entry.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase));

                if (nuspecEntry == null)
                {
                    return null;
                }

                using var stream = nuspecEntry.Open();
                var document = XDocument.Load(stream);

                var ns = document.Root?.Name.Namespace ?? XNamespace.None;
                var metadata = document.Root?.Element(ns + "metadata");
                if (metadata == null)
                {
                    return null;
                }

                var id = metadata.Element(ns + "id")?.Value?.Trim();
                var version = metadata.Element(ns + "version")?.Value?.Trim();
                var description = metadata.Element(ns + "description")?.Value?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(version))
                {
                    return null;
                }

                return new LocalPackageInfo(id, version, description, nupkgPath);
            }
            catch
            {
                return null;
            }
        }
    }
}
