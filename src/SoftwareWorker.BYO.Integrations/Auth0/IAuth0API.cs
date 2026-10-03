using Refit;
using SoftwareWorker.BYO.Integrations.Auth0.Model;

namespace SoftwareWorker.BYO.Integrations.Auth0
{
    /// <summary>
    /// https://auth0.com/docs/api/management/v2
    /// </summary>
    internal interface IAuth0API
    {
        [Get("/api/v2/users")]
        Task<List<Auth0User>> ListUsersAsync([Query] int? per_page = 50, [Query] int? page = 0);

        [Get("/api/v2/users/{id}")]
        Task<Auth0User> GetUserAsync([AliasAs("id")] string id);

        [Post("/api/v2/users")]
        Task<Auth0User> CreateUserAsync([Body] Auth0UserCreateRequest request);

        [Patch("/api/v2/users/{id}")]
        Task<Auth0User> UpdateUserAsync([AliasAs("id")] string id, [Body] Auth0UserUpdateRequest request);

        [Delete("/api/v2/users/{id}")]
        Task DeleteUserAsync([AliasAs("id")] string id);

        [Get("/api/v2/clients")]
        Task<List<Auth0Client>> ListClientsAsync([Query] int? per_page = 50, [Query] int? page = 0);

        [Get("/api/v2/clients/{id}")]
        Task<Auth0Client> GetClientAsync([AliasAs("id")] string id);

        [Post("/api/v2/clients")]
        Task<Auth0Client> CreateClientAsync([Body] Auth0ClientCreateRequest request);

        [Patch("/api/v2/clients/{id}")]
        Task<Auth0Client> UpdateClientAsync([AliasAs("id")] string id, [Body] Auth0ClientUpdateRequest request);

        [Delete("/api/v2/clients/{id}")]
        Task DeleteClientAsync([AliasAs("id")] string id);

        [Get("/api/v2/roles")]
        Task<Auth0RolesResponse> ListRolesAsync([Query] int? per_page = 50, [Query] int? page = 0);

        [Get("/api/v2/roles/{id}")]
        Task<Auth0Role> GetRoleAsync([AliasAs("id")] string id);

        [Post("/api/v2/roles")]
        Task<Auth0Role> CreateRoleAsync([Body] Auth0RoleCreateRequest request);

        [Patch("/api/v2/roles/{id}")]
        Task<Auth0Role> UpdateRoleAsync([AliasAs("id")] string id, [Body] Auth0RoleUpdateRequest request);

        [Delete("/api/v2/roles/{id}")]
        Task DeleteRoleAsync([AliasAs("id")] string id);

        [Get("/api/v2/users/{id}/roles")]
        Task<List<Auth0Role>> ListUserRolesAsync([AliasAs("id")] string id);

        [Post("/api/v2/users/{id}/roles")]
        Task AssignRolesToUserAsync([AliasAs("id")] string id, [Body] Auth0AssignRolesRequest request);

        [Delete("/api/v2/users/{id}/roles")]
        Task RemoveRolesFromUserAsync([AliasAs("id")] string id, [Body] Auth0RemoveRolesRequest request);

        [Get("/api/v2/connections")]
        Task<List<Auth0Connection>> ListConnectionsAsync([Query] int? per_page = 50, [Query] int? page = 0);

        [Get("/api/v2/connections/{id}")]
        Task<Auth0Connection> GetConnectionAsync([AliasAs("id")] string id);

        [Get("/api/v2/logs")]
        Task<List<Auth0Log>> ListLogsAsync([Query] int? per_page = 50, [Query] int? page = 0, [Query] string? q = null);
    }
}
