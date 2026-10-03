using Refit;
using SoftwareWorker.BYO.Integrations.JIRA.Model;
using SoftwareWorker.BYO.Integrations.JIRA.Model.Request;
using SoftwareWorker.BYO.Integrations.JIRA.Model.Response;

namespace SoftwareWorker.BYO.Integrations.JIRA
{
    /// <summary>
    /// https://developer.atlassian.com/cloud/jira/platform/rest/v3/intro/
    /// </summary>
    internal interface IJiraAPI
    {
        [Post("/rest/api/3/search/jql")]
        Task<JiraIssueQueryResponse> SearchIssuesAsync([HeaderCollection] IDictionary<string, string> headers, [Body] JqlRequest request);

        [Get("/rest/api/3/myself")]
        Task<JiraUser> GetCurrentUserAsync([HeaderCollection] IDictionary<string, string> headers);

        [Get("/rest/api/3/project/{projectId}/versions")]
        Task<List<JiraRelease>> ListFixVersionsAsync([HeaderCollection] IDictionary<string, string> headers, [AliasAs("projectId")] string projectId);

        [Get("/rest/agile/latest/board/{boardId}/sprint?startAt={startAt}")]
        Task<JiraSprintQueryResponse> ListSprintsAsync([HeaderCollection] IDictionary<string, string> headers, [AliasAs("boardId")] int boardId, [AliasAs("startAt")] int startAt);

        [Post("/rest/agile/1.0/sprint")]
        Task<JiraSprintCreateResponse> CreateSprintAsync([HeaderCollection] IDictionary<string, string> headers, [Body] SprintCreateRequest sprintCreateRequest);

        [Get("/rest/api/3/issue/{issueId}/comment")]
        Task<JiraCommentResponse> ListCommentsAsync([HeaderCollection] IDictionary<string, string> headers, [AliasAs("issueId")] string issueId);

        [Get("/rest/api/3/group/member?groupname={groupName}")]
        Task<JiraGroupMembersResponse> ListGroupMembersAsync([HeaderCollection] IDictionary<string, string> headers, [AliasAs("groupName")] string groupName);

        [Post("/rest/api/3/issue/{issueId}/comment")]
        Task CreateCommentAsync([HeaderCollection] IDictionary<string, string> headers, [AliasAs("issueId")] string issueId, [Body] string request);

        [Post("/rest/api/3/issue")]
        Task<JiraIssueCreateResponse> CreateIssueAsync([HeaderCollection] IDictionary<string, string> headers, [Body] JiraIssueCreateRequest request);

        [Put("/rest/api/3/issue/{issueId}/assignee")]
        Task UpdateIssueAssigneeAsync([HeaderCollection] IDictionary<string, string> headers, [AliasAs("issueId")] string issueId, [Body] JiraUser jiraUser);

        [Put("/rest/api/3/issue/{issueId}")]
        Task UpdateIssueAsync([HeaderCollection] IDictionary<string, string> headers, [AliasAs("issueId")] string issueId, [Body] StringContent request);

        [Post("/rest/api/3/issue/{issueId}/watchers")]
        Task CreateWatcherAsync([HeaderCollection] IDictionary<string, string> headers, [AliasAs("issueId")] string issueId, [Body] StringContent accountId);

        [Post("/rest/api/3/component")]
        Task<JiraComponentResponse> CreateComponentAsync([HeaderCollection] IDictionary<string, string> headers, [Body] ComponentRequest componentRequest);

        [Put("/rest/api/3/component/{componentId}")]
        Task<JiraComponentResponse> UpdateComponentAsync([HeaderCollection] IDictionary<string, string> headers, [AliasAs("componentId")] string componentId, [Body] ComponentRequest componentRequest);

        [Get("/rest/api/3/project/{projectId}/components")]
        Task<List<JiraComponentResponse>> ListComponentsAsync([HeaderCollection] IDictionary<string, string> headers, [AliasAs("projectId")] string projectId);

        [Get("/rest/agile/1.0/board")]
        Task<List<JiraComponentResponse>> ListBoardsAsync([HeaderCollection] IDictionary<string, string> headers);

        [Get("/rest/api/3/issue/{issueId}/transitions")]
        Task<JiraTransitionsResponse> ListTransitionsAsync([HeaderCollection] IDictionary<string, string> headers, [AliasAs("issueId")] string issueId);

        [Post("/rest/api/3/issue/{issueId}/transitions")]
        Task TransitionIssueAsync([HeaderCollection] IDictionary<string, string> headers, [AliasAs("issueId")] string issueId, [Body] JiraTransitionRequest request);

        [Get("/rest/api/3/issue/{issueId}/attachments")]
        Task<List<JiraAttachment>> ListAttachmentsAsync([HeaderCollection] IDictionary<string, string> headers, [AliasAs("issueId")] string issueId);

        [Post("/rest/api/3/issueLink")]
        Task CreateIssueLinkAsync([HeaderCollection] IDictionary<string, string> headers, [Body] JiraIssueLink request);

        [Get("/rest/servicedeskapi/request/{issueIdOrKey}?expand=requestType,serviceDesk,comment")]
        Task<JiraServiceDeskRequest> GetServiceDeskRequestAsync([HeaderCollection] IDictionary<string, string> headers, [AliasAs("issueIdOrKey")] string issueIdOrKey);
    }
}
