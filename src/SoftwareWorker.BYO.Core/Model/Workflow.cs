namespace SoftwareWorker.BYO.CLI.Core.Model
{
    /// <summary>
    /// Represents a workflow containing interactive steps for user execution
    /// </summary>
    public class Workflow
    {
        /// <summary>
        /// Name of the workflow for easy identification
        /// </summary>
        public string Name { get; set; } = string.Empty;


        /// <summary>
        /// List of steps to execute in sequence
        /// </summary>
        public List<WorkflowStep> Steps { get; set; } = [];

        /// <summary>
        /// Optional hierarchical folder path for organising workflows (e.g. "DevOps/Deploy").
        /// Use "/" as the separator between folder levels.
        /// </summary>
        public string? Bookmark { get; set; }

        /// <summary>
        /// Date and time when the workflow was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date and time when the workflow was last updated
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
    }
}
