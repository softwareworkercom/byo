using SoftwareWorker.BYO.CLI.Abstractions.Attributes;
using SoftwareWorker.BYO.CLI.Core.Service;
using SoftwareWorker.BYO.Core.Model.Enums;
using Spectre.Console;
using System.Reflection;

namespace SoftwareWorker.BYO.CLI.Core
{
    /// <summary>
    /// Base class for command handlers that provides automatic parameter binding.
    /// Properties decorated with [Parameter] attributes will be automatically populated
    /// from the command line arguments before ExecuteAsync is called.
    /// 
    /// Conversion Behavior:
    /// - If a parameter value cannot be converted to the target property type, 
    ///   the property will retain its default value (0 for numbers, false for bool, null for reference types).
    /// - Failed conversions are silent to maintain backward compatibility.
    /// - For enums, invalid string values will result in the default enum value (first member).
    /// </summary>
    public abstract class BaseCommandHandler
    {
        public IReadOnlyDictionary<string, string> DynamicParameters { get; private set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public ExportEnum? Export { get; private set; }
        public string? ContextKey { get; private set; }

        /// <summary>
        /// Executes the command with parameters automatically bound to properties.
        /// Override this method to implement your command logic.
        /// </summary>
        public abstract Task ExecuteAsync();

        public void SetDynamicParameters(Dictionary<string, string> dynamicParameters)
        {
            DynamicParameters = dynamicParameters ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public void SetExport(ExportEnum? export)
        {
            Export = export;
        }

        public void SetContext(string? context)
        {
            ContextKey = context;
        }

        /// <summary>
        /// Ensures all parameters are populated.
        /// Prompts the user interactively when running in an interactive console session.
        /// In non-interactive mode, validates that all required parameters are present and returns an error if any are missing.
        /// </summary>
        /// <param name="handlerType">The handler type to read parameter attributes from</param>
        /// <param name="options">The options dictionary from command line arguments</param>
        /// <returns>
        /// A tuple of the updated options dictionary and a (possibly null) validation error message.
        /// The caller must display the error and abort execution when the message is non-null.
        /// </returns>
        public static (Dictionary<string, object> Options, string? ValidationError) EnsureParameters(
            Type handlerType,
            Dictionary<string, object> options)
        {
            var parameters = handlerType.GetCustomAttributes<ParameterAttribute>().ToList();

            if (parameters.Count == 0)
            {
                return (options, null);
            }

            var updatedOptions = new Dictionary<string, object>(options);

            if (UserInterfaceService.IsInteractive)
            {
                // Interactive mode: prompt for every declared parameter that is not already supplied
                foreach (var param in parameters)
                {
                    if (!param.IsPromptable)
                    {
                        continue;
                    }

                    var currentValue = updatedOptions.FirstOrDefault(p =>
                        string.Equals(p.Key, param.Name, StringComparison.OrdinalIgnoreCase)).Value?.ToString();

                    if (string.IsNullOrEmpty(currentValue))
                    {
                        var promptedValue = PromptForParameter(param, handlerType);
                        if (!string.IsNullOrEmpty(promptedValue))
                        {
                            updatedOptions[param.Name] = promptedValue;
                        }
                    }
                }
            }
            else
            {
                // Non-interactive mode: validate that all required parameters are present
                var missingParams = new List<string>();

                foreach (var param in parameters)
                {
                    if (!param.IsRequired)
                        continue;

                    var currentValue = updatedOptions.FirstOrDefault(p =>
                        string.Equals(p.Key, param.Name, StringComparison.OrdinalIgnoreCase)).Value?.ToString();

                    if (string.IsNullOrEmpty(currentValue))
                    {
                        missingParams.Add($"--{param.Name}");
                    }
                }

                if (missingParams.Count > 0)
                {
                    var missing = string.Join(", ", missingParams);
                    return (updatedOptions, $"Missing required parameter(s): {missing}.");
                }
            }

            return (updatedOptions, null);
        }

        /// <summary>
        /// Prompts the user for a parameter value using Spectre.Console.
        /// Handles enumerated options (pipe-separated DefaultValue) with a selection prompt.
        /// </summary>
        /// <param name="param">The parameter attribute containing metadata</param>
        /// <param name="handlerType">The handler type</param>
        /// <returns>The user-provided value or null if skipped</returns>
        private static string? PromptForParameter(ParameterAttribute param, Type handlerType)
        {
            var defaultValueStr = param.DefaultValue?.ToString();
            var hasEnumeratedOptions = !string.IsNullOrEmpty(defaultValueStr) && defaultValueStr.Contains('|');

            if (hasEnumeratedOptions)
            {
                // Show selection prompt for enumerated options
                var options = defaultValueStr!.Split('|', StringSplitOptions.RemoveEmptyEntries)
                    .Select(o => o.Trim())
                    .ToList();

                return UserInterfaceService.Prompt(
                    new SelectionPrompt<string>()
                        .Title($"[cyan]Select {param.Name}[/] [grey]({param.Description}):[/]")
                        .PageSize(10)
                        .AddChoices(options));
            }
            else
            {
                // Use text prompt for free-form input
                var promptTitle = param.IsRequired
                    ? $"[cyan]{param.Name}[/] [red](required)[/] [grey]({param.Description}):[/]"
                    : $"[cyan]{param.Name}[/] [grey](optional - {param.Description}):[/]";

                if (param.IsRequired)
                {
                    // Required parameter - must have a value
                    return UserInterfaceService.Prompt(
                        new TextPrompt<string>(promptTitle)
                            .PromptStyle("green")
                            .ValidationErrorMessage("[red]This parameter is required[/]")
                            .Validate(value => !string.IsNullOrWhiteSpace(value)
                                ? ValidationResult.Success()
                                : ValidationResult.Error("[red]Please enter a value[/]")));
                }
                else
                {
                    // Optional parameter - allow empty
                    return UserInterfaceService.Prompt(
                        new TextPrompt<string>(promptTitle)
                            .PromptStyle("green")
                            .AllowEmpty());
                }
            }
        }

        /// <summary>
        /// Binds parameters from the dictionary to properties based on Parameter attributes.
        /// This method is called automatically by the framework before ExecuteAsync.
        /// </summary>
        public void BindParameters(Dictionary<string, object> parameters)
        {
            var type = GetType();
            var parameterAttributes = type.GetCustomAttributes<ParameterAttribute>();

            foreach (var paramAttr in parameterAttributes)
            {
                // Find a property with a matching name (case-insensitive)
                var property = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .FirstOrDefault(p => string.Equals(p.Name, paramAttr.Name, StringComparison.OrdinalIgnoreCase));

                if (property == null || !property.CanWrite)
                    continue;

                // Get the value from parameters dictionary (case-insensitive lookup)
                var paramValue = parameters.FirstOrDefault(p =>
                    string.Equals(p.Key, paramAttr.Name, StringComparison.OrdinalIgnoreCase)).Value;

                if (paramValue != null)
                {
                    try
                    {
                        var convertedValue = ConvertValue(paramValue, property.PropertyType);
                        property.SetValue(this, convertedValue);
                    }
                    catch
                    {
                        // If conversion fails, skip this property
                        continue;
                    }
                }
            }
        }

        /// <summary>
        /// Converts a value from the parameters dictionary to the target property type.
        /// For failed conversions, returns default values to maintain backward compatibility:
        /// - Numeric types: 0
        /// - Bool: false
        /// - Enums: default/first member
        /// - Reference types: null
        /// </summary>
        private static object? ConvertValue(object value, Type targetType)
        {
            if (value == null)
                return null;

            // Handle nullable types
            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            // If types match, return as-is
            if (value.GetType() == underlyingType)
                return value;

            // Handle string conversions
            if (value is string stringValue)
            {
                if (underlyingType == typeof(string))
                    return stringValue;

                if (underlyingType == typeof(bool))
                    return bool.TryParse(stringValue, out var boolResult) && boolResult;

                if (underlyingType == typeof(int))
                    return int.TryParse(stringValue, out var intResult) ? intResult : 0;

                if (underlyingType == typeof(long))
                    return long.TryParse(stringValue, out var longResult) ? longResult : 0L;

                if (underlyingType == typeof(double))
                    return double.TryParse(stringValue, out var doubleResult) ? doubleResult : 0.0;

                if (underlyingType == typeof(decimal))
                    return decimal.TryParse(stringValue, out var decimalResult) ? decimalResult : 0m;

                // Handle enums
                if (underlyingType.IsEnum)
                {
                    try
                    {
                        return Enum.Parse(underlyingType, stringValue, true);
                    }
                    catch
                    {
                        return Activator.CreateInstance(underlyingType);
                    }
                }
            }

            // Try direct conversion
            try
            {
                return Convert.ChangeType(value, underlyingType);
            }
            catch
            {
                return underlyingType.IsValueType ? Activator.CreateInstance(underlyingType) : null;
            }
        }
    }
}
