using Xunit;

namespace ComplianceServiceManager.Operations.Tests;

public sealed class OperationHistoryStoreTests
{
    [Fact]
    public async Task RecordAsync_then_GetHistoryAsync_round_trips_the_finished_operations_shape()
    {
        var directory = TestPaths.NewTempDirectory();
        var engine = new OperationEngine(new OperationHistoryStore(directory));
        var descriptor = ScriptedOperationExecutor.BuildDescriptor("history-1", ("a", 1), ("b", 2));
        var executor = new ScriptedOperationExecutor(descriptor);

        var instance = await OperationEngineTests.RunToCompletionAsync(engine, executor);

        var store = new OperationHistoryStore(directory);
        var history = await store.GetHistoryAsync();

        var record = Assert.Single(history);
        Assert.Equal(instance.RunId, record.RunId);
        Assert.Equal("Test", record.Kind);
        Assert.Equal("history-1", record.TargetIdentity);
        Assert.Equal("Completed", record.Outcome);
        Assert.Equal(1, record.AttemptNumber);
        Assert.False(record.IsResume);
        Assert.Equal(2, record.StageTimings.Count);
        Assert.NotNull(record.FinishedUtc);
        Assert.NotNull(record.Duration);
        Assert.NotNull(record.LogFilePath);
        Assert.True(File.Exists(record.LogFilePath));
    }

    [Fact]
    public async Task GetHistoryAsync_filters_by_target_identity()
    {
        var directory = TestPaths.NewTempDirectory();
        var engine = new OperationEngine(new OperationHistoryStore(directory));

        await OperationEngineTests.RunToCompletionAsync(engine, new ScriptedOperationExecutor(ScriptedOperationExecutor.BuildDescriptor("history-a", ("x", 1))));
        await OperationEngineTests.RunToCompletionAsync(engine, new ScriptedOperationExecutor(ScriptedOperationExecutor.BuildDescriptor("history-b", ("x", 1))));

        var store = new OperationHistoryStore(directory);
        var filtered = await store.GetHistoryAsync(targetIdentity: "history-a");

        var record = Assert.Single(filtered);
        Assert.Equal("history-a", record.TargetIdentity);
    }

    [Fact]
    public async Task GetHistoryAsync_filters_by_kind()
    {
        var directory = TestPaths.NewTempDirectory();
        var engine = new OperationEngine(new OperationHistoryStore(directory));
        await OperationEngineTests.RunToCompletionAsync(engine, new ScriptedOperationExecutor(ScriptedOperationExecutor.BuildDescriptor("history-c", ("x", 1))));

        var store = new OperationHistoryStore(directory);
        var matching = await store.GetHistoryAsync(kind: "Test");
        var nonMatching = await store.GetHistoryAsync(kind: "SomethingElse");

        Assert.Single(matching);
        Assert.Empty(nonMatching);
    }

    [Fact]
    public async Task GetHistoryAsync_returns_most_recent_first()
    {
        var directory = TestPaths.NewTempDirectory();
        var engine = new OperationEngine(new OperationHistoryStore(directory));

        var first = await OperationEngineTests.RunToCompletionAsync(engine, new ScriptedOperationExecutor(ScriptedOperationExecutor.BuildDescriptor("history-order", ("x", 1))));
        await Task.Delay(10); // ensure a distinct, later StartedUtc
        var second = engine.Retry(first);
        await OperationInstanceProgressTests.WaitForTerminalAsync(engine, second);

        var store = new OperationHistoryStore(directory);
        var history = await store.GetHistoryAsync(targetIdentity: "history-order");

        Assert.Equal(2, history.Count);
        Assert.Equal(second.RunId, history[0].RunId);
        Assert.Equal(first.RunId, history[1].RunId);
    }

    [Fact]
    public async Task GetHistoryAsync_on_an_empty_store_returns_an_empty_list_not_an_error()
    {
        var store = new OperationHistoryStore(TestPaths.NewTempDirectory());

        var history = await store.GetHistoryAsync();

        Assert.Empty(history);
    }

    [Fact]
    public async Task A_corrupted_history_file_is_skipped_rather_than_failing_the_whole_read()
    {
        var directory = TestPaths.NewTempDirectory();
        var engine = new OperationEngine(new OperationHistoryStore(directory));
        await OperationEngineTests.RunToCompletionAsync(engine, new ScriptedOperationExecutor(ScriptedOperationExecutor.BuildDescriptor("history-corrupt", ("x", 1))));

        await File.WriteAllTextAsync(Path.Combine(directory, "corrupted.json"), "{ not valid json");

        var store = new OperationHistoryStore(directory);
        var history = await store.GetHistoryAsync();

        Assert.Single(history);
    }
}
