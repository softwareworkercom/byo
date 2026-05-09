using Spectre.Console;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SoftwareWorker.BYO.CLI.Core.Service
{
    public static class TokenService
    {
        private const string TokenPattern = @"\{\{([\w:\.]+)\}\}";

        /// <summary>
        /// Detects and replaces all tokens in the given text.
        /// Automatically extracts tokens, resolves their values, and performs replacement.
        /// </summary>
        /// <param name="text">Text containing tokens to replace.</param>
        /// <returns>Text with all tokens replaced by their resolved values.</returns>
        public static string ResolveTokens(string text, object obj = null, IReadOnlyDictionary<string, string>? tokenOverrides = null)
        {
            var effectiveTokenOverrides = GetEffectiveTokenOverrides(tokenOverrides);
            var tokenMatches = Regex.Matches(text, TokenPattern);
            var tokens = tokenMatches.Select(m => m.Groups[1].Value).Distinct().ToList();

            foreach (var token in tokens)
            {
                string? value = ResolveToken(token, obj, effectiveTokenOverrides);
                if (value != null)
                {
                    text = Regex.Replace(text, $@"\{{\{{{token}\}}\}}", value, RegexOptions.IgnoreCase);
                    UserInterfaceService.ShowGrey($"Token {{{{{token}}}}} resolved with {value}");
                }
            }

            return text;
        }

        private static IReadOnlyDictionary<string, string>? GetEffectiveTokenOverrides(IReadOnlyDictionary<string, string>? tokenOverrides)
        {
            var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var (key, value) in ParseOverridesFromCommandLine())
            {
                merged[key] = value;
            }

            if (tokenOverrides != null)
            {
                foreach (var (key, value) in tokenOverrides)
                {
                    merged[NormalizeTokenName(key)] = value;
                }
            }

            return merged.Count == 0 ? tokenOverrides : merged;
        }


        private static Dictionary<string, string> ParseOverridesFromCommandLine()
        {
            var overrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var args = Environment.GetCommandLineArgs().Skip(1).ToList();

            for (var index = 0; index < args.Count; index++)
            {
                var arg = args[index];
                if (!arg.StartsWith("--", StringComparison.Ordinal))
                {
                    continue;
                }

                var optionText = arg[2..].Trim();
                if (string.IsNullOrWhiteSpace(optionText))
                {
                    continue;
                }

                string key;
                string value;
                var eqIndex = optionText.IndexOf('=');

                if (eqIndex <= 0)
                {
                    continue;
                }

                key = optionText[..eqIndex].Trim();
                value = optionText[(eqIndex + 1)..].Trim();

                key = NormalizeTokenName(key);
                if (!string.IsNullOrWhiteSpace(key))
                {
                    overrides[key] = value;
                }
            }

            return overrides;
        }
        private static string? ResolveToken(string token, object obj, IReadOnlyDictionary<string, string>? tokenOverrides)
        {
            string? value = null;

            if (TryResolveTokenFromOverrides(token, tokenOverrides, out value))
            {
                return value;
            }

            if (TryResolveTokenFromSystem(token, out value))
            {
                return value;
            }

            if (TryResolveTokenValueFromConfiguration(token, out value))
            {
                return value;
            }

            if (TryResolveTokensFromObject(token, out value, obj))
            {
                return value;
            }

            if (TryResolveTokensFromJsonElement(token, out value, obj))
            {
                return value;
            }

            value = ResolveTokenFromPrompt(token);
            return value;
        }

        private static bool TryResolveTokenFromOverrides(string token, IReadOnlyDictionary<string, string>? tokenOverrides, out string? value)
        {
            value = null;

            if (tokenOverrides == null || tokenOverrides.Count == 0)
            {
                return false;
            }

            var normalizedToken = NormalizeTokenName(token);

            foreach (var overrideItem in tokenOverrides)
            {
                var normalizedKey = NormalizeTokenName(overrideItem.Key);
                if (!string.Equals(normalizedKey, normalizedToken, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                value = overrideItem.Value;
                return true;
            }

            return false;
        }

        private static string NormalizeTokenName(string tokenName)
        {
            if (string.IsNullOrWhiteSpace(tokenName))
            {
                return string.Empty;
            }

            var normalized = tokenName.Trim();
            if (normalized.StartsWith("{{", StringComparison.Ordinal) &&
                normalized.EndsWith("}}", StringComparison.Ordinal) &&
                normalized.Length > 4)
            {
                normalized = normalized[2..^2].Trim();
            }

            return normalized;
        }

        private static string? ResolveTokenFromPrompt(string token)
        {
            try
            {
                var value = UserInterfaceService.Prompt(new TextPrompt<string>($"[cyan]Enter value for [bold]{token}[/] (or press Enter to skip):[/]"));
                return value;
            }
            catch (InvalidOperationException)
            {
                // Non-interactive mode: cannot prompt – leave token unreplaced
                return null;
            }
        }

        private static bool TryResolveTokenFromSystem(string token, out string? value)
        {
            value = null;
            switch (token)
            {
                case "Date":
                    var date = UserInterfaceService.SelectDate();
                    value = $"{date:yyyy-MM-dd}";
                    break;
                case "DateTimeRangeFromNow":
                    var startDate = UserInterfaceService.SelectDateTimeRangeFromNow();
                    value = $"{startDate:yyyy-MM-dd HH:mm}";
                    break;
                case "Guid":
                    value = Guid.NewGuid().ToString();
                    break;
            }

            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            return true;
        }

        private static bool TryResolveTokenValueFromConfiguration(string token, out string? value)
        {
            value = null;
            var settings = SettingsService.GetList(token);
            var secrets = SecretsService.GetList(token);

            // Merge settings and secrets
            var allConfigurations = (settings ?? new Dictionary<string, string>())
                .Concat(secrets ?? new Dictionary<string, string>())
                .GroupBy(kvp => kvp.Key)
                .ToDictionary(g => g.Key, g => g.Last().Value);

            if (allConfigurations.Count > 0)
            {
                //If multiple settings found, ask user to select one
                if (allConfigurations.Count > 1)
                {
                    var selectedToken = UserInterfaceService.SelectSingleItem("token", allConfigurations.Keys.ToList());
                    value = allConfigurations[selectedToken];
                }
                else if (allConfigurations.Count == 1)
                {
                    value = allConfigurations.Values.First();
                }
            }

            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            if (value.Contains("|"))
            {
                var items = value.Split('|').Select(v => v.Trim()).ToList();
                value = UserInterfaceService.SelectSingleItem("token", items);
            }

            return true;
        }

        private static bool TryResolveTokensFromObject(string token, out string? value, object obj)
        {
            value = null;

            if (obj == null)
                return false;

            // Skip JsonElement objects - they are handled by TryResolveTokensFromJsonElement
            if (obj is JsonElement)
                return false;

            // Split token by '.' to handle nested properties (e.g., "Component.Repository.Name")
            var tokenParts = token.Split('.');

            // Single-part tokens (no class prefix) are not resolved from object
            if (tokenParts.Length < 2)
                return false;

            object? currentObj = obj;
            bool propertyResolved = false;

            foreach (var part in tokenParts)
            {
                if (currentObj == null)
                    return false;

                var type = currentObj.GetType();
                var property = type.GetProperty(part, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                if (property == null)
                    continue; // skip class-name prefix parts

                propertyResolved = true;
                currentObj = property.GetValue(currentObj);
            }

            if (!propertyResolved)
                return false;

            value = currentObj?.ToString() ?? string.Empty;

            return true;
        }

        /// <summary>
        /// Resolves token values from a JsonElement object by traversing nested properties.
        /// Supports dot-notation for nested JSON objects (e.g., "Item.Address.City").
        /// </summary>
        private static bool TryResolveTokensFromJsonElement(string token, out string? value, object obj)
        {
            value = null;

            if (obj is not JsonElement element)
                return false;

            if (element.ValueKind != JsonValueKind.Object)
                return false;

            var tokenParts = token.Split('.');
            var currentElement = element;

            foreach (var part in tokenParts)
            {
                if (currentElement.ValueKind != JsonValueKind.Object)
                    return false;

                // Case-insensitive property lookup
                var found = false;
                foreach (var property in currentElement.EnumerateObject())
                {
                    if (string.Equals(property.Name, part, StringComparison.OrdinalIgnoreCase))
                    {
                        currentElement = property.Value;
                        found = true;
                        break;
                    }
                }

                if (!found)
                    return false;
            }

            value = currentElement.ValueKind switch
            {
                JsonValueKind.String => currentElement.GetString(),
                JsonValueKind.Number => currentElement.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => null,
                _ => currentElement.GetRawText()
            };

            return !string.IsNullOrEmpty(value);
        }
    }
}
