using Refit;
using SoftwareWorker.BYO.Integrations.Helpers;
using SoftwareWorker.BYO.Integrations.Toggl.Model;

namespace SoftwareWorker.BYO.Integrations.Toggl
{
    public class TogglConnector
    {
        private readonly ITogglAPI _api;

        public TogglConnector(string apiToken, bool isVerbose)
        {
            var settings = RefitHelper.GetSettings(isVerbose, "Toggl");
            settings.AuthorizationHeaderValueGetter = (_, __) => Task.FromResult($"Basic {Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{apiToken}:api_token"))}");
            _api = RestService.For<ITogglAPI>("https://api.track.toggl.com", settings);
        }

        public async Task<List<TogglTimeEntry>?> ListTimeEntriesAsync(string? startDate = null, string? endDate = null)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.ListTimeEntriesAsync(startDate, endDate));
            return result;
        }

        public async Task<TogglTimeEntry?> GetTimeEntryAsync(long timeEntryId)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetTimeEntryAsync(timeEntryId));
            return result;
        }

        public async Task<TogglTimeEntry?> CreateTimeEntryAsync(long workspaceId, string description, DateTime start, int duration, long? projectId = null, List<long>? tagIds = null)
        {
            var request = new TogglTimeEntryRequest
            {
                Description = description,
                Start = start.ToString("o"),
                Duration = duration,
                ProjectId = projectId,
                TagIds = tagIds,
                CreatedWith = "API"
            };

            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.CreateTimeEntryAsync(workspaceId, request));
            return result;
        }

        public async Task<TogglTimeEntry?> UpdateTimeEntryAsync(long workspaceId, long timeEntryId, string? description = null, DateTime? start = null, int? duration = null, long? projectId = null, List<long>? tagIds = null)
        {
            var request = new TogglTimeEntryRequest
            {
                Description = description,
                Start = start?.ToString("o"),
                Duration = duration,
                ProjectId = projectId,
                TagIds = tagIds
            };

            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.UpdateTimeEntryAsync(workspaceId, timeEntryId, request));
            return result;
        }

        public async Task<bool> DeleteTimeEntryAsync(long workspaceId, long timeEntryId)
        {
            try
            {
                await _api.DeleteTimeEntryAsync(workspaceId, timeEntryId);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<TogglTimeEntry?> StopTimeEntryAsync(long workspaceId, long timeEntryId)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.StopTimeEntryAsync(workspaceId, timeEntryId));
            return result;
        }

        public async Task<List<TogglProject>?> ListProjectsAsync(long workspaceId)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.ListProjectsAsync(workspaceId));
            return result;
        }

        public async Task<TogglProject?> GetProjectAsync(long workspaceId, long projectId)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetProjectAsync(workspaceId, projectId));
            return result;
        }

        public async Task<TogglProject?> CreateProjectAsync(long workspaceId, string name, long? clientId = null, string? color = null, bool isPrivate = true)
        {
            var request = new TogglProjectRequest
            {
                Name = name,
                ClientId = clientId,
                Color = color,
                IsPrivate = isPrivate
            };

            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.CreateProjectAsync(workspaceId, request));
            return result;
        }

        public async Task<TogglProject?> UpdateProjectAsync(long workspaceId, long projectId, string? name = null, long? clientId = null, string? color = null, bool? isPrivate = null, bool? active = null)
        {
            var request = new TogglProjectRequest
            {
                Name = name,
                ClientId = clientId,
                Color = color,
                IsPrivate = isPrivate,
                Active = active
            };

            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.UpdateProjectAsync(workspaceId, projectId, request));
            return result;
        }

        public async Task<bool> DeleteProjectAsync(long workspaceId, long projectId)
        {
            try
            {
                await _api.DeleteProjectAsync(workspaceId, projectId);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<TogglClient>?> ListClientsAsync(long workspaceId)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.ListClientsAsync(workspaceId));
            return result;
        }

        public async Task<TogglClient?> GetClientAsync(long workspaceId, long clientId)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetClientAsync(workspaceId, clientId));
            return result;
        }

        public async Task<TogglClient?> CreateClientAsync(long workspaceId, string name, string? notes = null)
        {
            var request = new TogglClientRequest
            {
                Name = name,
                Notes = notes
            };

            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.CreateClientAsync(workspaceId, request));
            return result;
        }

        public async Task<TogglClient?> UpdateClientAsync(long workspaceId, long clientId, string? name = null, string? notes = null)
        {
            var request = new TogglClientRequest
            {
                Name = name,
                Notes = notes
            };

            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.UpdateClientAsync(workspaceId, clientId, request));
            return result;
        }

        public async Task<bool> DeleteClientAsync(long workspaceId, long clientId)
        {
            try
            {
                await _api.DeleteClientAsync(workspaceId, clientId);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<TogglTag>?> ListTagsAsync(long workspaceId)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.ListTagsAsync(workspaceId));
            return result;
        }

        public async Task<TogglTag?> CreateTagAsync(long workspaceId, string name)
        {
            var request = new TogglTagRequest { Name = name };

            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.CreateTagAsync(workspaceId, request));
            return result;
        }

        public async Task<TogglTag?> UpdateTagAsync(long workspaceId, long tagId, string name)
        {
            var request = new TogglTagRequest { Name = name };

            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.UpdateTagAsync(workspaceId, tagId, request));
            return result;
        }

        public async Task<bool> DeleteTagAsync(long workspaceId, long tagId)
        {
            try
            {
                await _api.DeleteTagAsync(workspaceId, tagId);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<TogglWorkspace>?> ListWorkspacesAsync()
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.ListWorkspacesAsync());
            return result;
        }

        public async Task<TogglWorkspace?> GetWorkspaceAsync(long workspaceId)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetWorkspaceAsync(workspaceId));
            return result;
        }

        public async Task<TogglWorkspace?> UpdateWorkspaceAsync(long workspaceId, string? name = null)
        {
            var request = new TogglWorkspaceRequest { Name = name };

            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.UpdateWorkspaceAsync(workspaceId, request));
            return result;
        }

        public async Task<TogglUser?> GetCurrentUserAsync()
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetCurrentUserAsync());
            return result;
        }

        public async Task<TogglUser?> UpdateCurrentUserAsync(string? fullname = null, string? email = null, string? timezone = null)
        {
            var request = new TogglUserRequest
            {
                Fullname = fullname,
                Email = email,
                Timezone = timezone
            };

            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.UpdateCurrentUserAsync(request));
            return result;
        }
    }
}
