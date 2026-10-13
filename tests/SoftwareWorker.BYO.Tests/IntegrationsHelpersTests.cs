using SoftwareWorker.BYO.Integrations.Helpers;
using SoftwareWorker.BYO.Integrations.JIRA.JsonConverters;
using SoftwareWorker.BYO.Tests.Support;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Tests;

public sealed class IntegrationsHelpersTests
{
    #region RestHelper

    [Fact]
    public void GetSettings_ShouldConfigureJson_AndOnlyLogWhenVerbose()
    {
        var quiet = RestHelper.GetSettings(isVerbose: false);
        var verbose = RestHelper.GetSettings(isVerbose: true, "Sample");

        Assert.Null(quiet.HttpMessageHandlerFactory);
        Assert.Same(JsonNamingPolicy.CamelCase, quiet.SerializerOptions.PropertyNamingPolicy);
        Assert.Equal(JsonIgnoreCondition.WhenWritingNull, quiet.SerializerOptions.DefaultIgnoreCondition);
        Assert.True(quiet.SerializerOptions.PropertyNameCaseInsensitive);
        Assert.True(quiet.SerializerOptions.WriteIndented);
        Assert.Contains(quiet.SerializerOptions.Converters, converter => converter is DateTimeConverter);

        var handler = Assert.IsType<LoggingHandler>(verbose.HttpMessageHandlerFactory!());
        Assert.IsType<HttpClientHandler>(handler.InnerHandler);
    }

    #endregion

    #region LoggingHandler

    [Fact]
    public async Task LoggingHandler_ShouldWriteRequestAndResponseLogFiles()
    {
        using var console = new ConsoleScope();
        var connectorName = $"Logged{Guid.NewGuid():N}";
        var logDirectory = Path.Combine(AppContext.BaseDirectory, "Logs");
        var handler = new LoggingHandler(connectorName)
        {
            InnerHandler = new StubHandler(request => request.Method == HttpMethod.Post
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{"ok":true}""", Encoding.UTF8, "application/json") }
                : new HttpResponseMessage(HttpStatusCode.NoContent) { Content = new StringContent(string.Empty) })
        };
        using var client = new HttpClient(handler);

        try
        {
            var posted = await client.PostAsync("https://example.test/items", new StringContent("""{"name":"x"}""", Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);
            var fetched = await client.GetAsync("https://example.test/items", TestContext.Current.CancellationToken);

            Assert.Equal("""{"ok":true}""", await posted.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            Assert.Equal(HttpStatusCode.NoContent, fetched.StatusCode);

            var requestLogs = Directory.GetFiles(logDirectory, $"{connectorName}_Request_*.json");
            var responseLogs = Directory.GetFiles(logDirectory, $"{connectorName}_Response_*.json");
            Assert.Equal(2, requestLogs.Length);
            Assert.Equal(2, responseLogs.Length);

            var requestLog = string.Join(Environment.NewLine, requestLogs.Select(File.ReadAllText));
            Assert.Contains("https://example.test/items", requestLog);
            Assert.Contains("\"Method\": \"POST\"", requestLog);
            Assert.Contains("\"Method\": \"GET\"", requestLog);
            Assert.Contains("name", requestLog);
            Assert.Contains("\"ok\": true", string.Join(Environment.NewLine, responseLogs.Select(File.ReadAllText)));
            Assert.Contains("Request log saved to:", console.SystemOutput);
            Assert.Contains("Response log saved to:", console.SystemOutput);
        }
        finally
        {
            foreach (var file in Directory.GetFiles(logDirectory, $"{connectorName}_*.json"))
            {
                File.Delete(file);
            }
        }
    }

    #endregion

    #region JSON converters

    private sealed class DatePayload
    {
        public DateTime When { get; set; }
    }

    private sealed class DoublePayload
    {
        [JsonConverter(typeof(DoubleConverter))]
        public double? Value { get; set; }
    }

    private sealed class LongPayload
    {
        [JsonConverter(typeof(Int64Converter))]
        public long Value { get; set; }
    }

    private static readonly JsonSerializerOptions DateOptions = new(JsonSerializerDefaults.Web) { Converters = { new DateTimeConverter() } };

    [Fact]
    public void DateTimeConverter_ShouldParseStrings_AndRejectOtherTokens()
    {
        var parsed = JsonSerializer.Deserialize<DatePayload>("""{"when":"2024-01-02T03:04:05"}""", DateOptions);

        Assert.Equal(new DateTime(2024, 1, 2, 3, 4, 5), parsed!.When);
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DatePayload>("""{"when":"not a date"}""", DateOptions));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DatePayload>("""{"when":12345}""", DateOptions));
        Assert.Contains("2024-01-02T03:04:05", JsonSerializer.Serialize(parsed, DateOptions));
    }

    [Fact]
    public void DoubleConverter_ShouldParseStringsAndNumbers_AndRejectOtherTokens()
    {
        Assert.Equal(1.5, JsonSerializer.Deserialize<DoublePayload>("""{"Value":"1.5"}""")!.Value);
        Assert.Equal(2.5, JsonSerializer.Deserialize<DoublePayload>("""{"Value":2.5}""")!.Value);
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DoublePayload>("""{"Value":"abc"}"""));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DoublePayload>("""{"Value":true}"""));
        Assert.Equal("""{"Value":2.5}""", JsonSerializer.Serialize(new DoublePayload { Value = 2.5 }));

        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartArray();
            new DoubleConverter().Write(writer, null, new JsonSerializerOptions());
            writer.WriteEndArray();
        }

        Assert.Equal("[]", Encoding.UTF8.GetString(buffer.ToArray()));
    }

    [Fact]
    public void Int64Converter_ShouldParseStringsAndNumbers_AndFallBackToZero()
    {
        Assert.Equal(42L, JsonSerializer.Deserialize<LongPayload>("""{"Value":"42"}""")!.Value);
        Assert.Equal(0L, JsonSerializer.Deserialize<LongPayload>("""{"Value":"abc"}""")!.Value);
        Assert.Equal(7L, JsonSerializer.Deserialize<LongPayload>("""{"Value":7}""")!.Value);
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<LongPayload>("""{"Value":true}"""));
        Assert.Equal("""{"Value":9}""", JsonSerializer.Serialize(new LongPayload { Value = 9 }));
    }

    #endregion

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(respond(request));
        }
    }
}
