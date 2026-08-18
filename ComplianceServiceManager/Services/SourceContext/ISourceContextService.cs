using ComplianceServiceManager.Models.SourceContext;

namespace ComplianceServiceManager.Services.SourceContexts;

/// <summary>Read-only resolution of canonical Shared and workspace source contexts.</summary>
public interface ISourceContextService
{
    Task<SourceContext> ResolveAsync(string contextId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SourceContext>> ResolveAllAsync(CancellationToken cancellationToken = default);

    bool AcceptsSourcePath(SourceContext context, string path);

    SourceContext? TryResolveContextForPath(string path, IReadOnlyList<SourceContext>? cached = null);
}
