using SoftwareWorker.BYO.SDK.Abstractions.Attributes;
using SoftwareWorker.BYO.SDK;
using SoftwareWorker.BYO.SDK.Services;
using System.Text.Json;
using Spectre.Console;

namespace SoftwareWorker.BYO.CLI.Handlers.Settings
{
    [TrunkCommand("settings", "Settings operations")]
    [BranchCommand("import", "Import a setting value from a JSON file")]
    [Parameter("key", "The setting key to create or update", true, null)]
    [Parameter("path", "JSON path to the value to import", true, null)]
    [Parameter("file", "Path to the JSON file", true, null)]
    public class SettingsImportCommand : BaseCommandHandler
    {
        public string? Key { get; set; }
        public string? Path { get; set; }
        public string? File { get; set; }

        public override async Task ExecuteAsync()
        {
            if (string.IsNullOrWhiteSpace(Key) || string.IsNullOrWhiteSpace(Path) || string.IsNullOrWhiteSpace(File))
            {
                UserInterfaceService.ShowError("Key, path, and file are required. Use 'byo settings import --key <key> --path <json-path> --file <path>'.");
                return;
            }

            if (!System.IO.File.Exists(File))
            {
                UserInterfaceService.ShowError($"File not found: {Markup.Escape(File)}");
                return;
            }

            string value;
            try
            {
                using var document = JsonDocument.Parse(System.IO.File.ReadAllText(File));
                value = ReadSettingValue(document.RootElement, Path);
            }
            catch (JsonException ex)
            {
                UserInterfaceService.ShowError($"Could not parse JSON file: {Markup.Escape(ex.Message)}");
                return;
            }
            catch (IOException ex)
            {
                UserInterfaceService.ShowError($"Could not read file '{Markup.Escape(File)}': {Markup.Escape(ex.Message)}");
                return;
            }
            catch (UnauthorizedAccessException ex)
            {
                UserInterfaceService.ShowError($"Could not read file '{Markup.Escape(File)}': {Markup.Escape(ex.Message)}");
                return;
            }
            catch (InvalidDataException ex)
            {
                UserInterfaceService.ShowError(ex.Message);
                return;
            }

            var settings = SettingsService.GetList() ?? new Dictionary<string, string>();
            if (settings.TryGetValue(Key, out var currentValue))
            {
                var shouldReplace = UserInterfaceService.Confirm(
                    $"Setting '[cyan]{Markup.Escape(Key)}[/]' already exists with current value '[grey]{Markup.Escape(currentValue)}[/]'. Do you want to replace it?");

                if (!shouldReplace)
                {
                    UserInterfaceService.ShowWarning("Setting import cancelled.");
                    return;
                }
            }

            if (SettingsService.Update(Key, value) is null)
            {
                return;
            }

            UserInterfaceService.ShowGreen($"Imported setting '{Markup.Escape(Key)}' from '{Markup.Escape(File)}'.");
            await Task.CompletedTask;
        }

        private static string ReadSettingValue(JsonElement root, string jsonPath)
        {
            var current = root;
            var index = jsonPath.StartsWith("$.", StringComparison.Ordinal) ? 2 :
                jsonPath.StartsWith('$') ? 1 : 0;

            while (index < jsonPath.Length)
            {
                if (jsonPath[index] == '.')
                {
                    index++;
                    continue;
                }

                if (jsonPath[index] == '[')
                {
                    var closingBracket = jsonPath.IndexOf(']', index + 1);
                    if (closingBracket < 0 ||
                        !int.TryParse(jsonPath.AsSpan(index + 1, closingBracket - index - 1), out var arrayIndex) ||
                        arrayIndex < 0)
                    {
                        throw new InvalidDataException($"Invalid JSON path '{Markup.Escape(jsonPath)}'. Array indexes must be non-negative integers.");
                    }

                    if (current.ValueKind != JsonValueKind.Array || arrayIndex >= current.GetArrayLength())
                    {
                        throw new InvalidDataException($"JSON path '{Markup.Escape(jsonPath)}' does not identify a value.");
                    }

                    current = current[arrayIndex];
                    index = closingBracket + 1;
                    continue;
                }

                var propertyEnd = index;
                while (propertyEnd < jsonPath.Length && jsonPath[propertyEnd] is not '.' and not '[')
                {
                    propertyEnd++;
                }

                var propertyName = jsonPath[index..propertyEnd];
                if (propertyName.Length == 0 || current.ValueKind != JsonValueKind.Object ||
                    !current.TryGetProperty(propertyName, out current))
                {
                    throw new InvalidDataException($"JSON path '{Markup.Escape(jsonPath)}' does not identify a value.");
                }

                index = propertyEnd;
            }

            if (current.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                throw new InvalidDataException($"JSON path '{Markup.Escape(jsonPath)}' resolves to null.");
            }

            return current.ValueKind == JsonValueKind.String
                ? current.GetString() ?? string.Empty
                : current.GetRawText();
        }
    }
}
