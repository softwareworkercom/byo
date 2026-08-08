namespace SoftwareWorker.BYO.CLI.Abstractions.Attributes
{
    /// <summary>
    /// Attribute to mark a command handler with its trunk command information
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class TrunkCommandAttribute : Attribute
    {
        /// <summary>
        /// Trunk command name (e.g., "git", "jira", "export")
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Description of the trunk command
        /// </summary>
        public string Description { get; }

        public TrunkCommandAttribute(
            string Name,
            string Description)
        {
            this.Name = Name;
            this.Description = Description;
        }
    }
}
