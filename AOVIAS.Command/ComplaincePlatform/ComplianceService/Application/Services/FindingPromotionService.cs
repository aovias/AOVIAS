using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.Events;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Domain;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Services;

/// <summary>
/// Sprint 7B / Phase 2 — Finding → Deficiency (CAPA) promotion bridge.
/// Multiple CAPAs per finding are supported via ComplianceDeficiency SSOT.
/// </summary>
public sealed class FindingPromotionService : IFindingPromotionService
{
    private readonly ComplianceDbContext _db;
    private readonly IDeficiencyRepository _deficiencyRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly IDeficiencyRemediationTaskService _remediationTaskService;
    private readonly IFindingAuditTrailService _auditTrail;
    private readonly IComplianceEventEmitter _eventEmitter;
    private readonly IFindingPromotionEligibilityService _eligibility;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISlaEvaluationService _slaEvaluation;
    private readonly IPlatformSequenceStore _sequenceStore;
    private readonly ILogger<FindingPromotionService> _logger;

    public FindingPromotionService(
        ComplianceDbContext db,
        IDeficiencyRepository deficiencyRepository,
        IDomainEventDispatcher eventDispatcher,
        IDeficiencyRemediationTaskService remediationTaskService,
        IFindingAuditTrailService auditTrail,
        IComplianceEventEmitter eventEmitter,
        IFindingPromotionEligibilityService eligibility,
        IUnitOfWork unitOfWork,
        ISlaEvaluationService slaEvaluation,
        IPlatformSequenceStore sequenceStore,
        ILogger<FindingPromotionService> logger)
    {
        _db = db;
        _deficiencyRepository = deficiencyRepository;
        _eventDispatcher = eventDispatcher;
        _remediationTaskService = remediationTaskService;
        _auditTrail = auditTrail;
        _eventEmitter = eventEmitter;
        _eligibility = eligibility;
        _unitOfWork = unitOfWork;
        _slaEvaluation = slaEvaluation;
        _sequenceStore = sequenceStore;
        _logger = logger;
    }

    public async Task<FindingPromotionEligibilityDto> GetEligibilityAsync(
        Guid organizationId,
        Guid findingId,
        CancellationToken ct = default)
    {
        var finding = await LoadFindingAsync(organizationId, findingId, ct);
        return await _eligibility.EvaluateAsync(finding, ct);
    }

    public async Task<FindingPromotionStatusDto> GetPromotionStatusAsync(
        Guid organizationId,
        Guid findingId,
        Guid hospitalId,
        CancellationToken ct = default)
    {
        var finding = await LoadFindingAsync(organizationId, findingId, ct);
        return await BuildStatusAsync(finding, hospitalId, ct);
    }

