namespace SoftwareWorker.BYO.CLI.Core.Model
{
    /// <summary>
    /// Represents a browser bookmark with its associated metadata.
    /// </summary>
    public class Bookmark
    {
        /// <summary>
        /// Name of the bookmark for easy identification.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// URL of the bookmarked resource.
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Folder or category for organizing the bookmark.
        /// </summary>
        public string? Folder { get; set; }

        /// <summary>
        /// Date and time when the bookmark was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date and time when the bookmark was last updated.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
    }
}
