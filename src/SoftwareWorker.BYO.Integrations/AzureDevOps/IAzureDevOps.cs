using Refit;
using SoftwareWorker.BYO.Integrations.AzureDevOps.Model;

namespace SoftwareWorker.BYO.Integrations.AzureDevOps
{
    /// <summary>
    /// https://learn.microsoft.com/en-us/rest/api/azure/devops/
    /// </summary>
    public interface IAzureDevOps
    {
        [Get("/{organization}/{project}/_apis/pipelines?api-version=7.1-preview.1")]
        Task<AzureDevOpsPipelines> ListPipelines([HeaderCollection] IDictionary<string, string> headers, [AliasAs("organization")] string organization, [AliasAs("project")] string project);

        [Get("/{organization}/{project}/_apis/pipelines/{pipelineId}?api-version=7.1-preview.1")]
        Task<AzureDevOpsPipeline> GetPipeline([HeaderCollection] IDictionary<string, string> headers, [AliasAs("organization")] string organization, [AliasAs("project")] string project, int pipelineId);

        [Get("/{organization}/{project}/_apis/build/builds?definitions={buildId}&branchName={branchName}&statusFilter={statusFilter}&api-version=7.1-preview.7")]
        Task<AzureDevOpsBuilds> ListBuilds([HeaderCollection] IDictionary<string, string> headers, [AliasAs("organization")] string organization, [AliasAs("project")] string project, [AliasAs("buildId")] int buildId, [AliasAs("branchName")] string branchName, [AliasAs("statusFilter")] string statusFilter);

        [Get("/{organization}/{project}/_apis/build/builds?statusFilter=completed&resultFilter={resultFilter}&branchName={branchName}&$top={top}&api-version=7.1-preview.7")]
        Task<AzureDevOpsBuilds> ListBuildsByResult([HeaderCollection] IDictionary<string, string> headers, [AliasAs("organization")] string organization, [AliasAs("project")] string project, [AliasAs("resultFilter")] string resultFilter, [AliasAs("branchName")] string branchName, [AliasAs("top")] int top);

        [Get("/{organization}/{project}/_apis/pipelines/{pipelineId}/runs?api-version=7.2-preview.1")]
        Task<AzureDevOpsRuns> ListRuns([HeaderCollection] IDictionary<string, string> headers, [AliasAs("organization")] string organization, [AliasAs("project")] string project, [AliasAs("pipelineId")] int pipelineId);

        [Get("/{organization}/{project}/_apis/pipelines/{pipelineId}/runs/{runId}?api-version=7.1-preview.1")]
        Task<AzureDevOpsRun> GetRun([HeaderCollection] IDictionary<string, string> headers, [AliasAs("organization")] string organization, [AliasAs("project")] string project, [AliasAs("pipelineId")] int pipelineId, [AliasAs("runId")] int runId);

        [Post("/{organization}/{project}/_apis/pipelines/{pipelineId}/runs?api-version=7.1-preview.1")]
        Task<AzureDevOpsBuilds> RunBuild([HeaderCollection] IDictionary<string, string> headers, [AliasAs("organization")] string organization, [AliasAs("project")] string project, [AliasAs("pipelineId")] int pipelineId, [Body] AzureDevOpsRunBody body);

        [Post("/{organization}/{project}/_apis/pipelines/checks/query?api-version=7.1-preview.1")]
        Task<AzureDevOpsCheckQueryResponse> QueryChecks([HeaderCollection] IDictionary<string, string> headers, [AliasAs("organization")] string organization, [AliasAs("project")] string project, [Body] AzureDevOpsCheckQueryRequest request);

        [Get("/{organization}/{project}/_apis/pipelines/approvals?runId={runId}&state=pending&api-version=7.1-preview.1")]
        Task<AzureDevOpsApprovalsList> ListApprovals([HeaderCollection] IDictionary<string, string> headers, [AliasAs("organization")] string organization, [AliasAs("project")] string project, [AliasAs("runId")] int runId);

        [Patch("/{organization}/{project}/_apis/pipelines/checks/approvals/{approvalId}?api-version=7.1-preview.1")]
        Task<AzureDevOpsApprovalResponse> UpdateApproval([HeaderCollection] IDictionary<string, string> headers, [AliasAs("organization")] string organization, [AliasAs("project")] string project, [AliasAs("approvalId")] int approvalId, [Body] AzureDevOpsApprovalRequest request);

        [Get("/{organization}/{project}/_apis/wit/workitems/{id}?api-version=7.1")]
        Task<AzureDevOpsWorkItem> GetWorkItem([HeaderCollection] IDictionary<string, string> headers, [AliasAs("organization")] string organization, [AliasAs("project")] string project, [AliasAs("id")] int id);

        [Patch("/{organization}/{project}/_apis/wit/workitems/{type}?api-version=7.1")]
        Task<AzureDevOpsWorkItem> CreateWorkItem([HeaderCollection] IDictionary<string, string> headers, [AliasAs("organization")] string organization, [AliasAs("project")] string project, [AliasAs("type")] string type, [Body] List<AzureDevOpsWorkItemOperation> operations);

