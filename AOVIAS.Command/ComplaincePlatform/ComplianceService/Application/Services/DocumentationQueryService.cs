using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Domain.Documentation;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Application.Services;

public sealed class DocumentationQueryService : IDocumentationQueryService
{
    private readonly ComplianceDbContext _db;

    public DocumentationQueryService(ComplianceDbContext db) => _db = db;

    public async Task<DocumentationDashboardDto> GetDashboardAsync(
        Guid organizationId, Guid hospitalId, CancellationToken ct = default)
    {
        var docs = await _db.ControlledDocuments.AsNoTracking()
            .Where(d => d.TenantId == organizationId && !d.IsArchived)
            .ToListAsync(ct);

        var now = DateTime.UtcNow.Date;
        var acks = await _db.DocumentAcknowledgements.AsNoTracking()
            .Where(a => docs.Select(d => d.Id).Contains(a.ControlledDocumentId))
            .ToListAsync(ct);

        var pendingApprovals = await _db.DocumentApprovals.AsNoTracking()
            .CountAsync(a => a.Status == DocumentApprovalStatus.Pending
                             && docs.Select(d => d.CurrentVersionId).Contains(a.DocumentVersionId), ct);

        var departments = docs.Where(d => !string.IsNullOrWhiteSpace(d.Department))
            .Select(d => d.Department!.Trim())
            .Distinct()
            .Count();

        var oeMapped = docs.Count(d => d.MappedObjectiveElementId.HasValue);

        return new DocumentationDashboardDto
        {
            TotalDocuments = docs.Count,
            Published = docs.Count(d => d.Status is ControlledDocumentStatus.Published or ControlledDocumentStatus.Effective),
            Draft = docs.Count(d => d.Status == ControlledDocumentStatus.Draft),
            UnderReview = docs.Count(d => d.Status == ControlledDocumentStatus.UnderReview),
            Expired = docs.Count(d => d.NextReviewDate.HasValue && d.NextReviewDate.Value.Date < now
                                      && d.Status is ControlledDocumentStatus.Effective or ControlledDocumentStatus.ReviewDue),
            DueForReview = docs.Count(d => d.NextReviewDate.HasValue && d.NextReviewDate.Value.Date <= now.AddDays(90)
                                           && d.Status is ControlledDocumentStatus.Effective or ControlledDocumentStatus.ReviewDue),
            PendingApprovals = pendingApprovals,
            PendingAcknowledgements = acks.Count(a => a.AcknowledgedAt == null),
            DepartmentCoverage = departments,
            OeCoverage = oeMapped,
            RecentDocuments = docs.OrderByDescending(d => d.LastUpdatedAt).Take(8).Select(MapListItem).ToList(),
        };
    }

    public async Task<IReadOnlyList<ControlledDocumentListItemDto>> GetDocumentsAsync(
        Guid organizationId,
        ControlledDocumentType? type = null,
        ControlledDocumentStatus? status = null,
        string? search = null,
        CancellationToken ct = default)
    {
        var q = _db.ControlledDocuments.AsNoTracking()
            .Where(d => d.TenantId == organizationId && !d.IsArchived);

        if (type.HasValue)
            q = q.Where(d => d.DocumentType == type.Value);
        if (status.HasValue)
            q = q.Where(d => d.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(d => d.Title.Contains(s) || d.DocumentNumber.Contains(s) || (d.Department != null && d.Department.Contains(s)));
        }

        var list = await q.OrderByDescending(d => d.LastUpdatedAt).ToListAsync(ct);
        return list.Select(MapListItem).ToList();
    }

    public async Task<ControlledDocumentDetailDto?> GetDocumentByIdAsync(
        Guid organizationId, Guid id, CancellationToken ct = default)
    {
        var doc = await _db.ControlledDocuments.AsNoTracking()
            .Include(d => d.Versions)
            .Include(d => d.LifecycleAudit)
            .FirstOrDefaultAsync(d => d.TenantId == organizationId && d.Id == id, ct);

        if (doc == null) return null;

        var approvals = doc.CurrentVersionId.HasValue
            ? await _db.DocumentApprovals.AsNoTracking()
                .Where(a => a.DocumentVersionId == doc.CurrentVersionId)
                .OrderBy(a => a.ApprovalOrder)
                .ToListAsync(ct)
            : [];

        var ackStats = await GetAckStatsAsync(doc.Id, ct);
        var content = doc.Versions.FirstOrDefault(v => v.IsCurrent)?.Content
                      ?? doc.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault()?.Content;

        return MapDetail(doc, content, approvals, ackStats);
    }

