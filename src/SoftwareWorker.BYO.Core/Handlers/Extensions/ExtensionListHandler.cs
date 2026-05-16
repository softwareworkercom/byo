using SoftwareWorker.BYO.CLI.Abstractions.Attributes;
using SoftwareWorker.BYO.CLI.Core.Service;
using Spectre.Console;
using System.Text.Json;

namespace SoftwareWorker.BYO.CLI.Core.Handlers.Extensions
{
    [TrunkCommand("extensions", "Custom extension management")]
    [BranchCommand("list", "List available SoftwareWorker extensions")]
    internal sealed class ExtensionListHandler : BaseCommandHandler
    {
        private const string PackageIdPrefix = "SoftwareWorker.BYO.Extensions.";
        private const string Owner = "softwareworkercom";
        private const string NuGetServiceIndexUrl = "https://api.nuget.org/v3/index.json";
        private const string SearchQueryServiceType = "SearchQueryService";

        private static readonly HttpClient HttpClient = new()
        {
            Timeout = TimeSpan.FromMinutes(1)
        };

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
                var searchServiceUrl = await ResolveSearchServiceUrlAsync();
                if (string.IsNullOrWhiteSpace(searchServiceUrl))
                {
                    return [];
                }

                var queryUrl = $"{searchServiceUrl}?q={Uri.EscapeDataString(PackageIdPrefix)}&prerelease=false&take=100&semVerLevel=2.0.0";
                using var stream = await HttpClient.GetStreamAsync(queryUrl);
                using var document = await JsonDocument.ParseAsync(stream);

                if (!document.RootElement.TryGetProperty("data", out var dataElement) ||
                    dataElement.ValueKind != JsonValueKind.Array)
                {
                    return [];
                }

                var extensions = new List<ExtensionPackage>();

                foreach (var package in dataElement.EnumerateArray())
                {
                    var id = package.TryGetProperty("id", out var idElement) ? idElement.GetString() : null;
                    var version = package.TryGetProperty("version", out var versionElement) ? versionElement.GetString() : null;
                    var description = package.TryGetProperty("description", out var descriptionElement) ? descriptionElement.GetString() : string.Empty;
                    var owners = package.TryGetProperty("owners", out var ownersElement) ? ownersElement.GetString() : null;

                    if (string.IsNullOrWhiteSpace(id) ||
                        string.IsNullOrWhiteSpace(version) ||
                        !id.StartsWith(PackageIdPrefix, StringComparison.OrdinalIgnoreCase) ||
                        !IsOwnedBySoftwareWorker(owners))
                    {
                        continue;
                    }

                    extensions.Add(new ExtensionPackage(id, version, description ?? string.Empty));
                }

                return extensions
                    .OrderBy(extension => extension.Id, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch
            {
                return [];
            }
        }

        private static async Task<string?> ResolveSearchServiceUrlAsync()
        {
            using var stream = await HttpClient.GetStreamAsync(NuGetServiceIndexUrl);
            using var document = await JsonDocument.ParseAsync(stream);

            if (!document.RootElement.TryGetProperty("resources", out var resourcesElement) ||
                resourcesElement.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            foreach (var resource in resourcesElement.EnumerateArray())
            {
                if (!resource.TryGetProperty("@type", out var typeElement) ||
                    !resource.TryGetProperty("@id", out var idElement))
                {
                    continue;
                }

                var type = typeElement.GetString();
                var id = idElement.GetString();

                if (string.IsNullOrWhiteSpace(type) ||
                    string.IsNullOrWhiteSpace(id) ||
                    !type.StartsWith(SearchQueryServiceType, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return id.TrimEnd('/').TrimEnd('?');
            }

            return null;
        }

        private static bool IsOwnedBySoftwareWorker(string? owners)
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