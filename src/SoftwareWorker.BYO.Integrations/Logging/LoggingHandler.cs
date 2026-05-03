using System.Text.Json;

public class LoggingHandler : DelegatingHandler
{
    private readonly string _connectorName;

    public LoggingHandler(string connectorName = "Unknown")
    {
        _connectorName = connectorName;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var logDirectory = Path.Combine(AppContext.BaseDirectory, "Logs");
        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }

        await LogRequest(request, logDirectory);

        var response = await LogResponse(request, logDirectory, cancellationToken);

        return response;
    }

    private async Task<HttpResponseMessage> LogResponse(HttpRequestMessage request, string logDirectory, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (response.Content != null)
        {
            var responseLogFilePath = Path.Combine(logDirectory, $"{_connectorName}_Response_{DateTime.Now:yyyyMMddHHmmssfff}.json");
            var responseContent = await response.Content.ReadAsStringAsync();

            var responseJson = string.Empty;
            if (!string.IsNullOrEmpty(responseContent))
            {
                responseJson = JsonSerializer.Serialize(JsonSerializer.Deserialize<object>(responseContent), new JsonSerializerOptions { WriteIndented = true });
            }

            await File.WriteAllTextAsync(responseLogFilePath, responseJson);
            Console.WriteLine($"Response log saved to: {responseLogFilePath}");
        }

        return response;
    }

    private async Task LogRequest(HttpRequestMessage request, string logDirectory)
    {
        // Prepare request log object
        string? requestBody = null;
        if (request.Content != null)
        {
            requestBody = await request.Content.ReadAsStringAsync();
        }

        var requestLog = new
        {
            Timestamp = DateTime.Now,
            Method = request.Method.ToString(),
            Endpoint = request.RequestUri?.ToString(),
            Payload = requestBody
        };

        var requestLogFilePath = Path.Combine(logDirectory, $"{_connectorName}_Request_{DateTime.Now:yyyyMMddHHmmssfff}.json");
        await File.WriteAllTextAsync(
            requestLogFilePath,
            JsonSerializer.Serialize(requestLog, new JsonSerializerOptions { WriteIndented = true })
        );
        Console.WriteLine($"Request log saved to: {requestLogFilePath}");
    }
}
