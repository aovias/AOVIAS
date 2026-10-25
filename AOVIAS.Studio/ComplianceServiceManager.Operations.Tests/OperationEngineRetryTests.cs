using Xunit;

namespace ComplianceServiceManager.Operations.Tests;

/// <summary>Retry/resume tests. The engine's own contribution to "retry" is narrow by design (same
/// executor, new attempt, attempt count incremented, <see cref="OperationContext.IsResume"/> set) —
/// whether previously-completed work is actually skipped is entirely up to the executor/backend
/// (for Bootstrap, <c>BootstrapStateStore</c>), so these tests assert the engine's part of the
/// contract, not backend-specific resumability (that is covered by Aovias.Bootstrap.Core.Tests).</summary>
public sealed class OperationEngineRetryTests
{
    [Fact]
    public async Task Retry_starts_a_new_instance_against_the_same_executor_with_attempt_number_incremented()
    {
        var engine = new OperationEngine(new OperationHistoryStore(TestPaths.NewTempDirectory()));
        var descriptor = ScriptedOperationExecutor.BuildDescriptor("retry-1", ("a", 1), ("b", 1));
        var executor = new ScriptedOperationExecutor(descriptor, new Dictionary<string, StageScript>
        {
            ["b"] = new StageScript(ScriptedStageAction.Fail)
        });

        var firstAttempt = await OperationEngineTests.RunToCompletionAsync(engine, executor);
        Assert.Equal(1, firstAttempt.AttemptNumber);
        Assert.Equal(OperationStatus.Failed, firstAttempt.Status);

        var secondInstance = engine.Retry(firstAttempt);
        await OperationInstanceProgressTests.WaitForTerminalAsync(engine, secondInstance);

        Assert.Equal(2, secondInstance.AttemptNumber);
        Assert.NotSame(firstAttempt, secondInstance);
        // Retrying does not mutate the original failed instance's own recorded state.
        Assert.Equal(1, firstAttempt.AttemptNumber);
        Assert.Equal(OperationStatus.Failed, firstAttempt.Status);
    }

    [Fact]
    public async Task Retry_sets_IsResume_to_the_descriptors_SupportsResume_flag()
    {
        var engine = new OperationEngine(new OperationHistoryStore(TestPaths.NewTempDirectory()));
        var descriptor = ScriptedOperationExecutor.BuildDescriptor("retry-2", ("a", 1)); // SupportsResume = true, see ScriptedOperationExecutor.BuildDescriptor
        var executor = new ScriptedOperationExecutor(descriptor, new Dictionary<string, StageScript>
        {
            ["a"] = new StageScript(ScriptedStageAction.Fail)
        });

        var firstAttempt = await OperationEngineTests.RunToCompletionAsync(engine, executor);
        var retried = engine.Retry(firstAttempt);

        Assert.True(retried.IsResume);
        await OperationInstanceProgressTests.WaitForTerminalAsync(engine, retried);
    }

    [Fact]
    public async Task Retry_after_a_successful_run_still_increments_the_attempt_counter_for_that_target()
    {
        var engine = new OperationEngine(new OperationHistoryStore(TestPaths.NewTempDirectory()));
        var descriptor = ScriptedOperationExecutor.BuildDescriptor("retry-3", ("a", 1));
        var executor = new ScriptedOperationExecutor(descriptor);

        var first = await OperationEngineTests.RunToCompletionAsync(engine, executor);
        var second = engine.Retry(first);
        await OperationInstanceProgressTests.WaitForTerminalAsync(engine, second);

        Assert.Equal(1, first.AttemptNumber);
        Assert.Equal(2, second.AttemptNumber);
    }

    [Fact]
    public void Retry_on_an_instance_this_engine_never_started_throws()
    {
        var engine = new OperationEngine(new OperationHistoryStore(TestPaths.NewTempDirectory()));
        var otherEngine = new OperationEngine(new OperationHistoryStore(TestPaths.NewTempDirectory()));
        var descriptor = ScriptedOperationExecutor.BuildDescriptor("retry-4", ("a", 1));
        var foreignInstance = otherEngine.Start(new ScriptedOperationExecutor(descriptor));

        Assert.Throws<InvalidOperationException>(() => engine.Retry(foreignInstance));
    }
}
