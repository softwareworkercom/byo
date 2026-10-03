using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace SoftwareWorker.BYO.Integrations.Helpers
{
    public static class ResilienceHelper
    {
        public static AsyncRetryPolicy<T> GetRetryPolicy<T>(int maxRetryAttempts = 3)
        {
            return Policy<T>
                .Handle<HttpRequestException>()
                .Or<TimeoutException>()
                .WaitAndRetryAsync(
                    maxRetryAttempts,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        Console.WriteLine($"Retry {retryCount} after {timespan.TotalSeconds}s due to: {outcome.Exception?.Message}");
                    });
        }

        public static AsyncCircuitBreakerPolicy<T> GetCircuitBreakerPolicy<T>(int exceptionsAllowedBeforeBreaking = 5, int durationOfBreakInSeconds = 30)
        {
            return Policy<T>
                .Handle<HttpRequestException>()
                .Or<TimeoutException>()
                .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking,
                    TimeSpan.FromSeconds(durationOfBreakInSeconds),
                    onBreak: (outcome, duration) =>
                    {
                        Console.WriteLine($"Circuit breaker opened for {duration.TotalSeconds}s due to: {outcome.Exception?.Message}");
                    },
                    onReset: () =>
                    {
                        Console.WriteLine("Circuit breaker reset");
                    });
        }

        public static AsyncTimeoutPolicy<T> GetTimeoutPolicy<T>(int timeoutInSeconds = 30)
        {
            return Policy.TimeoutAsync<T>(TimeSpan.FromSeconds(timeoutInSeconds));
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

            return Policy.WrapAsync(retryPolicy, circuitBreakerPolicy, timeoutPolicy);
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
    }
}
