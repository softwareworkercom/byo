using Refit;
using SoftwareWorker.BYO.Integrations.GitHub.Model;
using SoftwareWorker.BYO.Integrations.Helpers;

namespace SoftwareWorker.BYO.Integrations.GitHub
{
    public class GitHubConnector
    {
        private readonly IGitHubAPI _api;
        private readonly Dictionary<string, string> _headers;

        public GitHubConnector(string token, bool isVerbose)
        {
            _headers = new Dictionary<string, string> {
                                                            { "Authorization", $"Bearer {token}" },
                                                            { "User-Agent", $"EngMgrCli" } //https://docs.github.com/en/rest/overview/resources-in-the-rest-api#user-agent-required
                                                      };

            RefitSettings settings = RefitHelper.GetSettings(isVerbose, "GitHub");
            _api = RestService.For<IGitHubAPI>("https://api.github.com", settings);
        }

        public async Task<GitHubBranch?> GetBranch(string organization, string repository, string jiraIssueId)
        {
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetBranch(_headers, organization, repository, jiraIssueId));
        }

        public async Task<List<GitHubCommit>?> GetCommits(string organization, string repository, string branchName)
        {
            var allCommits = new List<GitHubCommit>();
            var page = 1;
            var perPage = 100;

            while (true)
            {
                var commits = await ResilienceHelper.ExecuteWithResilienceAsync(
                    async () => await _api.GetCommitsPage(_headers, organization, repository, branchName, perPage, page));

                if (commits == null || commits.Count == 0)
                {
                    break;
                }

                allCommits.AddRange(commits);

                if (commits.Count < perPage)
                {
                    break;
                }

                page++;
            }

            return allCommits.Count > 0 ? allCommits : null;
        }

        public async Task<List<GitHubPullRequest>?> SearchPullRequests(string organization, string query)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.SearchPullRequests(_headers, organization, query));
            return result?.items.ToList();
        }

        public async Task<GitHubRepository?> GetRepository(string organization, string repository)
        {
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetRepository(_headers, organization, repository));
        }

        public async Task<List<GitHubRepository>?> ListRepositories(string organization)
        {
            var allRepositories = new List<GitHubRepository>();
            var page = 1;
            var perPage = 100;

            while (true)
            {
                var repositories = await ResilienceHelper.ExecuteWithResilienceAsync(
                    async () => await _api.ListRepositoriesPage(_headers, organization, perPage, page));

                if (repositories == null || repositories.Count == 0)
                {
                    break;
                }

                allRepositories.AddRange(repositories);

                if (repositories.Count < perPage)
                {
                    break;
                }

                page++;
            }

            return allRepositories.Count > 0 ? allRepositories : null;
        }

        public async Task<GitHubPullRequest?> GetPullRequest(string organization, string repository, int pullNumber)
        {
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetPullRequest(_headers, organization, repository, pullNumber));
        }

        public async Task<GitHubPullRequest?> CreatePullRequest(string organization, string repository, string title, string body, string head, string baseRef)
        {
            var request = new GitHubPullRequestCreateRequest
            {
                Title = title,
                Body = body,
                Head = head,
                Base = baseRef
            };
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.CreatePullRequest(_headers, organization, repository, request));
        }

        public async Task<GitHubPullRequest?> UpdatePullRequest(string organization, string repository, int pullNumber, string title, string body)
        {
            var request = new GitHubPullRequestCreateRequest
            {
                Title = title,
                Body = body
            };
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.UpdatePullRequest(_headers, organization, repository, pullNumber, request));
        }

        public async Task<bool> MergePullRequest(string organization, string repository, int pullNumber)
        {
            try
            {
                await ResilienceHelper.ExecuteWithResilienceAsync(
                    async () =>
                    {
                        await _api.MergePullRequest(_headers, organization, repository, pullNumber);
                        return new object();
                    });
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<GitHubIssue?> GetIssue(string organization, string repository, int issueNumber)
        {
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetIssue(_headers, organization, repository, issueNumber));
        }

        public async Task<List<GitHubIssue>?> ListIssues(string organization, string repository)
        {
            var allIssues = new List<GitHubIssue>();
            var page = 1;
            var perPage = 100;

            while (true)
            {
                var issues = await ResilienceHelper.ExecuteWithResilienceAsync(
                    async () => await _api.ListIssuesPage(_headers, organization, repository, perPage, page));

                if (issues == null || issues.Count == 0)
                {
                    break;
                }

                allIssues.AddRange(issues);

                if (issues.Count < perPage)
                {
                    break;
                }

                page++;
            }

            return allIssues.Count > 0 ? allIssues : null;
        }

        public async Task<GitHubIssue?> CreateIssue(string organization, string repository, string title, string body, string[]? assignees = null, string[]? labels = null)
        {
            var request = new GitHubIssueCreateRequest
            {
                Title = title,
                Body = body,
                Assignees = assignees,
                Labels = labels
            };
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.CreateIssue(_headers, organization, repository, request));
        }

        public async Task<GitHubIssue?> UpdateIssue(string organization, string repository, int issueNumber, string title, string body)
        {
            var request = new GitHubIssueCreateRequest
            {
                Title = title,
                Body = body
            };
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.UpdateIssue(_headers, organization, repository, issueNumber, request));
        }

        public async Task<List<GitHubRelease>?> ListReleases(string organization, string repository)
        {
            var allReleases = new List<GitHubRelease>();
            var page = 1;
            var perPage = 100;

            while (true)
            {
                var releases = await ResilienceHelper.ExecuteWithResilienceAsync(
                    async () => await _api.ListReleasesPage(_headers, organization, repository, perPage, page));

                if (releases == null || releases.Count == 0)
                {
                    break;
                }

                allReleases.AddRange(releases);

                if (releases.Count < perPage)
                {
                    break;
                }

                page++;
            }

            return allReleases.Count > 0 ? allReleases : null;
        }

        public async Task<GitHubWorkflowRuns?> ListWorkflowRuns(string organization, string repository)
        {
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.ListWorkflowRuns(_headers, organization, repository));
        }

        public async Task<GitHubWorkflowRun?> GetWorkflowRun(string organization, string repository, long runId)
        {
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetWorkflowRun(_headers, organization, repository, runId));
        }
    }
}
