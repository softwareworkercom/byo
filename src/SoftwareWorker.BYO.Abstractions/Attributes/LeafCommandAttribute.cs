namespace SoftwareWorker.BYO.CLI.Abstractions.Attributes
{
    /// <summary>
    /// Attribute to mark a command handler with its leaf command information (executable action)
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class LeafCommandAttribute : Attribute
    {
        /// <summary>
        /// Leaf command name (e.g., "create", "update", "delete")
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Description of the leaf command
        /// </summary>
        public string Description { get; }

        public LeafCommandAttribute(
            string Name,
            string Description)
        {
            this.Name = Name;
            this.Description = Description;
        }
    }
}
