using System.Threading.Channels;
using Aovias.Configuration.Settings;
using Aovias.Orchestration.Contracts;
using Aovias.Orchestration.Contracts.Enums;
using Aovias.Orchestration.Contracts.Events;
using Aovias.Orchestration.Contracts.Workflows;
using Aovias.Platform.Core;
using PlatformExecutionContext = Aovias.Platform.Core.ExecutionContext;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aovias.Orchestration.Engine;

/// <summary>
/// Queues and executes workflows using configured worker limits.
/// </summary>
public sealed class ExecutionEngine : IExecutionEngine, IAsyncDisposable
{
    private readonly IWorkflowRegistry registry;
    private readonly IWorkflowExecutor executor;
    private readonly InMemoryWorkflowStore store;
    private readonly IOptionsMonitor<OrchestrationSettings> orchestrationSettings;
    private readonly ILogger<ExecutionEngine> logger;
    private readonly Channel<(WorkflowType Type, IReadOnlyDictionary<string, string> Parameters, PlatformExecutionContext Context, TaskCompletionSource<WorkflowInstance>? Completion)> queue;
    private readonly CancellationTokenSource shutdown = new();
    private readonly List<Task> workers = [];

    public ExecutionEngine(
        IWorkflowRegistry registry,
        IWorkflowExecutor executor,
        InMemoryWorkflowStore store,
        IOptionsMonitor<OrchestrationSettings> orchestrationSettings,
        ILogger<ExecutionEngine> logger)
    {
        this.registry = registry;
        this.executor = executor;
        this.store = store;
        this.orchestrationSettings = orchestrationSettings;
        this.logger = logger;
        queue = Channel.CreateUnbounded<(WorkflowType, IReadOnlyDictionary<string, string>, PlatformExecutionContext, TaskCompletionSource<WorkflowInstance>?)>();

        var workerCount = Math.Max(1, orchestrationSettings.CurrentValue.MaximumWorkers);
        for (var index = 0; index < workerCount; index++)
        {
            workers.Add(Task.Run(() => ProcessQueueAsync(shutdown.Token)));
        }
    }

    /// <inheritdoc />
    public async Task<WorkflowInstance> QueueAsync(
        WorkflowType workflowType,
        IReadOnlyDictionary<string, string> parameters,
        PlatformExecutionContext executionContext,
        CancellationToken cancellationToken = default)
    {
        var completion = new TaskCompletionSource<WorkflowInstance>(TaskCreationOptions.RunContinuationsAsynchronously);
        await queue.Writer.WriteAsync((workflowType, parameters, executionContext, completion), cancellationToken);
        return await completion.Task.WaitAsync(TimeSpan.FromSeconds(orchestrationSettings.CurrentValue.ExecutionTimeoutSeconds), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowInstance> EnqueueAsync(
        WorkflowType workflowType,
        IReadOnlyDictionary<string, string> parameters,
        PlatformExecutionContext executionContext,
        CancellationToken cancellationToken = default)
    {
        var workflowId = $"WF-{Guid.NewGuid():N}";
        var workflow = registry.Get(workflowType);
        var mutableParameters = new Dictionary<string, string>(parameters, StringComparer.OrdinalIgnoreCase)
        {
            ["workflowId"] = workflowId
        };

        var placeholder = new WorkflowInstance
        {
            WorkflowId = workflowId,
            WorkflowName = workflow.WorkflowName,
            WorkflowType = workflow.WorkflowType,
            WorkflowVersion = workflow.WorkflowVersion,
            Steps = workflow.Steps,
            Status = WorkflowState.Queued,
            CorrelationId = executionContext.Correlation.CorrelationId,
            StartedAt = DateTimeOffset.UtcNow,
            Parameters = mutableParameters
        };

        store.Save(placeholder);
        await queue.Writer.WriteAsync((workflowType, mutableParameters, executionContext, null), cancellationToken);
        return placeholder;
    }

    /// <inheritdoc />
    public Task<WorkflowInstance?> GetAsync(string workflowId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(store.Get(workflowId));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<WorkflowInstance>> ListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(store.List());
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<WorkflowEvent>> GetEventsAsync(string workflowId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(store.GetEvents(workflowId));
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        shutdown.Cancel();
        queue.Writer.TryComplete();
        try
        {
            await Task.WhenAll(workers);
        }
        catch (OperationCanceledException)
        {
        }

        shutdown.Dispose();
    }

    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var item in queue.Reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    var workflow = registry.Get(item.Type);
                    using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(item.Context.CancellationToken, cancellationToken);
                    timeoutSource.CancelAfter(TimeSpan.FromSeconds(orchestrationSettings.CurrentValue.WorkflowTimeoutSeconds));
                    var instance = await executor.ExecuteAsync(workflow, item.Parameters, item.Context, timeoutSource.Token);
                    item.Completion?.SetResult(instance);
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Workflow execution failed for type {WorkflowType}", item.Type);
                    item.Completion?.SetException(exception);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }
}
