namespace SoftwareWorker.BYO.CLI.Core.Constants
{
    /// <summary>
    /// Contains constant keys for system configuration entries.
    /// </summary>
    public static class SystemConstants
    {
        public static readonly string USER_PROFILE = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        public static readonly string STORAGE_DIRECTORY = OperatingSystem.IsWindows()
            ? Path.Combine(Path.GetPathRoot(USER_PROFILE) ?? "C:\\", "cli")
            : Path.Combine(USER_PROFILE, ".config", "cli");

        public static readonly string AGENTS_DIRECTORY = Path.Combine(STORAGE_DIRECTORY, "agents", ".agents");
        public static readonly string AGENTS_CONTEXT_DIRECTORY = Path.Combine(AGENTS_DIRECTORY, "context");
        public static readonly string STORAGE_SECRETS_FILE = Path.Combine(STORAGE_DIRECTORY, "secrets.json");
        public static readonly string STORAGE_SETTINGS_FILE = Path.Combine(STORAGE_DIRECTORY, "settings.json");
        public static readonly string STORAGE_COMMANDS_FILE = Path.Combine(STORAGE_DIRECTORY, "commands.json");
        public static readonly string STORAGE_RUNBOOKS_FILE = Path.Combine(STORAGE_DIRECTORY, "runbooks.json");
        public static readonly string STORAGE_WORKFLOWS_FILE = Path.Combine(STORAGE_DIRECTORY, "workflows.json");
        public static readonly string STORAGE_COMPONENTS_FILE = Path.Combine(STORAGE_DIRECTORY, "components.json");
        public static readonly string STORAGE_BOOKMARKS_FILE = Path.Combine(STORAGE_DIRECTORY, "bookmarks.json");
        public static readonly string STORAGE_LOCAL_DB_FILE = Path.Combine(STORAGE_DIRECTORY, "softwareworker_local.db");
        public static readonly string STORAGE_RSA_KEY_FILE = Path.Combine(STORAGE_DIRECTORY, "rsa.key");


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
