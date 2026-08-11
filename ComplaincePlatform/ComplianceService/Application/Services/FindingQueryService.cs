using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.Events;
using ComplianceService.Domain;
using ComplianceService.Domain.Auditor;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Services;

public sealed class FindingQueryService : IFindingQueryService
{
    private readonly IComplianceAuditFindingRepository _repository;
    private readonly IFindingAdapter _adapter;
    private readonly IFindingAuditTrailService _auditTrail;
    private readonly IComplianceEventEmitter _eventEmitter;
    private readonly AddFindingAttachmentCommandHandler _addAttachment;
    private readonly ComplianceDbContext _db;
    private readonly ILogger<FindingQueryService> _logger;

    public FindingQueryService(
        IComplianceAuditFindingRepository repository,
        IFindingAdapter adapter,
        IFindingAuditTrailService auditTrail,
        IComplianceEventEmitter eventEmitter,
        AddFindingAttachmentCommandHandler addAttachment,
        ComplianceDbContext db,
        ILogger<FindingQueryService> logger)
    {
        _repository = repository;
        _adapter = adapter;
        _auditTrail = auditTrail;
        _eventEmitter = eventEmitter;
        _addAttachment = addAttachment;
        _db = db;
        _logger = logger;
    }

    public async Task<IReadOnlyList<FindingListItemDto>> GetFindingsAsync(
        Guid organizationId,
        ManagedFindingListFilters filters,
        CancellationToken cancellationToken = default)
    {
        var all = await _repository.GetOrganizationFindingsAsync(organizationId, cancellationToken);
        IEnumerable<Finding> q = all;

        if (filters.Severity is { } sev)
            q = q.Where(x => x.Severity == sev);
        if (filters.Status is { } st)
            q = q.Where(x => x.Status == st);
        if (filters.Department is { } d)
            q = q.Where(x => x.Department == d);

        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var s = filters.Search.Trim();
            q = q.Where(x =>
                x.Title.Contains(s, StringComparison.OrdinalIgnoreCase)
                || x.ClauseReference.Contains(s, StringComparison.OrdinalIgnoreCase)
                || x.Department.ToString().Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        var list = q
            .OrderByDescending(x => x.DateRaised)
            .Select(_adapter.ToManagedListDto)
            .ToList();

        return list;
    }

    public async Task<FindingDetailDto?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        var finding = await _repository.GetOrganizationFindingByIdAsync(organizationId, id, cancellationToken);
        return finding == null ? null : _adapter.ToManagedDetailDto(finding);
    }

