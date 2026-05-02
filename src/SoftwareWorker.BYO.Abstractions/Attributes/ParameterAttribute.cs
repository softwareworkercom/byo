namespace SoftwareWorker.BYO.CLI.Abstractions.Attributes
{
    /// <summary>
    /// Attribute to mark command parameters
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ParameterAttribute : Attribute
    {
        /// <summary>
        /// Parameter name (used as --{name})
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Parameter description
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Whether the parameter is required
        /// </summary>
        public bool IsRequired { get; }

        /// <summary>
        /// Default value or pipe-separated options (e.g., "xls|csv|json")
        /// </summary>
        public object? DefaultValue { get; }

        public ParameterAttribute(string name, string description, bool isRequired, object? defaultValue)
        {
            Name = name;
            Description = description;
            IsRequired = isRequired;
            DefaultValue = defaultValue;
        }
    }
}
