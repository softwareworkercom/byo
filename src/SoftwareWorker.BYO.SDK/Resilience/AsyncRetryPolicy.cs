namespace SoftwareWorker.BYO.SDK.Resilience
{
    /// <summary>
    /// Re-executes the action when it throws a handled exception, up to <c>retryCount</c> more times, waiting
    /// <c>sleepDurationProvider(retryAttempt)</c> before each retry. Unhandled exceptions, and the handled exception
    /// of the last attempt, propagate unchanged.
    /// </summary>
    public sealed class AsyncRetryPolicy<T> : AsyncPolicy<T>
    {
        private readonly Func<Exception, bool> _shouldHandle;
        private readonly int _retryCount;
        private readonly Func<int, TimeSpan> _sleepDurationProvider;
        private readonly Action<Exception, TimeSpan, int>? _onRetry;
        private readonly TimeProvider _timeProvider;

        /// <param name="shouldHandle">Whether an exception is transient and worth retrying.</param>
        /// <param name="retryCount">Retries after the first attempt.</param>
        /// <param name="sleepDurationProvider">The wait before a retry, given the retry attempt (1 for the first retry).</param>
        /// <param name="onRetry">Called before each wait with the exception, the wait and the retry attempt.</param>
        /// <param name="timeProvider">Time source for the waits. Defaults to <see cref="TimeProvider.System"/>.</param>
        public AsyncRetryPolicy(
            Func<Exception, bool> shouldHandle,
            int retryCount,
            Func<int, TimeSpan> sleepDurationProvider,
            Action<Exception, TimeSpan, int>? onRetry = null,
            TimeProvider? timeProvider = null)
        {
            ArgumentNullException.ThrowIfNull(shouldHandle);
            ArgumentNullException.ThrowIfNull(sleepDurationProvider);
            if (retryCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(retryCount), "Value must be greater than or equal to zero.");
            }

            _shouldHandle = shouldHandle;
            _retryCount = retryCount;
            _sleepDurationProvider = sleepDurationProvider;
            _onRetry = onRetry;
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        public override async Task<T> ExecuteAsync(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
        {
            for (var retryAttempt = 1; ; retryAttempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    return await action(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    if (!_shouldHandle(exception) || retryAttempt > _retryCount)
                    {
                        throw;
                    }

                    var sleepDuration = _sleepDurationProvider(retryAttempt);
                    _onRetry?.Invoke(exception, sleepDuration, retryAttempt);

                    if (sleepDuration > TimeSpan.Zero)
                    {
                        await Task.Delay(sleepDuration, _timeProvider, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}