        [Patch("/{organization}/{project}/_apis/wit/workitems/{id}?api-version=7.1")]
        Task<AzureDevOpsWorkItem> UpdateWorkItem([HeaderCollection] IDictionary<string, string> headers, [AliasAs("organization")] string organization, [AliasAs("project")] string project, [AliasAs("id")] int id, [Body] List<AzureDevOpsWorkItemOperation> operations);

        [Get("/{organization}/{project}/_apis/git/repositories/{repositoryId}/pullrequests?api-version=7.1")]
        Task<AzureDevOpsPullRequests> ListPullRequests([HeaderCollection] IDictionary<string, string> headers, [AliasAs("organization")] string organization, [AliasAs("project")] string project, [AliasAs("repositoryId")] string repositoryId);

        [Get("/{organization}/{project}/_apis/git/repositories/{repositoryId}/pullrequests/{pullRequestId}?api-version=7.1")]
        Task<AzureDevOpsPullRequest> GetPullRequest([HeaderCollection] IDictionary<string, string> headers, [AliasAs("organization")] string organization, [AliasAs("project")] string project, [AliasAs("repositoryId")] string repositoryId, [AliasAs("pullRequestId")] int pullRequestId);

        [Post("/{organization}/{project}/_apis/git/repositories/{repositoryId}/pullrequests?api-version=7.1")]
        Task<AzureDevOpsPullRequest> CreatePullRequest([HeaderCollection] IDictionary<string, string> headers, [AliasAs("organization")] string organization, [AliasAs("project")] string project, [AliasAs("repositoryId")] string repositoryId, [Body] AzureDevOpsPullRequestCreateRequest request);

        [Get("/{organization}/_apis/packaging/feeds?api-version=7.1-preview.1")]
        Task<AzureDevOpsFeeds> ListFeeds([HeaderCollection] IDictionary<string, string> headers, [AliasAs("organization")] string organization);

        [Get("/{organization}/{project}/_apis/packaging/feeds?api-version=7.1-preview.1")]
        Task<AzureDevOpsFeeds> ListProjectFeeds([HeaderCollection] IDictionary<string, string> headers, [AliasAs("organization")] string organization, [AliasAs("project")] string project);

        [Get("/{organization}/_apis/packaging/feeds/{feedId}?api-version=7.1-preview.1")]
        Task<AzureDevOpsFeed> GetFeed([HeaderCollection] IDictionary<string, string> headers, [AliasAs("organization")] string organization, [AliasAs("feedId")] string feedId);

        [Get("/{organization}/{project}/_apis/packaging/feeds/{feedId}?api-version=7.1-preview.1")]
        Task<AzureDevOpsFeed> GetProjectFeed([HeaderCollection] IDictionary<string, string> headers, [AliasAs("organization")] string organization, [AliasAs("project")] string project, [AliasAs("feedId")] string feedId);

        [Post("/{organization}/_apis/packaging/feeds?api-version=7.1-preview.1")]
        Task<AzureDevOpsFeed> CreateFeed([HeaderCollection] IDictionary<string, string> headers, [AliasAs("organization")] string organization, [Body] AzureDevOpsFeedCreateRequest request);

        [Post("/{organization}/{project}/_apis/packaging/feeds?api-version=7.1-preview.1")]
        Task<AzureDevOpsFeed> CreateProjectFeed([HeaderCollection] IDictionary<string, string> headers, [AliasAs("organization")] string organization, [AliasAs("project")] string project, [Body] AzureDevOpsFeedCreateRequest request);

        [Get("/{organization}/_apis/packaging/feeds/{feedId}/packages?api-version=7.1-preview.1")]
        Task<AzureDevOpsPackages> ListPackages([HeaderCollection] IDictionary<string, string> headers, [AliasAs("organization")] string organization, [AliasAs("feedId")] string feedId);

        [Get("/{organization}/{project}/_apis/packaging/feeds/{feedId}/packages?api-version=7.1-preview.1")]
        Task<AzureDevOpsPackages> ListProjectPackages([HeaderCollection] IDictionary<string, string> headers, [AliasAs("organization")] string organization, [AliasAs("project")] string project, [AliasAs("feedId")] string feedId);

        [Get("/{organization}/_apis/packaging/feeds/{feedId}/packages/{packageId}?api-version=7.1-preview.1")]
        Task<AzureDevOpsPackage> GetPackage([HeaderCollection] IDictionary<string, string> headers, [AliasAs("organization")] string organization, [AliasAs("feedId")] string feedId, [AliasAs("packageId")] string packageId);

        [Get("/{organization}/{project}/_apis/packaging/feeds/{feedId}/packages/{packageId}?api-version=7.1-preview.1")]
        Task<AzureDevOpsPackage> GetProjectPackage([HeaderCollection] IDictionary<string, string> headers, [AliasAs("organization")] string organization, [AliasAs("project")] string project, [AliasAs("feedId")] string feedId, [AliasAs("packageId")] string packageId);

    }
}
