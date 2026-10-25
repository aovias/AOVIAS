using System.Collections.Concurrent;
using Aovias.Orchestration.Contracts.Events;
using Aovias.Orchestration.Contracts.Workflows;

namespace Aovias.Orchestration.Engine;

/// <summary>
/// In-memory workflow and event store.
/// </summary>
public sealed class InMemoryWorkflowStore
{
    private readonly ConcurrentDictionary<string, WorkflowInstance> workflows = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, List<WorkflowEvent>> events = new(StringComparer.OrdinalIgnoreCase);

    public void Save(WorkflowInstance workflow) => workflows[workflow.WorkflowId] = workflow;

    public WorkflowInstance? Get(string workflowId)
    {
        workflows.TryGetValue(workflowId, out var workflow);
        return workflow;
    }

    public IReadOnlyList<WorkflowInstance> List()
        => workflows.Values.OrderByDescending(workflow => workflow.StartedAt).ToList();

    public void AppendEvent(WorkflowEvent workflowEvent)
    {
        var bucket = events.GetOrAdd(workflowEvent.WorkflowId, _ => []);
        lock (bucket)
        {
            bucket.Add(workflowEvent);
        }
    }

    public IReadOnlyList<WorkflowEvent> GetEvents(string workflowId)
    {
        if (!events.TryGetValue(workflowId, out var bucket))
        {
            return [];
        }

        lock (bucket)
        {
            return bucket.OrderBy(entry => entry.Timestamp).ToList();
        }
    }
}
