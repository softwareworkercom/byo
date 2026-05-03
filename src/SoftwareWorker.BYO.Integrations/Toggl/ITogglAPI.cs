using Refit;
using SoftwareWorker.BYO.Integrations.Toggl.Model;

namespace SoftwareWorker.BYO.Integrations.Toggl
{
    /// <summary>
    /// Toggl Track API v9 - https://developers.track.toggl.com/docs/
    /// </summary>
    public interface ITogglAPI
    {
        // Time Entry Operations
        [Get("/api/v9/me/time_entries")]
        Task<List<TogglTimeEntry>> ListTimeEntriesAsync([Query] string? start_date = null, [Query] string? end_date = null);

        [Get("/api/v9/me/time_entries/{time_entry_id}")]
        Task<TogglTimeEntry> GetTimeEntryAsync([AliasAs("time_entry_id")] long timeEntryId);

        [Post("/api/v9/workspaces/{workspace_id}/time_entries")]
        Task<TogglTimeEntry> CreateTimeEntryAsync([AliasAs("workspace_id")] long workspaceId, [Body] TogglTimeEntryRequest request);

        [Put("/api/v9/workspaces/{workspace_id}/time_entries/{time_entry_id}")]
        Task<TogglTimeEntry> UpdateTimeEntryAsync([AliasAs("workspace_id")] long workspaceId, [AliasAs("time_entry_id")] long timeEntryId, [Body] TogglTimeEntryRequest request);

        [Delete("/api/v9/workspaces/{workspace_id}/time_entries/{time_entry_id}")]
        Task DeleteTimeEntryAsync([AliasAs("workspace_id")] long workspaceId, [AliasAs("time_entry_id")] long timeEntryId);

        [Patch("/api/v9/workspaces/{workspace_id}/time_entries/{time_entry_id}/stop")]
        Task<TogglTimeEntry> StopTimeEntryAsync([AliasAs("workspace_id")] long workspaceId, [AliasAs("time_entry_id")] long timeEntryId);

        // Project Operations
        [Get("/api/v9/workspaces/{workspace_id}/projects")]
        Task<List<TogglProject>> ListProjectsAsync([AliasAs("workspace_id")] long workspaceId);

        [Get("/api/v9/workspaces/{workspace_id}/projects/{project_id}")]
        Task<TogglProject> GetProjectAsync([AliasAs("workspace_id")] long workspaceId, [AliasAs("project_id")] long projectId);

        [Post("/api/v9/workspaces/{workspace_id}/projects")]
        Task<TogglProject> CreateProjectAsync([AliasAs("workspace_id")] long workspaceId, [Body] TogglProjectRequest request);

        [Put("/api/v9/workspaces/{workspace_id}/projects/{project_id}")]
        Task<TogglProject> UpdateProjectAsync([AliasAs("workspace_id")] long workspaceId, [AliasAs("project_id")] long projectId, [Body] TogglProjectRequest request);

        [Delete("/api/v9/workspaces/{workspace_id}/projects/{project_id}")]
        Task DeleteProjectAsync([AliasAs("workspace_id")] long workspaceId, [AliasAs("project_id")] long projectId);

        // Client Operations
        [Get("/api/v9/workspaces/{workspace_id}/clients")]
        Task<List<TogglClient>> ListClientsAsync([AliasAs("workspace_id")] long workspaceId);

        [Get("/api/v9/workspaces/{workspace_id}/clients/{client_id}")]
        Task<TogglClient> GetClientAsync([AliasAs("workspace_id")] long workspaceId, [AliasAs("client_id")] long clientId);

        [Post("/api/v9/workspaces/{workspace_id}/clients")]
        Task<TogglClient> CreateClientAsync([AliasAs("workspace_id")] long workspaceId, [Body] TogglClientRequest request);

        [Put("/api/v9/workspaces/{workspace_id}/clients/{client_id}")]
        Task<TogglClient> UpdateClientAsync([AliasAs("workspace_id")] long workspaceId, [AliasAs("client_id")] long clientId, [Body] TogglClientRequest request);

        [Delete("/api/v9/workspaces/{workspace_id}/clients/{client_id}")]
        Task DeleteClientAsync([AliasAs("workspace_id")] long workspaceId, [AliasAs("client_id")] long clientId);

        // Tag Operations
        [Get("/api/v9/workspaces/{workspace_id}/tags")]
        Task<List<TogglTag>> ListTagsAsync([AliasAs("workspace_id")] long workspaceId);

        [Post("/api/v9/workspaces/{workspace_id}/tags")]
        Task<TogglTag> CreateTagAsync([AliasAs("workspace_id")] long workspaceId, [Body] TogglTagRequest request);

        [Put("/api/v9/workspaces/{workspace_id}/tags/{tag_id}")]
        Task<TogglTag> UpdateTagAsync([AliasAs("workspace_id")] long workspaceId, [AliasAs("tag_id")] long tagId, [Body] TogglTagRequest request);

        [Delete("/api/v9/workspaces/{workspace_id}/tags/{tag_id}")]
        Task DeleteTagAsync([AliasAs("workspace_id")] long workspaceId, [AliasAs("tag_id")] long tagId);

        // Workspace Operations
        [Get("/api/v9/workspaces")]
        Task<List<TogglWorkspace>> ListWorkspacesAsync();

        [Get("/api/v9/workspaces/{workspace_id}")]
        Task<TogglWorkspace> GetWorkspaceAsync([AliasAs("workspace_id")] long workspaceId);

        [Put("/api/v9/workspaces/{workspace_id}")]
        Task<TogglWorkspace> UpdateWorkspaceAsync([AliasAs("workspace_id")] long workspaceId, [Body] TogglWorkspaceRequest request);

        // User Operations
        [Get("/api/v9/me")]
        Task<TogglUser> GetCurrentUserAsync();

        [Put("/api/v9/me")]
        Task<TogglUser> UpdateCurrentUserAsync([Body] TogglUserRequest request);
    }
}
