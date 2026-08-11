using ComplianceService.Application.Contracts;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ComplianceService.Application.Services;

/// <summary>
/// Idempotent synchronization of OrganizationComplianceObligation records.
///
/// Creates one obligation per ObjectiveElement under the given framework for the
/// specified organization. Existing obligations are never modified or deleted.
/// Safe to call on every enrollment and re-enrollment.
/// </summary>
public class ObligationSynchronizationService : IObligationSynchronizationService
{
    private readonly ComplianceDbContext _db;
    private readonly IObligationRepository _obligationRepository;
    private readonly ILogger<ObligationSynchronizationService> _logger;

    public ObligationSynchronizationService(
        ComplianceDbContext db,
        IObligationRepository obligationRepository,
        ILogger<ObligationSynchronizationService> logger)
    {
        _db = db;
        _obligationRepository = obligationRepository;
        _logger = logger;
    }

    public async Task SyncForOrganizationAsync(
        Guid organizationId,
        Guid hospitalId,
        Guid tenantId,
        Guid frameworkId,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "ObligationSync: Starting sync for OrganizationId={OrganizationId} FrameworkId={FrameworkId}",
            organizationId, frameworkId);

        // 1. Load all ObjectiveElements under the framework (Chapter → Standard → OE).
        var allOes = await _db.ObjectiveElements
            .AsNoTracking()
            .Where(oe => oe.ComplianceStandard.Chapter.FrameworkId == frameworkId)
            .Select(oe => new OeProjection
            {
                Id = oe.Id,
                Code = oe.Code,
                Title = oe.Title,
                Description = oe.Description,
                Classification = oe.Classification,
                Weight = oe.Weight,
                ChapterId = oe.ComplianceStandard.ChapterId,
                StandardId = oe.ComplianceStandardId
            })
            .ToListAsync(ct);

        if (allOes.Count == 0)
        {
            _logger.LogWarning(
                "ObligationSync: No ObjectiveElements found for FrameworkId={FrameworkId}. Sync skipped.",
                frameworkId);
            return;
        }

        // 2. Load existing obligation OE IDs for this organization to skip duplicates.
        var existingOeIds = await _db.OrganizationComplianceObligations
            .IgnoreQueryFilters()
            .Where(o => o.OrganizationId == organizationId && o.FrameworkId == frameworkId)
            .Select(o => o.ObjectiveElementId)
            .ToHashSetAsync(ct);

        // 3. Compute the set of OEs that need new obligations.
        var newOes = allOes.Where(oe => !existingOeIds.Contains(oe.Id)).ToList();

        if (newOes.Count == 0)
        {
            _logger.LogInformation(
                "ObligationSync: All {Total} obligations already exist for OrganizationId={OrganizationId} FrameworkId={FrameworkId}. No action needed.",
                allOes.Count, organizationId, frameworkId);
            return;
        }

        // 4. Build new obligation entities.
        var now = DateTime.UtcNow;
        var obligations = newOes.Select(oe => new OrganizationComplianceObligation
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrganizationId = organizationId,
            HospitalId = hospitalId,
            FrameworkId = frameworkId,
            ChapterId = oe.ChapterId,
            StandardId = oe.StandardId,
            ObjectiveElementId = oe.Id,
            ComplianceNodeId = null,
            Code = oe.Code ?? string.Empty,
            Title = oe.Title ?? string.Empty,
            Description = oe.Description,
            MaturityLevel = oe.Classification,
            RiskCategory = null,
            Weight = oe.Weight,
            ImplementationStatus = ImplementationStatus.Unknown,
            AssessmentStatus = ObligationAssessmentStatus.NotAssessed,
            JourneyStatus = ObligationJourneyStatus.NotStarted,
            EvidenceStatus = EvidenceStatus.Missing,
            VerificationStatus = VerificationStatus.NotVerified,
            ReadinessScore = 0m,
            CompletionPercentage = 0m,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system",
            UpdatedBy = "system"
        }).ToList();

        // 5. Batch-insert new obligations (idempotent — duplicates guarded by unique constraint).
        await _obligationRepository.UpsertBatchAsync(obligations, ct);

        _logger.LogInformation(
            "ObligationSync: Created {NewCount} obligations (skipped {ExistingCount} existing) " +
            "for OrganizationId={OrganizationId} FrameworkId={FrameworkId}",
            newOes.Count, existingOeIds.Count, organizationId, frameworkId);
    }

    private sealed class OeProjection
    {
        public Guid Id { get; init; }
        public string? Code { get; init; }
        public string? Title { get; init; }
        public string? Description { get; init; }
        public ObjectiveElementClassification Classification { get; init; }
        public decimal Weight { get; init; }
        public Guid ChapterId { get; init; }
        public Guid StandardId { get; init; }
    }
}
