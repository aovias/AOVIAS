using System.Text.Json;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.Events;
using ComplianceService.Domain.Documentation;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ComplianceService.Application.Services;

public sealed class DocumentationCommandService : IDocumentationCommandService
{
    private readonly ComplianceDbContext _db;
    private readonly IDocumentationQueryService _queries;
    private readonly ILogger<DocumentationCommandService> _logger;

    private static readonly (int Order, string Role)[] DefaultApprovalChain =
    [
        (1, "Author"),
        (2, "Department Reviewer"),
        (3, "Quality Manager"),
        (4, "Medical Superintendent"),
    ];

    private static readonly ControlledDocumentType[] MandatoryTrainingHintTypes =
    [
        ControlledDocumentType.Sop,
        ControlledDocumentType.Procedure,
        ControlledDocumentType.Protocol,
        ControlledDocumentType.Guideline,
    ];

    public DocumentationCommandService(
        ComplianceDbContext db,
        IDocumentationQueryService queries,
        ILogger<DocumentationCommandService> logger)
    {
        _db = db;
        _queries = queries;
        _logger = logger;
    }

    public async Task<ControlledDocumentDetailDto> CreateDocumentAsync(
        Guid organizationId, Guid hospitalId, Guid userId, string? userName,
        CreateControlledDocumentRequest request, CancellationToken ct = default)
    {
        var content = request.Content;
        if (request.TemplateId.HasValue)
        {
            var template = await _db.DocumentTemplates.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == request.TemplateId.Value && t.IsActive, ct);
            if (template != null && string.IsNullOrWhiteSpace(content))
                content = template.ContentTemplate;
        }

        var year = DateTime.UtcNow.Year;
        var count = await _db.ControlledDocuments.CountAsync(d => d.TenantId == organizationId, ct);
        var docNumber = $"DOC-{year}-{(count + 1):D4}";

        var doc = new ControlledDocument
        {
            Id = Guid.NewGuid(),
            TenantId = organizationId,
            HospitalId = hospitalId,
            DocumentNumber = docNumber,
            Title = request.Title.Trim(),
            DocumentType = request.DocumentType,
            Department = request.Department?.Trim(),
            OwnerUserId = userId,
            OwnerName = request.OwnerName ?? userName,
            MappedFramework = request.MappedFramework,
            MappedChapter = request.MappedChapter,
            MappedStandard = request.MappedStandard,
            MappedObjectiveElementId = request.MappedObjectiveElementId,
            MappedObjectiveElement = request.MappedObjectiveElement,
            CreatedByUserId = userId,
            CreatedByName = userName,
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
            RowVersion = new byte[8],
        };

        var version = new DocumentVersion
        {
            Id = Guid.NewGuid(),
            ControlledDocumentId = doc.Id,
            VersionNumber = 1,
            VersionLabel = "1.0",
            Content = content ?? string.Empty,
            ChangeSummary = "Initial version",
            IsCurrent = true,
            CreatedByUserId = userId,
            CreatedByName = userName,
            CreatedAt = DateTime.UtcNow,
            RowVersion = new byte[8],
        };

        doc.CurrentVersionId = version.Id;
        doc.CurrentVersionLabel = version.VersionLabel;

        await AddAuditAsync(doc.Id, version.Id, "Created", null, ControlledDocumentStatus.Draft, userId, userName,
            "Document created", ct);

        _db.ControlledDocuments.Add(doc);
        _db.DocumentVersions.Add(version);
        await _db.SaveChangesAsync(ct);

