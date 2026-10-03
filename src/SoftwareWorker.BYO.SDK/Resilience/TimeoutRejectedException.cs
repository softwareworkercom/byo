namespace SoftwareWorker.BYO.SDK.Resilience
{
    /// <summary>
    /// Thrown by <see cref="AsyncTimeoutPolicy{T}"/> when the action is cancelled because the timeout elapsed.
    /// </summary>
    public sealed class TimeoutRejectedException : ExecutionRejectedException
    {
        public TimeoutRejectedException(Exception? innerException)
            : base("The delegate executed asynchronously through TimeoutPolicy did not complete within the timeout.", innerException)
        {
        }
    }
}
