using SoftwareWorker.BYO.SDK.Http;
using SoftwareWorker.BYO.SDK.Services;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Tests;

public sealed class HttpTests
{
    public interface IEchoApi
    {
        [Get("/echo")]
        Task<EchoResponse> GetAsync([HeaderCollection] IDictionary<string, string> headers);
    }

    public sealed class EchoResponse
    {
        public string? Value { get; set; }
    }

    [Fact]
    public async Task AuthorizationHeaderHandler_ShouldReplaceTheToken_AndKeepTheScheme()
    {
        var inner = new CapturingHandler();
        using var client = new HttpClient(new AuthorizationHeaderHandler((_, _) => ValueTask.FromResult("fresh")) { InnerHandler = inner });
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", "stale") }
        };

        await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal("Bearer fresh", inner.Request!.Headers.Authorization!.ToString());
    }

    [Fact]
    public async Task AuthorizationHeaderHandler_ShouldRemoveTheHeader_WhenTheTokenIsEmpty()
    {
        var inner = new CapturingHandler();
        using var client = new HttpClient(new AuthorizationHeaderHandler((_, _) => ValueTask.FromResult(string.Empty)) { InnerHandler = inner });
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Basic", "stale") }
        };

        await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Null(inner.Request!.Headers.Authorization);
    }

    [Fact]
    public async Task AuthorizationHeaderHandler_ShouldLeaveRequestsWithoutAuthorizationAlone()
    {
        var inner = new CapturingHandler();
        var calls = 0;
        using var client = new HttpClient(new AuthorizationHeaderHandler((_, _) =>
        {
            calls++;
            return ValueTask.FromResult("token");
        })
        { InnerHandler = inner });

        await client.GetAsync("https://example.test/", TestContext.Current.CancellationToken);

        Assert.Equal(0, calls);
        Assert.Null(inner.Request!.Headers.Authorization);
    }

    [Fact]
    public async Task RestService_ShouldUseTheHandlerFactory_AndTheAuthorizationGetter()
    {
        var inner = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"value":"pong"}""")
        });
        var settings = new RestSettings
        {
            HttpMessageHandlerFactory = () => inner,
            AuthorizationHeaderValueGetter = (_, _) => ValueTask.FromResult("fresh")
        };
        var api = RestService.For<IEchoApi>("https://example.test/base/", settings);

        var response = await api.GetAsync(new Dictionary<string, string> { ["Authorization"] = "Bearer stale" });

        Assert.Equal("pong", response.Value);
        Assert.Equal("https://example.test/base/echo", inner.Request!.RequestUri!.AbsoluteUri);
        Assert.Equal("Bearer fresh", inner.Request.Headers.Authorization!.ToString());
    }

    [Fact]
    public void RestSettings_ShouldDefaultToWebJsonWithEnumsAsStrings()
    {
        var settings = new RestSettings();

        Assert.Same(JsonNamingPolicy.CamelCase, settings.SerializerOptions.PropertyNamingPolicy);
        Assert.True(settings.SerializerOptions.PropertyNameCaseInsensitive);
        Assert.Contains(settings.SerializerOptions.Converters, converter => converter is JsonStringEnumConverter);
        Assert.Null(settings.HttpMessageHandlerFactory);
        Assert.Null(settings.AuthorizationHeaderValueGetter);
    }

    [Fact]
    public void ApiException_ShouldExposeStatusReasonAndContent()
    {
        var exception = new ApiException(HttpStatusCode.NotFound, "Not Found", "{}");

        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        Assert.Equal("{}", exception.Content);
        Assert.Equal("Response status code does not indicate success: 404 (Not Found).", exception.Message);
    }

    [Fact]
    public void HttpMethodAttributes_ShouldExposeTheVerbAndPath()
    {
        Assert.Equal((HttpMethod.Get, "/a"), (new GetAttribute("/a").Method, new GetAttribute("/a").Path));
        Assert.Equal(HttpMethod.Post, new PostAttribute("/b").Method);
        Assert.Equal(HttpMethod.Put, new PutAttribute("/c").Method);
        Assert.Equal(HttpMethod.Patch, new PatchAttribute("/d").Method);
        Assert.Equal(HttpMethod.Delete, new DeleteAttribute("/e").Method);
        Assert.Equal("alias", new AliasAsAttribute("alias").Name);
        Assert.Equal(BodySerializationMethod.UrlEncoded, new BodyAttribute(BodySerializationMethod.UrlEncoded).SerializationMethod);
        Assert.Equal(BodySerializationMethod.Default, new BodyAttribute().SerializationMethod);
    }

    private sealed class CapturingHandler(Func<HttpRequestMessage, HttpResponseMessage>? respond = null) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(respond?.Invoke(request) ?? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") });
        }
    }
}