    public async Task<PromoteFindingResultDto> PromoteAsync(
        Guid organizationId,
        Guid findingId,
        Guid hospitalId,
        Guid? userId,
        CancellationToken ct = default,
        bool allowAdditional = true)
    {
        var finding = await LoadFindingAsync(organizationId, findingId, ct);
        var status = await BuildStatusAsync(finding, hospitalId, ct);

        if (status.IsPromoted && status.PromotedDeficiencyId.HasValue && !allowAdditional)
        {
            var existingTask = await GetPrimaryTaskIdAsync(status.PromotedDeficiencyId.Value, ct);
            return new PromoteFindingResultDto
            {
                FindingId = findingId,
                DeficiencyId = status.PromotedDeficiencyId.Value,
                DeficiencyCode = await _db.ComplianceDeficiencies
                    .AsNoTracking()
                    .Where(d => d.Id == status.PromotedDeficiencyId.Value)
                    .Select(d => d.DeficiencyCode)
                    .FirstAsync(ct),
                PrimaryRemediationTaskId = existingTask,
                PromotedAt = finding.LastUpdatedAt,
            };
        }

        if (!status.AllowPromotion)
        {
            throw new InvalidOperationException(
                status.ValidationMessages.Count > 0
                    ? string.Join(" ", status.ValidationMessages)
                    : "Finding is not eligible for promotion.");
        }

        var obligation = await ResolveObligationAsync(finding, organizationId, hospitalId, ct)
            ?? throw new InvalidOperationException("Could not resolve compliance obligation for this finding.");

        var classification = finding.Classification
            ?? FindingsWorkspaceQueryService.MapLegacyType(finding.Type);
        var deficiencyType = FindingPromotionEligibilityService.MapDeficiencyType(classification);
        var severity = FindingPromotionEligibilityService.MapSeverity(finding.Severity);

        // AuditFinding-origin CAPAs may have multiple open deficiencies per obligation.
        // Skip unique-open block for finding promotion (always AuditFinding origin).

        var findingNumber = string.IsNullOrWhiteSpace(finding.FindingNumber)
            ? $"FND-{finding.Id.ToString()[..8].ToUpperInvariant()}"
            : finding.FindingNumber;

        var deficiencyCode = $"DEF-FND-{DateTime.UtcNow:yyyy}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
        var assignee = finding.AssignedToUserId?.ToString();

        var deficiency = ComplianceDeficiency.CreateFromFinding(
            tenantId: organizationId,
            organizationId: organizationId,
            hospitalId: obligation.HospitalId,
            frameworkId: obligation.FrameworkId,
            objectiveElementId: obligation.ObjectiveElementId,
            complianceObligationId: obligation.Id,
            findingId: finding.Id,
            findingNumber: findingNumber,
            deficiencyCode: deficiencyCode,
            title: finding.Title,
            description: finding.Description,
            deficiencyType: deficiencyType,
            severity: severity,
            maturityLevel: obligation.MaturityLevel,
            riskCategory: finding.RiskLevel.ToString(),
            assignedTo: assignee,
            dueDate: finding.DueDate,
            createdBy: userId?.ToString(),
            discoveryDate: finding.DateRaised);

        var capaNumber = await CapaNumberAllocator.AllocateAsync(_sequenceStore, obligation.HospitalId, ct);
        deficiency.AssignCapaNumber(capaNumber);

        var events = deficiency.DomainEvents.ToList();
        await _deficiencyRepository.AddAsync(deficiency, ct);
        await _slaEvaluation.AssignSlaToDeficiencyAsync(deficiency, ct);

        finding.MarkPromoted(deficiency.Id, userId);

        await _auditTrail.LogAsync(
            finding.Id,
            "FindingPromoted",
            userId,
            "User",
            null,
            System.Text.Json.JsonSerializer.Serialize(new
            {
                deficiencyId = deficiency.Id,
                deficiencyCode,
                capaNumber,
                findingNumber,
            }),
            ct);

        await _auditTrail.LogAsync(
            finding.Id,
            "DeficiencyCreated",
            userId,
            "System",
            null,
            System.Text.Json.JsonSerializer.Serialize(new { deficiencyId = deficiency.Id, capaNumber }),
            ct);

        if (events.Count > 0)
        {
            await _eventDispatcher.DispatchAsync(events, ct);
            deficiency.ClearDomainEvents();
        }

        await _unitOfWork.SaveChangesAsync(ct);

        var taskId = await _remediationTaskService.EnsureRemediationTaskAsync(deficiency.Id, ct);

        if (taskId.HasValue)
        {
            await _auditTrail.LogAsync(
                finding.Id,
                "PrimaryRemediationTaskCreated",
                userId,
                "System",
                null,
                System.Text.Json.JsonSerializer.Serialize(new { taskId = taskId.Value }),
                ct);

            await _eventEmitter.EmitAsync(
                ComplianceEventType.PrimaryRemediationTaskCreated,
                taskId.Value,
                nameof(ComplianceTask),
                organizationId,
                userId,
                new
                {
                    findingId = finding.Id,
                    deficiencyId = deficiency.Id,
                    taskId = taskId.Value,
                },
                ct);
        }

        await _eventEmitter.EmitAsync(
            ComplianceEventType.FindingPromotedToDeficiency,
            finding.Id,
            nameof(Finding),
            organizationId,
            userId,
            new
            {
                findingId = finding.Id,
                deficiencyId = deficiency.Id,
                capaNumber,
                taskId,
                findingNumber,
            },
            ct);

        await _eventEmitter.EmitAsync(
            ComplianceEventType.DeficiencyCreatedFromFinding,
            deficiency.Id,
            nameof(ComplianceDeficiency),
            organizationId,
            userId,
            new
            {
                findingId = finding.Id,
                deficiencyId = deficiency.Id,
                capaNumber,
                taskId,
                obligation.HospitalId,
                obligation.FrameworkId,
            },
            ct);

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "FindingPromotion: Finding {FindingId} → Deficiency {DeficiencyId} CAPA {CapaNumber}, Task {TaskId}.",
            finding.Id, deficiency.Id, capaNumber, taskId);

