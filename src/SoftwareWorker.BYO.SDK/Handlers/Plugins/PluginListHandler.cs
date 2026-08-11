using SoftwareWorker.BYO.CLI.Abstractions.Attributes;
using SoftwareWorker.BYO.CLI.Core.Helpers;
using SoftwareWorker.BYO.CLI.Core.Service;
using SoftwareWorker.BYO.Integrations.NuGet;
using Spectre.Console;

namespace SoftwareWorker.BYO.CLI.Core.Handlers.Plugins
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
            var plugins = await GetPluginsAsync();

            if (plugins.Count == 0)
            {
                UserInterfaceService.ShowWarning(string.IsNullOrWhiteSpace(Source)
                    ? "No plugins found on NuGet.org for owner 'softwareworkercom'."
                    : "No plugins found on NuGet.org or in the provided local source.");
                return;
            }

            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Cyan)
                .AddColumn("[bold]Package[/]")
                .AddColumn("[bold]Latest[/]")
                .AddColumn("[bold]Source[/]")
                .AddColumn("[bold]Description[/]");

            foreach (var plugin in plugins)
            {
                table.AddRow(
                    Markup.Escape(plugin.Id),
                    Markup.Escape(plugin.Version),
                    Markup.Escape(plugin.Source),
                    string.IsNullOrWhiteSpace(plugin.Description) ? "[grey]-[/]" : Markup.Escape(plugin.Description));
            }

            UserInterfaceService.ShowTable(table);
            UserInterfaceService.ShowGrey($"Total plugins: {plugins.Count}");
        }

        private async Task<List<ExtensionPackage>> GetPluginsAsync()
        {
            var plugins = new List<ExtensionPackage>();

            plugins.AddRange(await GetNuGetPluginsAsync());

            if (!string.IsNullOrWhiteSpace(Source))
            {
                plugins.AddRange(GetLocalPlugins(Source.Trim()));
            }

            return plugins
                .OrderBy(plugin => plugin.Id, StringComparer.OrdinalIgnoreCase)
                .ThenBy(plugin => plugin.Source, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static async Task<List<ExtensionPackage>> GetNuGetPluginsAsync()
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

        private static List<ExtensionPackage> GetLocalPlugins(string sourceDirectory)
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