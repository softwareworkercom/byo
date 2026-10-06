using SoftwareWorker.BYO.SDK.Constants;
using SoftwareWorker.BYO.CLI.Helpers;
using SoftwareWorker.BYO.CLI.Integrations.NuGet;
using SoftwareWorker.BYO.SDK;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace SoftwareWorker.BYO.CLI.Service
{
    /// <summary>
    /// Encapsulates all logic for installing a BYO CLI plugin package from any configured package
    /// source (a folder of .nupkg files, NuGet.org or another NuGet V3 feed), including transitive
    /// NuGet dependency resolution. Contains no UI concerns - callers render
    /// <see cref="PluginInstallResult"/> however they see fit.
    /// </summary>
    public static class PluginInstallationService
    {
        public sealed record PluginInstallResult(
            bool Success,
            string? ErrorMessage,
            string? PackageId,
            string? Version,
            string? Source,
            List<string> Warnings,
            List<string> Handlers)
        {
            public static PluginInstallResult Failure(string errorMessage, List<string>? warnings = null) =>
                new(false, errorMessage, null, null, null, warnings ?? [], []);
        }

        /// <summary>
        /// The package picked for an install: where it came from, which version, and the downloaded .nupkg.
        /// </summary>
        internal sealed record ResolvedPackage(IPluginPackageSource Source, string Version, string PackageFilePath);

        /// <summary>
        /// Installs a plugin package from the first configured package source that has it: the sources
        /// listed in settings, in order, then NuGet.org. See <see cref="PluginSourceService"/>.
        /// </summary>
        public static Task<PluginInstallResult> InstallPluginAsync(string packageId, string? version)
        {
            return InstallPluginAsync(packageId, version, PluginSourceService.GetSources());
        }

        /// <summary>
        /// Installs a plugin package from the first of <paramref name="sources"/> that has a matching version.
        /// Dependencies are fetched from that source first and from the remaining sources after it.
        /// </summary>
        internal static async Task<PluginInstallResult> InstallPluginAsync(string packageId, string? version, IReadOnlyList<IPluginPackageSource> sources)
        {
            var warnings = new List<string>();
            var packageIdLower = packageId.ToLowerInvariant();

            var resolved = await ResolveAndDownloadAsync(packageId, version, sources, warnings);
            if (resolved == null)
            {
                var requested = string.IsNullOrWhiteSpace(version) ? string.Empty : $" version '{version.Trim()}'";
                var searched = sources.Count == 0
                    ? "any package source: none is configured"
                    : string.Join(", ", sources.Select(candidate => candidate.Name));
                return PluginInstallResult.Failure($"Package '{packageId}'{requested} was not found in {searched}.", warnings);
            }

            var packageVersion = resolved.Version;
            var packageFilePath = resolved.PackageFilePath;
            var extractedDirectory = GetPackagePaths(packageIdLower, packageVersion).ExtractedDirectory;

            if (!ExtractPackage(packageFilePath, extractedDirectory))
            {
                return PluginInstallResult.Failure($"Failed to extract package '{packageId}' version '{packageVersion}'.", warnings);
            }

            var candidateDirectories = GetCandidateAssemblyDirectories(extractedDirectory);
            if (candidateDirectories.Count == 0)
            {
                return PluginInstallResult.Failure($"Package '{packageId}' does not contain .NET assemblies under lib/ or tools/.", warnings);
            }

            var selectedDirectory = SelectBestCandidateDirectory(candidateDirectories);
            var selectedAssemblies = Directory.GetFiles(selectedDirectory, "*.dll", SearchOption.TopDirectoryOnly).ToList();

            if (selectedAssemblies.Count == 0)
            {
                return PluginInstallResult.Failure($"No assemblies were found for package '{packageId}'.", warnings);
            }

            if (!TryFindValidHandlers(selectedAssemblies, out var handlers, out var scanErrors))
            {
                var details = scanErrors.Count == 0
                    ? string.Empty
                    : $" Details: {string.Join(" | ", scanErrors.Take(3))}";
                return PluginInstallResult.Failure(
                    $"Package '{packageId}' does not implement SoftwareWorker.BYO.Abstractions command handlers.{details}",
                    warnings);
            }

            var installedVersionDirectory = Path.Combine(SystemConstants.PLUGINS_BINARIES_DIRECTORY, packageIdLower, packageVersion);
            if (Directory.Exists(installedVersionDirectory))
            {
                Directory.Delete(installedVersionDirectory, true);
            }

            Directory.CreateDirectory(installedVersionDirectory);

            foreach (var file in Directory.GetFiles(selectedDirectory, "*", SearchOption.TopDirectoryOnly))
            {
                var destinationPath = Path.Combine(installedVersionDirectory, Path.GetFileName(file));
                File.Copy(file, destinationPath, overwrite: true);
            }

            // Plugin nupkgs only contain their own assembly - transitive NuGet dependencies (e.g. EF Core, Npgsql)
            // must be fetched and placed alongside it so the plugin's isolated AssemblyLoadContext can find them.
            // The source the plugin came from is asked first, so a private feed can supply private dependencies.
            IReadOnlyList<IPluginPackageSource> dependencySources =
                [resolved.Source, .. sources.Where(candidate => !ReferenceEquals(candidate, resolved.Source))];

            await InstallDependencyAssembliesAsync(
                extractedDirectory,
                installedVersionDirectory,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                warnings,
                dependencySources);

            return new PluginInstallResult(true, null, packageId, packageVersion, resolved.Source.Name, warnings, handlers.OrderBy(h => h).ToList());
        }

        /// <summary>
        /// Walks <paramref name="sources"/> in order and downloads the package from the first one that has
        /// a matching version: the requested version, or the latest stable (else latest prerelease) version
        /// when none was requested. Sources that cannot be queried are reported in <paramref name="warnings"/>
        /// and skipped, so a feed that is down does not block installs from the others.
        /// </summary>
        internal static async Task<ResolvedPackage?> ResolveAndDownloadAsync(
            string packageId,
            string? requestedVersion,
            IReadOnlyList<IPluginPackageSource> sources,
            List<string> warnings)
        {
            foreach (var source in sources)
            {
                var lookup = await source.GetVersionsAsync(packageId);
                if (!lookup.Succeeded)
                {
                    warnings.Add($"Source '{source.Name}' could not be queried: {lookup.Error}");
                    continue;
                }

                var version = string.IsNullOrWhiteSpace(requestedVersion)
                    ? NuGetVersionHelper.SelectLatest(lookup.Versions)
                    : NuGetVersionHelper.FindMatch(lookup.Versions, requestedVersion);

                if (version == null)
                {
                    continue;
                }

                var (rootDirectory, packageFilePath, _) = GetPackagePaths(packageId.ToLowerInvariant(), version);
                Directory.CreateDirectory(rootDirectory);

                if (!await source.DownloadPackageAsync(packageId, version, packageFilePath))
                {
                    warnings.Add($"Source '{source.Name}' lists '{packageId}' {version} but the package could not be downloaded from it.");
                    continue;
                }

                return new ResolvedPackage(source, version, packageFilePath);
            }

            return null;
        }

        private static async Task<bool> TryDownloadFromAnySourceAsync(
            string packageId,
            string version,
            string targetPath,
            IReadOnlyList<IPluginPackageSource> sources)
        {
            foreach (var source in sources)
            {
                if (await source.DownloadPackageAsync(packageId, version, targetPath))
                {
                    return true;
                }
            }

            return false;
        }

        private static (string RootDirectory, string PackageFilePath, string ExtractedDirectory) GetPackagePaths(string packageIdLower, string version)
        {
            var rootDirectory = Path.Combine(SystemConstants.PLUGINS_PACKAGES_DIRECTORY, packageIdLower, version);

            return (
                rootDirectory,
                Path.Combine(rootDirectory, $"{packageIdLower}.{version}.nupkg"),
                Path.Combine(rootDirectory, "extracted"));
        }

        /// <summary>
        /// Recursively resolves the NuGet dependencies declared in a package's nuspec, downloading
        /// and copying their assemblies next to the plugin's own assembly so they can be found by
        /// the plugin's isolated AssemblyLoadContext at runtime.
        /// </summary>
        private static async Task InstallDependencyAssembliesAsync(
            string extractedDirectory,
            string installedVersionDirectory,
            HashSet<string> visitedPackageIds,
            List<string> warnings,
            IReadOnlyList<IPluginPackageSource> sources)
        {
            var nuspecPath = Directory.GetFiles(extractedDirectory, "*.nuspec", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (nuspecPath == null)
            {
                return;
            }

            foreach (var (id, version) in ReadPackageDependencies(extractedDirectory))
            {
                if (id.StartsWith("SoftwareWorker.BYO.", StringComparison.OrdinalIgnoreCase))
                {
                    // Provided by the host CLI itself - the plugin must not bring its own copy.
                    continue;
                }

                if (!visitedPackageIds.Add(id.ToLowerInvariant()))
                {
                    continue;
                }

                var dependencyExtractedDirectory = await EnsureDependencyPackageExtractedAsync(id, version, sources);
                if (dependencyExtractedDirectory == null)
                {
                    warnings.Add($"Could not resolve dependency '{id}' {version}; the plugin may fail to load at runtime.");
                    continue;
                }

                foreach (var file in GetDependencyAssetFiles(dependencyExtractedDirectory))
                {
                    var destinationPath = Path.Combine(installedVersionDirectory, Path.GetFileName(file));
                    if (!File.Exists(destinationPath))
                    {
                        File.Copy(file, destinationPath);
                    }
                }

                await InstallDependencyAssembliesAsync(dependencyExtractedDirectory, installedVersionDirectory, visitedPackageIds, warnings, sources);
            }
        }

        /// <summary>
        /// Makes sure a dependency package is downloaded and extracted under the packages folder,
        /// fetching it from the first of <paramref name="sources"/> that has the exact version.
        /// </summary>
        internal static async Task<string?> EnsureDependencyPackageExtractedAsync(string packageId, string version, IReadOnlyList<IPluginPackageSource> sources)
        {
            var (packageRootDirectory, packageFilePath, extractedDirectory) = GetPackagePaths(packageId.ToLowerInvariant(), version);

            Directory.CreateDirectory(packageRootDirectory);

            if (Directory.Exists(extractedDirectory) &&
                Directory.GetFiles(extractedDirectory, "*.nuspec", SearchOption.TopDirectoryOnly).Length > 0)
            {
                return extractedDirectory;
            }

            if (!File.Exists(packageFilePath) &&
                !await TryDownloadFromAnySourceAsync(packageId, version, packageFilePath, sources))
            {
                return null;
            }

            return ExtractPackage(packageFilePath, extractedDirectory) ? extractedDirectory : null;
        }

        internal static string? TryGetExtractedPackageDirectory(string packageId, string version)
        {
            var packageIdLower = packageId.ToLowerInvariant();
            var extractedDirectory = Path.Combine(SystemConstants.PLUGINS_PACKAGES_DIRECTORY, packageIdLower, version, "extracted");

            if (!Directory.Exists(extractedDirectory))
            {
                return null;
            }

            return Directory.GetFiles(extractedDirectory, "*.nuspec", SearchOption.TopDirectoryOnly).Length > 0
                ? extractedDirectory
                : null;
        }

        internal static IReadOnlyList<(string Id, string Version)> ReadPackageDependencies(string extractedDirectory)
        {
            var nuspecPath = Directory.GetFiles(extractedDirectory, "*.nuspec", SearchOption.TopDirectoryOnly).FirstOrDefault();
            return nuspecPath == null ? [] : ReadNuspecDependencies(nuspecPath);
        }

        private static List<(string Id, string Version)> ReadNuspecDependencies(string nuspecPath)
        {
            var dependencies = new List<(string Id, string Version)>();

            try
            {
                var document = XDocument.Load(nuspecPath);
                var ns = document.Root?.Name.Namespace ?? XNamespace.None;
                var dependenciesElement = document.Root?.Element(ns + "metadata")?.Element(ns + "dependencies");
                if (dependenciesElement == null)
                {
                    return dependencies;
                }

                foreach (var dependency in SelectDependencyElements(dependenciesElement, ns))
                {
                    var id = dependency.Attribute("id")?.Value?.Trim();
                    var version = NormalizeVersion(dependency.Attribute("version")?.Value);

                    if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(version))
                    {
                        dependencies.Add((id, version));
                    }
                }
            }
            catch
            {
                // Malformed nuspec - skip dependency resolution rather than failing the install.
            }

            return dependencies;
        }

        private static IEnumerable<XElement> SelectDependencyElements(XElement dependenciesElement, XNamespace ns)
        {
            var directDependencies = dependenciesElement.Elements(ns + "dependency").ToList();
            if (directDependencies.Count > 0)
            {
                return directDependencies;
            }

            var groups = dependenciesElement.Elements(ns + "group").ToList();
            if (groups.Count == 0)
            {
                return [];
            }

            var selectedGroup = groups
                .Select(group => new
                {
                    Group = group,
                    Rank = GetTargetFrameworkRank(group.Attribute("targetFramework")?.Value)
                })
                .Where(candidate => candidate.Rank < int.MaxValue)
                .OrderBy(candidate => candidate.Rank)
                .FirstOrDefault();

            if (selectedGroup != null)
            {
                return selectedGroup.Group.Elements(ns + "dependency");
            }

            return groups
                .Where(group => string.IsNullOrWhiteSpace(group.Attribute("targetFramework")?.Value))
                .SelectMany(group => group.Elements(ns + "dependency"));
        }

        private static int GetTargetFrameworkRank(string? targetFramework)
        {
            if (string.IsNullOrWhiteSpace(targetFramework))
            {
                return 0;
            }

            var normalized = NormalizeTargetFrameworkMoniker(targetFramework);
            string[] preferredTargetFrameworks =
            [
                "net10.0",
                "net9.0",
                "net8.0",
                "net7.0",
                "net6.0",
                "netstandard2.1",
                "netstandard2.0"
            ];

            for (var index = 0; index < preferredTargetFrameworks.Length; index++)
            {
                if (normalized.StartsWith(preferredTargetFrameworks[index], StringComparison.OrdinalIgnoreCase))
                {
                    return index + 1;
                }
            }

            return int.MaxValue;
        }

        private static string NormalizeTargetFrameworkMoniker(string targetFramework)
        {
            return targetFramework.Trim().TrimStart('.').ToLowerInvariant();
        }

        private static string? NormalizeVersion(string? version)
        {
            if (string.IsNullOrWhiteSpace(version))
            {
                return null;
            }

            var trimmed = version.Trim().Trim('[', ']', '(', ')');
            var commaIndex = trimmed.IndexOf(',', StringComparison.Ordinal);
            return (commaIndex >= 0 ? trimmed[..commaIndex] : trimmed).Trim();
        }

        private static bool ExtractPackage(string packageFilePath, string extractedDirectory)
        {
            try
            {
                if (Directory.Exists(extractedDirectory))
                {
                    Directory.Delete(extractedDirectory, true);
                }

                Directory.CreateDirectory(extractedDirectory);
                ZipFile.ExtractToDirectory(packageFilePath, extractedDirectory, overwriteFiles: true);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static List<string> GetDependencyAssetFiles(string extractedDirectory)
        {
            var files = new List<string>();

            // Runtime-specific managed assets must win over same-named lib/ assets. Packages such as
            // Microsoft.Data.SqlClient ship a lib/ placeholder that throws PlatformNotSupportedException,
            // with the real implementation under runtimes/<rid>/lib/. Both flatten to the same file name
            // in the plugin folder, so only one of them can be kept.
            var preferredRuntimeIdentifiers = GetPreferredRuntimeIdentifiers().ToList();
            var runtimeManagedDirectories = GetRuntimeManagedAssetDirectories(extractedDirectory)
                .Where(directory => preferredRuntimeIdentifiers.Contains(GetRuntimeIdentifier(directory) ?? string.Empty, StringComparer.OrdinalIgnoreCase))
                .ToList();
            if (runtimeManagedDirectories.Count > 0)
            {
                var selected = SelectBestRuntimeAssetDirectory(runtimeManagedDirectories);
                files.AddRange(Directory.GetFiles(selected, "*.dll", SearchOption.TopDirectoryOnly));
            }

            var candidateDirectories = GetCandidateAssemblyDirectories(extractedDirectory);
            if (candidateDirectories.Count > 0)
            {
                var runtimeFileNames = new HashSet<string>(files.Select(Path.GetFileName)!, StringComparer.OrdinalIgnoreCase);
                var selected = SelectBestCandidateDirectory(candidateDirectories);
                files.AddRange(Directory.GetFiles(selected, "*.dll", SearchOption.TopDirectoryOnly)
                    .Where(file => !runtimeFileNames.Contains(Path.GetFileName(file))));
            }

            var runtimeNativeDirectories = GetRuntimeNativeAssetDirectories(extractedDirectory);
            if (runtimeNativeDirectories.Count > 0)
            {
                var selected = SelectBestRuntimeAssetDirectory(runtimeNativeDirectories);
                files.AddRange(Directory.GetFiles(selected, "*", SearchOption.TopDirectoryOnly)
                    .Where(file => !string.Equals(Path.GetExtension(file), ".pdb", StringComparison.OrdinalIgnoreCase)));
            }

            return files
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static List<string> GetCandidateAssemblyDirectories(string extractedDirectory)
        {
            var candidates = new List<string>();
            var libDirectory = Path.Combine(extractedDirectory, "lib");
            var toolsDirectory = Path.Combine(extractedDirectory, "tools");

            if (Directory.Exists(libDirectory))
            {
                candidates.AddRange(Directory.GetDirectories(libDirectory)
                    .Where(directory => Directory.GetFiles(directory, "*.dll", SearchOption.TopDirectoryOnly).Length > 0));
            }

            if (Directory.Exists(toolsDirectory))
            {
                candidates.AddRange(Directory.GetDirectories(toolsDirectory)
                    .Where(directory => Directory.GetFiles(directory, "*.dll", SearchOption.TopDirectoryOnly).Length > 0));

                if (Directory.GetFiles(toolsDirectory, "*.dll", SearchOption.TopDirectoryOnly).Length > 0)
                {
                    candidates.Add(toolsDirectory);
                }
            }

            if (candidates.Count == 0 && Directory.GetFiles(extractedDirectory, "*.dll", SearchOption.TopDirectoryOnly).Length > 0)
            {
                candidates.Add(extractedDirectory);
            }

            return candidates.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static List<string> GetRuntimeManagedAssetDirectories(string extractedDirectory)
        {
            var runtimesDirectory = Path.Combine(extractedDirectory, "runtimes");
            if (!Directory.Exists(runtimesDirectory))
            {
                return [];
            }

            return Directory.GetDirectories(runtimesDirectory, "*", SearchOption.AllDirectories)
                .Where(directory => IsRuntimeAssetDirectory(directory, runtimesDirectory, "lib"))
                .Where(directory => Directory.GetFiles(directory, "*.dll", SearchOption.TopDirectoryOnly).Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static List<string> GetRuntimeNativeAssetDirectories(string extractedDirectory)
        {
            var runtimesDirectory = Path.Combine(extractedDirectory, "runtimes");
            if (!Directory.Exists(runtimesDirectory))
            {
                return [];
            }

            return Directory.GetDirectories(runtimesDirectory, "*", SearchOption.AllDirectories)
                .Where(directory => IsRuntimeAssetDirectory(directory, runtimesDirectory, "native"))
                .Where(directory => Directory.GetFiles(directory, "*", SearchOption.TopDirectoryOnly).Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static bool IsRuntimeAssetDirectory(string directory, string runtimesDirectory, string assetSegment)
        {
            var relativePath = Path.GetRelativePath(runtimesDirectory, directory);
            var segments = relativePath.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);

            return segments.Length >= 2 &&
                string.Equals(segments[1], assetSegment, StringComparison.OrdinalIgnoreCase);
        }

        private static string SelectBestCandidateDirectory(IReadOnlyCollection<string> candidates)
        {
            if (candidates.Count == 1)
            {
                return candidates.First();
            }

            string[] priorities =
            [
                "net10.0",
                "net9.0",
                "net8.0",
                "net7.0",
                "net6.0",
                "netstandard2.1",
                "netstandard2.0"
            ];

            foreach (var priority in priorities)
            {
                var match = candidates.FirstOrDefault(path =>
                    string.Equals(Path.GetFileName(path), priority, StringComparison.OrdinalIgnoreCase));

                if (match != null)
                {
                    return match;
                }
            }

            return candidates
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .First();
        }

        private static string SelectBestRuntimeAssetDirectory(IReadOnlyCollection<string> candidates)
        {
            if (candidates.Count == 1)
            {
                return candidates.First();
            }

            foreach (var runtimeIdentifier in GetPreferredRuntimeIdentifiers())
            {
                var runtimeMatches = candidates
                    .Where(path => string.Equals(GetRuntimeIdentifier(path), runtimeIdentifier, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (runtimeMatches.Count == 0)
                {
                    continue;
                }

                var managedMatches = runtimeMatches
                    .Where(path => !string.Equals(Path.GetFileName(path), "native", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (managedMatches.Count > 0)
                {
                    return SelectBestCandidateDirectory(managedMatches);
                }

                return runtimeMatches
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .First();
            }

            var fallbackManagedMatches = candidates
                .Where(path => !string.Equals(Path.GetFileName(path), "native", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (fallbackManagedMatches.Count > 0)
            {
                return SelectBestCandidateDirectory(fallbackManagedMatches);
            }

            return candidates
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .First();
        }

        private static IEnumerable<string> GetPreferredRuntimeIdentifiers()
        {
            var architecture = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();

            if (OperatingSystem.IsWindows())
            {
                return [$"win-{architecture}", "win", "any"];
            }

            if (OperatingSystem.IsMacOS())
            {
                return [$"osx-{architecture}", "osx", "unix", "any"];
            }

            if (OperatingSystem.IsLinux())
            {
                return [$"linux-{architecture}", "linux", "unix", "any"];
            }

            return ["any"];
        }

        private static string? GetRuntimeIdentifier(string directory)
        {
            var segments = directory.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);

            for (var index = 0; index < segments.Length - 1; index++)
            {
                if (string.Equals(segments[index], "runtimes", StringComparison.OrdinalIgnoreCase))
                {
                    return segments[index + 1];
                }
            }

            return null;
        }

        private static bool TryFindValidHandlers(
            IReadOnlyCollection<string> assemblyPaths,
            out List<string> handlers,
            out List<string> errors)
        {
            handlers = new List<string>();
            errors = new List<string>();

            foreach (var assemblyPath in assemblyPaths)
            {
                try
                {
                    var assembly = Assembly.LoadFrom(assemblyPath);
                    Type[] types;

                    try
                    {
                        types = assembly.GetTypes();
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        types = ex.Types.Where(t => t != null).Cast<Type>().ToArray();
                    }

                    var found = types
                        .Where(type => type.IsClass && !type.IsAbstract)
                        .Where(type => typeof(BaseCommandHandler).IsAssignableFrom(type))
                        .Select(type => type.FullName ?? type.Name)
                        .ToList();

                    handlers.AddRange(found);
                }
                catch (Exception ex)
                {
                    errors.Add($"{Path.GetFileName(assemblyPath)}: {ex.Message}");
                }
            }

            handlers = handlers
                .Distinct(StringComparer.Ordinal)
                .ToList();

            return handlers.Count > 0;
        }
    }
}
