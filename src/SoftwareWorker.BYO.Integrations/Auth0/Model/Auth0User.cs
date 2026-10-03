using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.Auth0.Model
{
    public class Auth0User
    {
        [JsonPropertyName("user_id")]
        public string? UserId { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("email_verified")]
        public bool EmailVerified { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("phone_number")]
        public string? PhoneNumber { get; set; }

        [JsonPropertyName("phone_verified")]
        public bool PhoneVerified { get; set; }

        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public string? UpdatedAt { get; set; }

        [JsonPropertyName("identities")]
        public List<Auth0Identity>? Identities { get; set; }

        [JsonPropertyName("app_metadata")]
        public Dictionary<string, object>? AppMetadata { get; set; }

        [JsonPropertyName("user_metadata")]
        public Dictionary<string, object>? UserMetadata { get; set; }

        [JsonPropertyName("picture")]
        public string? Picture { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("nickname")]
        public string? Nickname { get; set; }

        [JsonPropertyName("multifactor")]
        public List<string>? Multifactor { get; set; }

        [JsonPropertyName("last_ip")]
        public string? LastIp { get; set; }

        [JsonPropertyName("last_login")]
        public string? LastLogin { get; set; }

        [JsonPropertyName("logins_count")]
        public int LoginsCount { get; set; }

        [JsonPropertyName("blocked")]
        public bool Blocked { get; set; }

        [JsonPropertyName("given_name")]
        public string? GivenName { get; set; }

        [JsonPropertyName("family_name")]
        public string? FamilyName { get; set; }
    }

    public class Auth0Identity
    {
        [JsonPropertyName("connection")]
        public string? Connection { get; set; }

        [JsonPropertyName("user_id")]
        public string? UserId { get; set; }

        [JsonPropertyName("provider")]
        public string? Provider { get; set; }

        [JsonPropertyName("isSocial")]
        public bool IsSocial { get; set; }
    }

    public class Auth0UserCreateRequest
    {
        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("password")]
        public string? Password { get; set; }

        [JsonPropertyName("connection")]
        public string? Connection { get; set; }

        [JsonPropertyName("email_verified")]
        public bool? EmailVerified { get; set; }

        [JsonPropertyName("phone_number")]
        public string? PhoneNumber { get; set; }

        [JsonPropertyName("phone_verified")]
        public bool? PhoneVerified { get; set; }

        [JsonPropertyName("user_metadata")]
        public Dictionary<string, object>? UserMetadata { get; set; }

        [JsonPropertyName("app_metadata")]
        public Dictionary<string, object>? AppMetadata { get; set; }

        [JsonPropertyName("given_name")]
        public string? GivenName { get; set; }

        [JsonPropertyName("family_name")]
        public string? FamilyName { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("nickname")]
        public string? Nickname { get; set; }

        [JsonPropertyName("picture")]
        public string? Picture { get; set; }
    }

    public class Auth0UserUpdateRequest
    {
        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("password")]
        public string? Password { get; set; }

        [JsonPropertyName("email_verified")]
        public bool? EmailVerified { get; set; }

        [JsonPropertyName("phone_number")]
        public string? PhoneNumber { get; set; }

        [JsonPropertyName("phone_verified")]
        public bool? PhoneVerified { get; set; }

        [JsonPropertyName("user_metadata")]
        public Dictionary<string, object>? UserMetadata { get; set; }

        [JsonPropertyName("app_metadata")]
        public Dictionary<string, object>? AppMetadata { get; set; }

        [JsonPropertyName("blocked")]
        public bool? Blocked { get; set; }

        [JsonPropertyName("given_name")]
        public string? GivenName { get; set; }

        [JsonPropertyName("family_name")]
        public string? FamilyName { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("nickname")]
        public string? Nickname { get; set; }

        [JsonPropertyName("picture")]
        public string? Picture { get; set; }
    }
}
