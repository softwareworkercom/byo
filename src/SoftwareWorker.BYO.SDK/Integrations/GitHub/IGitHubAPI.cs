using Refit;
using SoftwareWorker.BYO.Integrations.GitHub.Model;

namespace SoftwareWorker.BYO.Integrations.GitHub
{
    /// <summary>
    /// https://docs.github.com/en/rest
    /// </summary>
    public interface IGitHubAPI
    {
        [Get("/repos/{organization}/{repository}/branches/{branchName}")]
        Task<GitHubBranch> GetBranch([HeaderCollection] IDictionary<string, string> headers, [AliasAs("organization")] string organization, [AliasAs("repository")] string repository, [AliasAs("branchName")] string branchName);

        [Get("/repos/{organization}/{repository}/commits?sha={branchName}&per_page={perPage}&page={page}")]
        Task<List<GitHubCommit>> GetCommitsPage([HeaderCollection] IDictionary<string, string> headers, [AliasAs("organization")] string organization, [AliasAs("repository")] string repository, [AliasAs("branchName")] string branchName, [AliasAs("perPage")] int perPage, [AliasAs("page")] int page);

        [Get("/search/issues?q=org:{organization}+is:pr+in:title,body+{query}")]
        Task<GitHubPullRequests> SearchPullRequests([HeaderCollection] IDictionary<string, string> headers, [AliasAs("organization")] string organization, [AliasAs("query")] string query);

        [Get("/repos/{organization}/{repository}")]
        Task<GitHubRepository> GetRepository([HeaderCollection] IDictionary<string, string> headers, [AliasAs("organization")] string organization, [AliasAs("repository")] string repository);

        [Get("/orgs/{organization}/repos?per_page={perPage}&page={page}")]
        Task<List<GitHubRepository>> ListRepositoriesPage([HeaderCollection] IDictionary<string, string> headers, [AliasAs("organization")] string organization, [AliasAs("perPage")] int perPage, [AliasAs("page")] int page);

        [Get("/repos/{organization}/{repository}/pulls/{pullNumber}")]
        Task<GitHubPullRequest> GetPullRequest([HeaderCollection] IDictionary<string, string> headers, [AliasAs("organization")] string organization, [AliasAs("repository")] string repository, [AliasAs("pullNumber")] int pullNumber);

        [Post("/repos/{organization}/{repository}/pulls")]
        Task<GitHubPullRequest> CreatePullRequest([HeaderCollection] IDictionary<string, string> headers, [AliasAs("organization")] string organization, [AliasAs("repository")] string repository, [Body] GitHubPullRequestCreateRequest request);

        [Patch("/repos/{organization}/{repository}/pulls/{pullNumber}")]
        Task<GitHubPullRequest> UpdatePullRequest([HeaderCollection] IDictionary<string, string> headers, [AliasAs("organization")] string organization, [AliasAs("repository")] string repository, [AliasAs("pullNumber")] int pullNumber, [Body] GitHubPullRequestCreateRequest request);

        [Put("/repos/{organization}/{repository}/pulls/{pullNumber}/merge")]
        Task MergePullRequest([HeaderCollection] IDictionary<string, string> headers, [AliasAs("organization")] string organization, [AliasAs("repository")] string repository, [AliasAs("pullNumber")] int pullNumber);

        [Get("/repos/{organization}/{repository}/issues/{issueNumber}")]
        Task<GitHubIssue> GetIssue([HeaderCollection] IDictionary<string, string> headers, [AliasAs("organization")] string organization, [AliasAs("repository")] string repository, [AliasAs("issueNumber")] int issueNumber);

        [Get("/repos/{organization}/{repository}/issues?per_page={perPage}&page={page}")]
        Task<List<GitHubIssue>> ListIssuesPage([HeaderCollection] IDictionary<string, string> headers, [AliasAs("organization")] string organization, [AliasAs("repository")] string repository, [AliasAs("perPage")] int perPage, [AliasAs("page")] int page);

        [Post("/repos/{organization}/{repository}/issues")]
        Task<GitHubIssue> CreateIssue([HeaderCollection] IDictionary<string, string> headers, [AliasAs("organization")] string organization, [AliasAs("repository")] string repository, [Body] GitHubIssueCreateRequest request);

        [Patch("/repos/{organization}/{repository}/issues/{issueNumber}")]
        Task<GitHubIssue> UpdateIssue([HeaderCollection] IDictionary<string, string> headers, [AliasAs("organization")] string organization, [AliasAs("repository")] string repository, [AliasAs("issueNumber")] int issueNumber, [Body] GitHubIssueCreateRequest request);

        [Get("/repos/{organization}/{repository}/releases?per_page={perPage}&page={page}")]
        Task<List<GitHubRelease>> ListReleasesPage([HeaderCollection] IDictionary<string, string> headers, [AliasAs("organization")] string organization, [AliasAs("repository")] string repository, [AliasAs("perPage")] int perPage, [AliasAs("page")] int page);

        [Get("/repos/{organization}/{repository}/actions/runs")]
        Task<GitHubWorkflowRuns> ListWorkflowRuns([HeaderCollection] IDictionary<string, string> headers, [AliasAs("organization")] string organization, [AliasAs("repository")] string repository);

        [Get("/repos/{organization}/{repository}/actions/runs/{runId}")]
        Task<GitHubWorkflowRun> GetWorkflowRun([HeaderCollection] IDictionary<string, string> headers, [AliasAs("organization")] string organization, [AliasAs("repository")] string repository, [AliasAs("runId")] long runId);

        //[Get("/repos/humanforce/hf-cli/commits?sha=HF-19503&author=leandromonaco")]
        //Task<GitHubPullRequests> GetPullRequestsByUserId([HeaderCollection] IDictionary<string, string> headers);


        //[Post("/graphql")]
        //Task<GraphQLResponse> RunGraphQLQuery([HeaderCollection] IDictionary<string, string> headers, [Body] GitHubGraphQLRequest request);
    }
}
