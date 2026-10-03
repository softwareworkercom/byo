namespace SoftwareWorker.BYO.SDK.Resilience
{
    /// <summary>
    /// Base for exceptions a policy throws instead of (or after abandoning) the executed action.
    /// </summary>
    public abstract class ExecutionRejectedException : Exception
    {
        protected ExecutionRejectedException(string message, Exception? innerException)
            : base(message, innerException)
        {
        }
    }
}
