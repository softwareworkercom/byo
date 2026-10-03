using SoftwareWorker.BYO.SDK.Resilience;

namespace SoftwareWorker.BYO.SDK.Services
{
    public static class ResilienceService
    {
        public static AsyncRetryPolicy<T> GetRetryPolicy<T>(int maxRetryAttempts = 3)
        {
            return new AsyncRetryPolicy<T>(
                IsTransient,
                maxRetryAttempts,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timespan, retryCount) =>
                {
                    Console.WriteLine($"Retry {retryCount} after {timespan.TotalSeconds}s due to: {exception.Message}");
                });
        }

        public static AsyncCircuitBreakerPolicy<T> GetCircuitBreakerPolicy<T>(int exceptionsAllowedBeforeBreaking = 5, int durationOfBreakInSeconds = 30)
        {
            return new AsyncCircuitBreakerPolicy<T>(
                IsTransient,
                exceptionsAllowedBeforeBreaking,
                TimeSpan.FromSeconds(durationOfBreakInSeconds),
                onBreak: (exception, duration) =>
                {
                    Console.WriteLine($"Circuit breaker opened for {duration.TotalSeconds}s due to: {exception.Message}");
                },
                onReset: () =>
                {
                    Console.WriteLine("Circuit breaker reset");
                });
        }

        public static AsyncTimeoutPolicy<T> GetTimeoutPolicy<T>(int timeoutInSeconds = 30)
        {
            return new AsyncTimeoutPolicy<T>(TimeSpan.FromSeconds(timeoutInSeconds));
        }

        public static IAsyncPolicy<T> GetCombinedPolicy<T>(
            int maxRetryAttempts = 3,
            int exceptionsAllowedBeforeBreaking = 5,
            int durationOfBreakInSeconds = 30,
            int timeoutInSeconds = 30)
        {
            var retryPolicy = GetRetryPolicy<T>(maxRetryAttempts);
            var circuitBreakerPolicy = GetCircuitBreakerPolicy<T>(exceptionsAllowedBeforeBreaking, durationOfBreakInSeconds);
            var timeoutPolicy = GetTimeoutPolicy<T>(timeoutInSeconds);

            return new AsyncPolicyWrap<T>(retryPolicy, circuitBreakerPolicy, timeoutPolicy);
        }

        public static async Task<T?> ExecuteWithResilienceAsync<T>(
            Func<Task<T>> action,
            int maxRetryAttempts = 3,
            int exceptionsAllowedBeforeBreaking = 5,
            int durationOfBreakInSeconds = 30,
            int timeoutInSeconds = 30) where T : class
        {
            try
            {
                var policy = GetCombinedPolicy<T>(maxRetryAttempts, exceptionsAllowedBeforeBreaking, durationOfBreakInSeconds, timeoutInSeconds);
                return await policy.ExecuteAsync(action);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Request failed after all retries: {ex.Message}");
                return null;
            }
        }

        public static async Task ExecuteWithResilienceAsync(
            Func<Task> action,
            int maxRetryAttempts = 3,
            int exceptionsAllowedBeforeBreaking = 5,
            int durationOfBreakInSeconds = 30,
            int timeoutInSeconds = 30)
        {
            try
            {
                var policy = GetCombinedPolicy<bool>(maxRetryAttempts, exceptionsAllowedBeforeBreaking, durationOfBreakInSeconds, timeoutInSeconds);
                await policy.ExecuteAsync(async () =>
                {
                    await action();
                    return true;
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Request failed after all retries: {ex.Message}");
            }
        }

        // Only HttpRequestException (transport failures) and TimeoutException are transient. API error responses (ApiException),
        // HttpClient timeouts (TaskCanceledException) and TimeoutRejectedException are not retried.
        private static bool IsTransient(Exception exception)
        {
            return exception is HttpRequestException or TimeoutException;
        }
    }
}
