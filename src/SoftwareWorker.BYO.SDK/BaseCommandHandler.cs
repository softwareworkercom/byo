using SoftwareWorker.BYO.SDK.Abstractions.Attributes;
using SoftwareWorker.BYO.SDK.Services;
using System.Reflection;

namespace SoftwareWorker.BYO.SDK
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

        /// <summary>
        /// Executes the command with parameters automatically bound to properties.
        /// Override this method to implement your command logic.
        /// </summary>
        public abstract Task ExecuteAsync();

        public void SetDynamicParameters(Dictionary<string, string> dynamicParameters)
        {
            DynamicParameters = dynamicParameters ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks that every required parameter was supplied when running non-interactively.
        /// In an interactive console session nothing is checked, so the handler can resolve missing values itself,
        /// for example with a selection prompt.
        /// </summary>
        /// <param name="handlerType">The handler type to read parameter attributes from</param>
        /// <param name="options">The options dictionary from command line arguments</param>
        /// <returns>
        /// A tuple of the options dictionary and a (possibly null) validation error message.
        /// The caller must display the error and abort execution when the message is non-null.
        /// </returns>
        public static (Dictionary<string, object> Options, string? ValidationError) EnsureParameters(
            Type handlerType,
            Dictionary<string, object> options)
        {
            if (UserInterfaceService.IsInteractive)
            {
                return (options, null);
            }

            var missingParams = new List<string>();

            foreach (var param in handlerType.GetCustomAttributes<ParameterAttribute>())
            {
                if (!param.IsRequired)
                    continue;

                var currentValue = options.FirstOrDefault(p =>
                    string.Equals(p.Key, param.Name, StringComparison.OrdinalIgnoreCase)).Value?.ToString();

                if (string.IsNullOrEmpty(currentValue))
                {
                    missingParams.Add($"--{param.Name}");
                }
            }

            if (missingParams.Count > 0)
            {
                var missing = string.Join(", ", missingParams);
                return (options, $"Missing required parameter(s): {missing}.");
            }

            return (options, null);
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
