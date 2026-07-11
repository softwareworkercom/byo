using SoftwareWorker.BYO.CLI.Abstractions.Attributes;
using SoftwareWorker.BYO.CLI.Core.Constants;
using SoftwareWorker.BYO.CLI.Core.Helpers;
using SoftwareWorker.BYO.CLI.Core.Service;
using System.IO.Compression;
using System.Reflection;
using System.Text.Json;

namespace SoftwareWorker.BYO.CLI.Core.Handlers.Extensions
{
    [TrunkCommand("plugins", "Custom plugin management")]
    [BranchCommand("install", "Install BYO CLI Plugin from a local feed or NuGet.org")]
    [Parameter("package", "NuGet package id to install", true, null)]
    [Parameter("version", "NuGet package version (latest stable when omitted)", false, null)]
    [Parameter("source", "Local folder (NuGet feed) to install from before falling back to NuGet.org", false, null)]
    internal sealed class PluginInstallHandler : BaseCommandHandler
    {
        private static readonly HttpClient HttpClient = new()
        {
            Timeout = TimeSpan.FromMinutes(2)
        };

        public string? Package { get; set; }
        public string? Version { get; set; }
        public string? Source { get; set; }

        public override async Task ExecuteAsync()
        {
            if (string.IsNullOrWhiteSpace(Package))
            {
                UserInterfaceService.ShowError("Package id is required.");
                return;
            }

            var packageId = Package.Trim();
            var packageIdLower = packageId.ToLowerInvariant();

            var localPackage = TryResolveLocalPackage(packageId);

            var version = localPackage?.Version
                ?? (string.IsNullOrWhiteSpace(Version)
                    ? await ResolveLatestVersionAsync(packageIdLower)
                    : Version.Trim());

            if (string.IsNullOrWhiteSpace(version))
            {
                UserInterfaceService.ShowError($"Unable to resolve a version for package '{packageId}'.");
                return;
            }

            var packageVersion = version.Trim();
            var packageRootDirectory = Path.Combine(SystemConstants.EXTENSIONS_PACKAGES_DIRECTORY, packageIdLower, packageVersion);
            var packageFilePath = Path.Combine(packageRootDirectory, $"{packageIdLower}.{packageVersion}.nupkg");
            var extractedDirectory = Path.Combine(packageRootDirectory, "extracted");

            Directory.CreateDirectory(packageRootDirectory);

            if (localPackage != null)
            {
                UserInterfaceService.ShowGrey($"Installing {packageId} {packageVersion} from local source '{localPackage.FilePath}'...");

                if (!CopyLocalPackage(localPackage.FilePath, packageFilePath))
                {
                    UserInterfaceService.ShowError($"Failed to copy local package '{localPackage.FilePath}'.");
                    return;
                }
            }
            else
            {
                var packageUrl = $"https://api.nuget.org/v3-flatcontainer/{packageIdLower}/{packageVersion}/{packageIdLower}.{packageVersion}.nupkg";
                UserInterfaceService.ShowGrey($"Downloading {packageId} {packageVersion} from NuGet.org...");

                if (!await DownloadPackageAsync(packageUrl, packageFilePath))
                {
                    UserInterfaceService.ShowError($"Failed to download package '{packageId}' version '{packageVersion}'.");
                    return;
                }
            }

            if (!ExtractPackage(packageFilePath, extractedDirectory))
            {
                UserInterfaceService.ShowError($"Failed to extract package '{packageId}' version '{packageVersion}'.");
                return;
            }

            var candidateDirectories = GetCandidateAssemblyDirectories(extractedDirectory);
            if (candidateDirectories.Count == 0)
            {
                UserInterfaceService.ShowError($"Package '{packageId}' does not contain .NET assemblies under lib/ or tools/.");
                return;
            }

            var selectedDirectory = SelectBestCandidateDirectory(candidateDirectories);
            var selectedAssemblies = Directory.GetFiles(selectedDirectory, "*.dll", SearchOption.TopDirectoryOnly).ToList();

            if (selectedAssemblies.Count == 0)
            {
                UserInterfaceService.ShowError($"No assemblies were found for package '{packageId}'.");
                return;
            }

            if (!TryFindValidHandlers(selectedAssemblies, out var handlers, out var scanErrors))
            {
                var details = scanErrors.Count == 0
                    ? string.Empty
                    : $" Details: {string.Join(" | ", scanErrors.Take(3))}";
                UserInterfaceService.ShowError($"Package '{packageId}' does not implement SoftwareWorker.BYO.Abstractions command handlers.{details}");
                return;
            }

            var installedVersionDirectory = Path.Combine(SystemConstants.EXTENSIONS_BINARIES_DIRECTORY, packageIdLower, packageVersion);
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

            UserInterfaceService.ShowGreen($"Installed extension '{packageId}' version '{packageVersion}'.");
            UserInterfaceService.ShowGrey($"Detected handlers: {string.Join(", ", handlers.OrderBy(h => h))}");
            UserInterfaceService.ShowGrey("Run 'byo --help' to see newly available extension commands.");
        }

        private static async Task<string?> ResolveLatestVersionAsync(string packageIdLower)
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

        private static async Task<bool> DownloadPackageAsync(string packageUrl, string targetPath)
        {
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

        private LocalPackageHelper.LocalPackageInfo? TryResolveLocalPackage(string packageId)
        {
            if (string.IsNullOrWhiteSpace(Source))
            {
                return null;
            }

            var sourceDirectory = Source.Trim();

            if (!Directory.Exists(sourceDirectory))
            {
                UserInterfaceService.ShowWarning($"Local source '{sourceDirectory}' does not exist. Falling back to NuGet.org.");
                return null;
            }

            var localPackage = LocalPackageHelper.ResolvePackage(sourceDirectory, packageId, Version);

            if (localPackage == null)
            {
                UserInterfaceService.ShowWarning($"Package '{packageId}' was not found in local source '{sourceDirectory}'. Falling back to NuGet.org.");
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