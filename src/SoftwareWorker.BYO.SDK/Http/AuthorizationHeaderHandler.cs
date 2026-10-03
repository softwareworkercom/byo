using System.Net.Http.Headers;

namespace SoftwareWorker.BYO.SDK.Http
{
    /// <summary>
    /// Applies <see cref="RestSettings.AuthorizationHeaderValueGetter"/> to outgoing requests.
    /// </summary>
    internal sealed class AuthorizationHeaderHandler : DelegatingHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, ValueTask<string>> _getToken;

        public AuthorizationHeaderHandler(Func<HttpRequestMessage, CancellationToken, ValueTask<string>> getToken)
        {
            _getToken = getToken;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Headers.Authorization is { } authorization)
            {
                var token = await _getToken(request, cancellationToken);
                request.Headers.Authorization = string.IsNullOrEmpty(token) ? null : new AuthenticationHeaderValue(authorization.Scheme, token);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
