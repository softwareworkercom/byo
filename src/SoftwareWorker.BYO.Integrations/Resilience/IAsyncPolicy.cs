namespace SoftwareWorker.BYO.Integrations.Resilience
{
    /// <summary>
    /// A resilience strategy (retry, circuit breaker, timeout, ...) that executes an asynchronous action.
    /// Policies are composed with <see cref="AsyncPolicyWrap{T}"/>.
    /// </summary>
    public interface IAsyncPolicy<T>
    {
        Task<T> ExecuteAsync(Func<Task<T>> action);

        /// <summary>
        /// Executes the action, passing it a token that is cancelled when <paramref name="cancellationToken"/> is,
        /// or when an <see cref="AsyncTimeoutPolicy{T}"/> elapses.
        /// </summary>
        Task<T> ExecuteAsync(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken);
    }
}
