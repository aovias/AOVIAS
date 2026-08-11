namespace ComplianceService.Application.AI.Contracts;

/// <summary>Read-only deficiency intelligence for AI context (Sprint 12C Phase 3).</summary>
public interface IAIDeficiencyContextProvider
{
    Task<AIContextContribution> BuildAsync(AIContextBuildContext context, CancellationToken cancellationToken = default);
}

/// <summary>Read-only CAPA intelligence for AI context (Sprint 12C Phase 4).</summary>
public interface IAICapaContextProvider
{
    Task<AIContextContribution> BuildAsync(AIContextBuildContext context, CancellationToken cancellationToken = default);
}

/// <summary>Read-only SLA and escalation intelligence for AI context (Sprint 12C Phase 5).</summary>
public interface IAISlaContextProvider
{
    Task<AIContextContribution> BuildAsync(AIContextBuildContext context, CancellationToken cancellationToken = default);
}

/// <summary>Read-only compliance maturity and stage unlock intelligence (Sprint 12C Phase 6).</summary>
public interface IAIMaturityContextProvider
{
    Task<AIContextContribution> BuildAsync(AIContextBuildContext context, CancellationToken cancellationToken = default);
}

/// <summary>Read-only analytics overview intelligence (Sprint 12C Phase 7).</summary>
public interface IAIAnalyticsContextProvider
{
    Task<AIContextContribution> BuildAsync(AIContextBuildContext context, CancellationToken cancellationToken = default);
}
