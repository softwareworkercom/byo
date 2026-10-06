using SoftwareWorker.BYO.SDK.Abstractions.Attributes;
using SoftwareWorker.BYO.SDK;
using SoftwareWorker.BYO.SDK.Constants;
using SoftwareWorker.BYO.SDK.Services;
using SoftwareWorker.BYO.CLI.Integrations.NuGet;
using SoftwareWorker.BYO.CLI.Service;
using Spectre.Console;

namespace SoftwareWorker.BYO.CLI.Handlers.Plugins
{
    [TrunkCommand("plugins", "Custom plugin management")]
    [BranchCommand("list", "List the BYO CLI plugins available in the configured package sources")]
    internal sealed class PluginListHandler : BaseCommandHandler
    {
        private const string PackageIdPrefix = "BYO.Plugin.";
        private const string NuGetOrgOwner = "softwareworkercom";

        public override async Task ExecuteAsync()
        {
            var sources = PluginSourceService.GetSources();

            if (sources.Count == 0)
            {
                UserInterfaceService.ShowWarning($"No package sources are configured. Add one to the '{SystemConstants.SYSTEM_PluginSources}' setting or set '{SystemConstants.SYSTEM_PluginSourcesUseNuGetOrg}' to true.");
                return;
            }

            var plugins = await GetPluginsAsync(sources);

            if (plugins.Count == 0)
            {
                UserInterfaceService.ShowWarning($"No plugins found in {string.Join(", ", sources.Select(source => source.Name))}.");
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

        private static async Task<List<ExtensionPackage>> GetPluginsAsync(IReadOnlyList<IPluginPackageSource> sources)
        {
            var plugins = new List<ExtensionPackage>();

            foreach (var source in sources)
            {
                var result = await source.SearchAsync(PackageIdPrefix);

                if (!result.Succeeded)
                {
                    UserInterfaceService.ShowWarning($"Source '{source.Name}' could not be searched: {result.Error}");
                    continue;
                }

                plugins.AddRange(result.Packages
                    .Where(package => !IsNuGetOrg(source) || IsOwnedBySoftwareWorker(package.Owners))
                    .Select(package => new ExtensionPackage(package.Id, package.Version, package.Description, source.Name)));
            }

            return plugins
                .OrderBy(plugin => plugin.Id, StringComparer.OrdinalIgnoreCase)
                .ThenBy(plugin => plugin.Source, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Anyone can publish a BYO.Plugin.* package on NuGet.org, so only packages owned by SoftwareWorker
        /// are listed from there. Other sources were configured by the user and are listed in full.
        /// </summary>
        private static bool IsNuGetOrg(IPluginPackageSource source)
        {
            return source.Location.TrimEnd('/').Equals(PluginSourceService.NuGetOrgServiceIndex, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsOwnedBySoftwareWorker(IReadOnlyList<string> owners)
        {
            return owners.Any(owner => owner.Equals(NuGetOrgOwner, StringComparison.OrdinalIgnoreCase));
        }

        private sealed record ExtensionPackage(string Id, string Version, string Description, string Source);
    }
}
