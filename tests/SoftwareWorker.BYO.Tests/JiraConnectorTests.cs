using SoftwareWorker.BYO.Integrations.JIRA;
using SoftwareWorker.BYO.Integrations.JIRA.Model;
using SoftwareWorker.BYO.Integrations.JIRA.Model.Request;
using SoftwareWorker.BYO.Integrations.Turso;
using SoftwareWorker.BYO.Tests.Support;
using System.Text;
using System.Text.Json;

namespace SoftwareWorker.BYO.Tests;

public sealed class JiraConnectorTests
{
    private static readonly string ExpectedAuthorization = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("user:token"));

    private static JiraConnector CreateConnector(LocalHttpServer server) => new(server.BaseUrl, "user", "token", isVerbose: false);

    [Fact]
    public async Task SearchIssues_ShouldFollowPaginationUntilTheLastPage()
    {
        using var server = new LocalHttpServer(request =>
        {
            if (request.Method != "POST" || request.PathAndQuery != "/rest/api/3/search/jql")
            {
                return LocalHttpResponse.NotFound();
            }

            using var body = JsonDocument.Parse(request.Body);
            var isSecondPage = body.RootElement.TryGetProperty("nextPageToken", out var token) && token.GetString() == "page-2";

            return LocalHttpResponse.Json(isSecondPage
                ? """{"issues":[{"key":"A-2","fields":{"summary":"Second","created":"2024-01-03T00:00:00"}}],"isLast":true}"""
                : """
                  {"issues":[{"key":"A-1","fields":{"summary":"First","status":{"name":"Open"},"created":"2024-01-02T03:04:05",
                   "duedate":"2024-02-03","customfield_10005":3,"labels":["x"],"assignee":{"displayName":"Dev","accountId":"a1"}}}],
                   "nextPageToken":"page-2","isLast":false}
                  """);
        });
        var connector = CreateConnector(server);

        var issues = await connector.SearchIssuesAsync("project = A");

        Assert.Equal(["A-1", "A-2"], issues.Select(issue => issue.Key));
        Assert.Equal("Open", issues[0].Fields.Status.Name);
        Assert.Equal(new DateOnly(2024, 2, 3), issues[0].Fields.DueDate);
        Assert.Equal(3, issues[0].Fields.Points);
        Assert.Equal("Dev", issues[0].Fields.Assignee.DisplayName);
        Assert.Equal(2024, issues[1].Fields.Created.Year);
        Assert.Equal(2, server.Requests.Count);
        Assert.All(server.Requests, request => Assert.Equal(ExpectedAuthorization, request.Authorization));

        using var firstBody = JsonDocument.Parse(server.Requests[0].Body);
        Assert.Equal("project = A", firstBody.RootElement.GetProperty("jql").GetString());
        Assert.Equal(100, firstBody.RootElement.GetProperty("maxResults").GetInt32());
        Assert.Equal("changelog", firstBody.RootElement.GetProperty("expand").GetString());
        Assert.Contains("summary", firstBody.RootElement.GetProperty("fields").EnumerateArray().Select(field => field.GetString()));
    }

    [Fact]
    public async Task UserSprintsVersionsAndComponents_ShouldBeReadAndCreated()
    {
        static string Sprints(int from, int count) => string.Join(",", Enumerable.Range(from, count).Select(index => $$"""{"id":{{index}},"name":"Sprint {{index}}","state":"closed"}"""));

        using var server = new LocalHttpServer(request => (request.Method, request.PathAndQuery) switch
        {
            ("GET", "/rest/api/3/myself") => LocalHttpResponse.Json("""{"displayName":"Me","accountId":"acc-1","emailAddress":"me@example.test","active":true}"""),
            ("GET", "/rest/agile/latest/board/7/sprint?startAt=0") => LocalHttpResponse.Json($$"""{"total":60,"values":[{{Sprints(1, 50)}}]}"""),
            ("GET", "/rest/agile/latest/board/7/sprint?startAt=50") => LocalHttpResponse.Json($$"""{"total":60,"values":[{{Sprints(51, 10)}}]}"""),
            ("GET", "/rest/api/3/project/P/versions") => LocalHttpResponse.Json("""[{"id":"1","name":"1.0","released":true},{"id":"2","name":"1.1","released":false}]"""),
            ("GET", "/rest/api/3/project/P/components") => LocalHttpResponse.Json("""[{"id":"c1","name":"Comp","projectId":5}]"""),
            ("POST", "/rest/api/3/component") => LocalHttpResponse.Json("""{"id":"c2","name":"New"}"""),
            ("POST", "/rest/agile/1.0/sprint") => LocalHttpResponse.Json("""{"id":9,"name":"Sprint 9","state":"future","startDate":"2024-01-01T00:00:00","endDate":"2024-01-15T00:00:00","originBoardId":7}"""),
            _ => LocalHttpResponse.NotFound()
        });
        var connector = CreateConnector(server);

        var user = await connector.GetCurrentUserAsync();
        Assert.Equal("Me", user.DisplayName);
        Assert.Equal("acc-1", user.AccountId);
        Assert.True(user.Active);

        var sprints = await connector.ListSprintsAsync(7);
        Assert.Equal(60, sprints.Count);
        Assert.Equal("Sprint 60", sprints[59].Name);

        var versions = await connector.ListFixVersionsAsync("P");
        Assert.Equal(["1.0", "1.1"], versions.Select(version => version.name));
        Assert.True(versions[0].released);

        var components = await connector.ListComponentsAsync("P");
        Assert.Equal("Comp", Assert.Single(components).Name);
        Assert.Equal(5, components[0].ProjectId);

        var component = await connector.CreateComponentAsync(new ComponentRequest { Name = "New", Description = "desc", Project = "P", AssigneeType = "PROJECT_DEFAULT" });
        Assert.Equal("c2", component.Id);
        using (var body = JsonDocument.Parse(server.LastRequest.Body))
        {
            Assert.Equal("New", body.RootElement.GetProperty("name").GetString());
            Assert.Equal("P", body.RootElement.GetProperty("project").GetString());
        }

        var sprint = await connector.CreateSprintAsync(new SprintCreateRequest
        {
            Name = "Sprint 9",
            Goal = "Ship",
            StartDate = new DateOnly(2024, 1, 1),
            EndDate = new DateOnly(2024, 1, 15),
            OriginBoardId = 7
        });
        Assert.Equal(9, sprint.Id);
        Assert.Equal(new DateTime(2024, 1, 15), sprint.EndDate);
        using (var body = JsonDocument.Parse(server.LastRequest.Body))
        {
            Assert.Equal("2024-01-01", body.RootElement.GetProperty("startDate").GetString());
            Assert.Equal(7, body.RootElement.GetProperty("originBoardId").GetInt32());
        }
    }

    [Fact]
    public async Task IssueOperations_ShouldSendTheExpectedRequests()
    {
        using var server = new LocalHttpServer(request => (request.Method, request.PathAndQuery) switch
        {
            ("GET", "/rest/api/3/issue/A-1/comment") => LocalHttpResponse.Json("""{"startAt":0,"maxResults":50,"total":2,"comments":[{"id":"1","body":"hello","created":"2024-01-02T03:04:05"},{"id":"2","created":"2024-01-02T03:04:05"}]}"""),
            ("GET", "/rest/api/3/issue/EMPTY/comment") => LocalHttpResponse.Json("""{"total":0,"comments":[]}"""),
            ("GET", "/rest/api/3/group/member?groupname=devs") => LocalHttpResponse.Json("""{"isLast":true,"values":[{"accountId":"a1","displayName":"Dev"}]}"""),
            ("GET", "/rest/api/3/group/member?groupname=empty") => LocalHttpResponse.Json("""{"isLast":true,"values":[]}"""),
            ("POST", "/rest/api/3/issue/A-1/comment") => new LocalHttpResponse(201, string.Empty),
            ("PUT", "/rest/api/3/issue/A-1/assignee") => new LocalHttpResponse(204, string.Empty),
            ("PUT", "/rest/api/3/issue/A-1") => new LocalHttpResponse(204, string.Empty),
            ("POST", "/rest/api/3/issue/A-1/watchers") => new LocalHttpResponse(204, string.Empty),
            ("POST", "/rest/api/3/issue") => LocalHttpResponse.Json("""{"id":"10001","key":"A-3","self":"https://jira.example.test/rest/api/3/issue/10001"}"""),
            ("GET", "/rest/api/3/issue/A-1/transitions") => LocalHttpResponse.Json("""{"transitions":[{"id":"31","name":"Done","to":{"id":"3","name":"Done"}}]}"""),
            ("POST", "/rest/api/3/issue/A-1/transitions") => new LocalHttpResponse(204, string.Empty),
            ("GET", "/rest/api/3/issue/A-1/attachments") => LocalHttpResponse.Json("""[{"id":"1","filename":"f.txt","mimeType":"text/plain","size":10,"content":"u"}]"""),
            ("POST", "/rest/api/3/issueLink") => new LocalHttpResponse(201, string.Empty),
            ("GET", _) when request.PathAndQuery.StartsWith("/rest/servicedeskapi/request/SD-1") => LocalHttpResponse.Json("""
                {"issueId":"1","issueKey":"SD-1",
                 "requestFieldValues":[{"fieldId":"summary","label":"Summary","value":"Help"},{"fieldId":"count","label":"Count","value":2}],
                 "currentStatus":{"status":"Open"},
                 "requestType":{"id":"rt","name":"Support"},
                 "serviceDesk":{"id":"1","projectKey":"SD"},
                 "comment":{"values":[{"id":"c","body":"first","created":{"iso8601":"2024-01-02T03:04:05+0000","friendly":"Today"}}]}}
                """),
            _ => LocalHttpResponse.NotFound()
        });
        var connector = CreateConnector(server);

        var comments = await connector.ListCommentsAsync("A-1");
        Assert.Equal(["1", "2"], comments.Select(comment => comment.Id));
        Assert.Empty(await connector.ListCommentsAsync("EMPTY"));

        var members = await connector.ListGroupMembersAsync("devs");
        Assert.Equal("a1", Assert.Single(members).AccountId);
        Assert.Empty(await connector.ListGroupMembersAsync("empty"));

        await connector.CreateCommentAsync("A-1", "hi");
        Assert.Equal("""{"body":"hi"}""", server.LastRequest.Body);

        await connector.UpdateIssueAssigneeAsync("A-1", new JiraUser { AccountId = "acc" });
        using (var body = JsonDocument.Parse(server.LastRequest.Body))
        {
            Assert.Equal("acc", body.RootElement.GetProperty("accountId").GetString());
        }

        await connector.UpdateIssueAsync("A-1", """{"fields":{"summary":"Renamed"}}""");
        Assert.Equal("""{"fields":{"summary":"Renamed"}}""", server.LastRequest.Body);
        Assert.StartsWith("application/json", server.LastRequest.ContentType);

        await connector.ChangeIssueFieldListAsync("A-1", "fixVersions", "id", "1");
        Assert.Contains("\"update\"", server.LastRequest.Body);

        await connector.CreateWatcherAsync("A-1", "acc");
        Assert.Equal("\"acc\"", server.LastRequest.Body);

        var created = await connector.CreateIssueAsync(new JiraIssueCreateRequest
        {
            Fields = new JiraFieldsCreateRequest
            {
                Project = new JiraProject { Key = "A" },
                Summary = "New issue",
                IssueType = new JiraIssueType { Name = "Task" },
                Description = AtlassianDocument.FromPlainText("line1\nline2"),
                Labels = ["x"]
            }
        });
        Assert.Equal("A-3", created.Key);
        using (var body = JsonDocument.Parse(server.LastRequest.Body))
        {
            var fields = body.RootElement.GetProperty("fields");
            Assert.Equal("New issue", fields.GetProperty("summary").GetString());
            Assert.Equal("A", fields.GetProperty("project").GetProperty("key").GetString());
            Assert.Equal(2, fields.GetProperty("description").GetProperty("content").GetArrayLength());
        }

        var transitions = await connector.ListTransitionsAsync("A-1");
        Assert.Equal("Done", Assert.Single(transitions.Transitions).To?.Name);

        await connector.TransitionIssueAsync("A-1", "31");
        using (var body = JsonDocument.Parse(server.LastRequest.Body))
        {
            Assert.Equal("31", body.RootElement.GetProperty("transition").GetProperty("id").GetString());
        }

        var attachments = await connector.ListAttachmentsAsync("A-1");
        Assert.Equal("f.txt", Assert.Single(attachments).Filename);
        Assert.Equal(10, attachments[0].Size);

        await connector.CreateIssueLinkAsync("A-1", "A-2");
        using (var body = JsonDocument.Parse(server.LastRequest.Body))
        {
            Assert.Equal("Relates", body.RootElement.GetProperty("type").GetProperty("name").GetString());
            Assert.Equal("A-1", body.RootElement.GetProperty("inwardIssue").GetProperty("key").GetString());
            Assert.Equal("A-2", body.RootElement.GetProperty("outwardIssue").GetProperty("key").GetString());
        }

        await connector.CreateIssueLinkAsync("A-1", "A-2", "Blocks");
        Assert.Contains("Blocks", server.LastRequest.Body);

        var serviceDeskRequest = await connector.GetServiceDeskRequestAsync("SD-1");
        Assert.Equal("SD-1", serviceDeskRequest.IssueKey);
        Assert.Equal("Help", serviceDeskRequest.GetFieldValue("SUMMARY"));
        Assert.Equal("2", serviceDeskRequest.GetFieldValue("count"));
        Assert.Null(serviceDeskRequest.GetFieldValue("missing"));
        Assert.Equal("Open", serviceDeskRequest.CurrentStatus?.Status);
        Assert.Equal("Support", serviceDeskRequest.RequestType?.Name);
        Assert.Equal("SD", serviceDeskRequest.ServiceDesk?.ProjectKey);
        Assert.Equal("first", Assert.Single(serviceDeskRequest.Comment!.Values).Body);
        Assert.Equal("Today", serviceDeskRequest.Comment.Values[0].Created?.Friendly);
    }

    [Fact]
    public void AtlassianDocument_FromPlainText_ShouldCreateAParagraphPerLine()
    {
        var document = AtlassianDocument.FromPlainText("first\nsecond");

        Assert.Equal("doc", document.Type);
        Assert.Equal(1, document.Version);
        Assert.Equal(["first", "second"], document.Content.Select(paragraph => paragraph.Content![0].Text));
        Assert.All(document.Content, paragraph => Assert.Equal("paragraph", paragraph.Type));
    }

    [Fact]
    public void TursoConnector_ShouldValidateItsArguments()
    {
        Assert.Throws<ArgumentException>(() => new TursoConnector(" ", "token"));
        Assert.Throws<ArgumentException>(() => new TursoConnector("db.turso.io", ""));
        Assert.NotNull(new TursoConnector("libsql://db.turso.io", "token"));
        Assert.NotNull(new TursoConnector("https://db.turso.io", "token"));
    }
}
