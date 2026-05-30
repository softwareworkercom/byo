using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Refit;

namespace SoftwareWorker.BYO.Integrations.Helpers
{
    public static class RefitHelper
    {
        public static RefitSettings GetSettings(bool isVerbose, string connectorName = "Unknown")
        {
            var refitSettings = new RefitSettings
            {
                ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true,
                    // Explicitly configure a metadata resolver so deserialization works even when
                    // reflection-based serialization is disabled by default (e.g. trimmed/AOT publish).
                    TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
                    Converters = { new IntegrationsCommon.JsonConverters.DateTimeConverter() }
                }),
            };

            if (isVerbose)
            {
                refitSettings.HttpMessageHandlerFactory = () => new LoggingHandler(connectorName) { InnerHandler = new HttpClientHandler() };
            }

            return refitSettings;
        }
    }
}
