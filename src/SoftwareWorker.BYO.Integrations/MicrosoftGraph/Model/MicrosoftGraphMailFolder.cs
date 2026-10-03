using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.MicrosoftGraph.Model
{
    public class MicrosoftGraphMailFolder
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("parentFolderId")]
        public string? ParentFolderId { get; set; }

        [JsonPropertyName("childFolderCount")]
        public int? ChildFolderCount { get; set; }

        [JsonPropertyName("unreadItemCount")]
        public int? UnreadItemCount { get; set; }

        [JsonPropertyName("totalItemCount")]
        public int? TotalItemCount { get; set; }
    }
}