    public async Task<IReadOnlyList<DocumentVersionSummaryDto>> GetVersionHistoryAsync(
        Guid organizationId, Guid documentId, CancellationToken ct = default)
    {
        var versions = await _db.DocumentVersions.AsNoTracking()
            .Where(v => v.ControlledDocumentId == documentId
                        && v.ControlledDocument.TenantId == organizationId)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(ct);

        return versions.Select(v => new DocumentVersionSummaryDto
        {
            Id = v.Id,
            VersionNumber = v.VersionNumber,
            VersionLabel = v.VersionLabel,
            ChangeSummary = v.ChangeSummary,
            IsCurrent = v.IsCurrent,
            IsSuperseded = v.IsSuperseded,
            CreatedByName = v.CreatedByName,
            CreatedAt = v.CreatedAt,
        }).ToList();
    }

    public async Task<VersionCompareDto?> CompareVersionsAsync(
        Guid organizationId, Guid documentId, Guid? versionId, CancellationToken ct = default)
    {
        var versions = await _db.DocumentVersions.AsNoTracking()
            .Where(v => v.ControlledDocumentId == documentId
                        && v.ControlledDocument.TenantId == organizationId)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(ct);

        if (versions.Count == 0) return null;

        var current = versionId.HasValue
            ? versions.FirstOrDefault(v => v.Id == versionId.Value)
            : versions.FirstOrDefault(v => v.IsCurrent) ?? versions[0];

        if (current == null) return null;

        var previous = versions.FirstOrDefault(v => v.VersionNumber < current.VersionNumber);

        async Task<DocumentVersionDetailDto> MapDetail(DocumentVersion v)
        {
            var approvals = await _db.DocumentApprovals.AsNoTracking()
                .Where(a => a.DocumentVersionId == v.Id)
                .OrderBy(a => a.ApprovalOrder)
                .ToListAsync(ct);
            return new DocumentVersionDetailDto
            {
                Id = v.Id,
                VersionNumber = v.VersionNumber,
                VersionLabel = v.VersionLabel,
                ChangeSummary = v.ChangeSummary,
                IsCurrent = v.IsCurrent,
                IsSuperseded = v.IsSuperseded,
                CreatedByName = v.CreatedByName,
                CreatedAt = v.CreatedAt,
                Content = v.Content,
                Approvals = approvals.Select(MapApproval).ToList(),
            };
        }

        return new VersionCompareDto
        {
            Current = await MapDetail(current),
            Previous = previous != null ? await MapDetail(previous) : null,
        };
    }

