using SoftwareWorker.BYO.CLI.Core.Constants;
using SoftwareWorker.BYO.Core.Storage;

namespace SoftwareWorker.BYO.CLI.Core.Service
{
    public static class SettingsService
    {
        public static string? Get(string key, bool showErrorIfNotFound = true)
        {
            var settings = GetList(key);

            if (settings is null || settings.Count == 0)
            {
                if (showErrorIfNotFound)
                {
                    UserInterfaceService.ShowError($"Error: Key '{key}' was not found in Settings.");
                }
                return null;
            }

            var setting = settings.FirstOrDefault(c => c.Key.Equals(key));

            if (setting.Key is null)
            {
                if (showErrorIfNotFound)
                {
                    UserInterfaceService.ShowError($"Error: Key '{key}' was not found in Settings.");
                }
                return null;
            }

            return TokenService.ResolveTokens(setting.Value);
        }

        public static bool GetBoolean(string key, bool defaultValue = false)
        {
            var value = Get(key, showErrorIfNotFound: false);

            if (value == null)
            {
                return defaultValue;
            }

            if (bool.TryParse(value, out bool result))
            {
                return result;
            }

            return defaultValue;
        }

        public static Dictionary<string, string>? GetList(string startsWithKey = "")
        {
            var settings = StorageService.LoadDictionary(SystemConstants.STORAGE_SETTINGS_FILE);
            var result = new Dictionary<string, string>();

            // Add matching settings
            foreach (var setting in settings.Where(v => v.Key.StartsWith(startsWithKey)))
            {
                result[setting.Key] = setting.Value;
            }

            return result.Count > 0 ? result : null;
        }

        public static string? Update(string key, string value)
        {
            var secrets = StorageService.LoadDictionary(SystemConstants.STORAGE_SECRETS_FILE);

            // Validate mutual exclusivity between Settings and Secrets
            if (secrets.ContainsKey(key))
            {
                UserInterfaceService.ShowError($"Error: Cannot add key '{key}' to Settings because it already exists in Secrets. Delete the existing entry first.");
                return null;
            }

            var settings = StorageService.LoadDictionary(SystemConstants.STORAGE_SETTINGS_FILE);
            settings[key] = value;
            StorageService.SaveDictionary(SystemConstants.STORAGE_SETTINGS_FILE, settings);

            return value;
        }

        public static void Delete(string key)
        {
            var settings = StorageService.LoadDictionary(SystemConstants.STORAGE_SETTINGS_FILE);

            if (settings.ContainsKey(key))
            {
                settings.Remove(key);
                StorageService.SaveDictionary(SystemConstants.STORAGE_SETTINGS_FILE, settings);
            }
            else
            {
                UserInterfaceService.ShowError($"Error: Key '{key}' was not found in Settings.");
            }
        }
    }
}
