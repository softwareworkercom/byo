namespace SoftwareWorker.BYO.SDK.Resilience
{
    public enum CircuitState
    {
        /// <summary>Calls flow through; consecutive handled failures are counted.</summary>
        Closed,

        /// <summary>Calls are rejected with <see cref="BrokenCircuitException"/> until the break duration elapses.</summary>
        Open,

        /// <summary>The break has elapsed; one trial call per break duration decides whether the circuit closes or breaks again.</summary>
        HalfOpen
    }
}
