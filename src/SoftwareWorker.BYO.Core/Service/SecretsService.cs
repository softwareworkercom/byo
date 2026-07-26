using SoftwareWorker.BYO.CLI.Core.Constants;
using SoftwareWorker.BYO.Core.Storage;

namespace SoftwareWorker.BYO.CLI.Core.Service
{
    public static class SecretsService
    {
        public static string SecretsFilePath { get; set; } = SystemConstants.STORAGE_SECRETS_FILE;
        public static string SettingsFilePath { get; set; } = SystemConstants.STORAGE_SETTINGS_FILE;

        public static string? Get(string key)
        {
            var secrets = GetList(key);

            if (secrets is null || secrets.Count == 0)
            {
                UserInterfaceService.ShowError($"Error: Key '{key}' was not found in Secrets.");
                return null;
            }

            var secret = secrets.FirstOrDefault(c => c.Key.Equals(key));

            if (secret.Key is null)
            {
                UserInterfaceService.ShowError($"Error: Key '{key}' was not found in Secrets.");
                return null;
            }

            // GetList already returns decrypted values
            return secret.Value;
        }

        public static Dictionary<string, string>? GetList(string startsWithKey = "")
        {
            var secrets = StorageService.LoadDictionary(SecretsFilePath);
            var result = new Dictionary<string, string>();

            // Add matching secrets (decrypted)
            foreach (var secret in secrets.Where(v => v.Key.StartsWith(startsWithKey)))
            {
                result[secret.Key] = EncryptionService.DecryptVaultEntry(secret.Value);
            }

            return result.Count > 0 ? result : null;
        }

        public static string? Update(string key, string value)
        {
            var settings = SettingsService.GetList() ?? new Dictionary<string, string>();

            // Validate mutual exclusivity between Settings and Secrets
            if (settings.ContainsKey(key))
            {
                UserInterfaceService.ShowError($"Error: Cannot add key '{key}' to Secrets because it already exists in Settings. Delete the existing entry first.");
                return null;
            }

            var encryptedValue = EncryptionService.EncryptVaultEntry(value);
            var secrets = StorageService.LoadDictionary(SecretsFilePath);
            secrets[key] = encryptedValue;
            StorageService.SaveDictionary(SecretsFilePath, secrets);

            return encryptedValue;
        }

        public static void Delete(string key)
        {
            var secrets = StorageService.LoadDictionary(SecretsFilePath);

            if (secrets.ContainsKey(key))
            {
                secrets.Remove(key);
                StorageService.SaveDictionary(SecretsFilePath, secrets);
            }
            else
            {
                UserInterfaceService.ShowError($"Error: Key '{key}' was not found in Secrets.");
            }
        }
    }
}
