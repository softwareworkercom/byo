namespace SoftwareWorker.BYO.SDK.Resilience
{
    /// <summary>
    /// Thrown by <see cref="AsyncCircuitBreakerPolicy{T}"/> while the circuit is open. The inner exception is the last handled failure.
    /// </summary>
    public sealed class BrokenCircuitException : ExecutionRejectedException
    {
        public BrokenCircuitException(Exception? innerException)
            : base("The circuit is now open and is not allowing calls.", innerException)
        {
        }
    }
}
