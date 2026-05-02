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
        public string ActionName { get; }

        /// <summary>
        /// Description of the leaf command
        /// </summary>
        public string ActionDescription { get; }

        public LeafCommandAttribute(
            string actionName,
            string actionDescription)
        {
            ActionName = actionName;
            ActionDescription = actionDescription;
        }
    }
}