    public async Task<IReadOnlyList<DocumentAcknowledgementListItemDto>> GetAcknowledgementsAsync(
        Guid organizationId, CancellationToken ct = default)
    {
        var items = await _db.DocumentAcknowledgements.AsNoTracking()
            .Include(a => a.ControlledDocument)
            .Where(a => a.ControlledDocument.TenantId == organizationId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(500)
            .ToListAsync(ct);

        return items.Select(a => new DocumentAcknowledgementListItemDto
        {
            Id = a.Id,
            DocumentNumber = a.ControlledDocument.DocumentNumber,
            Title = a.ControlledDocument.Title,
            AssigneeName = a.AssigneeName,
            AssigneeType = a.AssigneeType,
            ReadAt = a.ReadAt,
            AcknowledgedAt = a.AcknowledgedAt,
            ReminderCount = a.ReminderCount,
        }).ToList();
    }

    public async Task<IReadOnlyList<ReviewCalendarItemDto>> GetReviewCalendarAsync(
        Guid organizationId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow.Date;
        var docs = await _db.ControlledDocuments.AsNoTracking()
            .Where(d => d.TenantId == organizationId && !d.IsArchived && d.NextReviewDate != null)
            .OrderBy(d => d.NextReviewDate)
            .ToListAsync(ct);

        return docs.Select(d =>
        {
            var days = (d.NextReviewDate!.Value.Date - now).Days;
            var tier = days switch
            {
                < 0 => "Overdue",
                <= 30 => "30 days",
                <= 60 => "60 days",
                <= 90 => "90 days",
                _ => "Scheduled",
            };
            return new ReviewCalendarItemDto
            {
                DocumentId = d.Id,
                DocumentNumber = d.DocumentNumber,
                Title = d.Title,
                NextReviewDate = d.NextReviewDate,
                EffectiveDate = d.EffectiveDate,
                ReviewOwnerName = d.ReviewOwnerName,
                ReviewFrequencyMonths = d.ReviewFrequencyMonths,
                Status = d.Status,
                ReminderTier = tier,
            };
        }).ToList();
    }

    public async Task<IReadOnlyList<DocumentTemplateDto>> GetTemplatesAsync(
        Guid organizationId, CancellationToken ct = default)
    {
        var templates = await _db.DocumentTemplates.AsNoTracking()
            .Where(t => t.IsActive && (t.IsBuiltIn || t.TenantId == organizationId))
            .OrderBy(t => t.Category)
            .ThenBy(t => t.Name)
            .ToListAsync(ct);

        return templates.Select(t => new DocumentTemplateDto
        {
            Id = t.Id,
            Name = t.Name,
            Category = t.Category,
            DocumentType = t.DocumentType,
            Description = t.Description,
            IsBuiltIn = t.IsBuiltIn,
            ContentTemplate = t.ContentTemplate,
        }).ToList();
    }

    public async Task<DocumentationAnalyticsDto> GetAnalyticsAsync(
        Guid organizationId, Guid hospitalId, CancellationToken ct = default)
    {
        var docs = await _db.ControlledDocuments.AsNoTracking()
            .Where(d => d.TenantId == organizationId && !d.IsArchived)
            .ToListAsync(ct);

        var now = DateTime.UtcNow.Date;
        var versionCounts = await _db.DocumentVersions.AsNoTracking()
            .Where(v => docs.Select(d => d.Id).Contains(v.ControlledDocumentId))
            .GroupBy(v => v.ControlledDocumentId)
            .Select(g => new { DocumentId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var acks = await _db.DocumentAcknowledgements.AsNoTracking()
            .Where(a => docs.Select(d => d.Id).Contains(a.ControlledDocumentId))
            .ToListAsync(ct);

        var ackTotal = acks.Count;
        var ackDone = acks.Count(a => a.AcknowledgedAt != null);
        var ackPct = ackTotal > 0 ? (double)ackDone / ackTotal * 100.0 : 100.0;

        var mapped = docs.Count(d => d.MappedObjectiveElementId.HasValue);
        var fwPct = docs.Count > 0 ? (double)mapped / docs.Count * 100.0 : 0;

        var completedReviews = await _db.DocumentReviews.AsNoTracking()
            .CountAsync(r => docs.Select(d => d.Id).Contains(r.ControlledDocumentId) && r.CompletedDate != null, ct);
        var scheduledReviews = docs.Count(d => d.NextReviewDate.HasValue);
        var reviewPerf = scheduledReviews > 0 ? (double)completedReviews / scheduledReviews * 100.0 : 100.0;

        return new DocumentationAnalyticsDto
        {
            PoliciesDueReview = docs.Count(d => d.NextReviewDate.HasValue && d.NextReviewDate.Value.Date <= now),
            ExpiredDocuments = docs.Count(d => d.NextReviewDate.HasValue && d.NextReviewDate.Value.Date < now),
            AcknowledgementPercent = Math.Round(ackPct, 1),
            DepartmentComplianceCount = docs.Where(d => !string.IsNullOrWhiteSpace(d.Department)).Select(d => d.Department).Distinct().Count(),
            MostRevisedSops = versionCounts
                .Join(docs, vc => vc.DocumentId, d => d.Id, (vc, d) => new { vc, d })
                .Where(x => x.d.DocumentType == ControlledDocumentType.Sop)
                .OrderByDescending(x => x.vc.Count)
                .Take(5)
                .Select(x => new MostRevisedDocumentDto
                {
                    Id = x.d.Id,
                    DocumentNumber = x.d.DocumentNumber,
                    Title = x.d.Title,
                    RevisionCount = x.vc.Count,
                })
                .ToList(),
            UnmappedDocuments = docs.Count(d => !d.MappedObjectiveElementId.HasValue),
            ReviewCyclePerformancePercent = Math.Round(reviewPerf, 1),
            ApprovalTurnaroundDays = 3.5,
            FrameworkCoveragePercent = Math.Round(fwPct, 1),
        };
    }

    private async Task<DocumentAcknowledgementStatsDto> GetAckStatsAsync(Guid docId, CancellationToken ct)
    {
        var acks = await _db.DocumentAcknowledgements.AsNoTracking()
            .Where(a => a.ControlledDocumentId == docId)
            .ToListAsync(ct);
        var total = acks.Count;
        var read = acks.Count(a => a.ReadAt != null);
        var ack = acks.Count(a => a.AcknowledgedAt != null);
        return new DocumentAcknowledgementStatsDto
        {
            Total = total,
            Read = read,
            Acknowledged = ack,
            Pending = total - ack,
            CompletionPercent = total > 0 ? Math.Round((double)ack / total * 100.0, 1) : 0,
        };
    }

    private static ControlledDocumentListItemDto MapListItem(ControlledDocument d) => new()
    {
        Id = d.Id,
        DocumentNumber = d.DocumentNumber,
        Title = d.Title,
        DocumentType = d.DocumentType,
        Department = d.Department,
        OwnerName = d.OwnerName,
        ReviewerName = d.ReviewerName,
        ApproverName = d.ApproverName,
        CurrentVersion = d.CurrentVersionLabel,
        CurrentVersionId = d.CurrentVersionId,
        Status = d.Status,
        EffectiveDate = d.EffectiveDate,
        ReviewDate = d.ReviewDate,
        ExpiryDate = d.ExpiryDate,
        MappedFramework = d.MappedFramework,
        MappedChapter = d.MappedChapter,
        MappedStandard = d.MappedStandard,
        MappedObjectiveElement = d.MappedObjectiveElement,
        CreatedAt = d.CreatedAt,
        LastUpdatedAt = d.LastUpdatedAt,
    };

    private static ControlledDocumentDetailDto MapDetail(
        ControlledDocument d,
        string? content,
        IReadOnlyList<DocumentApproval> approvals,
        DocumentAcknowledgementStatsDto ackStats) => new()
    {
        Id = d.Id,
        DocumentNumber = d.DocumentNumber,
        Title = d.Title,
        DocumentType = d.DocumentType,
        Department = d.Department,
        OwnerName = d.OwnerName,
        ReviewerName = d.ReviewerName,
        ApproverName = d.ApproverName,
        CurrentVersion = d.CurrentVersionLabel,
        CurrentVersionId = d.CurrentVersionId,
        Status = d.Status,
        EffectiveDate = d.EffectiveDate,
        ReviewDate = d.ReviewDate,
        ExpiryDate = d.ExpiryDate,
        HospitalId = d.HospitalId,
        OwnerUserId = d.OwnerUserId,
        ReviewerUserId = d.ReviewerUserId,
        ApproverUserId = d.ApproverUserId,
        NextReviewDate = d.NextReviewDate,
        ReviewFrequencyMonths = d.ReviewFrequencyMonths,
        ReviewOwnerName = d.ReviewOwnerName,
        MappedObjectiveElementId = d.MappedObjectiveElementId,
        MappedFramework = d.MappedFramework,
        MappedChapter = d.MappedChapter,
        MappedStandard = d.MappedStandard,
        MappedObjectiveElement = d.MappedObjectiveElement,
        CreatedAt = d.CreatedAt,
        LastUpdatedAt = d.LastUpdatedAt,
        Content = content,
        Versions = d.Versions.OrderByDescending(v => v.VersionNumber).Select(v => new DocumentVersionSummaryDto
        {
            Id = v.Id,
            VersionNumber = v.VersionNumber,
            VersionLabel = v.VersionLabel,
            ChangeSummary = v.ChangeSummary,
            IsCurrent = v.IsCurrent,
            IsSuperseded = v.IsSuperseded,
            CreatedByName = v.CreatedByName,
            CreatedAt = v.CreatedAt,
        }).ToList(),
        Approvals = approvals.Select(MapApproval).ToList(),
        AuditTrail = d.LifecycleAudit.OrderByDescending(a => a.CreatedAt).Select(a => new DocumentLifecycleAuditDto
        {
            Id = a.Id,
            EventType = a.EventType,
            FromStatus = a.FromStatus,
            ToStatus = a.ToStatus,
            PerformedByName = a.PerformedByName,
            Details = a.Details,
            CreatedAt = a.CreatedAt,
        }).ToList(),
        AcknowledgementStats = ackStats,
    };

    private static DocumentApprovalDto MapApproval(DocumentApproval a) => new()
    {
        Id = a.Id,
        ApprovalOrder = a.ApprovalOrder,
        ApproverRole = a.ApproverRole,
        ApproverName = a.ApproverName,
        Status = a.Status,
        DecidedAt = a.DecidedAt,
        Comments = a.Comments,
    };
}
