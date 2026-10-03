using Microsoft.Extensions.Time.Testing;
using SoftwareWorker.BYO.Integrations.Helpers;
using SoftwareWorker.BYO.Integrations.Resilience;
using System.Diagnostics;

namespace SoftwareWorker.BYO.Tests;

public sealed class ResilienceTests
{
    [Fact]
    public async Task RetryPolicy_ShouldWaitWithBackoffBetweenAttempts_ThenRethrowTheLastException()
    {
        var time = new FakeTimeProvider();
        var retries = new List<(string Message, TimeSpan Delay, int RetryAttempt)>();
        var policy = new AsyncRetryPolicy<string>(
            IsTransient,
            3,
            retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            (exception, delay, retryAttempt) => retries.Add((exception.Message, delay, retryAttempt)),
            time);
        var attempts = 0;

        var execution = policy.ExecuteAsync(() => Task.FromException<string>(new HttpRequestException($"failure {++attempts}")));

        Assert.Equal(1, attempts);
        time.Advance(TimeSpan.FromSeconds(2) - TimeSpan.FromTicks(1));
        await Task.Delay(50, TestContext.Current.CancellationToken);
        Assert.Equal(1, attempts);

        time.Advance(TimeSpan.FromTicks(1));
        await WaitUntil(() => attempts == 2);
        time.Advance(TimeSpan.FromSeconds(4));
        await WaitUntil(() => attempts == 3);
        time.Advance(TimeSpan.FromSeconds(8));

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => execution);
        Assert.Equal("failure 4", exception.Message);
        Assert.Equal(4, attempts);
        Assert.Equal(
            [("failure 1", TimeSpan.FromSeconds(2), 1), ("failure 2", TimeSpan.FromSeconds(4), 2), ("failure 3", TimeSpan.FromSeconds(8), 3)],
            retries);
    }

    [Fact]
    public async Task RetryPolicy_ShouldReturnTheResult_WhenARetrySucceeds()
    {
        var policy = new AsyncRetryPolicy<string>(IsTransient, 3, _ => TimeSpan.Zero);
        var attempts = 0;

        var result = await policy.ExecuteAsync(() => ++attempts < 3
            ? Task.FromException<string>(new TimeoutException())
            : Task.FromResult("ok"));

        Assert.Equal("ok", result);
        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task RetryPolicy_ShouldNotRetryUnhandledExceptions()
    {
        var retried = false;
        var policy = new AsyncRetryPolicy<string>(IsTransient, 3, _ => TimeSpan.Zero, (_, _, _) => retried = true);
        var attempts = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(() => policy.ExecuteAsync(() =>
        {
            attempts++;
            return Task.FromException<string>(new InvalidOperationException());
        }));

        Assert.Equal(1, attempts);
        Assert.False(retried);
    }

    [Fact]
    public async Task CircuitBreaker_ShouldOpenAfterConsecutiveHandledFailures_AndRejectCallsWhileOpen()
    {
        var time = new FakeTimeProvider();
        var breaks = new List<(Exception Exception, TimeSpan Duration)>();
        var policy = new AsyncCircuitBreakerPolicy<string>(IsTransient, 2, TimeSpan.FromSeconds(30), (exception, duration) => breaks.Add((exception, duration)), timeProvider: time);
        var second = new HttpRequestException("second");

        await Assert.ThrowsAsync<HttpRequestException>(() => policy.ExecuteAsync(() => Task.FromException<string>(new HttpRequestException("first"))));
        Assert.Equal(CircuitState.Closed, policy.CircuitState);

        await Assert.ThrowsAsync<HttpRequestException>(() => policy.ExecuteAsync(() => Task.FromException<string>(second)));
        Assert.Equal(CircuitState.Open, policy.CircuitState);
        Assert.Equal([(second, TimeSpan.FromSeconds(30))], breaks);

        var invoked = false;
        var rejection = await Assert.ThrowsAsync<BrokenCircuitException>(() => policy.ExecuteAsync(() =>
        {
            invoked = true;
            return Task.FromResult("ran");
        }));
        Assert.Same(second, rejection.InnerException);
        Assert.False(invoked);

        time.Advance(TimeSpan.FromSeconds(30) - TimeSpan.FromTicks(1));
        Assert.Equal(CircuitState.Open, policy.CircuitState);
    }

    [Fact]
    public async Task CircuitBreaker_ShouldAllowASingleTrialAfterTheBreak_AndCloseWhenItSucceeds()
    {
        var time = new FakeTimeProvider();
        var resets = 0;
        var policy = new AsyncCircuitBreakerPolicy<string>(IsTransient, 1, TimeSpan.FromSeconds(30), onReset: () => resets++, timeProvider: time);
        await Assert.ThrowsAsync<HttpRequestException>(() => policy.ExecuteAsync(() => Task.FromException<string>(new HttpRequestException())));

        time.Advance(TimeSpan.FromSeconds(30));
        Assert.Equal(CircuitState.HalfOpen, policy.CircuitState);

        var trialResult = new TaskCompletionSource<string>();
        var trial = policy.ExecuteAsync(() => trialResult.Task);
        await Assert.ThrowsAsync<BrokenCircuitException>(() => policy.ExecuteAsync(() => Task.FromResult("concurrent")));

        trialResult.SetResult("recovered");
        Assert.Equal("recovered", await trial);
        Assert.Equal(CircuitState.Closed, policy.CircuitState);
        Assert.Equal(1, resets);
    }

    [Fact]
    public async Task CircuitBreaker_ShouldBreakAgain_WhenTheTrialFails()
    {
        var time = new FakeTimeProvider();
        var breaks = 0;
        var policy = new AsyncCircuitBreakerPolicy<string>(IsTransient, 1, TimeSpan.FromSeconds(30), (_, _) => breaks++, timeProvider: time);
        await Assert.ThrowsAsync<HttpRequestException>(() => policy.ExecuteAsync(() => Task.FromException<string>(new HttpRequestException())));
        time.Advance(TimeSpan.FromSeconds(30));

        await Assert.ThrowsAsync<HttpRequestException>(() => policy.ExecuteAsync(() => Task.FromException<string>(new HttpRequestException())));

        Assert.Equal(2, breaks);
        Assert.Equal(CircuitState.Open, policy.CircuitState);
    }

    [Fact]
    public async Task CircuitBreaker_ShouldIgnoreUnhandledExceptions_AndResetTheCountOnSuccess()
    {
        var policy = new AsyncCircuitBreakerPolicy<string>(IsTransient, 2, TimeSpan.FromSeconds(30));

        await Assert.ThrowsAsync<HttpRequestException>(() => policy.ExecuteAsync(() => Task.FromException<string>(new HttpRequestException())));
        await policy.ExecuteAsync(() => Task.FromResult("ok"));
        await Assert.ThrowsAsync<HttpRequestException>(() => policy.ExecuteAsync(() => Task.FromException<string>(new HttpRequestException())));
        for (var i = 0; i < 3; i++)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => policy.ExecuteAsync(() => Task.FromException<string>(new InvalidOperationException())));
        }

        Assert.Equal(CircuitState.Closed, policy.CircuitState);
    }

    [Fact]
    public async Task TimeoutPolicy_ShouldReject_WhenAnActionObservingTheTokenRunsTooLong()
    {
        var time = new FakeTimeProvider();
        var policy = new AsyncTimeoutPolicy<string>(TimeSpan.FromSeconds(10), time);

        var execution = policy.ExecuteAsync(async token =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return "late";
        }, CancellationToken.None);
        time.Advance(TimeSpan.FromSeconds(10));

        var exception = await Assert.ThrowsAsync<TimeoutRejectedException>(() => execution);
        Assert.IsAssignableFrom<OperationCanceledException>(exception.InnerException);
    }

    [Fact]
    public async Task TimeoutPolicy_ShouldLetAnActionThatIgnoresTheTokenComplete()
    {
        var time = new FakeTimeProvider();
        var policy = new AsyncTimeoutPolicy<string>(TimeSpan.FromSeconds(10), time);
        var result = new TaskCompletionSource<string>();

        var execution = policy.ExecuteAsync(() => result.Task);
        time.Advance(TimeSpan.FromSeconds(20));
        result.SetResult("done");

        Assert.Equal("done", await execution);
    }

    [Fact]
    public async Task TimeoutPolicy_ShouldNotReportCallerCancellationAsATimeout()
    {
        var policy = new AsyncTimeoutPolicy<string>(TimeSpan.FromSeconds(10), new FakeTimeProvider());
        using var cancellation = new CancellationTokenSource();

        var execution = policy.ExecuteAsync(async token =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return "late";
        }, cancellation.Token);
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
    }

    [Fact]
    public async Task PolicyWrap_ShouldRunEveryRetryAttemptThroughTheInnerPolicies()
    {
        var retry = new AsyncRetryPolicy<string>(IsTransient, 3, _ => TimeSpan.Zero);
        var circuitBreaker = new AsyncCircuitBreakerPolicy<string>(IsTransient, 2, TimeSpan.FromSeconds(30));
        var policy = new AsyncPolicyWrap<string>(retry, circuitBreaker);
        var attempts = 0;

        // The breaker opens on the second attempt; the third is rejected, and the rejection isn't retried.
        await Assert.ThrowsAsync<BrokenCircuitException>(() => policy.ExecuteAsync(() =>
        {
            attempts++;
            return Task.FromException<string>(new HttpRequestException());
        }));

        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task ExecuteWithResilienceAsync_ShouldReturnTheResult_AndRunVoidActions()
    {
        var voidRuns = 0;

        var result = await ResilienceHelper.ExecuteWithResilienceAsync(() => Task.FromResult("value"));
        await ResilienceHelper.ExecuteWithResilienceAsync(() =>
        {
            voidRuns++;
            return Task.CompletedTask;
        });

        Assert.Equal("value", result);
        Assert.Equal(1, voidRuns);
    }

    [Fact]
    public async Task ExecuteWithResilienceAsync_ShouldLogAndReturnNull_WhenTheFailureIsNotTransient()
    {
        var originalOut = Console.Out;
        using var output = new StringWriter();
        Console.SetOut(output);
        var attempts = 0;
        try
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(() =>
            {
                attempts++;
                return Task.FromException<string>(new InvalidOperationException("not transient"));
            });

            Assert.Null(result);
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        Assert.Equal(1, attempts);
        Assert.Equal($"Request failed after all retries: not transient{Environment.NewLine}", output.ToString());
    }

    private static bool IsTransient(Exception exception)
    {
        return exception is HttpRequestException or TimeoutException;
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        var elapsed = Stopwatch.StartNew();
        while (!condition())
        {
            Assert.True(elapsed.Elapsed < TimeSpan.FromSeconds(5), "Timed out waiting for the condition.");
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }
    }
}
