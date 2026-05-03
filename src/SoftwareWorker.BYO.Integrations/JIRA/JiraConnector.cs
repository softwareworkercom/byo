using System.Text;
using Refit;
using SoftwareWorker.BYO.Integrations.Helpers;
using SoftwareWorker.BYO.Integrations.JIRA.Model;
using SoftwareWorker.BYO.Integrations.JIRA.Model.Request;
using SoftwareWorker.BYO.Integrations.JIRA.Model.Response;

namespace SoftwareWorker.BYO.Integrations.JIRA
{
    public class JiraConnector
    {
        Dictionary<string, string> _headers;
        IJiraAPI _api;

        public JiraConnector(string baseUrl, string user, string key, bool isVerbose)
        {
            var atlassianAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{key}"));

            _headers = new Dictionary<string, string> {
                                                            { "Authorization", $"Basic {atlassianAuth}" }
                                                      };

            var settings = RefitHelper.GetSettings(isVerbose, "Jira");
            _api = RestService.For<IJiraAPI>(baseUrl, settings);
        }

        public async Task<List<JiraIssue>> SearchIssuesAsync(string jql)
        {
            var allJiraIssues = new List<JiraIssue>();

            var jiraQueryResponse = new JiraIssueQueryResponse
            {
                IsLast = false,
                NextPageToken = null,
            };

            while (!jiraQueryResponse.IsLast)
            {
                var request = new JqlRequest
                {
                    Jql = jql,
                    MaxResults = 100,
                    Fields = ["summary", "status", "issuetype", "priority", "assignee", "reporter", "creator",
                              "project", "created", "updated", "resolution", "resolutiondate", "duedate", "labels", "components", "fixVersions",
                              "versions", "timeoriginalestimate", "timeestimate", "timespent", "progress", "description", "watches", "comment",
                              "customfield_10007", "customfield_10005", "subtasks", "parent", "issuelinks"],
                    Expand = "changelog",
                    NextPageToken = jiraQueryResponse?.NextPageToken
                };

                jiraQueryResponse = await _api.SearchIssuesAsync(_headers, request);

                allJiraIssues.AddRange(jiraQueryResponse.Issues);
            }

            return allJiraIssues;
        }

        public async Task<JiraUser> GetCurrentUserAsync()
        {
            return await _api.GetCurrentUserAsync(_headers);
        }

        public async Task<JiraIssueCreateResponse> CreateIssueAsync(JiraIssueCreateRequest jiraIssueCreateRequest)
        {
            var jiraResponse = await _api.CreateIssueAsync(_headers, jiraIssueCreateRequest);
            return jiraResponse;
        }

        public async Task UpdateIssueAssigneeAsync(string jiraId, JiraUser jiraUser)
        {
            await _api.UpdateIssueAssigneeAsync(_headers, jiraId, jiraUser);
        }
        public async Task<List<JiraSprint>> ListSprintsAsync(int boardId)
        {
            var allJiraSprints = new List<JiraSprint>();

            var increment = 50; //JIRA API max limit
            var startAt = 0;
            var finishAt = 1;

            while (startAt <= finishAt)
            {
                var response = await _api.ListSprintsAsync(_headers, boardId, startAt);
                startAt += increment;
                finishAt = (int) response.Total - 1;
                allJiraSprints.AddRange(response.Values);
            }

            return allJiraSprints;
        }


        public async Task<List<JiraRelease>> ListFixVersionsAsync(string projectId)
        {
            return await _api.ListFixVersionsAsync(_headers, projectId);
        }

        public async Task<List<JiraComment>> ListCommentsAsync(string issueId)
        {
            var allComments = new List<JiraComment>();
            var startAt = 0;
            var maxResults = 50;

            while (true)
            {
                var response = await _api.ListCommentsAsync(_headers, issueId);
                if (response?.Comments == null || response.Comments.Count == 0)
                    break;

                allComments.AddRange(response.Comments);

                if (startAt + response.Comments.Count >= response.total)
                    break;

                startAt += maxResults;
            }

            return allComments;
        }

