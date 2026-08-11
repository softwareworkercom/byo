namespace SoftwareWorker.BYO.CLI.Core.Constants
{
    /// <summary>
    /// Contains constant keys for system configuration entries.
    /// </summary>
    public static class SystemConstants
    {
        public static readonly string USER_PROFILE_FOLDER = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "byo");

        public static readonly string AGENTS_DIRECTORY = Path.Combine(USER_PROFILE_FOLDER, "agents", ".agents");
        public static readonly string AGENTS_CONTEXT_DIRECTORY = Path.Combine(AGENTS_DIRECTORY, "context");
        public static readonly string STORAGE_SECRETS_FILE = Path.Combine(USER_PROFILE_FOLDER, "secrets.json");
        public static readonly string STORAGE_SETTINGS_FILE = Path.Combine(USER_PROFILE_FOLDER, "settings.json");
        public static readonly string STORAGE_COMMANDS_FILE = Path.Combine(USER_PROFILE_FOLDER, "commands.json");
        public static readonly string STORAGE_RUNBOOKS_FILE = Path.Combine(USER_PROFILE_FOLDER, "runbooks.json");
        public static readonly string STORAGE_WORKFLOWS_FILE = Path.Combine(USER_PROFILE_FOLDER, "workflows.json");
        public static readonly string STORAGE_COMPONENTS_FILE = Path.Combine(USER_PROFILE_FOLDER, "components.json");
        public static readonly string STORAGE_BOOKMARKS_FILE = Path.Combine(USER_PROFILE_FOLDER, "bookmarks.json");
        public static readonly string STORAGE_LOCAL_DB_FILE = Path.Combine(USER_PROFILE_FOLDER, "softwareworker_local.db");
        public static string EXTENSIONS_DIRECTORY { get; set; } = Path.Combine(USER_PROFILE_FOLDER, "extensions");
        public static string EXTENSIONS_PACKAGES_DIRECTORY { get; set; } = Path.Combine(EXTENSIONS_DIRECTORY, "packages");
        public static string EXTENSIONS_BINARIES_DIRECTORY { get; set; } = Path.Combine(EXTENSIONS_DIRECTORY, "bin");


        public const string SYSTEM_SecretKey = "System:SecretKey";

        public const string SYSTEM_RSAKeyPair = "System:RSAKeyPair";
        public const string SYSTEM_IsLoggingEnabled = "System:IsLoggingEnabled";


        public const string SYSTEM_DATABASE_LOCAL = "local.db";
        public const string SYSTEM_DATABASE_REMOTE_URL = "System:Database:Turso:Url";
        public const string SYSTEM_DATABASE_REMOTE_AUTHTOKEN = "System:Database:Turso:AuthToken";

        public const string ExportedSecurityKey = "security.key";
        public const string ExportedConfigurationFile = "configuration.json";
    }
}
