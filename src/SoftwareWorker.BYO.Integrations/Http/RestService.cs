using System.Reflection;

namespace SoftwareWorker.BYO.Integrations.Http
{
    /// <summary>
    /// Creates implementations of declarative API interfaces, whose methods describe HTTP endpoints with
    /// <see cref="HttpMethodAttribute"/> and parameter attributes. See <see cref="RestServiceProxy"/> for how calls map to requests.
    /// </summary>
    internal static class RestService
    {
        public static T For<T>(string baseUrl, RestSettings? settings = null) where T : class
        {
            settings ??= new RestSettings();

            var handler = settings.HttpMessageHandlerFactory?.Invoke() ?? new HttpClientHandler();
            if (settings.AuthorizationHeaderValueGetter != null)
            {
                handler = new AuthorizationHeaderHandler(settings.AuthorizationHeaderValueGetter) { InnerHandler = handler };
            }

            var httpClient = new HttpClient(handler) { BaseAddress = new Uri(baseUrl.TrimEnd('/')) };
            return For<T>(httpClient, settings);
        }

        public static T For<T>(HttpClient httpClient, RestSettings? settings = null) where T : class
        {
            var api = DispatchProxy.Create<T, RestServiceProxy>();
            ((RestServiceProxy)(object)api).Initialize(httpClient, settings ?? new RestSettings());
            return api;
        }
    }
}
