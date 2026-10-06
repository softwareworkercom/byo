using SoftwareWorker.BYO.SDK.Abstractions.Attributes;
using SoftwareWorker.BYO.SDK;
using SoftwareWorker.BYO.SDK.Services;
using SoftwareWorker.BYO.CLI.Service;

namespace SoftwareWorker.BYO.CLI.Handlers.Plugins
{
    [TrunkCommand("plugins", "Custom plugin management")]
    [BranchCommand("install", "Install a BYO CLI plugin from the configured package sources")]
    [Parameter("package", "NuGet package id to install", true, null)]
    [Parameter("version", "NuGet package version (latest stable when omitted)", false, null)]
    internal sealed class PluginInstallHandler : BaseCommandHandler
    {
        public string? Package { get; set; }
        public string? Version { get; set; }

        public override async Task ExecuteAsync()
        {
            if (string.IsNullOrWhiteSpace(Package))
            {
                UserInterfaceService.ShowError("Package id is required.");
                return;
            }

            var packageId = Package.Trim();
            UserInterfaceService.ShowGrey($"Installing {packageId}...");

            var result = await PluginInstallationService.InstallPluginAsync(packageId, Version);

            foreach (var warning in result.Warnings)
            {
                UserInterfaceService.ShowWarning(warning);
            }

            if (!result.Success)
            {
                UserInterfaceService.ShowError(result.ErrorMessage!);
                return;
            }

            UserInterfaceService.ShowGreen($"Installed plugin '{result.PackageId}' version '{result.Version}' from {result.Source}.");
            UserInterfaceService.ShowGrey($"Detected handlers: {string.Join(", ", result.Handlers)}");
            UserInterfaceService.ShowGrey("Run 'byo --help' to see newly available plugin commands.");
        }
    }
}
