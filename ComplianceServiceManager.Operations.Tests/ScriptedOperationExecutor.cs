namespace ComplianceServiceManager.Operations.Tests;

public enum ScriptedStageAction { Complete, Warn, Skip, Fail, Throw }

public sealed record StageScript(ScriptedStageAction Action = ScriptedStageAction.Complete, double? SubProgressBeforeComplete = null, TimeSpan? Delay = null);

/// <summary>A fully controllable <see cref="IOperationExecutor"/> for engine tests — no I/O, no
/// real backend, deterministic. Upholds the same "stop at first failure" contract every real
/// executor must (see <see cref="IOperationExecutor"/>'s doc comment): once a stage is scripted to
/// Fail or Throw, no further stages run.</summary>
public sealed class ScriptedOperationExecutor : IOperationExecutor
{
    private readonly Dictionary<string, StageScript> scripts;

    public ScriptedOperationExecutor(OperationDescriptor descriptor, Dictionary<string, StageScript>? scripts = null)
    {
        Descriptor = descriptor;
        this.scripts = scripts ?? [];
    }

    public OperationDescriptor Descriptor { get; }

    public List<string> ExecutedStageIds { get; } = [];

    public async Task ExecuteAsync(OperationContext context)
    {
        foreach (var stage in Descriptor.Stages)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            ExecutedStageIds.Add(stage.Id);
            context.BeginStage(stage.Id);

            var script = scripts.GetValueOrDefault(stage.Id) ?? new StageScript();
            if (script.Delay is { } delay)
            {
                await Task.Delay(delay, context.CancellationToken);
            }

            if (script.SubProgressBeforeComplete is { } subProgress)
            {
                context.ReportTask(stage.Id, "Working...", subProgress);
            }

            switch (script.Action)
            {
                case ScriptedStageAction.Complete:
                    context.CompleteStage(stage.Id);
                    break;
                case ScriptedStageAction.Warn:
                    context.CompleteStage(stage.Id, OperationStageStatus.Warning);
                    break;
                case ScriptedStageAction.Skip:
                    context.SkipStage(stage.Id, "Skipped by script");
                    break;
                case ScriptedStageAction.Fail:
                    context.FailStage(stage.Id, new OperationFailure
                    {
                        Problem = "Scripted failure",
                        Cause = "Test-injected failure",
                        RecommendedAction = "None — this is a test."
                    });
                    return;
                case ScriptedStageAction.Throw:
                    throw new InvalidOperationException("Scripted exception");
            }
        }
    }

    public static OperationDescriptor BuildDescriptor(string targetIdentity, params (string Id, double Weight)[] stages) => new()
    {
        Kind = "Test",
        DisplayName = $"Test operation against {targetIdentity}",
        TargetIdentity = targetIdentity,
        SupportsResume = true,
        Stages = stages.Select(s => new OperationStageDefinition { Id = s.Id, Name = s.Id, Weight = s.Weight }).ToList()
    };
}
