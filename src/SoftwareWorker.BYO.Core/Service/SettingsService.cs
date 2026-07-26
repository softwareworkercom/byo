using SoftwareWorker.BYO.CLI.Core.Constants;
using SoftwareWorker.BYO.CLI.Core.Helpers;
using SoftwareWorker.BYO.Core.Storage;
using System.Text.Json;

namespace SoftwareWorker.BYO.CLI.Core.Service
{
    public static class SettingsService
    {
        public static string SettingsFilePath { get; set; } = SystemConstants.STORAGE_SETTINGS_FILE;
        public static string SecretsFilePath { get; set; } = SystemConstants.STORAGE_SECRETS_FILE;

        public static string? Get(string key, bool showErrorIfNotFound = true)
        {
            var settings = LoadSettings();

            if (!settings.TryGetValue(key, out var setting))
            {
                if (showErrorIfNotFound)
                {
                    UserInterfaceService.ShowError($"Error: Key '{key}' was not found in Settings.");
                }
                return null;
            }

            if (setting.ValueKind == JsonValueKind.String)
            {
                return TokenService.ResolveTokens(setting.GetString() ?? string.Empty);
            }

            return ToSettingValue(setting);
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

        public static List<string>? GetArray(string key, bool showErrorIfNotFound = true)
        {
            var settings = LoadSettings();

            if (!settings.TryGetValue(key, out var setting))
            {
                if (showErrorIfNotFound)
                {
                    UserInterfaceService.ShowError($"Error: Key '{key}' was not found in Settings.");
                }

                return null;
            }

            if (setting.ValueKind == JsonValueKind.Array)
            {
                return setting
                    .EnumerateArray()
                    .Select(item => item.ValueKind == JsonValueKind.String ? item.GetString() ?? string.Empty : item.GetRawText())
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .ToList();
            }

            if (setting.ValueKind == JsonValueKind.String)
            {
                var value = TokenService.ResolveTokens(setting.GetString() ?? string.Empty);

                if (string.IsNullOrWhiteSpace(value))
                {
                    return [];
                }

                try
                {
                    return JsonSerializer.Deserialize<List<string>>(value) ?? [value];
                }
                catch (JsonException)
                {
                    if (TryParseLooseArray(value, out var looseArray))
                    {
                        return looseArray;
                    }

                    return [value];
                }
            }

            if (showErrorIfNotFound)
            {
                UserInterfaceService.ShowError($"Error: Key '{key}' is not a string array in Settings.");
            }

            return null;
        }

        public static Dictionary<string, string>? GetList(string startsWithKey = "")
        {
            var settings = LoadSettings();
            var result = new Dictionary<string, string>();

            // Add matching settings
            foreach (var setting in settings.Where(v => v.Key.StartsWith(startsWithKey)))
            {
                result[setting.Key] = ToSettingValue(setting.Value) ?? string.Empty;
            }

            return result.Count > 0 ? result : null;
        }

        public static string? Update(string key, string value)
        {
            var secrets = StorageService.LoadDictionary(SecretsFilePath);

            // Validate mutual exclusivity between Settings and Secrets
            if (secrets.ContainsKey(key))
            {
                UserInterfaceService.ShowError($"Error: Cannot add key '{key}' to Settings because it already exists in Secrets. Delete the existing entry first.");
                return null;
            }

            var settings = LoadSettings();
            if (TryParseArray(value, out var parsedArray))
            {
                settings[key] = parsedArray;
            }
            else if (TryParseLooseArray(value, out var looseArray))
            {
                settings[key] = JsonSerializer.SerializeToElement(looseArray);
            }
            else
            {
                settings[key] = JsonSerializer.SerializeToElement(value);
            }

            SaveSettings(settings);

            return value;
        }

        public static void Delete(string key)
        {
            var settings = LoadSettings();

            if (settings.ContainsKey(key))
            {
                settings.Remove(key);
                SaveSettings(settings);
            }
            else
            {
                UserInterfaceService.ShowError($"Error: Key '{key}' was not found in Settings.");
            }
        }

        private static Dictionary<string, JsonElement> LoadSettings()
        {
            var content = File.Exists(SettingsFilePath) ? FileHelper.ReadFile(SettingsFilePath) : string.Empty;

            if (string.IsNullOrWhiteSpace(content))
            {
                return new Dictionary<string, JsonElement>();
            }

            return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content)
                ?? new Dictionary<string, JsonElement>();
        }

        private static void SaveSettings(Dictionary<string, JsonElement> settings)
        {
            var content = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            FileHelper.SaveFile(SettingsFilePath, content);
        }

        private static string? ToSettingValue(JsonElement value)
        {
            return value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : value.GetRawText();
        }

        private static bool TryParseArray(string value, out JsonElement result)
        {
            result = default;

            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            try
            {
                using var document = JsonDocument.Parse(value);

                if (document.RootElement.ValueKind != JsonValueKind.Array)
                {
                    return false;
                }

                result = document.RootElement.Clone();
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static bool TryParseLooseArray(string value, out List<string> result)
        {
            result = [];

            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var trimmed = value.Trim();

            if (!trimmed.StartsWith("[", StringComparison.Ordinal) || !trimmed.EndsWith("]", StringComparison.Ordinal))
            {
                return false;
            }

            var inner = trimmed[1..^1].Trim();
            if (string.IsNullOrWhiteSpace(inner))
            {
                return true;
            }

            result = inner
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim().Trim('"'))
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToList();

            return true;
        }
    }
}
