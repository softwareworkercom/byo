namespace SoftwareWorker.BYO.SDK.Constants
{
    /// <summary>
    /// Contains constant keys for system configuration entries.
    /// </summary>
    public static class SystemConstants
    {
        public static readonly string USER_PROFILE_FOLDER = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "byo");

        public static readonly string STORAGE_SECRETS_FILE = Path.Combine(USER_PROFILE_FOLDER, "secrets.json");
        public static readonly string STORAGE_SETTINGS_FILE = Path.Combine(USER_PROFILE_FOLDER, "settings.json");
        public static readonly string STORAGE_COMMANDS_FILE = Path.Combine(USER_PROFILE_FOLDER, "commands.json");
        public static readonly string STORAGE_WORKFLOWS_FILE = Path.Combine(USER_PROFILE_FOLDER, "workflows.json");
        public static readonly string STORAGE_SHELL_HISTORY_FILE = Path.Combine(USER_PROFILE_FOLDER, "shell_history.txt");
        public static string PLUGINS_DIRECTORY { get; set; } = Path.Combine(USER_PROFILE_FOLDER, "plugins");
        public static string PLUGINS_PACKAGES_DIRECTORY { get; set; } = Path.Combine(PLUGINS_DIRECTORY, "packages");
        public static string PLUGINS_BINARIES_DIRECTORY { get; set; } = Path.Combine(PLUGINS_DIRECTORY, "bin");

        public const string SYSTEM_Alias = "System:Alias";
        public const string SYSTEM_SkipAliasPrompt = "System:SkipAliasPrompt";

        /// <summary>
        /// Package sources plugins are installed from: a JSON array of locations (a NuGet V3 service index
        /// URL or a folder of .nupkg files), optionally named as "name=location". Searched in order before NuGet.org.
        /// </summary>
        public const string SYSTEM_PluginSources = "System:Plugins:Sources";

        /// <summary>
        /// Set to false to stop using NuGet.org as the last plugin package source.
        /// </summary>
        public const string SYSTEM_PluginSourcesUseNuGetOrg = "System:Plugins:UseNuGetOrg";

        /// <summary>
        /// Prefix of the secrets holding a named plugin source's credentials:
        /// "System:Plugins:Sources:&lt;name&gt;:Username" and "System:Plugins:Sources:&lt;name&gt;:Password".
        /// </summary>
        public const string SYSTEM_PluginSourceCredentialsPrefix = "System:Plugins:Sources:";
    }
}
