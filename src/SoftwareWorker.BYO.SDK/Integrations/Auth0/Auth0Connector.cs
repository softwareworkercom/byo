using Refit;
using SoftwareWorker.BYO.Integrations.Auth0.Model;
using SoftwareWorker.BYO.Integrations.Helpers;

namespace SoftwareWorker.BYO.Integrations.Auth0
{
    public class Auth0Connector
    {
        private readonly IAuth0API _api;

        public Auth0Connector(string domain, string accessToken, bool isVerbose)
        {
            var settings = RefitHelper.GetSettings(isVerbose, "Auth0");
            settings.AuthorizationHeaderValueGetter = (_, __) => ValueTask.FromResult($"Bearer {accessToken}");
            _api = RestService.For<IAuth0API>($"https://{domain}", settings);
        }

        public async Task<List<Auth0User>?> ListUsersAsync(int? maxResults = null)
        {
            var allUsers = new List<Auth0User>();
            int page = 0;
            const int perPage = 50;

            while (true)
            {
                var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                    async () => await _api.ListUsersAsync(perPage, page));

                if (result == null || result.Count == 0)
                    break;

                allUsers.AddRange(result);

                if (result.Count < perPage)
                    break;

                if (maxResults.HasValue && allUsers.Count >= maxResults.Value)
                {
                    allUsers = allUsers.Take(maxResults.Value).ToList();
                    break;
                }

                page++;
            }

            return allUsers;
        }

        public async Task<Auth0User?> GetUserAsync(string id)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetUserAsync(id));
            return result;
        }

        public async Task<Auth0User?> CreateUserAsync(Auth0UserCreateRequest request)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.CreateUserAsync(request));
            return result;
        }

        public async Task<Auth0User?> UpdateUserAsync(string id, Auth0UserUpdateRequest request)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.UpdateUserAsync(id, request));
            return result;
        }

        public async Task<bool> DeleteUserAsync(string id)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => { await _api.DeleteUserAsync(id); return new object(); });
            return result != null;
        }

        public async Task<List<Auth0Client>?> ListClientsAsync(int? maxResults = null)
        {
            var allClients = new List<Auth0Client>();
            int page = 0;
            const int perPage = 50;

            while (true)
            {
                var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                    async () => await _api.ListClientsAsync(perPage, page));

                if (result == null || result.Count == 0)
                    break;

                allClients.AddRange(result);

                if (result.Count < perPage)
                    break;

                if (maxResults.HasValue && allClients.Count >= maxResults.Value)
                {
                    allClients = allClients.Take(maxResults.Value).ToList();
                    break;
                }

                page++;
            }

            return allClients;
        }

        public async Task<Auth0Client?> GetClientAsync(string id)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetClientAsync(id));
            return result;
        }

        public async Task<Auth0Client?> CreateClientAsync(Auth0ClientCreateRequest request)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.CreateClientAsync(request));
            return result;
        }

        public async Task<Auth0Client?> UpdateClientAsync(string id, Auth0ClientUpdateRequest request)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.UpdateClientAsync(id, request));
            return result;
        }

        public async Task<bool> DeleteClientAsync(string id)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => { await _api.DeleteClientAsync(id); return new object(); });
            return result != null;
        }

        public async Task<List<Auth0Role>?> ListRolesAsync(int? maxResults = null)
        {
            var allRoles = new List<Auth0Role>();
            int page = 0;
            const int perPage = 50;

            while (true)
            {
                var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                    async () => await _api.ListRolesAsync(perPage, page));

                if (result == null || result.Roles == null || result.Roles.Count == 0)
                    break;

                allRoles.AddRange(result.Roles);

                if (result.Roles.Count < perPage)
                    break;

                if (maxResults.HasValue && allRoles.Count >= maxResults.Value)
                {
                    allRoles = allRoles.Take(maxResults.Value).ToList();
                    break;
                }

                page++;
            }

            return allRoles;
        }

        public async Task<Auth0Role?> GetRoleAsync(string id)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetRoleAsync(id));
            return result;
        }

        public async Task<Auth0Role?> CreateRoleAsync(Auth0RoleCreateRequest request)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.CreateRoleAsync(request));
            return result;
        }

        public async Task<Auth0Role?> UpdateRoleAsync(string id, Auth0RoleUpdateRequest request)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.UpdateRoleAsync(id, request));
            return result;
        }

        public async Task<bool> DeleteRoleAsync(string id)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => { await _api.DeleteRoleAsync(id); return new object(); });
            return result != null;
        }

        public async Task<List<Auth0Role>?> ListUserRolesAsync(string userId)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.ListUserRolesAsync(userId));
            return result;
        }

        public async Task<bool> AssignRolesToUserAsync(string userId, List<string> roleIds)
        {
            var request = new Auth0AssignRolesRequest { Roles = roleIds };
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => { await _api.AssignRolesToUserAsync(userId, request); return new object(); });
            return result != null;
        }

        public async Task<bool> RemoveRolesFromUserAsync(string userId, List<string> roleIds)
        {
            var request = new Auth0RemoveRolesRequest { Roles = roleIds };
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => { await _api.RemoveRolesFromUserAsync(userId, request); return new object(); });
            return result != null;
        }

        public async Task<List<Auth0Connection>?> ListConnectionsAsync(int? maxResults = null)
        {
            var allConnections = new List<Auth0Connection>();
            int page = 0;
            const int perPage = 50;

            while (true)
            {
                var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                    async () => await _api.ListConnectionsAsync(perPage, page));

                if (result == null || result.Count == 0)
                    break;

                allConnections.AddRange(result);

                if (result.Count < perPage)
                    break;

                if (maxResults.HasValue && allConnections.Count >= maxResults.Value)
                {
                    allConnections = allConnections.Take(maxResults.Value).ToList();
                    break;
                }

                page++;
            }

            return allConnections;
        }

        public async Task<Auth0Connection?> GetConnectionAsync(string id)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetConnectionAsync(id));
            return result;
        }

        public async Task<List<Auth0Log>?> ListLogsAsync(string? query = null, int? maxResults = null)
        {
            var allLogs = new List<Auth0Log>();
            int page = 0;
            const int perPage = 50;

            while (true)
            {
                var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                    async () => await _api.ListLogsAsync(perPage, page, query));

                if (result == null || result.Count == 0)
                    break;

                allLogs.AddRange(result);

                if (result.Count < perPage)
                    break;

                if (maxResults.HasValue && allLogs.Count >= maxResults.Value)
                {
                    allLogs = allLogs.Take(maxResults.Value).ToList();
                    break;
                }

                page++;
            }

            return allLogs;
        }
    }
}
