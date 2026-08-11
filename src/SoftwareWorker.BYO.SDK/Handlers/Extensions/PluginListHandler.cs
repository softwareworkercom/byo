using SoftwareWorker.BYO.CLI.Abstractions.Attributes;
using SoftwareWorker.BYO.CLI.Core.Helpers;
using SoftwareWorker.BYO.CLI.Core.Service;
using SoftwareWorker.BYO.Integrations.NuGet;
using Spectre.Console;

namespace SoftwareWorker.BYO.CLI.Core.Handlers.Extensions
{
    [TrunkCommand("plugins", "Custom plugin management")]
    [BranchCommand("list", "List available BYO CLI Plugins")]
    [Parameter("source", "Local folder (NuGet feed) to include alongside NuGet.org", false, null)]
    internal sealed class PluginListHandler : BaseCommandHandler
    {
        private const string PackageIdPrefix = "BYO.Plugin.";
        private const string Owner = "softwareworkercom";
        private const string NuGetSource = "NuGet.org";
        private const string LocalSource = "Local";

        public string? Source { get; set; }

        public override async Task ExecuteAsync()
        {
            var extensions = await GetExtensionsAsync();

            if (extensions.Count == 0)
            {
                UserInterfaceService.ShowWarning(string.IsNullOrWhiteSpace(Source)
                    ? "No extensions found on NuGet.org for owner 'softwareworkercom'."
                    : "No extensions found on NuGet.org or in the provided local source.");
                return;
            }

            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Cyan)
                .AddColumn("[bold]Package[/]")
                .AddColumn("[bold]Latest[/]")
                .AddColumn("[bold]Source[/]")
                .AddColumn("[bold]Description[/]");

            foreach (var extension in extensions)
            {
                table.AddRow(
                    Markup.Escape(extension.Id),
                    Markup.Escape(extension.Version),
                    Markup.Escape(extension.Source),
                    string.IsNullOrWhiteSpace(extension.Description) ? "[grey]-[/]" : Markup.Escape(extension.Description));
            }

            UserInterfaceService.ShowTable(table);
            UserInterfaceService.ShowGrey($"Total extensions: {extensions.Count}");
        }

        private async Task<List<ExtensionPackage>> GetExtensionsAsync()
        {
            var extensions = new List<ExtensionPackage>();

            extensions.AddRange(await GetNuGetExtensionsAsync());

            if (!string.IsNullOrWhiteSpace(Source))
            {
                extensions.AddRange(GetLocalExtensions(Source.Trim()));
            }

            return extensions
                .OrderBy(extension => extension.Id, StringComparer.OrdinalIgnoreCase)
                .ThenBy(extension => extension.Source, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static async Task<List<ExtensionPackage>> GetNuGetExtensionsAsync()
        {
            try
            {
                var connector = new NuGetConnector(isVerbose: false);
                var packages = await connector.ListPackagesAsync(PackageIdPrefix);

                if (packages == null)
                {
                    return [];
                }

                return packages
                    .Where(package =>
                        !string.IsNullOrWhiteSpace(package.Id) &&
                        !string.IsNullOrWhiteSpace(package.Version) &&
                        package.Id.StartsWith(PackageIdPrefix, StringComparison.OrdinalIgnoreCase) &&
                        IsOwnedBySoftwareWorker(package.Owners.FirstOrDefault()))
                    .Select(package => new ExtensionPackage(package.Id, package.Version, package.Description ?? string.Empty, NuGetSource))
                    .ToList();
            }
            catch
            {
                return [];
            }
        }

        private static List<ExtensionPackage> GetLocalExtensions(string sourceDirectory)
        {
            if (!Directory.Exists(sourceDirectory))
            {
                UserInterfaceService.ShowWarning($"Local source '{sourceDirectory}' does not exist.");
                return [];
            }

            return LocalPackageHelper.GetLatestPackages(sourceDirectory)
                .Select(package => new ExtensionPackage(package.Id, package.Version, package.Description, LocalSource))
                .ToList();
        }

        private static bool IsOwnedBySoftwareWorker(string owners)
        {
            if (string.IsNullOrWhiteSpace(owners))
            {
                return false;
            }

            var ownerCandidates = owners
                .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            return ownerCandidates.Any(owner => owner.Equals(Owner, StringComparison.OrdinalIgnoreCase));
        }

        private sealed record ExtensionPackage(string Id, string Version, string Description, string Source);
    }
}