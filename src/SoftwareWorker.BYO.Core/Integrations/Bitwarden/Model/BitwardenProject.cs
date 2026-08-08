using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.Bitwarden.Model
{
    public class BitwardenProject
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("organizationId")]
        public string? OrganizationId { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("creationDate")]
        public DateTime? CreationDate { get; set; }

        [JsonPropertyName("revisionDate")]
        public DateTime? RevisionDate { get; set; }
    }

    public class BitwardenProjectsListResponse
    {
        [JsonPropertyName("data")]
        public List<BitwardenProject>? Data { get; set; }
    }
}
