using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.Auth0.Model
{
    public class Auth0Role
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }

    public class Auth0RolesResponse
    {
        [JsonPropertyName("roles")]
        public List<Auth0Role>? Roles { get; set; }

        [JsonPropertyName("start")]
        public int Start { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }
    }

    public class Auth0RoleCreateRequest
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }

    public class Auth0RoleUpdateRequest
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }

    public class Auth0AssignRolesRequest
    {
        [JsonPropertyName("roles")]
        public List<string>? Roles { get; set; }
    }

    public class Auth0RemoveRolesRequest
    {
        [JsonPropertyName("roles")]
        public List<string>? Roles { get; set; }
    }
}
