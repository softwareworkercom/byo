namespace SoftwareWorker.BYO.SDK.Resilience
{
    public abstract class AsyncPolicy<T> : IAsyncPolicy<T>
    {
        public Task<T> ExecuteAsync(Func<Task<T>> action)
        {
            return ExecuteAsync(_ => action(), CancellationToken.None);
        }

        public abstract Task<T> ExecuteAsync(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken);
    }
}
