using System.Net;

namespace SoftwareWorker.BYO.Integrations.Http
{
    /// <summary>
    /// Thrown when an API call returns a non-success status code. Deliberately not an
    /// <see cref="HttpRequestException"/>: resilience policies retry those as transient transport
    /// failures, whereas an error response (404, 401, ...) won't change on retry.
    /// </summary>
    internal sealed class ApiException : Exception
    {
        public ApiException(HttpStatusCode statusCode, string? reasonPhrase, string? content)
            : base($"Response status code does not indicate success: {(int)statusCode} ({reasonPhrase}).")
        {
            StatusCode = statusCode;
            Content = content;
        }

        public HttpStatusCode StatusCode { get; }

        public string? Content { get; }
    }
}
