using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.Toggl.Model
{
    public class TogglWorkspaceRequest
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