        public async Task<List<JiraUser>> ListGroupMembersAsync(string groupName)
        {
            var allUsers = new List<JiraUser>();
            var startAt = 0;
            var maxResults = 50;

            while (true)
            {
                var response = await _api.ListGroupMembersAsync(_headers, groupName);
                if (response?.Users == null || response.Users.Length == 0)
                    break;

                allUsers.AddRange(response.Users);

                if (response.isLast)
                    break;

                startAt += maxResults;
            }

            return allUsers;
        }

        public async Task CreateCommentAsync(string issueId, string comment)
        {
            var body = $"{{\"body\":\"{comment}\"}}";
            await _api.CreateCommentAsync(_headers, issueId, body);
        }

        public async Task UpdateIssueAsync(string issueId, string body)
        {
            var request = new StringContent(body, Encoding.UTF8, "application/json");
            await _api.UpdateIssueAsync(_headers, issueId, request);
        }

        public async Task ChangeIssueFieldListAsync(string issueId, string fieldCollection, string fieldName, string fieldValue)
        {
            var body = @"{
                          ""update"": {
                            ""{fieldCollection}"": [
                              {
                                ""add"": {
                                  ""id"": ""fixVersionId""
                                }
                              }
                            ]
                          }
                        }
                        ";

            var request = new StringContent(body, Encoding.UTF8, "application/json");
            await _api.UpdateIssueAsync(_headers, issueId, request);
        }

        public async Task CreateWatcherAsync(string issueId, string accountId)
        {
            string watcherAccountId = $"\"{accountId}\"";
            var watcherAccountIdStringContent = new StringContent(watcherAccountId, Encoding.UTF8, "application/json");
            await _api.CreateWatcherAsync(_headers, issueId, watcherAccountIdStringContent);
        }

        public async Task<List<JiraComponentResponse>> ListComponentsAsync(string projectId)
        {
            var response = await _api.ListComponentsAsync(_headers, projectId);
            return response;
        }

        public async Task<JiraComponentResponse> CreateComponentAsync(ComponentRequest componentRequest)
        {
            var response = await _api.CreateComponentAsync(_headers, componentRequest);
            return response;
        }


        public async Task<JiraSprintCreateResponse> CreateSprintAsync(SprintCreateRequest sprintCreateRequest)
        {
            var response = await _api.CreateSprintAsync(_headers, sprintCreateRequest);
            return response;
        }

        public async Task<JiraTransitionsResponse> ListTransitionsAsync(string issueId)
        {
            return await _api.ListTransitionsAsync(_headers, issueId);
        }

        public async Task TransitionIssueAsync(string issueId, string transitionId)
        {
            var request = new JiraTransitionRequest
            {
                Transition = new JiraTransitionInfo { Id = transitionId }
            };
            await _api.TransitionIssueAsync(_headers, issueId, request);
        }

        public async Task<List<JiraAttachment>> ListAttachmentsAsync(string issueId)
        {
            return await _api.ListAttachmentsAsync(_headers, issueId);
        }

        public async Task<JiraServiceDeskRequest> GetServiceDeskRequestAsync(string issueIdOrKey)
        {
            return await _api.GetServiceDeskRequestAsync(_headers, issueIdOrKey);
        }

        public async Task CreateIssueLinkAsync(string inwardIssueKey, string outwardIssueKey, string linkTypeName = "Relates")
        {
            var request = new JiraIssueLink
            {
                Type = new JiraIssueLinkType { Name = linkTypeName },
                InwardIssue = new JiraLinkedIssue { Key = inwardIssueKey },
                OutwardIssue = new JiraLinkedIssue { Key = outwardIssueKey }
            };
            await _api.CreateIssueLinkAsync(_headers, request);
        }
    }
}
