using SoftwareWorker.BYO.SDK.Services;
using SoftwareWorker.BYO.SDK.Http;
using System.Net;
using System.Text;

namespace SoftwareWorker.BYO.Tests;

public sealed class RestServiceTests
{
    [Fact]
    public async Task For_ShouldAppendPathToBaseAddressPath_AndEscapePathPlaceholders()
    {
        var handler = new CapturingHandler();
        var api = CreateApi(handler, "https://api.example.com/bot123:abc/");

        await api.GetItemAsync("a b/c");

        Assert.Equal(HttpMethod.Get, handler.Method);
        Assert.Equal("https://api.example.com/bot123:abc/items/a%20b%2Fc", handler.Uri!.AbsoluteUri);
    }

    [Fact]
    public async Task For_ShouldKeepTemplateQueryVerbatim_AndAppendRemainingParameters_SkippingNulls()
    {
        var handler = new CapturingHandler(_ => Json("[]"));
        var api = CreateApi(handler);

        await api.SearchAsync("acme corp", 50, cursor: null, top: 10, ascending: true);

        Assert.Equal("?q=org:acme%20corp+is:pr&per_page=50&%24top=10&ascending=True", handler.Uri!.Query);
    }

    [Fact]
    public async Task For_ShouldSendJsonBodyAndHeaders_AndDeserializeResponse()
    {
        var handler = new CapturingHandler(_ => Json("""{"name":"created","count":5}"""));
        var api = CreateApi(handler);
        var headers = new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer token",
            ["Content-Type"] = "application/json"
        };

