using System.IO.Compression;
using System.Xml.Linq;

namespace SoftwareWorker.BYO.CLI.Core.Helpers
{
    /// <summary>
    /// Reads locally published NuGet packages (<c>.nupkg</c> files) from a folder that
    /// acts as a local feed, resolving package metadata directly from the embedded
    /// <c>.nuspec</c> so callers can list and install packages without contacting NuGet.org.
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
                    .OrderBy(package => package.Version, Comparer<string>.Create(CompareVersions))
                    .Last())
                .OrderBy(package => package.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Resolves a single package from the local feed by id, honoring an explicit version
        /// when supplied and otherwise selecting the latest (stable preferred) version.
        /// </summary>
        public static LocalPackageInfo? ResolvePackage(string sourceDirectory, string packageId, string? version)
        {
            var matches = GetAllPackages(sourceDirectory)
                .Where(package => package.Id.Equals(packageId, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matches.Count == 0)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(version))
            {
                var requested = version.Trim();
                return matches.FirstOrDefault(package =>
                    package.Version.Equals(requested, StringComparison.OrdinalIgnoreCase));
            }

            var stable = matches
                .Where(package => !package.Version.Contains('-', StringComparison.Ordinal))
                .ToList();

            return (stable.Count > 0 ? stable : matches)
                .OrderBy(package => package.Version, Comparer<string>.Create(CompareVersions))
                .Last();
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

        /// <summary>
        /// Compares two NuGet-style version strings in ascending order. Numeric release parts
        /// are compared segment by segment and a prerelease version sorts below the matching
        /// release version (e.g. <c>1.0.0-beta</c> &lt; <c>1.0.0</c>).
        /// </summary>
        private static int CompareVersions(string left, string right)
        {
            var (releaseLeft, prereleaseLeft) = SplitVersion(left);
            var (releaseRight, prereleaseRight) = SplitVersion(right);

            var length = Math.Max(releaseLeft.Length, releaseRight.Length);
            for (var index = 0; index < length; index++)
            {
                var partLeft = index < releaseLeft.Length ? releaseLeft[index] : 0;
                var partRight = index < releaseRight.Length ? releaseRight[index] : 0;

                if (partLeft != partRight)
                {
                    return partLeft.CompareTo(partRight);
                }
            }

            var leftHasPrerelease = !string.IsNullOrEmpty(prereleaseLeft);
            var rightHasPrerelease = !string.IsNullOrEmpty(prereleaseRight);

            if (leftHasPrerelease != rightHasPrerelease)
            {
                return leftHasPrerelease ? -1 : 1;
            }

            return string.Compare(prereleaseLeft, prereleaseRight, StringComparison.OrdinalIgnoreCase);
        }

        private static (int[] Release, string Prerelease) SplitVersion(string version)
        {
            var main = version.Trim();

            var buildMetadataIndex = main.IndexOf('+', StringComparison.Ordinal);
            if (buildMetadataIndex >= 0)
            {
                main = main[..buildMetadataIndex];
            }

            var prerelease = string.Empty;
            var prereleaseIndex = main.IndexOf('-', StringComparison.Ordinal);
            if (prereleaseIndex >= 0)
            {
                prerelease = main[(prereleaseIndex + 1)..];
                main = main[..prereleaseIndex];
            }

            var release = main
                .Split('.', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => int.TryParse(part, out var number) ? number : 0)
                .ToArray();

            return (release, prerelease);
        }
    }
}
