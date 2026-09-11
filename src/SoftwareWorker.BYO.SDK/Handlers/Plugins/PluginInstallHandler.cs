using SoftwareWorker.BYO.CLI.Abstractions.Attributes;
using SoftwareWorker.BYO.CLI.Core.Service;

namespace SoftwareWorker.BYO.CLI.Core.Handlers.Plugins
{
    [TrunkCommand("plugins", "Custom plugin management")]
    [BranchCommand("install", "Install BYO CLI Plugin from a local feed or NuGet.org")]
    [Parameter("package", "NuGet package id to install", true, null)]
    [Parameter("version", "NuGet package version (latest stable when omitted)", false, null)]
    [Parameter("source", "Local folder (NuGet feed) to install from before falling back to NuGet.org", false, null)]
    internal sealed class PluginInstallHandler : BaseCommandHandler
    {
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
            UserInterfaceService.ShowGrey($"Installing {packageId}...");

            var result = await InstallationService.InstallPluginAsync(packageId, Version, Source);

            foreach (var warning in result.Warnings)
            {
                UserInterfaceService.ShowWarning(warning);
            }

            if (!result.Success)
            {
                UserInterfaceService.ShowError(result.ErrorMessage!);
                return;
            }

            UserInterfaceService.ShowGreen($"Installed plugin '{result.PackageId}' version '{result.Version}'.");
            UserInterfaceService.ShowGrey($"Detected handlers: {string.Join(", ", result.Handlers)}");
            UserInterfaceService.ShowGrey("Run 'byo --help' to see newly available plugin commands.");
        }
    }
}
