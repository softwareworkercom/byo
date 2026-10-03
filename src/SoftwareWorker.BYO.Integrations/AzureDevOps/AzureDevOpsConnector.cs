using Refit;
using SoftwareWorker.BYO.Integrations.AzureDevOps.Model;
using SoftwareWorker.BYO.Integrations.Helpers;
using System.Text;

namespace SoftwareWorker.BYO.Integrations.AzureDevOps
{
    public class AzureDevOpsConnector
    {
        private readonly Dictionary<string, string> _headers;
        private readonly IAzureDevOps _api;
        private readonly string _organization;
        private readonly string _project;

        public AzureDevOpsConnector(string organization, string project, string personalAccessToken, bool isVerbose)
        {
            var baseUrl = "https://dev.azure.com";
            _organization = organization;
            _project = project;

            var pat = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{personalAccessToken}"));

            _headers = new Dictionary<string, string> {
                                                            { "Authorization", $"Basic {pat}"}
                                                      };

            RefitSettings settings = RefitHelper.GetSettings(isVerbose);
            _api = RestService.For<IAzureDevOps>(baseUrl, settings);
        }

        public async Task<List<AzureDevOpsPipeline>?> ListPipelines(int? maxItems = null)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.ListPipelines(_headers, _organization, _project));
            var items = result?.value.ToList();
            return maxItems.HasValue && items != null ? items.Take(maxItems.Value).ToList() : items;
        }

        public async Task<AzureDevOpsPipeline?> GetPipeline(int pipelineId)
        {
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetPipeline(_headers, _organization, _project, pipelineId));
        }

        public async Task<List<AzureDevOpsBuild>?> ListBuilds(int buildId, string branch, string statusFilter, int? maxItems = null)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.ListBuilds(_headers, _organization, _project, buildId, branch, statusFilter));
            var items = result?.value.OrderByDescending(b => b.queueTime).ToList();
            return maxItems.HasValue && items != null ? items.Take(maxItems.Value).ToList() : items;
        }


        public async Task<List<AzureDevOpsBuild>?> ListBuildsByResult(string resultFilter, string branchName, int top = 20)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.ListBuildsByResult(_headers, _organization, _project, resultFilter, branchName, top));
            var items = result?.value.OrderByDescending(b => b.finishTime).ToList();
            return items;
        }

        public async Task<List<AzureDevOpsBuild>?> RunBuild(int pipelineId, int runId, string branch, string commitId, List<string> stagesToSkip)
        {
            var body = new AzureDevOpsRunBody
            {
                previewRun = false,
                resources = new AzureDevOpsResources
                {
                    pipelines = new AzureDevOpsPipelines
                    {
                        self = new AzureDevOpsPipelineSelf()
                        {
                            runId = runId
                        }
                    },
                    repositories = new AzureDevOpsRepositories
                    {
                        self = new AzureDevOpsRepositoriesSelf()
                        {
                            refName = branch,
                            version = commitId
                        }
                    }
                },
                stagesToSkip = stagesToSkip.ToArray()
            };

            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.RunBuild(_headers, _organization, _project, pipelineId, body));
            return result?.value.OrderByDescending(b => b.queueTime).ToList();
        }


        public async Task<List<AzureDevOpsRun>?> ListRuns(int pipelineId, int? maxItems = null)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.ListRuns(_headers, _organization, _project, pipelineId));
            var items = result?.value.OrderByDescending(b => b.createdDate).ToList();
            return maxItems.HasValue && items != null ? items.Take(maxItems.Value).ToList() : items;
        }

        public async Task<AzureDevOpsRun?> GetRun(int pipelineId, int runId)
        {
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetRun(_headers, _organization, _project, pipelineId, runId));
        }

        public async Task<AzureDevOpsCheckQueryResponse?> QueryChecks(AzureDevOpsCheckQueryRequest request)
        {
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.QueryChecks(_headers, _organization, _project, request));
        }

        public async Task<List<AzureDevOpsApprovalItem>?> ListApprovals(int runId)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.ListApprovals(_headers, _organization, _project, runId));
            return result?.Value.ToList();
        }

        public async Task<AzureDevOpsApprovalResponse?> UpdateApproval(int approvalId, string status, string comment)
        {
            var request = new AzureDevOpsApprovalRequest
            {
                Status = status,
                Comment = comment
            };
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.UpdateApproval(_headers, _organization, _project, approvalId, request));
        }

        public async Task<AzureDevOpsWorkItem?> GetWorkItem(int id)
        {
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetWorkItem(_headers, _organization, _project, id));
        }

        public async Task<AzureDevOpsWorkItem?> CreateWorkItem(string type, List<AzureDevOpsWorkItemOperation> operations)
        {
            var headers = new Dictionary<string, string>(_headers)
            {
                ["Content-Type"] = "application/json-patch+json"
            };
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.CreateWorkItem(headers, _organization, _project, type, operations));
        }

        public async Task<AzureDevOpsWorkItem?> UpdateWorkItem(int id, List<AzureDevOpsWorkItemOperation> operations)
        {
            var headers = new Dictionary<string, string>(_headers)
            {
                ["Content-Type"] = "application/json-patch+json"
            };
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.UpdateWorkItem(headers, _organization, _project, id, operations));
        }

        public async Task<List<AzureDevOpsPullRequest>?> ListPullRequests(string repositoryId, int? maxItems = null)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.ListPullRequests(_headers, _organization, _project, repositoryId));
            var items = result?.Value.ToList();
            return maxItems.HasValue && items != null ? items.Take(maxItems.Value).ToList() : items;
        }

        public async Task<AzureDevOpsPullRequest?> GetPullRequest(string repositoryId, int pullRequestId)
        {
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetPullRequest(_headers, _organization, _project, repositoryId, pullRequestId));
        }

        public async Task<AzureDevOpsPullRequest?> CreatePullRequest(string repositoryId, string sourceRefName, string targetRefName, string title, string description)
        {
            var request = new AzureDevOpsPullRequestCreateRequest
            {
                SourceRefName = sourceRefName,
                TargetRefName = targetRefName,
                Title = title,
                Description = description
            };
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.CreatePullRequest(_headers, _organization, _project, repositoryId, request));
        }

        public async Task<List<AzureDevOpsFeed>?> ListFeeds(int? maxItems = null)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.ListFeeds(_headers, _organization));
            var items = result?.Value.ToList();
            return maxItems.HasValue && items != null ? items.Take(maxItems.Value).ToList() : items;
        }

        public async Task<List<AzureDevOpsFeed>?> ListProjectFeeds(int? maxItems = null)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.ListProjectFeeds(_headers, _organization, _project));
            var items = result?.Value.ToList();
            return maxItems.HasValue && items != null ? items.Take(maxItems.Value).ToList() : items;
        }

        public async Task<AzureDevOpsFeed?> GetFeed(string feedId)
        {
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetFeed(_headers, _organization, feedId));
        }

        public async Task<AzureDevOpsFeed?> GetProjectFeed(string feedId)
        {
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetProjectFeed(_headers, _organization, _project, feedId));
        }

        public async Task<AzureDevOpsFeed?> CreateFeed(string name, string description = "")
        {
            var request = new AzureDevOpsFeedCreateRequest
            {
                Name = name,
                Description = description,
                HideDeletedPackageVersions = true
            };
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.CreateFeed(_headers, _organization, request));
        }

        public async Task<AzureDevOpsFeed?> CreateProjectFeed(string name, string description = "")
        {
            var request = new AzureDevOpsFeedCreateRequest
            {
                Name = name,
                Description = description,
                HideDeletedPackageVersions = true
            };
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.CreateProjectFeed(_headers, _organization, _project, request));
        }

        public async Task<List<AzureDevOpsPackage>?> ListPackages(string feedId, int? maxItems = null)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.ListPackages(_headers, _organization, feedId));
            var items = result?.Value.ToList();
            return maxItems.HasValue && items != null ? items.Take(maxItems.Value).ToList() : items;
        }

        public async Task<List<AzureDevOpsPackage>?> ListProjectPackages(string feedId, int? maxItems = null)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.ListProjectPackages(_headers, _organization, _project, feedId));
            var items = result?.Value.ToList();
            return maxItems.HasValue && items != null ? items.Take(maxItems.Value).ToList() : items;
        }

        public async Task<AzureDevOpsPackage?> GetPackage(string feedId, string packageId)
        {
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetPackage(_headers, _organization, feedId, packageId));
        }

        public async Task<AzureDevOpsPackage?> GetProjectPackage(string feedId, string packageId)
        {
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetProjectPackage(_headers, _organization, _project, feedId, packageId));
        }

    }
}
