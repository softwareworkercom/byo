using System.Text.Json;
using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.SDK.Http
{
    public sealed class RestSettings
    {
        /// <summary>
        /// Options used to serialize JSON request bodies and deserialize responses.
        /// Defaults to the web defaults (camelCase, case-insensitive) with enums as strings.
        /// </summary>
        public JsonSerializerOptions SerializerOptions { get; set; } = new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };

        /// <summary>
        /// Creates the innermost message handler, e.g. to log traffic. Defaults to <see cref="HttpClientHandler"/>.
        /// </summary>
        public Func<HttpMessageHandler>? HttpMessageHandlerFactory { get; set; }

        /// <summary>
        /// Supplies the token for an Authorization header already present on the request; the header's scheme is kept.
        /// Requests without an Authorization header are sent unchanged, and an empty token removes the header.
        /// </summary>
        public Func<HttpRequestMessage, CancellationToken, ValueTask<string>>? AuthorizationHeaderValueGetter { get; set; }
    }
}
