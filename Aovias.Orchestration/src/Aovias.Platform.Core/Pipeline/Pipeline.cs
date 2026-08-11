namespace Aovias.Platform.Core.Pipeline;

/// <summary>
/// Sequential pipeline executor for replaceable steps.
/// </summary>
public sealed class Pipeline<TContext>
{
    private readonly IReadOnlyList<IPipelineStep<TContext>> steps;

    public Pipeline(IEnumerable<IPipelineStep<TContext>> steps)
    {
        this.steps = steps.ToList();
    }

    public IReadOnlyList<string> StepNames => steps.Select(step => step.Name).ToList();

    public async Task<ExecutionResult> ExecuteAsync(TContext context, CancellationToken cancellationToken = default)
    {
        foreach (var step in steps)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await step.ExecuteAsync(context, cancellationToken);
            if (!result.IsSuccess)
            {
                return ExecutionResult.Failure($"Step '{step.Name}' failed: {result.Message}");
            }
        }

        return ExecutionResult.Success("Pipeline completed.");
    }
}
