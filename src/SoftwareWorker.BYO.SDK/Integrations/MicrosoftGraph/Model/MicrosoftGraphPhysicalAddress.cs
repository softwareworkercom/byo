using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.MicrosoftGraph.Model
{
    public class MicrosoftGraphPhysicalAddress
    {
        [JsonPropertyName("street")]
        public string? Street { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("countryOrRegion")]
        public string? CountryOrRegion { get; set; }

        [JsonPropertyName("postalCode")]
        public string? PostalCode { get; set; }
    }
}
