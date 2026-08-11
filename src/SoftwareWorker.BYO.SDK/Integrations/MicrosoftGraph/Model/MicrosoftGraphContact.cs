using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.MicrosoftGraph.Model
{
    public class MicrosoftGraphContact
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("givenName")]
        public string? GivenName { get; set; }

        [JsonPropertyName("surname")]
        public string? Surname { get; set; }

        [JsonPropertyName("emailAddresses")]
        public List<MicrosoftGraphEmailAddress>? EmailAddresses { get; set; }

        [JsonPropertyName("businessPhones")]
        public List<string>? BusinessPhones { get; set; }

        [JsonPropertyName("mobilePhone")]
        public string? MobilePhone { get; set; }

        [JsonPropertyName("homePhones")]
        public List<string>? HomePhones { get; set; }

        [JsonPropertyName("companyName")]
        public string? CompanyName { get; set; }

        [JsonPropertyName("jobTitle")]
        public string? JobTitle { get; set; }
    }
}
