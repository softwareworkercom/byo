using SoftwareWorker.BYO.Integrations.AgentMail;
using SoftwareWorker.BYO.Integrations.Bitwarden;
using SoftwareWorker.BYO.Integrations.Confluence;
using SoftwareWorker.BYO.Integrations.HashiCorpVault;
using SoftwareWorker.BYO.Tests.Support;
using System.Text;
using System.Text.Json;

namespace SoftwareWorker.BYO.Tests;

/// <summary>
/// Connectors build their own HTTP client, so they are exercised against a loopback server that
/// records every request and answers with canned JSON.
/// </summary>
public sealed class ConnectorTests
{
    #region Confluence

    [Fact]
    public async Task Confluence_ShouldReadUpdateCreateAndDeletePages()
    {
        using var server = new LocalHttpServer(request => (request.Method, request.PathAndQuery) switch
        {
            ("GET", "/wiki/rest/api/content/123") => LocalHttpResponse.Json("""{"id":"123","title":"Page","version":{"number":4},"space":{"key":"DEV"},"_links":{"webui":"/x"}}"""),
            ("PUT", "/wiki/rest/api/content/123") => LocalHttpResponse.Json("""{"id":"123","title":"Page","version":{"number":5}}"""),
            ("POST", "/wiki/rest/api/content") => LocalHttpResponse.Json("""{"id":"456","title":"New","version":{"number":1}}"""),
            ("DELETE", "/wiki/rest/api/content/123") => new LocalHttpResponse(204, string.Empty),
            _ => LocalHttpResponse.NotFound()
        });
        var connector = new ConfluenceConnector(server.BaseUrl, "user", "key", isVerbose: false);
        var expectedAuthorization = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("user:key"));

        var page = await connector.GetPageAsync("123");
        Assert.Equal("Page", page.Title);
        Assert.Equal(4, page.Version.Number);
        Assert.Equal("DEV", page.Space?.Key);
        Assert.Equal("/x", page.Links?.WebUi);
        Assert.Equal(expectedAuthorization, server.LastRequest.Authorization);

        await connector.UpdatePageAsync("123", "<p>updated</p>", "a comment");
        var update = server.LastRequest;
        Assert.Equal("PUT", update.Method);
        using (var body = JsonDocument.Parse(update.Body))
        {
            Assert.Equal("Page", body.RootElement.GetProperty("title").GetString());
            Assert.Equal(5, body.RootElement.GetProperty("version").GetProperty("number").GetInt32());
            Assert.Equal("a comment", body.RootElement.GetProperty("version").GetProperty("message").GetString());
            Assert.Equal("<p>updated</p>", body.RootElement.GetProperty("body").GetProperty("storage").GetProperty("value").GetString());
        }

        var created = await connector.CreatePageAsync("DEV", "New", "<p>new</p>");
        Assert.Equal("456", created.Id);
        using (var body = JsonDocument.Parse(server.LastRequest.Body))
        {
            Assert.Equal("DEV", body.RootElement.GetProperty("space").GetProperty("key").GetString());
            Assert.Equal("New", body.RootElement.GetProperty("title").GetString());
        }

        await connector.DeletePageAsync("123");
        Assert.Equal("DELETE", server.LastRequest.Method);

