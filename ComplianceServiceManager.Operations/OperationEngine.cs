namespace ComplianceServiceManager.Operations;

/// <summary>
/// Generic, operation-agnostic execution engine. Starts an <see cref="IOperationExecutor"/>,
/// tracks its live <see cref="OperationInstance"/>, enforces at most one running operation per
/// target identity, supports cooperative cancellation and retry/resume, and records every finished
/// run to <see cref="OperationHistoryStore"/>. Knows nothing about Bootstrap, Publish, or any other
/// specific operation — see <see cref="IOperationExecutor"/>.
///
/// Cancellation is honest, not immediate: <see cref="Cancel"/> signals the executor's
/// <see cref="OperationContext.CancellationToken"/>; the executor is expected to observe it between
/// discrete units of work (matching, for Bootstrap, the "finishes the current remote command, then
/// stops" granularity <c>SshBootstrapTargetExecutor</c> actually has). The engine never force-aborts
/// a running executor.
/// </summary>
public sealed class OperationEngine
{
    private readonly OperationHistoryStore historyStore;
    private readonly object gate = new();
    private readonly Dictionary<string, OperationInstance> activeByTarget = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> attemptsByTarget = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<OperationInstance, (IOperationExecutor Executor, CancellationTokenSource Cts)> runs = [];

    public OperationEngine(OperationHistoryStore historyStore)
    {
        this.historyStore = historyStore;
    }

    /// <summary>Raised the moment a new run starts (before any stage has begun).</summary>
    public event Action<OperationInstance>? OperationStarted;

    /// <summary>Raised once a run reaches a terminal state (<see cref="OperationStatus.Completed"/>, <see cref="OperationStatus.Failed"/>, or <see cref="OperationStatus.Cancelled"/>), after its history record has been written.</summary>
    public event Action<OperationInstance>? OperationCompleted;

    /// <summary>Starts a new run. Throws <see cref="InvalidOperationException"/> if a run is already active for <paramref name="executor"/>'s <see cref="OperationDescriptor.TargetIdentity"/> — callers (a wizard's "Bootstrap Environment" button, a "Retry" button) are expected to disable themselves while a run is active rather than relying on this exception for normal flow control.</summary>
    public OperationInstance Start(IOperationExecutor executor, bool isResume = false)
    {
        ArgumentNullException.ThrowIfNull(executor);

        OperationInstance instance;
        CancellationTokenSource cts;
        lock (gate)
        {
            var descriptor = executor.Descriptor;
            if (activeByTarget.TryGetValue(descriptor.TargetIdentity, out var existing) && existing.Status == OperationStatus.Running)
            {
                throw new InvalidOperationException($"An operation is already running for target '{descriptor.TargetIdentity}'.");
            }

            var attempt = attemptsByTarget.TryGetValue(descriptor.TargetIdentity, out var previousAttempt) ? previousAttempt + 1 : 1;
            attemptsByTarget[descriptor.TargetIdentity] = attempt;

            instance = new OperationInstance(descriptor, isResume, attempt);
            cts = new CancellationTokenSource();
            runs[instance] = (executor, cts);
            activeByTarget[descriptor.TargetIdentity] = instance;
        }

        OperationStarted?.Invoke(instance);
        _ = RunAsync(instance, executor, cts);
        return instance;
    }

    /// <summary>Requests cancellation of a running operation. A no-op if the operation is already terminal or unknown to this engine instance.</summary>
    public void Cancel(OperationInstance instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        if (runs.TryGetValue(instance, out var run))
        {
            run.Cts.Cancel();
        }
    }

    /// <summary>Re-invokes the same executor that produced <paramref name="instance"/> as a new run against the same target. Whether previously-completed work is actually skipped is entirely up to the executor/backend (for Bootstrap, <c>BootstrapStateStore</c>'s resumability) — the engine's own contribution is only "same executor, new attempt, attempt count incremented."</summary>
    public OperationInstance Retry(OperationInstance instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        if (!runs.TryGetValue(instance, out var run))
        {
            throw new InvalidOperationException("This operation instance was not started by this engine.");
        }

        return Start(run.Executor, isResume: instance.Descriptor.SupportsResume);
    }

    /// <summary>History for a specific target and/or operation kind — delegates directly to <see cref="OperationHistoryStore"/>.</summary>
    public Task<IReadOnlyList<OperationHistoryRecord>> GetHistoryAsync(string? targetIdentity = null, string? kind = null, CancellationToken cancellationToken = default)
        => historyStore.GetHistoryAsync(targetIdentity, kind, cancellationToken);

    private async Task RunAsync(OperationInstance instance, IOperationExecutor executor, CancellationTokenSource cts)
    {
        var context = new OperationContext(instance, cts.Token);

        try
        {
            await Task.Run(() => executor.ExecuteAsync(context));

            var failedStage = instance.Stages.FirstOrDefault(stage => stage.Status == OperationStageStatus.Failed);
            instance.MarkTerminal(
                failedStage is null ? OperationStatus.Completed : OperationStatus.Failed,
                failedStage?.Failure);
        }
        catch (OperationCanceledException)
        {
            instance.MarkTerminal(OperationStatus.Cancelled, null);
        }
        catch (Exception exception)
        {
            // Safety net for an executor that throws instead of calling FailStage — every run still
            // reaches a terminal state and a categorized failure, never a raw unhandled exception
            // surfacing to the presentation layer.
            var failure = new OperationFailure
            {
                Problem = $"{instance.Descriptor.Kind} failed unexpectedly.",
                Cause = exception.Message,
                RecommendedAction = "Review the details below and retry.",
                Details = exception.ToString()
            };

            var runningStage = instance.Stages.FirstOrDefault(stage => stage.Status == OperationStageStatus.Running);
            if (runningStage is not null)
            {
                instance.FailStage(runningStage.Definition.Id, failure);
            }

            instance.MarkTerminal(OperationStatus.Failed, failure);
        }
        finally
        {
            await historyStore.RecordAsync(instance);
            OperationCompleted?.Invoke(instance);
        }
    }
}
