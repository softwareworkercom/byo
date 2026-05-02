namespace SoftwareWorker.BYO.CLI.Abstractions.Attributes
{
    /// <summary>
    /// Attribute to mark a command handler with its branch command information
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class BranchCommandAttribute : Attribute
    {
        /// <summary>
        /// Branch command name (e.g., "issue", "page", "clone", "create")
        /// </summary>
        public string SubCommandName { get; }

        /// <summary>
        /// Description of the branch command
        /// </summary>
        public string SubCommandDescription { get; }

        public BranchCommandAttribute(
            string subCommandName,
            string subCommandDescription)
        {
            SubCommandName = subCommandName;
            SubCommandDescription = subCommandDescription;
        }
    }
}
