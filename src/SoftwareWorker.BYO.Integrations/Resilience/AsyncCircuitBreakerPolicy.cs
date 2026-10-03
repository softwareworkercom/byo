namespace SoftwareWorker.BYO.Integrations.Resilience
{
    /// <summary>
    /// Stops calling a failing dependency. After <c>exceptionsAllowedBeforeBreaking</c> consecutive handled exceptions the
    /// circuit opens and calls fail fast with <see cref="BrokenCircuitException"/> for <c>durationOfBreak</c>. It then
    /// becomes half-open: one trial call per break duration either closes the circuit (success) or breaks it again
    /// (handled exception). Unhandled exceptions don't affect the circuit.
    /// </summary>
    /// <remarks>
    /// State lives in the policy instance, so the circuit only protects calls executed through the same instance.
    /// </remarks>
    public sealed class AsyncCircuitBreakerPolicy<T> : AsyncPolicy<T>
    {
        private readonly object _lock = new();
        private readonly Func<Exception, bool> _shouldHandle;
        private readonly int _exceptionsAllowedBeforeBreaking;
        private readonly TimeSpan _durationOfBreak;
        private readonly Action<Exception, TimeSpan>? _onBreak;
        private readonly Action? _onReset;
        private readonly TimeProvider _timeProvider;

        private CircuitState _circuitState = CircuitState.Closed;
        private int _consecutiveFailureCount;
        private DateTimeOffset _blockedUntil = DateTimeOffset.MinValue;
        private Exception? _lastException;

        /// <param name="shouldHandle">Whether an exception counts as a failure of the dependency.</param>
        /// <param name="exceptionsAllowedBeforeBreaking">Consecutive handled exceptions that open the circuit.</param>
        /// <param name="durationOfBreak">How long the circuit stays open before allowing a trial call.</param>
        /// <param name="onBreak">Called when the circuit opens, with the exception that opened it and the break duration.</param>
        /// <param name="onReset">Called when a successful trial call closes the circuit.</param>
        /// <param name="timeProvider">Clock for the break duration. Defaults to <see cref="TimeProvider.System"/>.</param>
        public AsyncCircuitBreakerPolicy(
            Func<Exception, bool> shouldHandle,
            int exceptionsAllowedBeforeBreaking,
            TimeSpan durationOfBreak,
            Action<Exception, TimeSpan>? onBreak = null,
            Action? onReset = null,
            TimeProvider? timeProvider = null)
        {
            ArgumentNullException.ThrowIfNull(shouldHandle);
            if (exceptionsAllowedBeforeBreaking <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(exceptionsAllowedBeforeBreaking), "Value must be greater than zero.");
            }

            if (durationOfBreak < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(durationOfBreak), "Value must be greater than zero.");
            }

            _shouldHandle = shouldHandle;
            _exceptionsAllowedBeforeBreaking = exceptionsAllowedBeforeBreaking;
            _durationOfBreak = durationOfBreak;
            _onBreak = onBreak;
            _onReset = onReset;
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        public CircuitState CircuitState
        {
            get
            {
                lock (_lock)
                {
                    if (_circuitState == CircuitState.Open && _timeProvider.GetUtcNow() >= _blockedUntil)
                    {
                        _circuitState = CircuitState.HalfOpen;
                    }

                    return _circuitState;
                }
            }
        }

        public override async Task<T> ExecuteAsync(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            OnActionPreExecute();

            T result;
            try
            {
                result = await action(cancellationToken);
            }
            catch (Exception exception) when (_shouldHandle(exception))
            {
                OnActionFailure(exception);
                throw;
            }

            OnActionSuccess();
            return result;
        }

        private void OnActionPreExecute()
        {
            lock (_lock)
            {
                switch (CircuitState)
                {
                    case CircuitState.Open:
                        throw new BrokenCircuitException(_lastException);

                    case CircuitState.HalfOpen:
                        // Permit one trial call, then block others until it settles the circuit or another break duration passes.
                        var now = _timeProvider.GetUtcNow();
                        if (now < _blockedUntil)
                        {
                            throw new BrokenCircuitException(_lastException);
                        }

                        _blockedUntil = AddClamped(now, _durationOfBreak);
                        break;
                }
            }
        }

        private void OnActionFailure(Exception exception)
        {
            lock (_lock)
            {
                _lastException = exception;

                switch (_circuitState)
                {
                    case CircuitState.HalfOpen:
                        Break();
                        break;

                    case CircuitState.Closed:
                        _consecutiveFailureCount++;
                        if (_consecutiveFailureCount >= _exceptionsAllowedBeforeBreaking)
                        {
                            Break();
                        }
                        break;

                    // Open: the call started before the circuit broke. Breaking again would extend the break and signal it twice.
                }
            }
        }

        private void OnActionSuccess()
        {
            lock (_lock)
            {
                switch (_circuitState)
                {
                    case CircuitState.HalfOpen:
                        _circuitState = CircuitState.Closed;
                        _consecutiveFailureCount = 0;
                        _blockedUntil = DateTimeOffset.MinValue;
                        _lastException = null;
                        _onReset?.Invoke();
                        break;

                    case CircuitState.Closed:
                        _consecutiveFailureCount = 0;
                        break;

                    // Open: the call started before the circuit broke. Only the break duration elapsing moves it on.
                }
            }
        }

        private void Break()
        {
            _blockedUntil = AddClamped(_timeProvider.GetUtcNow(), _durationOfBreak);
            _circuitState = CircuitState.Open;
            _onBreak?.Invoke(_lastException!, _durationOfBreak);
        }

        private static DateTimeOffset AddClamped(DateTimeOffset time, TimeSpan duration)
        {
            return duration > DateTimeOffset.MaxValue - time ? DateTimeOffset.MaxValue : time + duration;
        }
    }
}