        return (await _queries.GetDocumentByIdAsync(organizationId, doc.Id, ct))!;
    }

    public async Task<ControlledDocumentDetailDto?> UpdateDocumentAsync(
        Guid organizationId, Guid id, Guid userId, string? userName,
        UpdateControlledDocumentRequest request, CancellationToken ct = default)
    {
        var doc = await _db.ControlledDocuments
            .FirstOrDefaultAsync(d => d.TenantId == organizationId && d.Id == id && !d.IsArchived, ct);
        if (doc == null) return null;

        doc.EnsureMutable();

        if (request.Title != null) doc.Title = request.Title.Trim();
        if (request.Department != null) doc.Department = request.Department.Trim();
        if (request.OwnerName != null) doc.OwnerName = request.OwnerName.Trim();
        if (request.ReviewerName != null) doc.ReviewerName = request.ReviewerName.Trim();
        if (request.ApproverName != null) doc.ApproverName = request.ApproverName.Trim();
        if (request.EffectiveDate.HasValue) doc.EffectiveDate = request.EffectiveDate;
        if (request.ReviewDate.HasValue) doc.ReviewDate = request.ReviewDate;
        if (request.ExpiryDate.HasValue) doc.ExpiryDate = request.ExpiryDate;
        if (request.ReviewFrequencyMonths.HasValue) doc.ReviewFrequencyMonths = request.ReviewFrequencyMonths.Value;
        if (request.ReviewOwnerName != null) doc.ReviewOwnerName = request.ReviewOwnerName.Trim();
        if (request.MappedFramework != null) doc.MappedFramework = request.MappedFramework;
        if (request.MappedChapter != null) doc.MappedChapter = request.MappedChapter;
        if (request.MappedStandard != null) doc.MappedStandard = request.MappedStandard;
        if (request.MappedObjectiveElementId.HasValue) doc.MappedObjectiveElementId = request.MappedObjectiveElementId;
        if (request.MappedObjectiveElement != null) doc.MappedObjectiveElement = request.MappedObjectiveElement;

        if (doc.Status == ControlledDocumentStatus.Effective && doc.IsPastExpiry())
            throw new InvalidOperationException("Expired document cannot remain Effective. Archive or renew before updating.");

        doc.LastUpdatedAt = DateTime.UtcNow;
        doc.LastUpdatedByUserId = userId;

        await AddAuditAsync(doc.Id, doc.CurrentVersionId, "Updated", doc.Status, doc.Status, userId, userName,
            "Metadata updated", ct);
        await _db.SaveChangesAsync(ct);

        return await _queries.GetDocumentByIdAsync(organizationId, id, ct);
    }

    public async Task<ControlledDocumentDetailDto?> CreateVersionAsync(
        Guid organizationId, Guid documentId, Guid userId, string? userName,
        CreateDocumentVersionRequest request, CancellationToken ct = default)
    {
        var doc = await _db.ControlledDocuments
            .Include(d => d.Versions)
            .FirstOrDefaultAsync(d => d.TenantId == organizationId && d.Id == documentId && !d.IsArchived, ct);
        if (doc == null) return null;

        doc.EnsureMutable();

        var fromStatus = doc.Status;
        SupersedePriorVersions(doc.Versions);

        var nextNum = doc.Versions.Count > 0 ? doc.Versions.Max(v => v.VersionNumber) + 1 : 1;
        var version = new DocumentVersion
        {
            Id = Guid.NewGuid(),
            ControlledDocumentId = doc.Id,
            VersionNumber = nextNum,
            VersionLabel = $"{nextNum}.0",
            Content = request.Content,
            ChangeSummary = request.ChangeSummary ?? $"Revision {nextNum}",
            IsCurrent = true,
            CreatedByUserId = userId,
            CreatedByName = userName,
            CreatedAt = DateTime.UtcNow,
            RowVersion = new byte[8],
        };

        doc.CurrentVersionId = version.Id;
        doc.CurrentVersionLabel = version.VersionLabel;
        doc.Status = ControlledDocumentStatus.Draft;
        doc.LastUpdatedAt = DateTime.UtcNow;
        doc.LastUpdatedByUserId = userId;

        _db.DocumentVersions.Add(version);
        await AddAuditAsync(doc.Id, version.Id, "VersionCreated", fromStatus, ControlledDocumentStatus.Draft,
            userId, userName, version.ChangeSummary, ct);
        await _db.SaveChangesAsync(ct);

        return await _queries.GetDocumentByIdAsync(organizationId, documentId, ct);
    }

    public async Task<ControlledDocumentDetailDto?> TransitionStatusAsync(
        Guid organizationId, Guid documentId, Guid userId, string? userName,
        TransitionDocumentStatusRequest request, CancellationToken ct = default)
    {
        var doc = await _db.ControlledDocuments
            .FirstOrDefaultAsync(d => d.TenantId == organizationId && d.Id == documentId && !d.IsArchived, ct);
        if (doc == null) return null;

        if (request.TargetStatus != ControlledDocumentStatus.Archived)
            doc.EnsureMutable();

        var from = doc.Status;
        doc.TransitionTo(request.TargetStatus, userId);

        if (request.TargetStatus == ControlledDocumentStatus.UnderReview && doc.CurrentVersionId.HasValue)
            await EnsureApprovalChainAsync(doc.CurrentVersionId.Value, doc, ct);

        await AddAuditAsync(doc.Id, doc.CurrentVersionId, "StatusTransition", from, request.TargetStatus,
            userId, userName, request.Comments, ct);
        await _db.SaveChangesAsync(ct);

        return await _queries.GetDocumentByIdAsync(organizationId, documentId, ct);
    }

    public async Task<ControlledDocumentDetailDto?> ApproveAsync(
        Guid organizationId, Guid documentId, Guid userId, string? userName,
        ApproveDocumentRequest request, CancellationToken ct = default)
    {
        var doc = await _db.ControlledDocuments
            .FirstOrDefaultAsync(d => d.TenantId == organizationId && d.Id == documentId, ct);
        if (doc == null) return null;

        doc.EnsureMutable();

        var approval = await _db.DocumentApprovals
            .Where(a => a.DocumentVersionId == request.DocumentVersionId && a.Status == DocumentApprovalStatus.Pending)
            .OrderBy(a => a.ApprovalOrder)
            .FirstOrDefaultAsync(ct);

        if (approval == null)
            throw new InvalidOperationException("No pending approval step found.");

        approval.Status = request.Approved ? DocumentApprovalStatus.Approved : DocumentApprovalStatus.Rejected;
        approval.ApproverUserId = userId;
        approval.ApproverName = userName;
        approval.DecidedAt = DateTime.UtcNow;
        approval.Comments = request.Comments;

        if (request.Approved)
        {
            var remaining = await _db.DocumentApprovals
                .CountAsync(a => a.DocumentVersionId == request.DocumentVersionId
                                 && a.Status == DocumentApprovalStatus.Pending
                                 && a.Id != approval.Id, ct);
            if (remaining == 0)
            {
                var from = doc.Status;
                doc.TransitionTo(ControlledDocumentStatus.Approved, userId);
                await AddAuditAsync(doc.Id, request.DocumentVersionId, "Approved", from,
                    ControlledDocumentStatus.Approved, userId, userName, request.Comments, ct);
            }
        }
        else
        {
            var from = doc.Status;
            doc.TransitionTo(ControlledDocumentStatus.Draft, userId);
            await AddAuditAsync(doc.Id, request.DocumentVersionId, "ApprovalRejected", from,
                ControlledDocumentStatus.Draft, userId, userName, request.Comments, ct);
        }

        doc.LastUpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return await _queries.GetDocumentByIdAsync(organizationId, documentId, ct);
    }

    public async Task<ControlledDocumentDetailDto?> PublishAsync(
        Guid organizationId, Guid documentId, Guid userId, string? userName,
        PublishDocumentRequest request, CancellationToken ct = default)
    {
        var doc = await _db.ControlledDocuments
            .Include(d => d.Versions)
            .FirstOrDefaultAsync(d => d.TenantId == organizationId && d.Id == documentId, ct);
        if (doc == null) return null;

        doc.EnsureMutable();

        if (doc.Status != ControlledDocumentStatus.Approved)
            throw new InvalidOperationException("Document must be approved before publishing.");

        if (request.ExpiryDate.HasValue)
            doc.ExpiryDate = request.ExpiryDate;

        if (doc.IsPastExpiry())
            throw new InvalidOperationException("Cannot publish an expired document. Extend ExpiryDate first.");

        SupersedePriorVersions(doc.Versions, currentVersionId: doc.CurrentVersionId);

        if (request.RequireReAcknowledge)
            await ResetAcknowledgementsForReAckAsync(doc, ct);

        var from = doc.Status;
        doc.TransitionTo(ControlledDocumentStatus.Published, userId);
        doc.EffectiveDate = request.EffectiveDate ?? DateTime.UtcNow.Date;
        doc.NextReviewDate = doc.EffectiveDate.Value.AddMonths(doc.ReviewFrequencyMonths);
        doc.ReviewDate = doc.NextReviewDate;

        if (request.Assignments?.Count > 0 && doc.CurrentVersionId.HasValue)
        {
            foreach (var a in request.Assignments)
            {
                _db.DocumentAcknowledgements.Add(new DocumentAcknowledgement
                {
                    Id = Guid.NewGuid(),
                    ControlledDocumentId = doc.Id,
                    DocumentVersionId = doc.CurrentVersionId.Value,
                    AssigneeType = a.AssigneeType,
                    AssigneeKey = a.AssigneeKey,
                    AssigneeName = a.AssigneeName,
                    AssigneeUserId = a.AssigneeUserId,
                    CreatedAt = DateTime.UtcNow,
                    RowVersion = new byte[8],
                });
            }
        }

        await AddAuditAsync(doc.Id, doc.CurrentVersionId, "Published", from, ControlledDocumentStatus.Published,
            userId, userName, "Document published", ct);

        var from2 = doc.Status;
        doc.TransitionTo(ControlledDocumentStatus.Effective, userId);
        await AddAuditAsync(doc.Id, doc.CurrentVersionId, "Effective", from2, ControlledDocumentStatus.Effective,
            userId, userName, null, ct);

        doc.LastUpdatedAt = DateTime.UtcNow;

        // Phase 4 Sprint 2: stub outbox + log so Professional training can react to WI/SOP publish.
        if (MandatoryTrainingHintTypes.Contains(doc.DocumentType))
        {
            await EmitMandatoryTrainingHintAsync(doc, userId, ct);
        }

        await _db.SaveChangesAsync(ct);

        return await _queries.GetDocumentByIdAsync(organizationId, documentId, ct);
    }

    private async Task EmitMandatoryTrainingHintAsync(ControlledDocument doc, Guid userId, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(new
        {
            ControlledDocumentId = doc.Id,
            doc.DocumentNumber,
            doc.Title,
            DocumentType = doc.DocumentType.ToString(),
            doc.TenantId,
            doc.HospitalId,
            doc.Department,
            PublishedByUserId = userId,
            Hint = "Consider creating or renewing mandatory training linked to this controlled document."
        });

        _db.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateId = doc.Id,
            OccurredOn = DateTime.UtcNow,
            Type = $"ComplianceEvent.{ComplianceEventType.DocumentPublishedForMandatoryTraining}",
            Payload = payload,
            RetryCount = 0,
        });

        _logger.LogInformation(
            "DocumentPublishedForMandatoryTraining stub for {DocumentNumber} ({DocumentId}) type {DocumentType}",
            doc.DocumentNumber, doc.Id, doc.DocumentType);

        await Task.CompletedTask;
    }

    public async Task<DocumentAcknowledgementListItemDto?> AcknowledgeAsync(
        Guid organizationId, Guid userId, AcknowledgeDocumentRequest request, CancellationToken ct = default)
    {
        var ack = await _db.DocumentAcknowledgements
            .Include(a => a.ControlledDocument)
            .FirstOrDefaultAsync(a => a.Id == request.AcknowledgementId
                                      && a.ControlledDocument.TenantId == organizationId, ct);
        if (ack == null) return null;

        var now = DateTime.UtcNow;
        if (ack.ReadAt == null)
            ack.ReadAt = now;
        if (!request.MarkReadOnly)
            ack.AcknowledgedAt = now;

        await _db.SaveChangesAsync(ct);

        return new DocumentAcknowledgementListItemDto
        {
            Id = ack.Id,
            DocumentNumber = ack.ControlledDocument.DocumentNumber,
            Title = ack.ControlledDocument.Title,
            AssigneeName = ack.AssigneeName,
            AssigneeType = ack.AssigneeType,
            ReadAt = ack.ReadAt,
            AcknowledgedAt = ack.AcknowledgedAt,
            ReminderCount = ack.ReminderCount,
        };
    }

    public async Task<ControlledDocumentDetailDto?> CompleteReviewAsync(
        Guid organizationId, Guid documentId, Guid userId, string? userName,
        CompleteDocumentReviewRequest request, CancellationToken ct = default)
    {
        var doc = await _db.ControlledDocuments
            .FirstOrDefaultAsync(d => d.TenantId == organizationId && d.Id == documentId && !d.IsArchived, ct);
        if (doc == null) return null;

        doc.EnsureMutable();

        if (doc.Status is not (ControlledDocumentStatus.ReviewDue or ControlledDocumentStatus.Effective))
            throw new InvalidOperationException("Complete review is only allowed for Effective or ReviewDue documents.");

        var openReview = await _db.DocumentReviews
            .Where(r => r.ControlledDocumentId == doc.Id && r.CompletedDate == null)
            .OrderByDescending(r => r.ScheduledDate ?? r.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (openReview == null)
        {
            openReview = new DocumentReview
            {
                Id = Guid.NewGuid(),
                ControlledDocumentId = doc.Id,
                DocumentVersionId = doc.CurrentVersionId,
                ScheduledDate = doc.NextReviewDate ?? DateTime.UtcNow.Date,
                ReviewerUserId = userId,
                ReviewerName = userName,
                CreatedAt = DateTime.UtcNow,
                RowVersion = new byte[8],
            };
            _db.DocumentReviews.Add(openReview);
        }

        openReview.CompletedDate = DateTime.UtcNow;
        openReview.ReviewerUserId = userId;
        openReview.ReviewerName = userName;
        openReview.Outcome = request.Outcome ?? (request.NoChangesRequired ? "NoChanges" : "RevisionRequired");
        openReview.Notes = request.Notes;

        var from = doc.Status;
        if (request.NoChangesRequired)
        {
            if (doc.Status == ControlledDocumentStatus.ReviewDue)
                doc.TransitionTo(ControlledDocumentStatus.Effective, userId);

            var next = request.NextReviewDate
                       ?? DateTime.UtcNow.Date.AddMonths(Math.Max(1, doc.ReviewFrequencyMonths));
            doc.NextReviewDate = next;
            doc.ReviewDate = next;
        }
        else
        {
            if (doc.Status == ControlledDocumentStatus.Effective)
                doc.TransitionTo(ControlledDocumentStatus.ReviewDue, userId);
            if (doc.Status == ControlledDocumentStatus.ReviewDue)
                doc.TransitionTo(ControlledDocumentStatus.UnderReview, userId);

            if (doc.CurrentVersionId.HasValue)
                await EnsureApprovalChainAsync(doc.CurrentVersionId.Value, doc, ct);
        }

        doc.LastUpdatedAt = DateTime.UtcNow;
        doc.LastUpdatedByUserId = userId;

        await AddAuditAsync(doc.Id, doc.CurrentVersionId, "ReviewCompleted", from, doc.Status,
            userId, userName, openReview.Outcome, ct);
        await _db.SaveChangesAsync(ct);

        return await _queries.GetDocumentByIdAsync(organizationId, documentId, ct);
    }

    /// <summary>Marks all non-current (or all prior) versions as superseded.</summary>
    internal static void SupersedePriorVersions(
        IEnumerable<DocumentVersion> versions,
        Guid? currentVersionId = null)
    {
        foreach (var v in versions)
        {
            if (currentVersionId.HasValue)
            {
                if (v.Id != currentVersionId.Value)
                {
                    v.IsSuperseded = true;
                    v.IsCurrent = false;
                }
            }
            else if (v.IsCurrent)
            {
                v.IsCurrent = false;
                v.IsSuperseded = true;
            }
            else
            {
                v.IsSuperseded = true;
            }
        }
    }

    private async Task ResetAcknowledgementsForReAckAsync(ControlledDocument doc, CancellationToken ct)
    {
        var prior = await _db.DocumentAcknowledgements
            .Where(a => a.ControlledDocumentId == doc.Id)
            .ToListAsync(ct);

        foreach (var ack in prior)
        {
            if (doc.CurrentVersionId.HasValue)
                ack.DocumentVersionId = doc.CurrentVersionId.Value;
            ack.ReadAt = null;
            ack.AcknowledgedAt = null;
            ack.ReminderCount = 0;
            ack.LastReminderAt = null;
        }
    }

    private async Task EnsureApprovalChainAsync(Guid versionId, ControlledDocument doc, CancellationToken ct)
    {
        var existing = await _db.DocumentApprovals.AnyAsync(a => a.DocumentVersionId == versionId, ct);
        if (existing) return;

        foreach (var (order, role) in DefaultApprovalChain)
        {
            _db.DocumentApprovals.Add(new DocumentApproval
            {
                Id = Guid.NewGuid(),
                DocumentVersionId = versionId,
                ApprovalOrder = order,
                ApproverRole = role,
                ApproverName = role switch
                {
                    "Author" => doc.OwnerName,
                    "Department Reviewer" => doc.ReviewerName,
                    _ => doc.ApproverName,
                },
                Status = DocumentApprovalStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                RowVersion = new byte[8],
            });
        }
    }

    private async Task AddAuditAsync(
        Guid docId, Guid? versionId, string eventType,
        ControlledDocumentStatus? from, ControlledDocumentStatus? to,
        Guid? userId, string? userName, string? details, CancellationToken ct)
    {
        _db.DocumentLifecycleAuditEntries.Add(new DocumentLifecycleAuditEntry
        {
            Id = Guid.NewGuid(),
            ControlledDocumentId = docId,
            DocumentVersionId = versionId,
            EventType = eventType,
            FromStatus = from,
            ToStatus = to,
            PerformedByUserId = userId,
            PerformedByName = userName,
            Details = details,
            CreatedAt = DateTime.UtcNow,
            RowVersion = new byte[8],
        });
        await Task.CompletedTask;
    }
}