        // GET, GET + PUT (update reads the current version first), POST, DELETE
        Assert.Equal(["GET", "GET", "PUT", "POST", "DELETE"], server.Requests.Select(request => request.Method));
    }

    [Fact]
    public async Task Confluence_ShouldListSpaces_AndSearchContent()
    {
        using var server = new LocalHttpServer(request => request.PathAndQuery switch
        {
            "/wiki/rest/api/space" => LocalHttpResponse.Json("""{"results":[{"id":1,"key":"A","name":"Alpha","type":"global"},{"id":2,"key":"B","name":"Beta","type":"global"}],"size":2}"""),
            "/wiki/rest/api/space/A" => LocalHttpResponse.Json("""{"id":1,"key":"A","name":"Alpha","type":"global"}"""),
            _ when request.PathAndQuery.StartsWith("/wiki/rest/api/content/search") => LocalHttpResponse.Json("""
                {"results":[
                  {"id":"1","type":"page","title":"T1","history":{"lastUpdated":{"when":"2024-01-02T03:04:05","by":{"displayName":"X"}}}},
                  {"id":"2","type":"page","title":"T2"}
                ]}
                """),
            _ => LocalHttpResponse.NotFound()
        });
        var connector = new ConfluenceConnector(server.BaseUrl, "user", "key", isVerbose: false);

        Assert.Equal(["Alpha", "Beta"], (await connector.ListSpacesAsync()).Select(space => space.Name));
        Assert.Single(await connector.ListSpacesAsync(maxItems: 1));
        Assert.Equal("Alpha", (await connector.GetSpaceAsync("A")).Name);

        var byUser = await connector.SearchContentByUserAsync("abc123", maxItems: 1);
        Assert.Equal("T1", Assert.Single(byUser).Title);
        Assert.Equal(new DateTime(2024, 1, 2, 3, 4, 5), byUser[0].LastModified);
        Assert.Contains("abc123", server.LastRequest.PathAndQuery);

        var recent = await connector.SearchRecentlyModifiedAsync(7, maxItems: 50);
        Assert.Equal(2, recent.Count);
        Assert.Null(recent[1].LastModified);
        Assert.Contains("limit=50", server.LastRequest.PathAndQuery);
        Assert.Contains("-7d", Uri.UnescapeDataString(server.LastRequest.PathAndQuery));
    }

    #endregion

    #region Bitwarden

    [Fact]
    public async Task Bitwarden_ShouldCallTheSecretsAndProjectsEndpoints()
    {
        using var server = new LocalHttpServer(request => (request.Method, request.PathAndQuery) switch
        {
            ("GET", "/api/secrets?organizationId=org") => LocalHttpResponse.Json("""{"data":[{"id":"s1","key":"K","value":"V"}]}"""),
            ("GET", "/api/secrets") => LocalHttpResponse.Json("""{"data":[]}"""),
            ("GET", "/api/secrets/s1") => LocalHttpResponse.Json("""{"id":"s1","key":"K","value":"V","note":"n"}"""),
            ("POST", "/api/secrets/org") => LocalHttpResponse.Json("""{"id":"s2","key":"K2"}"""),
            ("PUT", "/api/secrets/s1") => LocalHttpResponse.Json("""{"id":"s1","key":"K3"}"""),
            ("DELETE", "/api/secrets/s1") => new LocalHttpResponse(200, string.Empty),
            ("GET", "/api/projects?organizationId=org") => LocalHttpResponse.Json("""{"data":[{"id":"p1","name":"P"}]}"""),
            ("GET", "/api/projects/p1") => LocalHttpResponse.Json("""{"id":"p1","name":"P"}"""),
            _ => LocalHttpResponse.NotFound()
        });
        var connector = new BitwardenConnector(server.BaseUrl, "token");

        Assert.Equal("K", Assert.Single((await connector.ListSecretsAsync("org"))!).Key);
        Assert.Empty((await connector.ListSecretsAsync())!);
        Assert.Equal("n", (await connector.GetSecretAsync("s1"))!.Note);

        var created = await connector.CreateSecretAsync("org", "K2", "V2", "note", ["p1"]);
        Assert.Equal("s2", created!.Id);
        using (var body = JsonDocument.Parse(server.LastRequest.Body))
        {
            Assert.Equal("K2", body.RootElement.GetProperty("key").GetString());
            Assert.Equal("V2", body.RootElement.GetProperty("value").GetString());
            Assert.Equal("p1", body.RootElement.GetProperty("projectIds")[0].GetString());
        }

        var updated = await connector.UpdateSecretAsync("s1", key: "K3");
        Assert.Equal("K3", updated!.Key);
        using (var body = JsonDocument.Parse(server.LastRequest.Body))
        {
            Assert.Equal("K3", body.RootElement.GetProperty("key").GetString());
            Assert.False(body.RootElement.TryGetProperty("value", out _));
        }

        Assert.True(await connector.DeleteSecretAsync("s1"));
        Assert.Equal("P", Assert.Single((await connector.ListProjectsAsync("org"))!).Name);
        Assert.Equal("P", (await connector.GetProjectAsync("p1"))!.Name);
    }

    [Fact]
    public async Task Bitwarden_ShouldReturnNullOrFalse_WhenTheApiRejectsTheRequest()
    {
        using var console = new ConsoleScope();
        using var server = new LocalHttpServer(_ => LocalHttpResponse.NotFound());
        var connector = new BitwardenConnector(server.BaseUrl, "token");

        Assert.Null(await connector.GetSecretAsync("missing"));
        Assert.False(await connector.DeleteSecretAsync("missing"));
        Assert.Null(await connector.ListProjectsAsync());
        Assert.Contains("Request failed after all retries", console.SystemOutput);
    }

    #endregion

    #region HashiCorp Vault

    [Fact]
    public async Task HashiCorpVault_ShouldCoverSecretsMountsPoliciesAndTokens()
    {
        using var server = new LocalHttpServer(request => (request.Method, request.PathAndQuery) switch
        {
            ("GET", "/v1/sys/health") => LocalHttpResponse.Json("""{"initialized":true,"sealed":false,"version":"1.15.0"}"""),
            ("GET", "/v1/secret/data/db") => LocalHttpResponse.Json("""{"request_id":"r","data":{"data":{"user":"u","pass":"p"},"metadata":{"version":3}}}"""),
            ("POST", "/v1/secret/data/db") => LocalHttpResponse.Json("""{"data":{"version":4,"created_time":"t"}}"""),
            ("DELETE", "/v1/secret/data/db") => new LocalHttpResponse(204, string.Empty),
            ("GET", "/v1/secret/metadata/db") => LocalHttpResponse.Json("""{"data":{"current_version":4,"max_versions":10,"versions":{"1":{"destroyed":true}}}}"""),
            ("GET", "/v1/sys/mounts") => LocalHttpResponse.Json("""{"data":{"secret/":{"type":"kv","description":"d"}}}"""),
            ("POST", "/v1/sys/mounts/kv2") => new LocalHttpResponse(204, string.Empty),
            ("DELETE", "/v1/sys/mounts/kv2") => new LocalHttpResponse(204, string.Empty),
            ("GET", "/v1/sys/policy") => LocalHttpResponse.Json("""{"policies":["default","root"]}"""),
            ("GET", "/v1/sys/policy/default") => LocalHttpResponse.Json("""{"name":"default","policy":"path \"secret/*\" {}"}"""),
            ("POST", "/v1/sys/policy/p") => new LocalHttpResponse(204, string.Empty),
            ("DELETE", "/v1/sys/policy/p") => new LocalHttpResponse(204, string.Empty),
            ("POST", "/v1/auth/token/create") => LocalHttpResponse.Json("""{"auth":{"client_token":"tok","policies":["default"],"lease_duration":3600,"renewable":true}}"""),
            ("POST", "/v1/auth/token/renew") => LocalHttpResponse.Json("""{"auth":{"client_token":"tok","lease_duration":7200}}"""),
            ("POST", "/v1/auth/token/revoke") => new LocalHttpResponse(204, string.Empty),
            ("GET", "/v1/auth/token/lookup-self") => LocalHttpResponse.Json("""{"data":{"id":"tok","display_name":"me","ttl":100,"policies":["default"]}}"""),
            _ => LocalHttpResponse.NotFound()
        });
        var connector = new HashiCorpVaultConnector(server.BaseUrl, "token", isVerbose: false);

        var health = await connector.GetHealthAsync();
        Assert.True(health!.Initialized);
        Assert.Equal("1.15.0", health.Version);

        var secret = await connector.ReadSecretAsync("secret", "db");
        Assert.Equal("u", secret!.Data!.Data!["user"].ToString());
        Assert.Equal(3, secret.Data.Metadata!.Version);

        var written = await connector.WriteSecretAsync("secret", "db", new Dictionary<string, object> { ["user"] = "u2" });
        Assert.Equal(4, written!.Data!.Version);
        using (var body = JsonDocument.Parse(server.LastRequest.Body))
        {
            Assert.Equal("u2", body.RootElement.GetProperty("data").GetProperty("user").GetString());
        }

        Assert.True(await connector.DeleteSecretAsync("secret", "db"));
        Assert.Equal(4, (await connector.GetSecretMetadataAsync("secret", "db"))!.Data!.CurrentVersion);
        Assert.Equal("kv", (await connector.ListMountsAsync())!["secret/"].Type);

        Assert.True(await connector.CreateMountAsync("kv2", "kv", "second"));
        using (var body = JsonDocument.Parse(server.LastRequest.Body))
        {
            Assert.Equal("kv", body.RootElement.GetProperty("type").GetString());
            Assert.Equal("second", body.RootElement.GetProperty("description").GetString());
        }

        Assert.True(await connector.DeleteMountAsync("kv2"));
        Assert.Equal(["default", "root"], await connector.ListPoliciesAsync());
        Assert.Equal("default", (await connector.GetPolicyAsync("default"))!.Name);
        Assert.True(await connector.CreatePolicyAsync("p", "path \"x\" {}"));
        Assert.Contains("path", server.LastRequest.Body);
        Assert.True(await connector.DeletePolicyAsync("p"));

        var token = await connector.CreateTokenAsync("me", 3600, ["default"]);
        Assert.Equal("tok", token!.Auth!.ClientToken);
        using (var body = JsonDocument.Parse(server.LastRequest.Body))
        {
            Assert.Equal("me", body.RootElement.GetProperty("display_name").GetString());
            Assert.Equal(3600, body.RootElement.GetProperty("ttl").GetInt32());
        }

        Assert.Equal(7200, (await connector.RenewTokenAsync("tok", 7200))!.Auth!.LeaseDuration);
        Assert.True(await connector.RevokeTokenAsync("tok"));
        Assert.Equal("tok", (await connector.LookupTokenAsync())!.Data!.Id);
    }

    #endregion

    #region AgentMail

    [Fact]
    public async Task AgentMail_ShouldValidateArguments()
    {
        using var server = new LocalHttpServer(_ => LocalHttpResponse.NotFound());
        var connector = new AgentMailConnector("key", isVerbose: false, server.BaseUrl);

        Assert.Throws<ArgumentException>(() => new AgentMailConnector(" ", isVerbose: false));
        await Assert.ThrowsAsync<ArgumentException>(() => connector.GetInboxAsync(" "));
        await Assert.ThrowsAsync<ArgumentException>(() => connector.DeleteInboxAsync(""));
        await Assert.ThrowsAsync<ArgumentException>(() => connector.ListMessagesAsync(""));
        await Assert.ThrowsAsync<ArgumentException>(() => connector.GetMessageAsync("", "m"));
        await Assert.ThrowsAsync<ArgumentException>(() => connector.GetMessageAsync("i", ""));
        Assert.Empty(server.Requests);
    }

    [Fact]
    public async Task AgentMail_ShouldListAndManageInboxesAndMessages()
    {
        var listResponses = new Queue<string>(
        [
            """{"data":[{"id":"i1","email_address":"a@b.c","display_name":"A"}],"next_page_token":"n"}""",
            """{"items":[{"id":"i2","email_address":"d@e.f"}]}""",
            """{"inboxes":[{"id":"i3","email_address":"g@h.i"}]}""",
            """{}"""
        ]);
        using var server = new LocalHttpServer(request => (request.Method, request.PathAndQuery) switch
        {
            ("GET", _) when request.PathAndQuery.StartsWith("/v0/inboxes?") || request.PathAndQuery == "/v0/inboxes" => LocalHttpResponse.Json(listResponses.Dequeue()),
            ("GET", "/v0/inboxes/i1") => LocalHttpResponse.Json("""{"id":"i1","email_address":"a@b.c"}"""),
            ("POST", "/v0/inboxes") => LocalHttpResponse.Json("""{"id":"i2","username":"u","domain":"d","display_name":"n"}"""),
            ("DELETE", "/v0/inboxes/i1") => new LocalHttpResponse(204, string.Empty),
            ("GET", "/v0/inboxes/i1/messages?limit=5") => LocalHttpResponse.Json("""{"data":[{"id":"m1","subject":"S","from":{"email":"x@y"},"to":[{"email":"z@y"}]}]}"""),
            ("GET", "/v0/inboxes/i1/messages/m1") => LocalHttpResponse.Json("""{"id":"m1","subject":"S","text":"hello","received_at":"2024-01-02T03:04:05Z","has_attachments":false}"""),
            _ => LocalHttpResponse.NotFound()
        });
        var connector = new AgentMailConnector("api-key", isVerbose: false, server.BaseUrl);

        var first = await connector.ListInboxesAsync(limit: 2, ascending: true);
        Assert.Equal("a@b.c", Assert.Single(first).Email);
        Assert.Equal("/v0/inboxes?limit=2&ascending=True", server.LastRequest.PathAndQuery);
        Assert.Equal("Bearer api-key", server.LastRequest.Authorization);
        Assert.Equal("i2", Assert.Single(await connector.ListInboxesAsync()).Id);
        Assert.Equal("i3", Assert.Single(await connector.ListInboxesAsync()).Id);
        Assert.Empty(await connector.ListInboxesAsync());

        Assert.Equal("a@b.c", (await connector.GetInboxAsync("i1"))!.EmailAddress);

        var created = await connector.CreateInboxAsync("u", "d", "n");
        Assert.Equal("i2", created!.Id);
        using (var body = JsonDocument.Parse(server.LastRequest.Body))
        {
            Assert.Equal("u", body.RootElement.GetProperty("username").GetString());
            Assert.Equal("d", body.RootElement.GetProperty("domain").GetString());
            Assert.Equal("n", body.RootElement.GetProperty("display_name").GetString());
        }

        await connector.DeleteInboxAsync("i1");
        Assert.Equal("DELETE", server.LastRequest.Method);

        var messages = await connector.ListMessagesAsync("i1", limit: 5);
        Assert.Equal("S", Assert.Single(messages).Subject);
        Assert.Equal("x@y", messages[0].From?.Email);

        var message = await connector.GetMessageAsync("i1", "m1");
        Assert.Equal("hello", message!.Text);
        Assert.Equal(new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero), message.ReceivedAt);
        Assert.False(message.HasAttachments);
    }

    #endregion
}
