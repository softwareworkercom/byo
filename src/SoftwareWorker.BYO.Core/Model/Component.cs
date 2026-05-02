namespace SoftwareWorker.BYO.CLI.Core.Model
{
    /// <summary>
    /// Represents a component with its associated metadata.
    /// </summary>
    public class Component
    {
        public ComponentRepository Repository { get; set; } = new();
        public string Name { get; set; } = string.Empty;
        public string? Owner { get; set; }
    }

    /// <summary>
    /// Represents repository information for a component.
    /// </summary>
    public class ComponentRepository
    {
        public string DefaultBranch { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
