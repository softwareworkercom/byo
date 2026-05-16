using SoftwareWorker.BYO.CLI.Abstractions.Attributes;
using SoftwareWorker.BYO.CLI.Core.Service;
using SoftwareWorker.BYO.Integrations.NuGet;
using Spectre.Console;

namespace SoftwareWorker.BYO.CLI.Core.Handlers.Extensions
{
    [TrunkCommand("extensions", "Custom extension management")]
    [BranchCommand("list", "List available SoftwareWorker extensions")]
    internal sealed class ExtensionListHandler : BaseCommandHandler
    {
        private const string PackageIdPrefix = "SoftwareWorker.BYO.Extensions.";
        private const string Owner = "softwareworkercom";

        public override async Task ExecuteAsync()
        {
            var extensions = await GetExtensionsAsync();

            if (extensions.Count == 0)
            {
                UserInterfaceService.ShowWarning("No extensions found on NuGet.org for owner 'softwareworkercom'.");
                return;
            }

            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Cyan)
                .AddColumn("[bold]Package[/]")
                .AddColumn("[bold]Latest[/]")
                .AddColumn("[bold]Description[/]");

            foreach (var extension in extensions)
            {
                table.AddRow(
                    Markup.Escape(extension.Id),
                    Markup.Escape(extension.Version),
                    string.IsNullOrWhiteSpace(extension.Description) ? "[grey]-[/]" : Markup.Escape(extension.Description));
            }

            UserInterfaceService.ShowTable(table);
            UserInterfaceService.ShowGrey($"Total extensions: {extensions.Count}");
        }

        private static async Task<List<ExtensionPackage>> GetExtensionsAsync()
        {
            try
            {
                var connector = new NuGetConnector(isVerbose: false);
                var packages = await connector.ListPackagesAsync(PackageIdPrefix);

                return packages
                    .Where(package =>
                        !string.IsNullOrWhiteSpace(package.Id) &&
                        !string.IsNullOrWhiteSpace(package.Version) &&
                        package.Id.StartsWith(PackageIdPrefix, StringComparison.OrdinalIgnoreCase) &&
                        IsOwnedBySoftwareWorker(package.Owners.FirstOrDefault()))
                    .Select(package => new ExtensionPackage(package.Id, package.Version, package.Description ?? string.Empty))
                    .OrderBy(extension => extension.Id, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch
            {
                return [];
            }
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

        private sealed record ExtensionPackage(string Id, string Version, string Description);
    }
}