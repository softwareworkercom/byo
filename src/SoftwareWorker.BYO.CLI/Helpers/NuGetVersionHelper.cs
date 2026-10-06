namespace SoftwareWorker.BYO.CLI.Helpers
{
    /// <summary>
    /// Compares and selects NuGet-style version strings without depending on the NuGet client libraries.
    /// </summary>
    internal static class NuGetVersionHelper
    {
        /// <summary>
        /// Orders versions ascending: <c>1.0.0-beta</c> &lt; <c>1.0.0</c> &lt; <c>1.0.1</c>.
        /// </summary>
        public static readonly IComparer<string> Comparer = Comparer<string>.Create(Compare);

        /// <summary>
        /// True when the version carries a prerelease label, e.g. <c>1.0.0-beta</c>.
        /// </summary>
        public static bool IsPrerelease(string version)
        {
            return StripBuildMetadata(version).Contains('-', StringComparison.Ordinal);
        }

        /// <summary>
        /// Picks the version to install when none was requested: the highest stable version,
        /// or the highest prerelease when the package has no stable release yet.
        /// </summary>
        public static string? SelectLatest(IEnumerable<string> versions)
        {
            var all = versions.Where(version => !string.IsNullOrWhiteSpace(version)).ToList();
            if (all.Count == 0)
            {
                return null;
            }

            var stable = all.Where(version => !IsPrerelease(version)).ToList();
            return (stable.Count > 0 ? stable : all).OrderBy(version => version, Comparer).Last();
        }

        /// <summary>
        /// Finds the version matching a requested one: an exact (case-insensitive) match first, otherwise
        /// the version that compares equal once build metadata and missing trailing parts are ignored,
        /// so <c>1.2.3</c> matches a locally packed <c>1.2.3+g7360a40ae3</c>.
        /// </summary>
        public static string? FindMatch(IEnumerable<string> versions, string requested)
        {
            var all = versions.ToList();
            var trimmed = requested.Trim();

            return all.FirstOrDefault(version => version.Equals(trimmed, StringComparison.OrdinalIgnoreCase))
                ?? all.FirstOrDefault(version => Compare(version, trimmed) == 0);
        }

        /// <summary>
        /// Lowercases a version and drops build metadata, which is how versions appear in NuGet V3 URLs.
        /// </summary>
        public static string Normalize(string version)
        {
            return StripBuildMetadata(version).Trim().ToLowerInvariant();
        }

        /// <summary>
        /// Compares two NuGet-style version strings in ascending order. Numeric release parts
        /// are compared segment by segment and a prerelease version sorts below the matching
        /// release version (e.g. <c>1.0.0-beta</c> &lt; <c>1.0.0</c>). Build metadata is ignored.
        /// </summary>
        public static int Compare(string left, string right)
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
            var main = StripBuildMetadata(version).Trim();

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

        private static string StripBuildMetadata(string version)
        {
            var index = version.IndexOf('+', StringComparison.Ordinal);
            return index >= 0 ? version[..index] : version;
        }
    }
}
