using Xunit;

namespace ComplianceServiceManager.Operations.Tests;

/// <summary>Progress-math tests: <see cref="OperationEngine.Start"/> returns the
/// <see cref="OperationInstance"/> synchronously, before the executor's background work begins, so
/// a test can subscribe to <see cref="OperationInstance.Changed"/> and sample state at precise
/// moments during the run — proving the weighted-progress and null-until-real-progress ETA rules
/// the plan requires, rather than asserting only on the final state.</summary>
public sealed class OperationInstanceProgressTests
{
    [Fact]
    public async Task OverallProgress_weights_stages_unequally_by_their_declared_weight()
    {
        // Weight 1/3/1, total 5 — stage "b" is three times as expensive as "a" or "c".
        var engine = new OperationEngine(new OperationHistoryStore(TestPaths.NewTempDirectory()));
        var descriptor = ScriptedOperationExecutor.BuildDescriptor("progress-1", ("a", 1), ("b", 3), ("c", 1));
        var executor = new ScriptedOperationExecutor(descriptor);

        var progressAfterA = double.NaN;
        var instance = engine.Start(executor);
        instance.Changed += _ =>
        {
            if (instance.Stages[0].Status == OperationStageStatus.Completed && double.IsNaN(progressAfterA))
            {
                progressAfterA = instance.OverallProgress;
            }
        };

        await WaitForTerminalAsync(engine, instance);

        // After only "a" (weight 1 of 5 total) completes, overall progress is 1/5 = 0.2 — not 1/3
        // (which is what an equal-weight-per-stage assumption would wrongly produce).
        Assert.Equal(0.2, progressAfterA, precision: 6);
        Assert.Equal(1.0, instance.OverallProgress, precision: 6);
    }

    [Fact]
    public async Task OverallProgress_reflects_a_running_stages_sub_progress_before_it_completes()
    {
        var engine = new OperationEngine(new OperationHistoryStore(TestPaths.NewTempDirectory()));
        var descriptor = ScriptedOperationExecutor.BuildDescriptor("progress-2", ("a", 1), ("b", 1));
        var executor = new ScriptedOperationExecutor(descriptor, new Dictionary<string, StageScript>
        {
            ["a"] = new StageScript(SubProgressBeforeComplete: 0.5)
        });

        double? progressAtHalfway = null;
        var instance = engine.Start(executor);
        instance.Changed += _ =>
        {
            if (instance.Stages[0].Status == OperationStageStatus.Running
                && Math.Abs(instance.Stages[0].SubProgress - 0.5) < 0.0001
                && progressAtHalfway is null)
            {
                progressAtHalfway = instance.OverallProgress;
            }
        };

        await WaitForTerminalAsync(engine, instance);

        // Stage "a" (weight 1 of 2 total) reported 50% sub-progress: overall = 0.5 * (1/2) = 0.25.
        Assert.NotNull(progressAtHalfway);
        Assert.Equal(0.25, progressAtHalfway!.Value, precision: 6);
    }

    [Fact]
    public async Task EstimatedRemaining_is_null_until_at_least_one_weighted_stage_has_completed()
    {
        var engine = new OperationEngine(new OperationHistoryStore(TestPaths.NewTempDirectory()));
        var descriptor = ScriptedOperationExecutor.BuildDescriptor("progress-3", ("a", 1), ("b", 1));
        var executor = new ScriptedOperationExecutor(descriptor, new Dictionary<string, StageScript>
        {
            ["a"] = new StageScript(SubProgressBeforeComplete: 0.5)
        });

        var etaWhileFirstStageRunning = "unset";
        var instance = engine.Start(executor);
        instance.Changed += _ =>
        {
            if (instance.Stages[0].Status == OperationStageStatus.Running && etaWhileFirstStageRunning == "unset")
            {
                etaWhileFirstStageRunning = instance.EstimatedRemaining is null ? "null" : "non-null";
            }
        };

        await WaitForTerminalAsync(engine, instance);

        Assert.Equal("null", etaWhileFirstStageRunning);
        // Once finished, no "remaining" time is meaningful either.
        Assert.Null(instance.EstimatedRemaining);
    }

    [Fact]
    public async Task EstimatedRemaining_becomes_non_null_once_a_stage_has_completed_with_more_remaining()
    {
        var engine = new OperationEngine(new OperationHistoryStore(TestPaths.NewTempDirectory()));
        var descriptor = ScriptedOperationExecutor.BuildDescriptor("progress-3b", ("a", 1), ("b", 1), ("c", 1));
        var executor = new ScriptedOperationExecutor(descriptor, new Dictionary<string, StageScript>
        {
            ["a"] = new StageScript(Delay: TimeSpan.FromMilliseconds(20))
        });

        TimeSpan? etaAfterFirstStage = null;
        var instance = engine.Start(executor);
        instance.Changed += _ =>
        {
            if (instance.Stages[0].Status == OperationStageStatus.Completed
                && instance.Stages[1].Status != OperationStageStatus.Completed
                && etaAfterFirstStage is null)
            {
                etaAfterFirstStage = instance.EstimatedRemaining;
            }
        };

        await WaitForTerminalAsync(engine, instance);

        Assert.NotNull(etaAfterFirstStage);
        Assert.True(etaAfterFirstStage!.Value >= TimeSpan.Zero);
    }

    [Fact]
    public async Task Skipped_and_Warning_stages_count_toward_completed_weight_the_same_as_Completed()
    {
        var engine = new OperationEngine(new OperationHistoryStore(TestPaths.NewTempDirectory()));
        var descriptor = ScriptedOperationExecutor.BuildDescriptor("progress-4", ("a", 1), ("b", 1), ("c", 1));
        var executor = new ScriptedOperationExecutor(descriptor, new Dictionary<string, StageScript>
        {
            ["a"] = new StageScript(ScriptedStageAction.Skip),
            ["b"] = new StageScript(ScriptedStageAction.Warn)
        });

        var instance = engine.Start(executor);
        await WaitForTerminalAsync(engine, instance);

        Assert.Equal(1.0, instance.OverallProgress, precision: 6);
    }

    internal static async Task WaitForTerminalAsync(OperationEngine engine, OperationInstance instance)
    {
        if (instance.Status != OperationStatus.Running)
        {
            return;
        }

        var tcs = new TaskCompletionSource();
        void Handler(OperationInstance completed)
        {
            if (ReferenceEquals(completed, instance))
            {
                tcs.TrySetResult();
            }
        }

        engine.OperationCompleted += Handler;
        try
        {
            if (instance.Status != OperationStatus.Running)
            {
                return;
            }

            await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }
        finally
        {
            engine.OperationCompleted -= Handler;
        }
    }
}
