using SoftwareWorker.BYO.CLI.Core.Constants;
using SoftwareWorker.BYO.CLI.Core.Helpers;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace SoftwareWorker.BYO.CLI.Core.Service
{
    /// <summary>
    /// Encapsulates all logic for installing a BYO CLI plugin package (from a local feed or
    /// NuGet.org), including transitive NuGet dependency resolution. Contains no UI concerns -
    /// callers render <see cref="PluginInstallResult"/> however they see fit.
    /// </summary>
    public static class InstallationService
    {
        public sealed record PluginInstallResult(
            bool Success,
            string? ErrorMessage,
            string? PackageId,
            string? Version,
            List<string> Warnings,
            List<string> Handlers)
        {
            public static PluginInstallResult Failure(string errorMessage, List<string>? warnings = null) =>
                new(false, errorMessage, null, null, warnings ?? [], []);
        }

        /// <summary>
        /// Installs a plugin package, preferring a local feed (when <paramref name="source"/> is
        /// provided and contains the package) before falling back to NuGet.org.
        /// </summary>
        public static async Task<PluginInstallResult> InstallPluginAsync(string packageId, string? version, string? source)
        {
            var warnings = new List<string>();
            var packageIdLower = packageId.ToLowerInvariant();

            var localPackage = TryResolveLocalPackage(packageId, version, source, warnings);

            var resolvedVersion = localPackage?.Version
                ?? (string.IsNullOrWhiteSpace(version)
                    ? await NugetHelper.ResolveLatestVersionAsync(packageIdLower)
                    : version.Trim());

            if (string.IsNullOrWhiteSpace(resolvedVersion))
            {
                return PluginInstallResult.Failure($"Unable to resolve a version for package '{packageId}'.", warnings);
            }

            var packageVersion = resolvedVersion.Trim();
            var packageRootDirectory = Path.Combine(SystemConstants.PLUGINS_PACKAGES_DIRECTORY, packageIdLower, packageVersion);
            var packageFilePath = Path.Combine(packageRootDirectory, $"{packageIdLower}.{packageVersion}.nupkg");
            var extractedDirectory = Path.Combine(packageRootDirectory, "extracted");

            Directory.CreateDirectory(packageRootDirectory);

            if (localPackage != null)
            {
                if (!CopyLocalPackage(localPackage.FilePath, packageFilePath))
                {
                    return PluginInstallResult.Failure($"Failed to copy local package '{localPackage.FilePath}'.", warnings);
                }
            }
            else if (!await NugetHelper.DownloadPackageAsync(packageIdLower, packageVersion, packageFilePath))
            {
                return PluginInstallResult.Failure($"Failed to download package '{packageId}' version '{packageVersion}'.", warnings);
            }

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
            await InstallDependencyAssembliesAsync(
                extractedDirectory,
                installedVersionDirectory,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                warnings);

            return new PluginInstallResult(true, null, packageId, packageVersion, warnings, handlers.OrderBy(h => h).ToList());
        }

        private static LocalPackageHelper.LocalPackageInfo? TryResolveLocalPackage(string packageId, string? version, string? source, List<string> warnings)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return null;
            }

            var sourceDirectory = source.Trim();

            if (!Directory.Exists(sourceDirectory))
            {
                warnings.Add($"Local source '{sourceDirectory}' does not exist. Falling back to NuGet.org.");
                return null;
            }

            var localPackage = LocalPackageHelper.ResolvePackage(sourceDirectory, packageId, version);

            if (localPackage == null)
            {
                warnings.Add($"Package '{packageId}' was not found in local source '{sourceDirectory}'. Falling back to NuGet.org.");
            }

            return localPackage;
        }

        private static bool CopyLocalPackage(string sourcePath, string targetPath)
        {
            try
            {
                File.Copy(sourcePath, targetPath, overwrite: true);
                return true;
            }
            catch
            {
                return false;
            }
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
            List<string> warnings)
        {
            var nuspecPath = Directory.GetFiles(extractedDirectory, "*.nuspec", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (nuspecPath == null)
            {
                return;
            }

            foreach (var (id, version) in ReadNuspecDependencies(nuspecPath))
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

                var dependencyExtractedDirectory = await EnsureDependencyPackageExtractedAsync(id, version);
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

                await InstallDependencyAssembliesAsync(dependencyExtractedDirectory, installedVersionDirectory, visitedPackageIds, warnings);
            }
        }

        private static async Task<string?> EnsureDependencyPackageExtractedAsync(string packageId, string version)
        {
            var packageIdLower = packageId.ToLowerInvariant();
            var packageRootDirectory = Path.Combine(SystemConstants.PLUGINS_PACKAGES_DIRECTORY, packageIdLower, version);
            var packageFilePath = Path.Combine(packageRootDirectory, $"{packageIdLower}.{version}.nupkg");
            var extractedDirectory = Path.Combine(packageRootDirectory, "extracted");

            Directory.CreateDirectory(packageRootDirectory);

            if (Directory.Exists(extractedDirectory) &&
                Directory.GetFiles(extractedDirectory, "*.nuspec", SearchOption.TopDirectoryOnly).Length > 0)
            {
                return extractedDirectory;
            }

            if (!File.Exists(packageFilePath))
            {
                if (!await NugetHelper.DownloadPackageAsync(packageIdLower, version, packageFilePath))
                {
                    return null;
                }
            }

            return ExtractPackage(packageFilePath, extractedDirectory) ? extractedDirectory : null;
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

                var groups = dependenciesElement.Elements(ns + "group").ToList();
                var dependencyElements = groups.Count > 0
                    ? groups.SelectMany(group => group.Elements(ns + "dependency"))
                    : dependenciesElement.Elements(ns + "dependency");

                foreach (var dependency in dependencyElements)
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

            var candidateDirectories = GetCandidateAssemblyDirectories(extractedDirectory);
            if (candidateDirectories.Count > 0)
            {
                var selected = SelectBestCandidateDirectory(candidateDirectories);
                files.AddRange(Directory.GetFiles(selected, "*.dll", SearchOption.TopDirectoryOnly));
            }

            var runtimeManagedDirectories = GetRuntimeManagedAssetDirectories(extractedDirectory);
            if (runtimeManagedDirectories.Count > 0)
            {
                var selected = SelectBestRuntimeAssetDirectory(runtimeManagedDirectories);
                files.AddRange(Directory.GetFiles(selected, "*.dll", SearchOption.TopDirectoryOnly));
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
