using System.Text.Json;
using System.Text.Json.Serialization;
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
