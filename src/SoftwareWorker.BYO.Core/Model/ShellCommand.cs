using SoftwareWorker.BYO.Core.Model.Enums;

namespace SoftwareWorker.BYO.CLI.Core.Model
{
    public class ShellCommand
    {
        public string Description { get; set; }
        public string Executable { get; set; }
        public string? WorkingDirectory { get; set; }

        /// <summary>
        /// Shell to use for executing the command.
        /// If null, defaults to PowerShell.
        /// </summary>
        public ShellTypeEnum? Shell { get; set; }

        /// <summary>
        /// Optional hierarchical folder path for organising commands (e.g. "DevOps/Deploy").
        /// Use "/" as the separator between folder levels.
        /// </summary>
        public string? FolderPath { get; set; }

        /// <summary>
        /// Date and time when the command was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date and time when the command was last updated.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
    }
}
