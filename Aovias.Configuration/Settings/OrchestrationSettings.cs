using System.ComponentModel.DataAnnotations;

namespace Aovias.Configuration.Settings;

/// <summary>
/// Orchestration engine execution settings.
/// </summary>
public sealed class OrchestrationSettings
{
    public const string SectionName = "Orchestration";

    [Range(1, 86400)]
    public int ExecutionTimeoutSeconds { get; set; }

    [Range(1, 86400)]
    public int WorkflowTimeoutSeconds { get; set; }

    [Range(1, 64)]
    public int MaximumWorkers { get; set; }

    public bool ParallelExecutionEnabled { get; set; }

    [Range(0, 10)]
    public int RetryCount { get; set; }

    [Range(0, 60000)]
    public int RetryDelayMilliseconds { get; set; }
}
