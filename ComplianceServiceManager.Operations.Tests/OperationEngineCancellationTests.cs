using Xunit;

namespace ComplianceServiceManager.Operations.Tests;

/// <summary>Cancellation tests. Matches the plan's "cancellation must be honest" rule: the engine
/// only ever signals <see cref="OperationContext.CancellationToken"/> — it never force-aborts a
/// running executor, so these tests assert on cooperative cancellation (the executor observing the
/// token between stages), not on the engine somehow interrupting mid-stage work it has no way to
/// interrupt.</summary>
public sealed class OperationEngineCancellationTests
{
    [Fact]
    public async Task Cancel_before_the_next_stage_begins_results_in_a_Cancelled_operation()
    {
        var engine = new OperationEngine(new OperationHistoryStore(TestPaths.NewTempDirectory()));
        var descriptor = ScriptedOperationExecutor.BuildDescriptor("cancel-1", ("a", 1), ("b", 1), ("c", 1));
        var executor = new ScriptedOperationExecutor(descriptor, new Dictionary<string, StageScript>
        {
            ["a"] = new StageScript(Delay: TimeSpan.FromMilliseconds(50))
        });

        var instance = engine.Start(executor);
        // Cancel while stage "a" is still mid-delay — the executor observes the token at the top of
        // its next loop iteration (ThrowIfCancellationRequested), the same "between discrete units of
        // work" granularity SshBootstrapTargetExecutor has for real remote commands.
        engine.Cancel(instance);

        await OperationInstanceProgressTests.WaitForTerminalAsync(engine, instance);

        Assert.Equal(OperationStatus.Cancelled, instance.Status);
        // Stage "a" was already Running when cancellation was requested; it never got the chance to
        // report a different terminal status via FailStage/CompleteStage, so it is left exactly as
        // the executor left it (Running) — the engine does not invent a "Cancelled" per-stage status
        // for a stage that never called back to say it stopped mid-flight.
        Assert.NotEqual(OperationStageStatus.Completed, instance.Stages[0].Status);
    }

    [Fact]
    public async Task Cancel_after_the_operation_already_finished_is_a_harmless_no_op()
    {
        var engine = new OperationEngine(new OperationHistoryStore(TestPaths.NewTempDirectory()));
        var descriptor = ScriptedOperationExecutor.BuildDescriptor("cancel-2", ("a", 1));
        var executor = new ScriptedOperationExecutor(descriptor);

        var instance = await OperationEngineTests.RunToCompletionAsync(engine, executor);

        var exception = Record.Exception(() => engine.Cancel(instance));

        Assert.Null(exception);
        Assert.Equal(OperationStatus.Completed, instance.Status);
    }

    [Fact]
    public async Task Completed_stages_before_cancellation_stay_Completed()
    {
        var engine = new OperationEngine(new OperationHistoryStore(TestPaths.NewTempDirectory()));
        var descriptor = ScriptedOperationExecutor.BuildDescriptor("cancel-3", ("a", 1), ("b", 1));
        var executor = new ScriptedOperationExecutor(descriptor, new Dictionary<string, StageScript>
        {
            ["b"] = new StageScript(Delay: TimeSpan.FromMilliseconds(100))
        });

        var instance = engine.Start(executor);
        var cancelledAfterA = false;
        instance.Changed += _ =>
        {
            if (!cancelledAfterA && instance.Stages[0].Status == OperationStageStatus.Completed)
            {
                cancelledAfterA = true;
                engine.Cancel(instance);
            }
        };

        await OperationInstanceProgressTests.WaitForTerminalAsync(engine, instance);

        Assert.Equal(OperationStatus.Cancelled, instance.Status);
        Assert.Equal(OperationStageStatus.Completed, instance.Stages[0].Status);
    }
}
