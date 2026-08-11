using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.EvidenceReview;
using ComplianceService.Application.Events;
using ComplianceService.Application.Journey;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Application.Services;

/// <summary>
/// Central review pipeline: state transitions, audit trail, obligation projection, deficiency sync, events, snapshots.
/// </summary>
public sealed class EvidenceReviewService : IEvidenceReviewService
{
    private readonly ComplianceDbContext _db;
    private readonly IObligationProjectionService _obligationProjection;
    private readonly IDeficiencyTaskLifecycleService _deficiencyLifecycle;
    private readonly IComplianceEventEmitter _eventEmitter;
    private readonly IComplianceSnapshotRecalculationQueue _snapshotQueue;
    private readonly IComplianceNotificationPublisher _notifications;
    private readonly ILogger<EvidenceReviewService> _logger;

    public EvidenceReviewService(
        ComplianceDbContext db,
        IObligationProjectionService obligationProjection,
        IDeficiencyTaskLifecycleService deficiencyLifecycle,
        IComplianceEventEmitter eventEmitter,
        IComplianceSnapshotRecalculationQueue snapshotQueue,
        IComplianceNotificationPublisher notifications,
        ILogger<EvidenceReviewService> logger)
    {
        _db = db;
        _obligationProjection = obligationProjection;
        _deficiencyLifecycle = deficiencyLifecycle;
        _eventEmitter = eventEmitter;
        _snapshotQueue = snapshotQueue;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<EvidenceApprovalResultDto> ApplyReviewAsync(
        Guid evidenceId,
        Guid reviewerId,
        EvidenceReviewDecision decision,
        string? comment,
        string? recommendation,
        string? reviewerRole,
        string? expectedRowVersionBase64 = null,
        Guid? objectiveElementId = null,
        CancellationToken cancellationToken = default)
    {
        if (evidenceId == Guid.Empty)
        {
            throw new ArgumentException("EvidenceId is required.", nameof(evidenceId));
        }

        if (reviewerId == Guid.Empty)
        {
            throw new ArgumentException("ReviewerId is required.", nameof(reviewerId));
        }

        var targetStatus = EvidenceVerificationStateMachine.MapReviewAction(decision);

        var linksQuery = _db.ObjectiveElementEvidences.Where(x => x.EvidenceId == evidenceId);
        if (objectiveElementId.HasValue && objectiveElementId.Value != Guid.Empty)
        {
            linksQuery = linksQuery.Where(x => x.ObjectiveElementId == objectiveElementId.Value);
        }

        var links = await linksQuery.OrderByDescending(x => x.LinkedAt).ToListAsync(cancellationToken);
        if (links.Count == 0)
        {
            throw new InvalidOperationException("Evidence is not mapped to any objective element.");
        }

        if (!string.IsNullOrWhiteSpace(expectedRowVersionBase64))
        {
            var expected = ParseRowVersion(expectedRowVersionBase64);
            if (expected == null)
            {
                throw new ArgumentException("Invalid row version supplied.", nameof(expectedRowVersionBase64));
            }

            if (!links.Any(l => l.RowVersion != null && l.RowVersion.SequenceEqual(expected)))
            {
                throw new DbUpdateConcurrencyException("Evidence verification state was updated by another user.");
            }
        }

        if (decision is EvidenceReviewDecision.Reject or EvidenceReviewDecision.RequestMoreEvidence
            && string.IsNullOrWhiteSpace(comment))
        {
            _logger.LogWarning(
                "Evidence review submitted without comment. EvidenceId={EvidenceId} Decision={Decision} UserId={UserId}",
                evidenceId,
                decision,
                reviewerId);
        }

        var evidenceRow = await _db.Evidences
            .FirstOrDefaultAsync(e => e.Id == evidenceId, cancellationToken)
            ?? throw new InvalidOperationException("Evidence record not found.");

        var now = DateTime.UtcNow;
        var auditEntries = new List<EvidenceReviewAuditEntry>();

        foreach (var link in links)
        {
            // Closed/approved evidence is immutable — reject double-approve and further mutations.
            if (EvidenceVerificationStateMachine.IsApproved(link.VerificationStatus)
                || link.VerificationStatus == EvidenceVerificationStatus.Verified)
            {
                throw new InvalidOperationException(
                    $"Evidence verification is closed (Verified). Cannot apply {decision} to ObjectiveElement {link.ObjectiveElementId}.");
            }

            if (decision == EvidenceReviewDecision.Approve
                && EvidenceVerificationStateMachine.IsApproved(link.VerificationStatus))
            {
                throw new InvalidOperationException(
                    $"Evidence is already approved for ObjectiveElement {link.ObjectiveElementId}. Double-approve is not allowed.");
            }

            if (!EvidenceVerificationStateMachine.CanTransition(link.VerificationStatus, targetStatus))
            {
                throw new InvalidOperationException(
                    $"Invalid verification transition from {link.VerificationStatus} to {targetStatus}.");
            }

            var previous = link.VerificationStatus;
            link.VerificationStatus = targetStatus;
            link.VerifiedBy = reviewerId;
            link.VerifiedAt = now;
            link.ReviewComment = decision == EvidenceReviewDecision.Approve ? null : NormalizeComment(comment);
            link.ReviewRecommendation = NormalizeComment(recommendation);
            link.Remarks = link.ReviewComment;

            auditEntries.Add(new EvidenceReviewAuditEntry
            {
                Id = Guid.NewGuid(),
                EvidenceId = evidenceId,
                ObjectiveElementId = link.ObjectiveElementId,
                HospitalId = evidenceRow.HospitalId,
                FrameworkId = evidenceRow.FrameworkId,
                ReviewerId = reviewerId,
                ReviewerRole = reviewerRole,
                ReviewedAt = now,
                Decision = decision.ToString(),
                PreviousStatus = previous,
                NewStatus = targetStatus,
                Comment = link.ReviewComment,
                Recommendation = link.ReviewRecommendation,
                EvidenceVersion = evidenceRow.CurrentVersion
            });
        }

        await _db.EvidenceReviewAuditEntries.AddRangeAsync(auditEntries, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        var latest = links.OrderByDescending(x => x.LinkedAt).First();
        _logger.LogInformation(
            "Evidence review applied. EvidenceId={EvidenceId} Decision={Decision} UserId={UserId}",
            evidenceId,
            decision,
            reviewerId);

        var projectionTrigger = decision switch
        {
            EvidenceReviewDecision.Approve => ObligationProjectionTriggers.EvidenceApproved,
            EvidenceReviewDecision.Reject => ObligationProjectionTriggers.EvidenceRejected,
            EvidenceReviewDecision.RequestMoreEvidence => ObligationProjectionTriggers.EvidenceNeedsMore,
            _ => ObligationProjectionTriggers.EvidenceApproved
        };

        foreach (var oeId in links.Select(l => l.ObjectiveElementId).Distinct())
        {
            await _obligationProjection.ProjectByObjectiveElementAsync(
                evidenceRow.HospitalId,
                oeId,
                projectionTrigger,
                cancellationToken);
        }

        if (evidenceRow.TaskId != Guid.Empty)
        {
            var lifeTrigger = MapLifecycleTrigger(decision);
            if (lifeTrigger.HasValue)
            {
                await _deficiencyLifecycle.SyncFromEvidenceAsync(
                    evidenceRow.TaskId,
                    lifeTrigger.Value,
                    cancellationToken);
            }

            if (decision == EvidenceReviewDecision.Reject)
            {
                await ReopenTaskIfCompletedAsync(evidenceRow.TaskId, evidenceRow.HospitalId, cancellationToken);
            }
        }

        await EmitReviewEventsAsync(decision, evidenceRow, reviewerId, links, cancellationToken);

        var notificationEvent = decision switch
        {
            EvidenceReviewDecision.Approve => "evidence.approved",
            EvidenceReviewDecision.Reject => "evidence.rejected",
            EvidenceReviewDecision.RequestMoreEvidence => "evidence.needs_more",
            _ => "evidence.reviewed",
        };

        await _notifications.PublishAsync(new ComplianceNotificationMessage
        {
            EventType = notificationEvent,
            Title = $"Evidence {decision}",
            Body = $"Evidence {evidenceId} marked {MapStatusLabel(latest.VerificationStatus)} by reviewer {reviewerId}",
            HospitalId = evidenceRow.HospitalId,
            Data = new Dictionary<string, string>
            {
                ["evidenceId"] = evidenceId.ToString(),
                ["decision"] = decision.ToString(),
                ["status"] = MapStatusLabel(latest.VerificationStatus),
                ["frameworkId"] = evidenceRow.FrameworkId.ToString(),
            },
        }, cancellationToken);

        _snapshotQueue.EnqueueHospitalFramework(evidenceRow.HospitalId, evidenceRow.FrameworkId);

        return new EvidenceApprovalResultDto
        {
            EvidenceId = evidenceId,
            Status = MapStatusLabel(latest.VerificationStatus),
            IsApproved = ToIsApproved(latest.VerificationStatus),
            ReviewComment = latest.ReviewComment,
            VerifiedBy = latest.VerifiedBy?.ToString(),
            VerifiedAt = latest.VerifiedAt,
            RowVersion = EncodeRowVersion(latest.RowVersion)
        };
    }

    public async Task<PagedResultDto<EvidenceReviewQueueItemDto>> GetReviewQueueAsync(
        Guid hospitalId,
        Guid organizationId,
        Guid frameworkId,
        EvidenceReviewQueueQuery query,
        CancellationToken cancellationToken = default)
    {
        var fwId = await ResolveFrameworkOrJourneyIdAsync(hospitalId, frameworkId, cancellationToken);
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var baseQuery =
            from link in _db.ObjectiveElementEvidences.AsNoTracking()
            join e in _db.Evidences.AsNoTracking() on link.EvidenceId equals e.Id
            join oe in _db.ObjectiveElements.AsNoTracking() on link.ObjectiveElementId equals oe.Id
            join st in _db.ComplianceStandards.AsNoTracking() on oe.ComplianceStandardId equals st.Id
            join ch in _db.ComplianceChapters.AsNoTracking() on st.ChapterId equals ch.Id
            join t in _db.ComplianceTasks.AsNoTracking() on e.TaskId equals t.Id into tj
            from task in tj.DefaultIfEmpty()
            where e.HospitalId == hospitalId && e.FrameworkId == fwId && ch.FrameworkId == fwId && oe.IsApplicable
            select new ReviewQueueRow
            {
                LinkId = link.Id,
                ObjectiveElementId = link.ObjectiveElementId,
                VerificationStatus = link.VerificationStatus,
                RowVersion = link.RowVersion,
                EvidenceId = e.Id,
                FileUrl = e.FileUrl,
                Title = e.Title,
                EvidenceType = e.EvidenceType,
                UploadedBy = e.UploadedBy,
                UploadedOn = e.UploadedOn,
                CurrentVersion = e.CurrentVersion,
                ObjectiveElementCode = oe.Code,
                ObjectiveElementName = oe.Title,
                ChapterCode = ch.Code,
                ChapterName = ch.Name,
                TaskId = task != null ? task.Id : (Guid?)null,
                TaskTitle = task != null ? task.Title : null,
                TaskDepartmentId = task != null ? task.DepartmentId : null,
                TaskDeptName = task != null ? task.DepartmentName : null,
                OwnerId = task != null ? task.AssignedToUserId : null,
                EvidenceDepartment = e.Department,
                MaturityLevel = _db.OrganizationComplianceObligations.AsNoTracking()
                    .Where(obl => obl.ObjectiveElementId == oe.Id
                        && obl.OrganizationId == organizationId
                        && obl.HospitalId == hospitalId
                        && obl.FrameworkId == fwId)
                    .Select(obl => (ObjectiveElementClassification?)obl.MaturityLevel)
                    .FirstOrDefault() ?? oe.Classification
            };

        baseQuery = ApplyReviewQueueFilters(baseQuery, query);

        var totalCount = await baseQuery.CountAsync(cancellationToken);
        var rows = await baseQuery
            .OrderByDescending(x => x.UploadedOn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = rows.ConvertAll(row => new EvidenceReviewQueueItemDto
        {
            LinkId = row.LinkId,
            EvidenceId = row.EvidenceId,
            ObjectiveElementId = row.ObjectiveElementId,
            FileName = EvidenceLibraryQueryHelper.FileNameFromUrl(row.FileUrl, row.Title),
            FileType = EvidenceLibraryQueryHelper.NormalizeFileType(row.FileUrl),
            ObjectiveElementCode = row.ObjectiveElementCode ?? string.Empty,
            ObjectiveElementName = row.ObjectiveElementName ?? string.Empty,
            ChapterCode = row.ChapterCode ?? string.Empty,
            ChapterName = row.ChapterName ?? string.Empty,
            MaturityStage = row.MaturityLevel.ToString(),
            DepartmentId = row.TaskDepartmentId,
            Department = row.TaskDeptName ?? row.EvidenceDepartment ?? string.Empty,
            UploadedBy = row.UploadedBy,
            UploadDate = row.UploadedOn,
            Status = EvidenceVerificationStateMachine.ToApiLabel(row.VerificationStatus),
            EvidenceType = row.EvidenceType,
            TaskId = row.TaskId,
            LinkedTask = row.TaskTitle,
            OwnerId = ParseOwnerGuid(row.OwnerId),
            RowVersion = EvidenceLibraryQueryHelper.EncodeRowVersion(row.RowVersion),
            Version = row.CurrentVersion
        });

        return new PagedResultDto<EvidenceReviewQueueItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<IReadOnlyList<EvidenceReviewAuditEntryDto>> GetAuditTrailAsync(
        Guid hospitalId,
        Guid frameworkId,
        Guid evidenceId,
        Guid? objectiveElementId = null,
        CancellationToken cancellationToken = default)
    {
        var fwId = await ResolveFrameworkOrJourneyIdAsync(hospitalId, frameworkId, cancellationToken);

        var query = _db.EvidenceReviewAuditEntries.AsNoTracking()
            .Where(x => x.EvidenceId == evidenceId && x.HospitalId == hospitalId && x.FrameworkId == fwId);

        if (objectiveElementId.HasValue && objectiveElementId.Value != Guid.Empty)
        {
            query = query.Where(x => x.ObjectiveElementId == objectiveElementId.Value);
        }

        var rows = await query.OrderByDescending(x => x.ReviewedAt).ToListAsync(cancellationToken);
        return rows.ConvertAll(x => new EvidenceReviewAuditEntryDto
        {
            Id = x.Id,
            EvidenceId = x.EvidenceId,
            ObjectiveElementId = x.ObjectiveElementId,
            ReviewerId = x.ReviewerId,
            ReviewerRole = x.ReviewerRole,
            ReviewedAt = x.ReviewedAt,
            Decision = x.Decision,
            PreviousStatus = EvidenceVerificationStateMachine.ToApiLabel(x.PreviousStatus),
            NewStatus = EvidenceVerificationStateMachine.ToApiLabel(x.NewStatus),
            Comment = x.Comment,
            Recommendation = x.Recommendation,
            EvidenceVersion = x.EvidenceVersion
        });
    }

    private async Task EmitReviewEventsAsync(
        EvidenceReviewDecision decision,
        Evidence evidenceRow,
        Guid reviewerId,
        IReadOnlyList<ObjectiveElementEvidence> links,
        CancellationToken cancellationToken)
    {
        var eventType = decision switch
        {
            EvidenceReviewDecision.Approve => ComplianceEventType.EvidenceApproved,
            EvidenceReviewDecision.Reject => ComplianceEventType.EvidenceRejected,
            EvidenceReviewDecision.RequestMoreEvidence => ComplianceEventType.NeedsMoreEvidence,
            _ => ComplianceEventType.EvidenceUploaded
        };

        var entityId = evidenceRow.TaskId != Guid.Empty ? evidenceRow.TaskId : evidenceRow.Id;
        var entityType = evidenceRow.TaskId != Guid.Empty ? nameof(ComplianceTask) : nameof(Evidence);

        await _eventEmitter.EmitAsync(
            eventType,
            entityId,
            entityType,
            evidenceRow.HospitalId,
            reviewerId,
            new
            {
                evidenceId = evidenceRow.Id,
                evidenceRow.TaskId,
                objectiveElementIds = links.Select(l => l.ObjectiveElementId).Distinct().ToList()
            },
            cancellationToken);
    }

    private async Task ReopenTaskIfCompletedAsync(
        Guid taskId,
        Guid hospitalId,
        CancellationToken cancellationToken)
    {
        var task = await _db.ComplianceTasks.FirstOrDefaultAsync(
            t => t.Id == taskId && t.HospitalId == hospitalId,
            cancellationToken);

        if (task == null || task.Status != ComplianceTaskStatus.Completed)
        {
            return;
        }

        task.Status = ComplianceTaskStatus.InProgress;
        await _db.SaveChangesAsync(cancellationToken);

        await _obligationProjection.ProjectByObjectiveElementAsync(
            hospitalId,
            task.ObjectiveElementId,
            ObligationProjectionTriggers.TaskReopened,
            cancellationToken);

        _logger.LogInformation("Task {TaskId} reopened after evidence rejection.", taskId);
    }

    private static EvidenceLifecycleTrigger? MapLifecycleTrigger(EvidenceReviewDecision decision) =>
        decision switch
        {
            EvidenceReviewDecision.Approve => EvidenceLifecycleTrigger.Approved,
            EvidenceReviewDecision.Reject => EvidenceLifecycleTrigger.Rejected,
            EvidenceReviewDecision.RequestMoreEvidence => EvidenceLifecycleTrigger.NeedsMoreEvidence,
            _ => null
        };

    private static IQueryable<ReviewQueueRow> ApplyReviewQueueFilters(
        IQueryable<ReviewQueueRow> query,
        EvidenceReviewQueueQuery filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            var parsed = EvidenceVerificationStateMachine.ParseFilter(filter.Status);
            if (parsed.HasValue)
            {
                query = query.Where(x => x.VerificationStatus == parsed.Value);
            }
            else if (filter.Status.Trim().Equals("pendingReview", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(x =>
                    x.VerificationStatus == EvidenceVerificationStatus.Pending
                    || x.VerificationStatus == EvidenceVerificationStatus.Submitted);
            }
        }
        else
        {
            query = query.Where(x =>
                x.VerificationStatus == EvidenceVerificationStatus.Pending
                || x.VerificationStatus == EvidenceVerificationStatus.Submitted
                || x.VerificationStatus == EvidenceVerificationStatus.NeedsMoreEvidence);
        }

        if (!string.IsNullOrWhiteSpace(filter.Stage))
        {
            if (Enum.TryParse<ObjectiveElementClassification>(filter.Stage, true, out var stage))
            {
                query = query.Where(x => x.MaturityLevel == stage);
            }
        }

        if (filter.DepartmentId is { } dept && dept != Guid.Empty)
        {
            query = query.Where(x => x.TaskDepartmentId == dept);
        }

        if (filter.OwnerId is { } owner && owner != Guid.Empty)
        {
            var ownerStr = owner.ToString();
            query = query.Where(x => x.OwnerId == ownerStr);
        }

        if (!string.IsNullOrWhiteSpace(filter.EvidenceType))
        {
            var et = filter.EvidenceType.Trim();
            query = query.Where(x => x.EvidenceType == et);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            var pattern = "%" + term.Replace("%", "[%]").Replace("_", "[_]") + "%";
            query = query.Where(x =>
                EF.Functions.Like(x.Title ?? "", pattern) ||
                EF.Functions.Like(x.FileUrl ?? "", pattern) ||
                EF.Functions.Like(x.ObjectiveElementCode ?? "", pattern));
        }

        return query;
    }

    private sealed class ReviewQueueRow
    {
        public Guid LinkId { get; init; }
        public Guid ObjectiveElementId { get; init; }
        public EvidenceVerificationStatus VerificationStatus { get; init; }
        public byte[]? RowVersion { get; init; }
        public Guid EvidenceId { get; init; }
        public string? FileUrl { get; init; }
        public string? Title { get; init; }
        public string? EvidenceType { get; init; }
        public string UploadedBy { get; init; } = string.Empty;
        public DateTime UploadedOn { get; init; }
        public int CurrentVersion { get; init; }
        public string? ObjectiveElementCode { get; init; }
        public string? ObjectiveElementName { get; init; }
        public string? ChapterCode { get; init; }
        public string? ChapterName { get; init; }
        public Guid? TaskId { get; init; }
        public string? TaskTitle { get; init; }
        public Guid? TaskDepartmentId { get; init; }
        public string? TaskDeptName { get; init; }
        public string? OwnerId { get; init; }
        public string? EvidenceDepartment { get; init; }
        public ObjectiveElementClassification MaturityLevel { get; init; }
    }

    private async Task<Guid> ResolveFrameworkOrJourneyIdAsync(
        Guid hospitalId,
        Guid frameworkId,
        CancellationToken cancellationToken)
    {
        return await ComplianceJourneySnapshotResolver.ResolveFrameworkOrJourneyIdAsync(
            _db,
            hospitalId,
            frameworkId,
            cancellationToken);
    }

    private static Guid? ParseOwnerGuid(string? assignedTo)
    {
        if (string.IsNullOrWhiteSpace(assignedTo))
        {
            return null;
        }

        return Guid.TryParse(assignedTo, out var g) ? g : null;
    }

    private static string? NormalizeComment(string? comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
        {
            return null;
        }

        var trimmed = comment.Trim();
        return trimmed.Length > 2000 ? trimmed[..2000] : trimmed;
    }

    private static byte[]? ParseRowVersion(string? base64)
    {
        if (string.IsNullOrWhiteSpace(base64))
        {
            return null;
        }

        try
        {
            return Convert.FromBase64String(base64.Trim());
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static string? EncodeRowVersion(byte[]? rowVersion) =>
        rowVersion == null || rowVersion.Length == 0 ? null : Convert.ToBase64String(rowVersion);

    private static string MapStatusLabel(EvidenceVerificationStatus status) =>
        EvidenceVerificationStateMachine.ToApiLabel(status);

    private static bool? ToIsApproved(EvidenceVerificationStatus status) =>
        status switch
        {
            EvidenceVerificationStatus.Verified => true,
            EvidenceVerificationStatus.Rejected => false,
            _ => null
        };
}
