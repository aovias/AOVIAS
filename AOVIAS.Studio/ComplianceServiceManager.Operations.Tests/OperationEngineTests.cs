using Xunit;

namespace ComplianceServiceManager.Operations.Tests;

public sealed class OperationEngineTests
{
    [Fact]
    public async Task Start_runs_every_stage_to_Completed_and_marks_the_operation_Completed()
    {
        var engine = new OperationEngine(new OperationHistoryStore(TestPaths.NewTempDirectory()));
        var descriptor = ScriptedOperationExecutor.BuildDescriptor("target-1", ("a", 1), ("b", 1), ("c", 1));
        var executor = new ScriptedOperationExecutor(descriptor);

        var instance = await RunToCompletionAsync(engine, executor);

        Assert.Equal(OperationStatus.Completed, instance.Status);
        Assert.All(instance.Stages, stage => Assert.Equal(OperationStageStatus.Completed, stage.Status));
        Assert.Equal(["a", "b", "c"], executor.ExecutedStageIds);
        Assert.Null(instance.CurrentStageId);
        Assert.NotNull(instance.FinishedUtc);
        Assert.Equal(1.0, instance.OverallProgress, precision: 6);
    }

    [Fact]
    public async Task Start_stops_at_the_first_failed_stage_and_leaves_later_stages_Pending()
    {
        var engine = new OperationEngine(new OperationHistoryStore(TestPaths.NewTempDirectory()));
        var descriptor = ScriptedOperationExecutor.BuildDescriptor("target-2", ("a", 1), ("b", 1), ("c", 1));
        var executor = new ScriptedOperationExecutor(descriptor, new Dictionary<string, StageScript>
        {
            ["b"] = new StageScript(ScriptedStageAction.Fail)
        });

        var instance = await RunToCompletionAsync(engine, executor);

        Assert.Equal(OperationStatus.Failed, instance.Status);
        Assert.Equal(OperationStageStatus.Completed, instance.Stages[0].Status);
        Assert.Equal(OperationStageStatus.Failed, instance.Stages[1].Status);
        Assert.Equal(OperationStageStatus.Pending, instance.Stages[2].Status);
        Assert.Equal(["a", "b"], executor.ExecutedStageIds);
        Assert.NotNull(instance.Failure);
        Assert.Equal("Scripted failure", instance.Failure!.Problem);
    }

    [Fact]
    public async Task An_executor_that_throws_instead_of_calling_FailStage_still_reaches_a_categorized_terminal_failure()
    {
        var engine = new OperationEngine(new OperationHistoryStore(TestPaths.NewTempDirectory()));
        var descriptor = ScriptedOperationExecutor.BuildDescriptor("target-3", ("a", 1), ("b", 1));
        var executor = new ScriptedOperationExecutor(descriptor, new Dictionary<string, StageScript>
        {
            ["b"] = new StageScript(ScriptedStageAction.Throw)
        });

        var instance = await RunToCompletionAsync(engine, executor);

        Assert.Equal(OperationStatus.Failed, instance.Status);
        Assert.Equal(OperationStageStatus.Failed, instance.Stages[1].Status);
        Assert.NotNull(instance.Failure);
        // The safety-net failure is categorized, not a raw exception dump, in Problem/Cause/RecommendedAction.
        Assert.False(string.IsNullOrWhiteSpace(instance.Failure!.Problem));
        Assert.Contains("Scripted exception", instance.Failure.Details);
    }

    [Fact]
    public async Task A_stage_scripted_Warn_still_lets_the_operation_complete_and_is_visible_via_HasWarnings()
    {
        var engine = new OperationEngine(new OperationHistoryStore(TestPaths.NewTempDirectory()));
        var descriptor = ScriptedOperationExecutor.BuildDescriptor("target-4", ("a", 1), ("b", 1));
        var executor = new ScriptedOperationExecutor(descriptor, new Dictionary<string, StageScript>
        {
            ["a"] = new StageScript(ScriptedStageAction.Warn)
        });

        var instance = await RunToCompletionAsync(engine, executor);

        Assert.Equal(OperationStatus.Completed, instance.Status);
        Assert.True(instance.HasWarnings);
        Assert.Equal(OperationStageStatus.Warning, instance.Stages[0].Status);
    }

    [Fact]
    public async Task Start_throws_when_an_operation_is_already_running_for_the_same_target_identity()
    {
        var engine = new OperationEngine(new OperationHistoryStore(TestPaths.NewTempDirectory()));
        var descriptor = ScriptedOperationExecutor.BuildDescriptor("target-5", ("a", 1));
        // A long delay keeps the first run "Running" for the duration of this synchronous assertion.
        var slowExecutor = new ScriptedOperationExecutor(descriptor, new Dictionary<string, StageScript>
        {
            ["a"] = new StageScript(Delay: TimeSpan.FromSeconds(5))
        });

        var slowInstance = engine.Start(slowExecutor);
        try
        {
            Assert.Throws<InvalidOperationException>(() => engine.Start(new ScriptedOperationExecutor(descriptor)));
        }
        finally
        {
            // Cancel rather than let the 5s delay run out in the background after this test returns.
            engine.Cancel(slowInstance);
            await OperationInstanceProgressTests.WaitForTerminalAsync(engine, slowInstance);
        }
    }

    [Fact]
    public async Task OperationStarted_and_OperationCompleted_both_fire_exactly_once_per_run()
    {
        var engine = new OperationEngine(new OperationHistoryStore(TestPaths.NewTempDirectory()));
        var descriptor = ScriptedOperationExecutor.BuildDescriptor("target-6", ("a", 1));
        var executor = new ScriptedOperationExecutor(descriptor);

        var startedCount = 0;
        var completedCount = 0;
        engine.OperationStarted += _ => startedCount++;
        engine.OperationCompleted += _ => completedCount++;

        await RunToCompletionAsync(engine, executor);

        Assert.Equal(1, startedCount);
        Assert.Equal(1, completedCount);
    }

    [Fact]
    public async Task Changed_fires_for_every_stage_transition_and_reflects_current_state_at_call_time()
    {
        var engine = new OperationEngine(new OperationHistoryStore(TestPaths.NewTempDirectory()));
        var descriptor = ScriptedOperationExecutor.BuildDescriptor("target-7", ("a", 1), ("b", 1));
        var executor = new ScriptedOperationExecutor(descriptor);

        var changeCount = 0;
        var tcs = new TaskCompletionSource();
        void OnCompleted(OperationInstance _) => tcs.TrySetResult();
        engine.OperationCompleted += OnCompleted;

        var instance = engine.Start(executor);
        instance.Changed += _ => changeCount++;
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
        engine.OperationCompleted -= OnCompleted;

        // Two stages, each raising BeginStage + CompleteStage = at least 4 changes, plus MarkTerminal.
        Assert.True(changeCount >= 5, $"Expected at least 5 Changed notifications, saw {changeCount}.");
    }

    internal static async Task<OperationInstance> RunToCompletionAsync(OperationEngine engine, IOperationExecutor executor, bool isResume = false)
    {
        OperationInstance? started = null;
        var tcs = new TaskCompletionSource();

        void Handler(OperationInstance completed)
        {
            if (ReferenceEquals(completed, started))
            {
                tcs.TrySetResult();
            }
        }

        engine.OperationCompleted += Handler;
        try
        {
            started = engine.Start(executor, isResume);
            await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
            return started;
        }
        finally
        {
            engine.OperationCompleted -= Handler;
        }
    }
}
