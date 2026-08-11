using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.Auth0.Model
{
    public class Auth0Client
    {
        [JsonPropertyName("client_id")]
        public string? ClientId { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("client_secret")]
        public string? ClientSecret { get; set; }

        [JsonPropertyName("app_type")]
        public string? AppType { get; set; }

        [JsonPropertyName("logo_uri")]
        public string? LogoUri { get; set; }

        [JsonPropertyName("is_first_party")]
        public bool IsFirstParty { get; set; }

        [JsonPropertyName("is_token_endpoint_ip_header_trusted")]
        public bool IsTokenEndpointIpHeaderTrusted { get; set; }

        [JsonPropertyName("oidc_conformant")]
        public bool OidcConformant { get; set; }

        [JsonPropertyName("callbacks")]
        public List<string>? Callbacks { get; set; }

        [JsonPropertyName("allowed_logout_urls")]
        public List<string>? AllowedLogoutUrls { get; set; }

        [JsonPropertyName("allowed_origins")]
        public List<string>? AllowedOrigins { get; set; }

        [JsonPropertyName("web_origins")]
        public List<string>? WebOrigins { get; set; }

        [JsonPropertyName("grant_types")]
        public List<string>? GrantTypes { get; set; }

        [JsonPropertyName("jwt_configuration")]
        public Auth0JwtConfiguration? JwtConfiguration { get; set; }
    }

    public class Auth0JwtConfiguration
    {
        [JsonPropertyName("lifetime_in_seconds")]
        public int LifetimeInSeconds { get; set; }

        [JsonPropertyName("secret_encoded")]
        public bool SecretEncoded { get; set; }

        [JsonPropertyName("alg")]
        public string? Alg { get; set; }
    }

    public class Auth0ClientCreateRequest
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("app_type")]
        public string? AppType { get; set; }

        [JsonPropertyName("logo_uri")]
        public string? LogoUri { get; set; }

        [JsonPropertyName("callbacks")]
        public List<string>? Callbacks { get; set; }

        [JsonPropertyName("allowed_logout_urls")]
        public List<string>? AllowedLogoutUrls { get; set; }

        [JsonPropertyName("allowed_origins")]
        public List<string>? AllowedOrigins { get; set; }

        [JsonPropertyName("web_origins")]
        public List<string>? WebOrigins { get; set; }

        [JsonPropertyName("grant_types")]
        public List<string>? GrantTypes { get; set; }

        [JsonPropertyName("jwt_configuration")]
        public Auth0JwtConfiguration? JwtConfiguration { get; set; }
    }

    public class Auth0ClientUpdateRequest
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("app_type")]
        public string? AppType { get; set; }

        [JsonPropertyName("logo_uri")]
        public string? LogoUri { get; set; }

        [JsonPropertyName("callbacks")]
        public List<string>? Callbacks { get; set; }

        [JsonPropertyName("allowed_logout_urls")]
        public List<string>? AllowedLogoutUrls { get; set; }

        [JsonPropertyName("allowed_origins")]
        public List<string>? AllowedOrigins { get; set; }

        [JsonPropertyName("web_origins")]
        public List<string>? WebOrigins { get; set; }

        [JsonPropertyName("grant_types")]
        public List<string>? GrantTypes { get; set; }

        [JsonPropertyName("jwt_configuration")]
        public Auth0JwtConfiguration? JwtConfiguration { get; set; }
    }
}
