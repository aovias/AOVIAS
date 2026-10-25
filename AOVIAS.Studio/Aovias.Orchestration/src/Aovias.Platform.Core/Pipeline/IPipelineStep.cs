namespace Aovias.Platform.Core.Pipeline;

/// <summary>
/// A single step in a platform pipeline.
/// </summary>
public interface IPipelineStep<TContext>
{
    string Name { get; }

    Task<ExecutionResult> ExecuteAsync(TContext context, CancellationToken cancellationToken = default);
}
