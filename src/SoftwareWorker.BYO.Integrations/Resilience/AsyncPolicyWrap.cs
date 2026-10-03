namespace SoftwareWorker.BYO.Integrations.Resilience
{
    /// <summary>
    /// Composes policies, outermost first: each policy executes the next one, and the innermost executes the action.
    /// For example retry, circuit breaker, timeout: every retry attempt goes through the circuit breaker and gets its own timeout.
    /// </summary>
    public sealed class AsyncPolicyWrap<T> : AsyncPolicy<T>
    {
        private readonly IAsyncPolicy<T>[] _policies;

        public AsyncPolicyWrap(params IAsyncPolicy<T>[] policies)
        {
            ArgumentNullException.ThrowIfNull(policies);
            if (policies.Length < 2)
            {
                throw new ArgumentException("The enumerable of policies to form the wrap must contain at least two policies.", nameof(policies));
            }

            _policies = [.. policies];
        }

        public override Task<T> ExecuteAsync(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
        {
            var next = action;
            for (var i = _policies.Length - 1; i >= 0; i--)
            {
                var policy = _policies[i];
                var inner = next;
                next = token => policy.ExecuteAsync(inner, token);
            }

            return next(cancellationToken);
        }
    }
}