    /// <summary>
    /// DEF-M6-001 — resolves the compliance journey a new finding belongs to. Sole assignment point
    /// for <see cref="Finding.ComplianceJourneyId"/>.
    ///
    /// <para>
    /// When the finding is mapped at creation, the framework follows from the objective element's
    /// chapter and the journey is unambiguous. An unmapped finding has no framework to follow, so it
    /// binds to the tenant's journey only when there is exactly one — guessing between frameworks
    /// would put the finding in the wrong report, which is worse than leaving it out of all of them.
    /// </para>
    /// </summary>
    private async Task<Guid?> ResolveJourneyIdAsync(
        Guid organizationId,
        Guid? objectiveElementId,
        CancellationToken cancellationToken)
    {
        if (objectiveElementId is { } oeId && oeId != Guid.Empty)
        {
            var frameworkId = await (
                from oe in _db.ObjectiveElements.AsNoTracking()
                where oe.Id == oeId
                join st in _db.ComplianceStandards.AsNoTracking() on oe.ComplianceStandardId equals st.Id
                join ch in _db.ComplianceChapters.AsNoTracking() on st.ChapterId equals ch.Id
                select (Guid?)ch.FrameworkId).FirstOrDefaultAsync(cancellationToken);

            if (frameworkId is { } fwId && fwId != Guid.Empty)
            {
                var scoped = await _db.ComplianceJourneys.AsNoTracking()
                    .Where(j => j.TenantId == organizationId && j.FrameworkId == fwId)
                    .OrderByDescending(j => j.UpdatedAt)
                    .Select(j => (Guid?)j.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (scoped.HasValue)
                {
                    return scoped;
                }
            }
        }

        var journeyIds = await _db.ComplianceJourneys.AsNoTracking()
            .Where(j => j.TenantId == organizationId)
            .Select(j => j.Id)
            .Take(2)
            .ToListAsync(cancellationToken);

        if (journeyIds.Count == 1)
        {
            return journeyIds[0];
        }

        _logger.LogWarning(
            "CreateFinding: could not resolve a single compliance journey for organization {OrganizationId} "
            + "(candidates={Count}); the finding will not appear in audit reports.",
            organizationId,
            journeyIds.Count);

        return null;
    }

    public async Task<FindingDetailDto> CreateAsync(
        Guid organizationId,
        Guid userId,
        CreateManagedFindingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Title is required.", nameof(request));

        if (string.IsNullOrWhiteSpace(request.ClauseReference))
            throw new ArgumentException("ClauseReference is required.", nameof(request));

        if (request.AuditSessionId is { } sessionId && sessionId != Guid.Empty)
        {
            var session = await _db.AuditSessions
                .FirstOrDefaultAsync(s => s.TenantId == organizationId && s.Id == sessionId, cancellationToken)
                ?? throw new InvalidOperationException("Audit session not found.");
            session.EnsureAcceptsFindings();
        }

        // DEF-M6-001 — bind the finding to its compliance journey at creation.
        //
        // ComplianceJourneyId was never assigned by anyone: it is absent from
        // CreateManagedFindingRequest, so no caller could supply it, and nothing derived it here.
        // AuditReportService requires `f.ComplianceJourneyId.HasValue`, so every finding raised
        // through the product was invisible to the audit report — the report showed 0 findings while
        // the workbench showed 28. The server is the right owner: the journey follows from tenant
        // and framework, which the caller must not be trusted to assert.
        var journeyId = await ResolveJourneyIdAsync(organizationId, request.ObjectiveElementId, cancellationToken);

        var now = DateTime.UtcNow;
        var dateRaised = request.DateRaised ?? now;
        var year = now.Year;
        var seq = await _repository.GetOrganizationFindingsAsync(organizationId, cancellationToken);
        var findingNumber = $"FND-{year}-{(seq.Count + 1):D5}";

        var finding = new Finding
        {
            Id = Guid.NewGuid(),
            Scope = FindingScope.Organization,
            TenantId = organizationId,
            ObjectiveElementId = request.ObjectiveElementId,
            FindingNumber = findingNumber,
            Classification = request.Classification ?? FindingClassification.AuditFinding,
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description)
                ? (string.IsNullOrWhiteSpace(request.Observation) ? string.Empty : request.Observation.Trim())
                : request.Description.Trim(),
            ClauseReference = request.ClauseReference.Trim(),
            Severity = request.Severity,
            Status = request.Status ?? FindingStatus.Open,
            Department = request.Department,
            RaisedBy = request.RaisedBy,
            DateRaised = dateRaised,
            AuditDate = request.AuditDate ?? dateRaised,
            AuditorName = string.IsNullOrWhiteSpace(request.AuditorName) ? null : request.AuditorName.Trim(),
            AuditCycle = string.IsNullOrWhiteSpace(request.AuditCycle) ? null : request.AuditCycle.Trim(),
            Observation = string.IsNullOrWhiteSpace(request.Observation) ? null : request.Observation.Trim(),
            Requirement = string.IsNullOrWhiteSpace(request.Requirement) ? null : request.Requirement.Trim(),
            Impact = string.IsNullOrWhiteSpace(request.Impact) ? null : request.Impact.Trim(),
            Recommendation = string.IsNullOrWhiteSpace(request.Recommendation) ? null : request.Recommendation.Trim(),
            CreatedAt = now,
            LastUpdatedAt = now,
            CreatedByUserId = userId,
            LastUpdatedBy = userId,
            EvidenceCount = 0,
            AuditSessionId = request.AuditSessionId,
            ComplianceJourneyId = journeyId,
            ComplianceStatus = ComplianceEvaluationStatus.NonCompliant,
            IsMandatory = false
        };

        await _repository.AddAsync(finding, cancellationToken);
        await _eventEmitter.EmitAsync(
            ComplianceEventType.FindingCreated,
            finding.Id,
            nameof(Finding),
            organizationId,
            userId,
            new
            {
                finding.Scope,
                finding.Status,
                finding.ComplianceStatus,
                finding.Severity
            },
            cancellationToken);
        await _auditTrail.LogAsync(
            finding.Id,
            "FindingCreated",
            userId,
            "User",
            null,
            System.Text.Json.JsonSerializer.Serialize(new
            {
                finding.Title,
                finding.Status,
                finding.ComplianceStatus
            }),
            cancellationToken);
        _logger.LogInformation("Created managed finding {FindingId} for organization {OrgId}", finding.Id, organizationId);

        if (request.Attachments is { Count: > 0 })
        {
            foreach (var attachment in request.Attachments)
            {
                if (string.IsNullOrWhiteSpace(attachment.FileName) || string.IsNullOrWhiteSpace(attachment.FileUrl))
                {
                    continue;
                }

                await _addAttachment.HandleAsync(
                    organizationId,
                    finding.Id,
                    userId,
                    attachment.FileName,
                    attachment.FileUrl,
                    attachment.ContentType,
                    attachment.AttachmentType,
                    attachment.FileSizeBytes,
                    cancellationToken);
            }
        }

        return _adapter.ToManagedDetailDto(finding);
    }
}
