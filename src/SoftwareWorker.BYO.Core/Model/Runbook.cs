namespace SoftwareWorker.BYO.CLI.Core.Model
{
    /// <summary>
    /// Represents a runbook containing a sequential set of commands to execute.
    /// </summary>
    public class Runbook
    {
        /// <summary>
        /// Name of the runbook for easy identification.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Optional description of what the runbook does.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// List of commands to execute in sequence.
        /// </summary>
        public List<ShellCommand> Commands { get; set; } = [];

        /// <summary>
        /// Date and time when the runbook was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date and time when the runbook was last updated.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
    }
}
