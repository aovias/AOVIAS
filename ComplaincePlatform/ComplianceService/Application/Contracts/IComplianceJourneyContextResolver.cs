namespace ComplianceService.Application.Contracts;

/// <summary>
/// Resolves hospital + framework-or-journey identifiers to canonical compliance context.
/// The portal stores <see cref="ResolvedJourneyContext.JourneyId"/> in session as "frameworkId";
/// deficiency and obligation records are keyed by <see cref="ResolvedJourneyContext.FrameworkId"/>.
/// </summary>
public interface IComplianceJourneyContextResolver
{
    /// <summary>
    /// Resolves <paramref name="frameworkOrJourneyId"/> to the canonical framework GUID used by
    /// OrganizationComplianceObligation and ComplianceDeficiency tables.
    /// </summary>
    Task<ResolvedJourneyContext?> ResolveAsync(
        Guid hospitalId,
        Guid frameworkOrJourneyId,
        CancellationToken cancellationToken = default);
}

/// <summary>Canonical identifiers for a hospital's active compliance journey.</summary>
public sealed record ResolvedJourneyContext(
    Guid HospitalId,
    Guid FrameworkId,
    Guid JourneyId,
    Guid OrganizationId);
