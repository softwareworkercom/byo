using SoftwareWorker.BYO.SDK.Http;
using System.Collections;
using System.Globalization;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;

namespace SoftwareWorker.BYO.SDK.Services
{
    /// <summary>
    /// Implementation behind the interfaces created by <see cref="RestService"/>. Each call becomes a request:
    /// <list type="bullet">
    /// <item>{name} placeholders in the path are replaced with the URL-escaped value of the matching parameter.</item>
    /// <item>Any other parameter is appended to the query string, unless it is null.</item>
    /// <item>A <see cref="BodyAttribute"/> parameter is the body; a <see cref="HeaderCollectionAttribute"/> parameter supplies headers.</item>
    /// </list>
    /// Task&lt;T&gt; methods deserialize the JSON response, or return default for an empty body.
    /// Non-success responses throw <see cref="ApiException"/>.
    /// </summary>
    internal class RestServiceProxy : DispatchProxy
    {
        private static readonly MethodInfo SendAndDeserializeMethod =
            typeof(RestServiceProxy).GetMethod(nameof(SendAndDeserializeAsync), BindingFlags.Instance | BindingFlags.NonPublic)!;

        private HttpClient _httpClient = null!;
        private RestSettings _settings = null!;

        internal void Initialize(HttpClient httpClient, RestSettings settings)
        {
            _httpClient = httpClient;
            _settings = settings;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            args ??= [];

            var returnType = targetMethod.ReturnType;
            if (returnType == typeof(Task))
            {
                return SendAsync(targetMethod, args);
            }

            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                return SendAndDeserializeMethod.MakeGenericMethod(returnType.GetGenericArguments()[0]).Invoke(this, [targetMethod, args]);
            }

            throw new NotSupportedException($"{targetMethod.DeclaringType?.Name}.{targetMethod.Name} must return Task or Task<T>.");
        }

        private async Task SendAsync(MethodInfo method, object?[] args)
        {
            (await SendRequestAsync(method, args)).Dispose();
        }

        private async Task<T?> SendAndDeserializeAsync<T>(MethodInfo method, object?[] args)
        {
            using var response = await SendRequestAsync(method, args);
            var content = await response.Content.ReadAsStringAsync();

            return string.IsNullOrWhiteSpace(content) ? default : JsonSerializer.Deserialize<T>(content, _settings.SerializerOptions);
        }

        private async Task<HttpResponseMessage> SendRequestAsync(MethodInfo method, object?[] args)
        {
            using var request = BuildRequest(method, args);
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                using (response)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    throw new ApiException(response.StatusCode, response.ReasonPhrase, content);
                }
            }

            return response;
        }

        private HttpRequestMessage BuildRequest(MethodInfo method, object?[] args)
        {
            var verb = method.GetCustomAttribute<HttpMethodAttribute>()
                ?? throw new NotSupportedException($"{method.DeclaringType?.Name}.{method.Name} has no HTTP method attribute.");

            var template = verb.Path;
            var queryParameters = new List<KeyValuePair<string, string>>();
            IDictionary<string, string>? headers = null;
            HttpContent? content = null;

            var parameters = method.GetParameters();
            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var value = args[i];

                if (parameter.IsDefined(typeof(HeaderCollectionAttribute)))
                {
                    headers = value as IDictionary<string, string>;
                }
                else if (parameter.GetCustomAttribute<BodyAttribute>() is { } body)
                {
                    content = CreateContent(value, parameter.ParameterType, body.SerializationMethod);
                }
                else
                {
                    var name = parameter.GetCustomAttribute<AliasAsAttribute>()?.Name ?? parameter.Name!;
                    var placeholder = $"{{{name}}}";

                    if (template.Contains(placeholder, StringComparison.OrdinalIgnoreCase))
                    {
                        template = template.Replace(placeholder, Uri.EscapeDataString(FormatValue(value)), StringComparison.OrdinalIgnoreCase);
                    }
                    else if (value != null)
                    {
                        queryParameters.Add(new(name, FormatValue(value)));
                    }
                }
            }

            var request = new HttpRequestMessage(verb.Method, BuildUri(template, queryParameters)) { Content = content };

            if (headers is { Count: > 0 })
            {
                // Requests that can carry a body get an empty one, so content headers such as Content-Type are still sent.
                if (request.Content == null && verb.Method != HttpMethod.Get && verb.Method != HttpMethod.Head)
                {
                    request.Content = new ByteArrayContent([]);
                }

                AddHeaders(request, headers);
            }

            return request;
        }

        // The path is appended to the base address's own path (e.g. https://api.telegram.org/bot<token>). A query string
        // written in the template is kept verbatim and the remaining parameters are appended to it.
        private Uri BuildUri(string pathAndQuery, List<KeyValuePair<string, string>> queryParameters)
        {
            var baseAddress = _httpClient.BaseAddress ?? throw new InvalidOperationException("The HttpClient must have a BaseAddress.");

            var query = string.Join('&', queryParameters.Select(parameter => $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value)}"));
            if (query.Length > 0)
            {
                pathAndQuery += (pathAndQuery.Contains('?') ? "&" : "?") + query;
            }

            return new Uri(baseAddress.GetLeftPart(UriPartial.Authority) + baseAddress.AbsolutePath.TrimEnd('/') + pathAndQuery);
        }

        private HttpContent? CreateContent(object? value, Type declaredType, BodySerializationMethod serializationMethod) => value switch
        {
            null => null,
            HttpContent httpContent => httpContent,
            _ when serializationMethod == BodySerializationMethod.UrlEncoded => new FormUrlEncodedContent(ToFormFields(value)),
            string text => new StringContent(text),
            _ => JsonContent.Create(value, declaredType, options: _settings.SerializerOptions)
        };

        private static IEnumerable<KeyValuePair<string, string>> ToFormFields(object value)
        {
            if (value is not IDictionary dictionary)
            {
                throw new NotSupportedException($"A url-encoded body must be a dictionary, not {value.GetType().Name}.");
            }

            foreach (DictionaryEntry entry in dictionary)
            {
                if (entry.Value != null)
                {
                    yield return new(FormatValue(entry.Key), FormatValue(entry.Value));
                }
            }
        }

        private static void AddHeaders(HttpRequestMessage request, IDictionary<string, string> headers)
        {
            foreach (var (name, value) in headers)
            {
                if (request.Headers.TryAddWithoutValidation(name, value) || request.Content == null)
                {
                    continue;
                }

                // A content header such as Content-Type replaces the one the body set rather than doubling it up.
                request.Content.Headers.Remove(name);
                request.Content.Headers.TryAddWithoutValidation(name, value);
            }
        }

        private static string FormatValue(object? value) => value switch
        {
            null => string.Empty,
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
    }
}
