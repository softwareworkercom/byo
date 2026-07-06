using SoftwareWorker.BYO.CLI.Abstractions.Attributes;
using SoftwareWorker.BYO.CLI.Core.Constants;
using SoftwareWorker.BYO.CLI.Core.Service;

namespace SoftwareWorker.BYO.CLI.Core.Handlers.Extensions
{
    [TrunkCommand("plugins", "Custom plugin management")]
    [BranchCommand("uninstall", "Uninstall an extension package")]
    [Parameter("package", "NuGet package id to uninstall", true, null)]
    [Parameter("version", "NuGet package version to uninstall (all versions when omitted)", false, null)]
    internal sealed class ExtensionUninstallHandler : BaseCommandHandler
    {
        public string? Package { get; set; }
        public string? Version { get; set; }

        public override Task ExecuteAsync()
        {
            if (string.IsNullOrWhiteSpace(Package))
            {
                UserInterfaceService.ShowError("Package id is required.");
                return Task.CompletedTask;
            }

            var packageId = Package.Trim();
            var packageIdLower = packageId.ToLowerInvariant();
            var version = string.IsNullOrWhiteSpace(Version) ? null : Version.Trim();

            var packagePath = Path.Combine(SystemConstants.EXTENSIONS_PACKAGES_DIRECTORY, packageIdLower);
            var binariesPath = Path.Combine(SystemConstants.EXTENSIONS_BINARIES_DIRECTORY, packageIdLower);

            var removedAny = false;

            if (version == null)
            {
                removedAny |= DeleteDirectoryIfExists(packagePath);
                removedAny |= DeleteDirectoryIfExists(binariesPath);

                if (!removedAny)
                {
                    UserInterfaceService.ShowWarning($"No installed extension files were found for '{packageId}'.");
                    return Task.CompletedTask;
                }

                UserInterfaceService.ShowGreen($"Uninstalled extension '{packageId}' (all versions).");
                return Task.CompletedTask;
            }

            var packageVersionPath = Path.Combine(packagePath, version);
            var binariesVersionPath = Path.Combine(binariesPath, version);

            removedAny |= DeleteDirectoryIfExists(packageVersionPath);
            removedAny |= DeleteDirectoryIfExists(binariesVersionPath);

            CleanupIfEmpty(packagePath);
            CleanupIfEmpty(binariesPath);

            if (!removedAny)
            {
                UserInterfaceService.ShowWarning($"Extension '{packageId}' version '{version}' was not found.");
                return Task.CompletedTask;
            }

            UserInterfaceService.ShowGreen($"Uninstalled extension '{packageId}' version '{version}'.");
            return Task.CompletedTask;
        }

        private static bool DeleteDirectoryIfExists(string path)
        {
            if (!Directory.Exists(path))
            {
                return false;
            }

            Directory.Delete(path, recursive: true);
            return true;
        }

        private static void CleanupIfEmpty(string path)
        {
            if (!Directory.Exists(path))
            {
                return;
            }

            if (Directory.EnumerateFileSystemEntries(path).Any())
            {
                return;
            }

            Directory.Delete(path, recursive: false);
        }
    }
}