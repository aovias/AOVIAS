using ComplianceService.Domain;
using ComplianceService.Domain.Enums;

namespace ComplianceService.Application.Contracts;

/// <summary>
/// Persistence contract for OrganizationComplianceObligation.
/// </summary>
public interface IObligationRepository
{
    /// <summary>Returns all obligations for the given organization and framework, ordered by MaturityLevel then Code.</summary>
    Task<IReadOnlyList<OrganizationComplianceObligation>> GetByOrganizationAndFrameworkAsync(
        Guid organizationId,
        Guid frameworkId,
        CancellationToken ct = default);

    /// <summary>Returns the obligation uniquely identified by (organizationId, objectiveElementId), or null.</summary>
    Task<OrganizationComplianceObligation?> GetByKeyAsync(
        Guid organizationId,
        Guid objectiveElementId,
        CancellationToken ct = default);

    /// <summary>
    /// Idempotent batch upsert. Inserts obligations that do not yet exist for the given organization.
    /// Existing obligations are never modified by this method.
    /// </summary>
    Task UpsertBatchAsync(
        IReadOnlyList<OrganizationComplianceObligation> obligations,
        CancellationToken ct = default);

    /// <summary>
    /// Returns aggregated status counts per MaturityLevel for the given organization and framework.
    /// Key = MaturityLevel int value; Value = count per status dimension.
    /// </summary>
    Task<IReadOnlyList<ObligationStatusCount>> GetSummaryCountsAsync(
        Guid organizationId,
        Guid frameworkId,
        CancellationToken ct = default);
}

/// <summary>Projection for summary count aggregation queries.</summary>
public sealed class ObligationStatusCount
{
    public int MaturityLevel { get; init; }
    public int ImplementationStatus { get; init; }
    public int AssessmentStatus { get; init; }
    public int VerificationStatus { get; init; }
    public int Count { get; init; }
}
