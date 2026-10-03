using System.Reflection;
using SoftwareWorker.BYO.SDK;

namespace SoftwareWorker.BYO.CLI.Helpers
{
    public static class PluginCompatibilityHelper
    {
        public static bool IsCompatibilityException(Exception ex)
        {
            return ex is MissingMethodException || ex is TypeLoadException || ex is MissingFieldException;
        }

        /// <summary>
        /// Builds the error shown when a plugin fails with a binding error, including the BYO.SDK
        /// version the CLI is running and the BYO.SDK version the plugin was compiled against.
        /// </summary>
        /// <param name="ex">The MissingMethod/TypeLoad/MissingField exception.</param>
        /// <param name="pluginAssembly">The plugin assembly, when known. Otherwise it is inferred from the exception.</param>
        public static string BuildErrorMessage(Exception ex, Assembly? pluginAssembly = null)
        {
            var sdkAssembly = typeof(BaseCommandHandler).Assembly;
            pluginAssembly ??= InferPluginAssembly(ex, sdkAssembly);

            var details = ex.Message?.Trim();
            if (string.IsNullOrWhiteSpace(details) && ex is TypeLoadException typeLoadException)
            {
                details = typeLoadException.TypeName;
            }

            var lines = new List<string>
            {
                $"Error: {ex.GetType().Name}: {details}",
                $"CLI BYO.SDK version: {GetInformationalVersion(sdkAssembly)}"
            };

            if (pluginAssembly != null && pluginAssembly != sdkAssembly)
            {
                var pluginName = pluginAssembly.GetName();
                var referencedSdk = pluginAssembly.GetReferencedAssemblies()
                    .FirstOrDefault(a => string.Equals(a.Name, sdkAssembly.GetName().Name, StringComparison.OrdinalIgnoreCase));

                lines.Add($"Plugin: {pluginName.Name} {GetInformationalVersion(pluginAssembly)}");
                lines.Add($"Plugin built against BYO.SDK version: {FormatVersion(referencedSdk?.Version)}");
            }

            lines.Add(
                "This usually means an installed plugin was built against a different version of BYO.SDK than the one currently installed. " +
                "Try updating or reinstalling the plugin (byo plugin install <plugin>) to a version compatible with the current CLI.");

            return string.Join("\n", lines);
        }

        private static Assembly? InferPluginAssembly(Exception ex, Assembly sdkAssembly)
        {
            // The method that failed to bind lives in the plugin, so its declaring type points at the plugin assembly.
            var assembly = ex.TargetSite?.DeclaringType?.Assembly;
            return assembly == sdkAssembly ? null : assembly;
        }

        private static string GetInformationalVersion(Assembly assembly)
        {
            var informationalVersion = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;

            if (string.IsNullOrWhiteSpace(informationalVersion))
            {
                return FormatVersion(assembly.GetName().Version);
            }

            // Drop the git commit metadata (e.g. "0.35.1+abc123") to keep the message readable.
            var metadataIndex = informationalVersion.IndexOf('+');
            return metadataIndex > 0 ? informationalVersion[..metadataIndex] : informationalVersion;
        }

        private static string FormatVersion(Version? version)
        {
            if (version == null)
            {
                return "unknown";
            }

            return version.Build >= 0 ? version.ToString(3) : version.ToString();
        }
    }
}