        return new PromoteFindingResultDto
        {
            FindingId = finding.Id,
            DeficiencyId = deficiency.Id,
            DeficiencyCode = deficiency.DeficiencyCode,
            PrimaryRemediationTaskId = taskId,
            PromotedAt = deficiency.PromotedAt ?? DateTime.UtcNow,
        };
    }

    private async Task<FindingPromotionStatusDto> BuildStatusAsync(
        Finding finding,
        Guid hospitalId,
        CancellationToken ct)
    {
        var eligibility = await _eligibility.EvaluateAsync(finding, ct);
        var messages = eligibility.ValidationReasons.ToList();
        var isPromoted = finding.PromotedDeficiencyId.HasValue;

        OrganizationComplianceObligation? obligation = null;
        if (finding.ObjectiveElementId.HasValue && hospitalId != Guid.Empty)
        {
            obligation = await ResolveObligationAsync(finding, finding.TenantId, hospitalId, ct);
            if (obligation == null && eligibility.IsEligible)
            {
                messages.Add("No compliance obligation found for the mapped objective element.");
            }
        }
        else if (hospitalId == Guid.Empty && !isPromoted)
        {
            messages.Add("Hospital context is required for promotion.");
        }

        var isReady = eligibility.IsEligible
            && messages.Count == eligibility.ValidationReasons.Count
            && obligation != null;

        Guid? taskId = null;
        if (isPromoted && finding.PromotedDeficiencyId.HasValue)
        {
            taskId = await GetPrimaryTaskIdAsync(finding.PromotedDeficiencyId.Value, ct);
        }

        var readiness = FindingPromotionReadiness.Compute(finding);
        var eligibilityStatus = readiness == FindingPromotionReadiness.AwaitingAcceptance
            ? "Awaiting Acceptance"
            : eligibility.IsEligible
                ? isPromoted ? "EligibleAdditional" : "Eligible"
                : "Not Eligible";

        return new FindingPromotionStatusDto
        {
            FindingId = finding.Id,
            FindingNumber = finding.FindingNumber,
            EligibilityStatus = eligibilityStatus,
            IsEligible = eligibility.IsEligible,
            IsReady = isReady,
            IsPromoted = isPromoted,
            AllowPromotion = isReady,
            PromotedDeficiencyId = finding.PromotedDeficiencyId,
            PrimaryRemediationTaskId = taskId,
            ValidationMessages = messages,
        };
    }

    private async Task<Finding> LoadFindingAsync(Guid organizationId, Guid findingId, CancellationToken ct)
    {
        var finding = await _db.Findings
            .FirstOrDefaultAsync(f => f.Id == findingId && f.TenantId == organizationId, ct);

        return finding ?? throw new InvalidOperationException("Finding not found.");
    }

    private async Task<OrganizationComplianceObligation?> ResolveObligationAsync(
        Finding finding,
        Guid organizationId,
        Guid hospitalId,
        CancellationToken ct)
    {
        if (!finding.ObjectiveElementId.HasValue)
        {
            return null;
        }

        if (finding.ComplianceJourneyId.HasValue)
        {
            var journey = await _db.ComplianceJourneys
                .AsNoTracking()
                .FirstOrDefaultAsync(j => j.Id == finding.ComplianceJourneyId.Value, ct);

            if (journey != null)
            {
                return await _db.OrganizationComplianceObligations
                                        .AsNoTracking()
                    .FirstOrDefaultAsync(
                        o => o.HospitalId == journey.HospitalId
                             && o.FrameworkId == journey.FrameworkId
                             && o.ObjectiveElementId == finding.ObjectiveElementId.Value,
                        ct);
            }
        }

        return await _db.OrganizationComplianceObligations
                        .AsNoTracking()
            .FirstOrDefaultAsync(
                o => o.OrganizationId == organizationId
                     && o.HospitalId == hospitalId
                     && o.ObjectiveElementId == finding.ObjectiveElementId.Value,
                ct);
    }

    private async Task<Guid?> GetPrimaryTaskIdAsync(Guid deficiencyId, CancellationToken ct)
    {
        return await _db.ComplianceTasks
            .AsNoTracking()
            .Where(t => t.SourceDeficiencyId == deficiencyId && t.Status != ComplianceTaskStatus.Completed)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => (Guid?)t.Id)
            .FirstOrDefaultAsync(ct);
    }
}