        var result = await api.CreateItemAsync(headers, new TestItem { Name = "new", Count = 2 });

        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal("""{"name":"new","count":2}""", handler.Body);
        Assert.Equal("application/json", handler.ContentType);
        Assert.Equal("Bearer token", handler.Authorization);
        Assert.Equal("created", result.Name);
        Assert.Equal(5, result.Count);
    }

    [Fact]
    public async Task For_ShouldSendContentHeadersOnAnEmptyBody_ExceptOnGet()
    {
        var handler = new CapturingHandler();
        var api = CreateApi(handler);
        var headers = new Dictionary<string, string>
        {
            ["Authorization"] = "Basic abc",
            ["Content-Type"] = "application/json"
        };

        await api.ListItemsAsync(headers);
        Assert.Equal(HttpMethod.Get, handler.Method);
        Assert.Null(handler.Body);
        Assert.Equal("Basic abc", handler.Authorization);

        await api.DeleteItemAsync(headers, "42");
        Assert.Equal(HttpMethod.Delete, handler.Method);
        Assert.Equal(string.Empty, handler.Body);
        Assert.Equal("application/json", handler.ContentType);
        Assert.Equal("Basic abc", handler.Authorization);
    }

    [Fact]
    public async Task For_ShouldSendUrlEncodedForm_SkippingNullValues()
    {
        var handler = new CapturingHandler();
        var api = CreateApi(handler);

        await api.SubmitFormAsync(new Dictionary<string, object?>
        {
            ["metadata[order]"] = "a b",
            ["amount"] = 100L,
            ["capture"] = false,
            ["skipped"] = null
        });

        Assert.Equal("application/x-www-form-urlencoded", handler.ContentType);
        Assert.Equal("metadata%5Border%5D=a+b&amount=100&capture=False", handler.Body);
    }

    [Fact]
    public async Task For_ShouldSendHttpContentAndStringBodiesAsIs()
    {
        var handler = new CapturingHandler();
        var api = CreateApi(handler);

        await api.SendContentAsync("1", new StringContent("""{"raw":true}""", Encoding.UTF8, "application/json"));
        Assert.Equal("""{"raw":true}""", handler.Body);
        Assert.Equal("application/json; charset=utf-8", handler.ContentType);

        await api.SendTextAsync("plain");
        Assert.Equal("plain", handler.Body);
        Assert.Equal("text/plain; charset=utf-8", handler.ContentType);
    }

    [Fact]
    public async Task For_ShouldReturnDefault_WhenResponseBodyIsEmpty_UsingSuppliedHttpClient()
    {
        var handler = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        var api = RestService.For<ITestAPI>(new HttpClient(handler) { BaseAddress = new Uri("https://api.example.com") });

        var result = await api.GetItemAsync("1");

        Assert.Null(result);
        Assert.Equal("https://api.example.com/items/1", handler.Uri!.AbsoluteUri);
    }

    [Fact]
    public async Task For_ShouldThrowApiException_WhenResponseIsNotSuccessful()
    {
        var handler = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("missing") });
        var api = CreateApi(handler);

        var exception = await Assert.ThrowsAsync<ApiException>(() => api.GetItemAsync("1"));

        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        Assert.Equal("missing", exception.Content);
        // Resilience policies retry HttpRequestException as a transient failure; error responses must not be retried.
        Assert.False(typeof(HttpRequestException).IsAssignableFrom(typeof(ApiException)));
    }

    [Fact]
    public async Task For_ShouldFillDeclaredAuthorizationToken_AndLeaveOtherRequestsUnchanged()
    {
        var handler = new CapturingHandler();
        var settings = new RestSettings
        {
            HttpMessageHandlerFactory = () => handler,
            AuthorizationHeaderValueGetter = (_, _) => ValueTask.FromResult("token")
        };
        var api = RestService.For<ITestAPI>("https://api.example.com", settings);

        await api.DeleteItemAsync(new Dictionary<string, string> { ["Authorization"] = "Bearer" }, "1");
        Assert.Equal("Bearer token", handler.Authorization);

        await api.DeleteItemAsync(new Dictionary<string, string>(), "1");
        Assert.Null(handler.Authorization);
    }

    private static ITestAPI CreateApi(CapturingHandler handler, string baseUrl = "https://api.example.com")
    {
        return RestService.For<ITestAPI>(baseUrl, new RestSettings { HttpMessageHandlerFactory = () => handler });
    }

    private static HttpResponseMessage Json(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

        public CapturingHandler(Func<HttpRequestMessage, HttpResponseMessage>? respond = null)
        {
            _respond = respond ?? (_ => new HttpResponseMessage(HttpStatusCode.OK));
        }

        public HttpMethod? Method { get; private set; }
        public Uri? Uri { get; private set; }
        public string? Authorization { get; private set; }
        public string? ContentType { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Method = request.Method;
            Uri = request.RequestUri;
            Authorization = request.Headers.Authorization?.ToString();
            ContentType = request.Content?.Headers.ContentType?.ToString();
            Body = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return _respond(request);
        }
    }
}

internal interface ITestAPI
{
    [Get("/items/{id}")]
    Task<TestItem> GetItemAsync([AliasAs("id")] string itemId);

    [Get("/items")]
    Task<List<TestItem>> ListItemsAsync([HeaderCollection] IDictionary<string, string> headers);

    [Get("/search?q=org:{org}+is:pr&per_page={perPage}")]
    Task<List<TestItem>> SearchAsync(string org, int perPage, [Query] string? cursor = null, [AliasAs("$top")] int? top = null, bool? ascending = null);

    [Post("/items")]
    Task<TestItem> CreateItemAsync([HeaderCollection] IDictionary<string, string> headers, [Body] TestItem item);

    [Delete("/items/{id}")]
    Task DeleteItemAsync([HeaderCollection] IDictionary<string, string> headers, string id);

    [Post("/form")]
    Task SubmitFormAsync([Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, object?> data);

    [Put("/raw/{id}")]
    Task SendContentAsync(string id, [Body] HttpContent content);

    [Post("/text")]
    Task SendTextAsync([Body] string text);
}

internal sealed class TestItem
{
    public string? Name { get; set; }
    public int Count { get; set; }
}
