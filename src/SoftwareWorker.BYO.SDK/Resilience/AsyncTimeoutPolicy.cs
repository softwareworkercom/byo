namespace SoftwareWorker.BYO.SDK.Resilience
{
    /// <summary>
    /// Optimistic timeout: the token passed to the action is cancelled once the timeout elapses, and an
    /// <see cref="OperationCanceledException"/> caused by that becomes a <see cref="TimeoutRejectedException"/>.
    /// An action that doesn't observe the token is never interrupted and runs to completion.
    /// </summary>
    public sealed class AsyncTimeoutPolicy<T> : AsyncPolicy<T>
    {
        private readonly TimeSpan _timeout;
        private readonly TimeProvider _timeProvider;

        /// <param name="timeout">A positive duration, or <see cref="Timeout.InfiniteTimeSpan"/> for no timeout.</param>
        /// <param name="timeProvider">Clock for the timeout. Defaults to <see cref="TimeProvider.System"/>.</param>
        public AsyncTimeoutPolicy(TimeSpan timeout, TimeProvider? timeProvider = null)
        {
            if (timeout <= TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout), timeout, $"{nameof(timeout)} must be a positive TimeSpan (or Timeout.InfiniteTimeSpan to indicate no timeout)");
            }

            _timeout = timeout;
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        public override async Task<T> ExecuteAsync(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var timeoutSource = new CancellationTokenSource(_timeout, _timeProvider);
            using var combinedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);

            try
            {
                return await action(combinedSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException exception) when (timeoutSource.IsCancellationRequested)
            {
                throw new TimeoutRejectedException(exception);
            }
        }
    }
}
