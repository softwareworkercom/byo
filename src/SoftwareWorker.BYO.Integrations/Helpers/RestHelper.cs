using SoftwareWorker.BYO.Integrations.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace SoftwareWorker.BYO.Integrations.Helpers
{
    internal static class RestHelper
    {
        public static RestSettings GetSettings(bool isVerbose, string connectorName = "Unknown")
        {
            var restSettings = new RestSettings
            {
                SerializerOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true,
                    // Explicitly configure a metadata resolver so deserialization works even when
                    // reflection-based serialization is disabled by default (e.g. trimmed/AOT publish).
                    TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
                    Converters = { new JIRA.JsonConverters.DateTimeConverter() }
                },
            };

            if (isVerbose)
            {
                restSettings.HttpMessageHandlerFactory = () => new LoggingHandler(connectorName) { InnerHandler = new HttpClientHandler() };
            }

            return restSettings;
        }
    }
}
