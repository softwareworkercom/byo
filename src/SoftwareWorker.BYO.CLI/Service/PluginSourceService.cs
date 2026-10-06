using SoftwareWorker.BYO.CLI.Integrations.NuGet;
using SoftwareWorker.BYO.SDK.Constants;
using SoftwareWorker.BYO.SDK.Services;
using System.Net;

namespace SoftwareWorker.BYO.CLI.Service
{
    /// <summary>
    /// Builds the ordered list of package sources that plugins are looked up in: the sources configured
    /// in the <see cref="SystemConstants.SYSTEM_PluginSources"/> setting, in order, then NuGet.org.
    /// Each source is either a folder of <c>.nupkg</c> files or a NuGet V3 feed identified by its
    /// service index URL. Credentials for a named source are read from secrets, see
    /// <see cref="SystemConstants.SYSTEM_PluginSourceCredentialsPrefix"/>.
    /// </summary>
    internal static class PluginSourceService
    {
        public const string NuGetOrgName = "nuget.org";

        public const string NuGetOrgServiceIndex = "https://api.nuget.org/v3/index.json";

        /// <summary>
        /// Returns the sources to search, in order: the configured sources as listed in settings, then
        /// NuGet.org unless it is already listed or <see cref="SystemConstants.SYSTEM_PluginSourcesUseNuGetOrg"/>
        /// is <c>false</c>.
        /// </summary>
        public static IReadOnlyList<IPluginPackageSource> GetSources()
        {
            var sources = new List<IPluginPackageSource>();

            foreach (var source in GetConfiguredSources())
            {
                if (!sources.Any(existing => IsSameLocation(existing, source.Location)))
                {
                    sources.Add(source);
                }
            }

            if (SettingsService.GetBoolean(SystemConstants.SYSTEM_PluginSourcesUseNuGetOrg, defaultValue: true) &&
                !sources.Any(source => IsSameLocation(source, NuGetOrgServiceIndex)))
            {
                sources.Add(new NuGetV3PackageSource(NuGetOrgName, NuGetOrgServiceIndex));
            }

            return sources;
        }

        /// <summary>
        /// Reads the sources configured in settings, attaching the credentials stored in secrets for named ones.
        /// </summary>
        internal static List<IPluginPackageSource> GetConfiguredSources()
        {
            var definitions = SettingsService.GetArray(SystemConstants.SYSTEM_PluginSources, showErrorIfNotFound: false) ?? [];
            var secrets = new Dictionary<string, string>(
                SecretsService.GetList(SystemConstants.SYSTEM_PluginSourceCredentialsPrefix) ?? new Dictionary<string, string>(),
                StringComparer.OrdinalIgnoreCase);

            var sources = new List<IPluginPackageSource>();

            foreach (var definition in definitions)
            {
                if (TryParseDefinition(definition, out var name, out var location))
                {
                    sources.Add(CreateSource(location, name, GetCredential(secrets, name)));
                }
            }

            return sources;
        }

        /// <summary>
        /// Creates a source from a NuGet V3 service index URL or a folder path.
        /// </summary>
        internal static IPluginPackageSource CreateSource(string location, string? name = null, NetworkCredential? credential = null)
        {
            if (IsUrl(location))
            {
                return new NuGetV3PackageSource(string.IsNullOrWhiteSpace(name) ? location : name, location, credential);
            }

            return new LocalFolderPackageSource(name, location);
        }

        /// <summary>
        /// Splits a "name=location" definition. The name must be a plain identifier (letters, digits,
        /// '-', '_' or '.'), so URLs with query strings and paths are never mistaken for a name.
        /// </summary>
        internal static bool TryParseDefinition(string definition, out string? name, out string location)
        {
            name = null;
            location = definition?.Trim() ?? string.Empty;

            if (location.Length == 0)
            {
                return false;
            }

            var separator = location.IndexOf('=', StringComparison.Ordinal);
            if (separator > 0)
            {
                var candidate = location[..separator].Trim();
                if (candidate.Length > 0 && candidate.All(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '.'))
                {
                    name = candidate;
                    location = location[(separator + 1)..].Trim();
                }
            }

            return location.Length > 0;
        }

        private static NetworkCredential? GetCredential(IReadOnlyDictionary<string, string> secrets, string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            secrets.TryGetValue($"{SystemConstants.SYSTEM_PluginSourceCredentialsPrefix}{name}:Username", out var username);
            secrets.TryGetValue($"{SystemConstants.SYSTEM_PluginSourceCredentialsPrefix}{name}:Password", out var password);

            if (string.IsNullOrEmpty(username) && string.IsNullOrEmpty(password))
            {
                return null;
            }

            return new NetworkCredential(username ?? string.Empty, password ?? string.Empty);
        }

        private static bool IsUrl(string location)
        {
            return Uri.TryCreate(location, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        private static bool IsSameLocation(IPluginPackageSource source, string location)
        {
            return Normalize(source.Location).Equals(Normalize(location), StringComparison.OrdinalIgnoreCase);
        }

        private static string Normalize(string location)
        {
            return location.Trim().TrimEnd('/', '\\');
        }
    }
}
