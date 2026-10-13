using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SoftwareWorker.BYO.Tests.Support;

internal sealed record RecordedRequest(string Method, string PathAndQuery, string? Authorization, string? ContentType, string Body);

internal sealed record LocalHttpResponse(int StatusCode, string Body, string ContentType = "application/json")
{
    public static LocalHttpResponse Json(string body) => new(200, body);

    public static LocalHttpResponse NotFound(string body = """{"error":"not found"}""") => new(404, body);
}

/// <summary>
/// A loopback HTTP server for connectors that build their own <see cref="HttpClient"/>: every request is
/// recorded and answered by the handler passed to the constructor.
/// </summary>
internal sealed class LocalHttpServer : IDisposable
{
    private readonly HttpListener _listener = new();
    private readonly Func<RecordedRequest, LocalHttpResponse> _handler;
    private readonly List<RecordedRequest> _requests = [];
    private readonly Task _loop;

    public LocalHttpServer(Func<RecordedRequest, LocalHttpResponse> handler)
    {
        _handler = handler;

        var port = GetFreePort();
        BaseUrl = $"http://localhost:{port}";
        _listener.Prefixes.Add($"{BaseUrl}/");
        _listener.Start();
        _loop = Task.Run(ServeAsync);
    }

    public string BaseUrl { get; }

    public IReadOnlyList<RecordedRequest> Requests
    {
        get
        {
            lock (_requests)
            {
                return _requests.ToList();
            }
        }
    }

    public RecordedRequest LastRequest => Requests[^1];

    private async Task ServeAsync()
    {
        while (true)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync();
            }
            catch
            {
                // The listener was stopped.
                return;
            }

            try
            {
                string body;
                using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                {
                    body = await reader.ReadToEndAsync();
                }

                var recorded = new RecordedRequest(
                    context.Request.HttpMethod,
                    context.Request.Url?.PathAndQuery ?? string.Empty,
                    context.Request.Headers["Authorization"],
                    context.Request.ContentType,
                    body);

                lock (_requests)
                {
                    _requests.Add(recorded);
                }

                var response = _handler(recorded);
                var bytes = Encoding.UTF8.GetBytes(response.Body);

                context.Response.StatusCode = response.StatusCode;
                context.Response.ContentType = response.ContentType;
                context.Response.ContentLength64 = bytes.Length;
                await context.Response.OutputStream.WriteAsync(bytes);
            }
            catch
            {
                // A failing handler must not take the server down; the client sees a closed response.
            }
            finally
            {
                try
                {
                    context.Response.Close();
                }
                catch
                {
                    // Already closed.
                }
            }
        }
    }

    private static int GetFreePort()
    {
        var socket = new TcpListener(IPAddress.Loopback, 0);
        socket.Start();
        var port = ((IPEndPoint)socket.LocalEndpoint).Port;
        socket.Stop();
        return port;
    }

    public void Dispose()
    {
        try
        {
            _listener.Stop();
            _listener.Close();
        }
        catch
        {
            // Already stopped.
        }

        try
        {
            _loop.Wait(TimeSpan.FromSeconds(2));
        }
        catch
        {
            // The serving loop ended with the listener.
        }
    }
}
