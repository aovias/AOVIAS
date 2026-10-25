using System.Text.Json;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.EvidenceRegistration;
using ComplianceService.Application.EvidenceReview;
using ComplianceService.Application.Events;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Services;

/// <summary>
/// Unified evidence registration pipeline:
/// Upload → Evidence → ObjectiveElementEvidence → Obligation Projection → Deficiency Evaluation → Outbox → Snapshot.
/// </summary>
public sealed class EvidenceRegistrationService : IEvidenceRegistrationService
{
    private readonly ComplianceDbContext _db;
    private readonly IEvidenceService _evidenceService;
    private readonly IFileStorageService _fileStorage;
    private readonly IComplianceJourneyRepository _journeyRepository;
    private readonly IComplianceTaskEnsureService _taskEnsure;
    private readonly IObligationProjectionService _obligationProjection;
    private readonly IDeficiencyTaskLifecycleService _deficiencyLifecycle;
    private readonly IComplianceEventEmitter _eventEmitter;
    private readonly IComplianceSnapshotRecalculationQueue _snapshotQueue;
    private readonly IEvidenceVirusScanProvider _virusScan;
    private readonly IComplianceNotificationPublisher _notifications;
    private readonly ILogger<EvidenceRegistrationService> _logger;

    public EvidenceRegistrationService(
        ComplianceDbContext db,
        IEvidenceService evidenceService,
        IFileStorageService fileStorage,
        IComplianceJourneyRepository journeyRepository,
        IComplianceTaskEnsureService taskEnsure,
        IObligationProjectionService obligationProjection,
        IDeficiencyTaskLifecycleService deficiencyLifecycle,
        IComplianceEventEmitter eventEmitter,
        IComplianceSnapshotRecalculationQueue snapshotQueue,
        IEvidenceVirusScanProvider virusScan,
        IComplianceNotificationPublisher notifications,
        ILogger<EvidenceRegistrationService> logger)
    {
        _db = db;
        _evidenceService = evidenceService;
        _fileStorage = fileStorage;
        _journeyRepository = journeyRepository;
        _taskEnsure = taskEnsure;
        _obligationProjection = obligationProjection;
        _deficiencyLifecycle = deficiencyLifecycle;
        _eventEmitter = eventEmitter;
        _snapshotQueue = snapshotQueue;
        _virusScan = virusScan;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<EvidenceRegistrationResult> RegisterFromUploadAsync(
        EvidenceRegistrationUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.FileStream);
        request.Context.ValidateAnchors();

        var evidenceType = EvidenceTypeCatalog.Normalize(request.EvidenceType);
        var resolved = await ResolveTargetsAsync(request.Context, cancellationToken);
        await EnsureJourneyWritableAsync(resolved.Journey, cancellationToken);

        // Virus-scan hook (NoOp in Dev). Seekable streams are rewound after scan.
        if (request.FileStream.CanSeek)
        {
            request.FileStream.Position = 0;
        }

        var scan = await _virusScan.ScanAsync(
            request.FileStream,
            request.FileName,
            request.ContentType,
            cancellationToken);
        if (!scan.IsClean)
        {
            throw new InvalidOperationException(
                $"Evidence upload rejected by virus scan ({scan.Provider}): {scan.ThreatName ?? "threat detected"}.");
        }

        if (request.FileStream.CanSeek)
        {
            request.FileStream.Position = 0;
        }

        var evidenceId = Guid.NewGuid();
        var safeRelative = BuildStorageRelativePath(resolved, evidenceId);
        var safeFile = Path.GetFileName(string.IsNullOrWhiteSpace(request.FileName) ? "upload.bin" : request.FileName);
        var destName = $"{Guid.NewGuid():N}_{safeFile}";
        var effectiveTitle = !string.IsNullOrWhiteSpace(request.DisplayName)
            ? request.DisplayName.Trim()
            : (!string.IsNullOrWhiteSpace(request.Title) ? request.Title.Trim() : safeFile);

        var fileUrl = await _fileStorage.SaveAsync(
            request.FileStream,
            destName,
            request.ContentType ?? "application/octet-stream",
            safeRelative,
            cancellationToken);

        var metaPayload = request.Options.MetadataJsonOverride
            ?? BuildEvidenceMetadataJson(request.ContentType, safeFile, request.Metadata);

        var evidence = new Evidence
        {
            Id = evidenceId,
            TaskId = resolved.TaskId,
            HospitalId = resolved.HospitalId,
            FrameworkId = resolved.FrameworkId,
            EvidenceType = evidenceType,
            FileUrl = fileUrl,
            Hash = string.Empty,
            UploadedBy = request.Context.UserId.ToString(),
            Metadata = metaPayload,
            Title = effectiveTitle,
            Department = resolved.DepartmentName,
            UploadedOn = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            Source = ResolveSourceLabel(request.Context)
        };

        var created = await _evidenceService.CreateEvidenceAsync(evidence, cancellationToken);

        _logger.LogInformation(
            "EvidenceRegistration: Created evidence {EvidenceId} for OE {ObjectiveElementId} task {TaskId}.",
            created.Id,
            resolved.ObjectiveElementId,
            resolved.TaskId);

        return await LinkAndCompleteAsync(
            created,
            resolved,
            request.Context.UserId,
            request.Options,
            cancellationToken);
    }

    public async Task<EvidenceRegistrationResult> RegisterFromUrlAsync(
        EvidenceRegistrationUrlRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Context.ValidateAnchors();

        if (string.IsNullOrWhiteSpace(request.FileUrl))
        {
            throw new ArgumentException("FileUrl is required.", nameof(request));
        }

        var evidenceType = EvidenceTypeCatalog.Normalize(request.EvidenceType);
        var resolved = await ResolveTargetsAsync(request.Context, cancellationToken);
        await EnsureJourneyWritableAsync(resolved.Journey, cancellationToken);

        var evidence = new Evidence
        {
            Id = Guid.NewGuid(),
            TaskId = resolved.TaskId,
            HospitalId = resolved.HospitalId,
            FrameworkId = resolved.FrameworkId,
            EvidenceType = evidenceType,
            FileUrl = request.FileUrl.Trim(),
            Hash = string.Empty,
            UploadedBy = request.Context.UserId.ToString(),
            Metadata = request.Metadata ?? string.Empty,
            Title = request.Title,
            Department = resolved.DepartmentName,
            UploadedOn = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            Source = ResolveSourceLabel(request.Context)
        };

        var created = await _evidenceService.CreateEvidenceAsync(evidence, cancellationToken);

        return await LinkAndCompleteAsync(
            created,
            resolved,
            request.Context.UserId,
            request.Options,
            cancellationToken);
    }

    public async Task<EvidenceRegistrationResult> LinkExistingEvidenceAsync(
        EvidenceRegistrationLinkRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.EvidenceId == Guid.Empty || request.ObjectiveElementId == Guid.Empty)
        {
            throw new ArgumentException("EvidenceId and ObjectiveElementId are required.");
        }

        var evidence = await _db.Evidences
            .FirstOrDefaultAsync(e => e.Id == request.EvidenceId, cancellationToken)
            ?? throw new InvalidOperationException("Evidence not found.");

        var hospitalId = request.HospitalId != Guid.Empty ? request.HospitalId : evidence.HospitalId;
        ComplianceJourney? journey = null;
        if (request.JourneyId is Guid jid && jid != Guid.Empty)
        {
            journey = await _journeyRepository.GetByIdAsync(jid, cancellationToken);
        }

        var resolved = new ResolvedEvidenceTarget(
            evidence.TaskId,
            request.ObjectiveElementId,
            hospitalId,
            evidence.FrameworkId,
            journey,
            DepartmentName: evidence.Department);

        return await LinkAndCompleteAsync(
            evidence,
            resolved,
            request.LinkedByUserId,
            request.Options,
            cancellationToken);
    }

    public async Task<EvidenceIntegrityReport> AuditIntegrityAsync(
        Guid hospitalId,
        Guid frameworkId,
        CancellationToken cancellationToken = default)
    {
        var evidenceIds = await _db.Evidences.AsNoTracking()
            .Where(e => e.HospitalId == hospitalId && e.FrameworkId == frameworkId)
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);

        var linkedIds = await _db.ObjectiveElementEvidences.AsNoTracking()
            .Where(l => evidenceIds.Contains(l.EvidenceId))
            .Select(l => l.EvidenceId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var linkedSet = linkedIds.ToHashSet();
        var orphaned = evidenceIds.Where(id => !linkedSet.Contains(id)).ToList();

        var taskLinkedOrphans = await (
            from e in _db.Evidences.AsNoTracking()
            where orphaned.Contains(e.Id) && e.TaskId != Guid.Empty
            select e.Id).ToListAsync(cancellationToken);

        var duplicateHashGroups = await _db.Evidences.AsNoTracking()
            .Where(e => e.HospitalId == hospitalId && e.FrameworkId == frameworkId && e.Hash != string.Empty)
            .GroupBy(e => e.Hash)
            .Where(g => g.Count() > 1)
            .CountAsync(cancellationToken);

        return new EvidenceIntegrityReport
        {
            OrphanedEvidenceCount = orphaned.Count,
            MissingObjectiveElementLinkCount = orphaned.Count,
            DuplicateHashCount = duplicateHashGroups,
            OrphanedEvidenceIds = orphaned,
            EvidenceMissingLinkIds = taskLinkedOrphans
        };
    }

    private async Task<EvidenceRegistrationResult> LinkAndCompleteAsync(
        Evidence evidence,
        ResolvedEvidenceTarget resolved,
        Guid userId,
        EvidenceRegistrationOptions options,
        CancellationToken cancellationToken)
    {
        var exists = await _db.ObjectiveElementEvidences
            .FirstOrDefaultAsync(
                x => x.ObjectiveElementId == resolved.ObjectiveElementId && x.EvidenceId == evidence.Id,
                cancellationToken);

        if (exists == null)
        {
            await _db.ObjectiveElementEvidences.AddAsync(new ObjectiveElementEvidence
            {
                Id = Guid.NewGuid(),
                ObjectiveElementId = resolved.ObjectiveElementId,
                EvidenceId = evidence.Id,
                LinkedBy = userId,
                LinkedAt = DateTime.UtcNow,
                VerificationStatus = EvidenceVerificationStatus.Pending,
                Remarks = options.Remarks
            }, cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);
        }
        else if (exists.VerificationStatus is EvidenceVerificationStatus.NeedsMoreEvidence
                 or EvidenceVerificationStatus.Rejected)
        {
            exists.VerificationStatus = EvidenceVerificationStatus.Pending;
            exists.VerifiedBy = null;
            exists.VerifiedAt = null;
            exists.ReviewComment = null;
            exists.ReviewRecommendation = null;
            exists.LinkedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        // DEF-M5-001 / M5.A — domain state first, derived read models second.
        //
        // The deficiency's Uploaded → EvidenceSubmitted transition is part of this business
        // operation and belongs with the evidence it describes. It used to run *after* the
        // obligation projection, so any projection fault discarded it and the deficiency was left
        // behind its own evidence. Projecting first also meant projecting stale state.
        if (evidence.TaskId != Guid.Empty)
        {
            await _deficiencyLifecycle.SyncFromEvidenceAsync(
                evidence.TaskId,
                EvidenceLifecycleTrigger.Uploaded,
                cancellationToken);

            await _eventEmitter.EmitAsync(
                ComplianceEventType.VerificationRequired,
                evidence.TaskId,
                nameof(ComplianceTask),
                resolved.HospitalId,
                userId,
                new { evidenceId = evidence.Id, evidence.TaskId, resolved.ObjectiveElementId },
                cancellationToken);
        }

        // Obligation projection and the dashboard rebuild it flushes are *derived* from the state
        // committed above. They are retryable — the outbox keeps unprocessed messages — so a fault
        // here must not fail a registration that has already happened, nor strand a committed
        // evidence row behind a 400 the operator reads as "nothing was saved".
        try
        {
            await _obligationProjection.ProjectByObjectiveElementAsync(
                resolved.HospitalId,
                resolved.ObjectiveElementId,
                ObligationProjectionTriggers.EvidenceUploaded,
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "EvidenceRegistration: Evidence {EvidenceId} for OE {ObjectiveElementId} was registered, "
                + "but obligation projection failed. Outbox messages remain unprocessed and will be retried.",
                evidence.Id,
                resolved.ObjectiveElementId);
        }

        await _eventEmitter.EmitAsync(
            ComplianceEventType.EvidenceUploaded,
            evidence.Id,
            nameof(Evidence),
            resolved.HospitalId,
            userId,
            new
            {
                evidence.TaskId,
                evidence.HospitalId,
                evidence.FrameworkId,
                evidence.EvidenceType,
                resolved.ObjectiveElementId
            },
            cancellationToken);

        if (resolved.Journey != null && !options.SuppressSnapshotRecalc)
        {
            _snapshotQueue.EnqueueHospitalFramework(resolved.Journey.HospitalId, resolved.Journey.FrameworkId);
        }

        await _notifications.PublishAsync(new ComplianceNotificationMessage
        {
            EventType = "evidence.submitted",
            Title = "Evidence submitted",
            Body = $"Evidence {evidence.Id} submitted for OE {resolved.ObjectiveElementId}",
            HospitalId = resolved.HospitalId,
            RecipientUserId = userId.ToString(),
            Data = new Dictionary<string, string>
            {
                ["evidenceId"] = evidence.Id.ToString(),
                ["objectiveElementId"] = resolved.ObjectiveElementId.ToString(),
                ["frameworkId"] = resolved.FrameworkId.ToString(),
            },
        }, cancellationToken);

        return new EvidenceRegistrationResult(
            evidence.Id,
            evidence.TaskId,
            resolved.ObjectiveElementId,
            resolved.HospitalId,
            resolved.FrameworkId);
    }

    private async Task<ResolvedEvidenceTarget> ResolveTargetsAsync(
        EvidenceRegistrationContext context,
        CancellationToken cancellationToken)
    {
        Guid objectiveElementId = Guid.Empty;
        Guid hospitalId = Guid.Empty;
        Guid frameworkId = Guid.Empty;
        Guid taskId = Guid.Empty;
        string? departmentName = null;
        ComplianceJourney? journey = null;

        if (context.ComplianceDeficiencyId is Guid defId && defId != Guid.Empty)
        {
            var deficiency = await _db.ComplianceDeficiencies.AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == defId, cancellationToken)
                ?? throw new InvalidOperationException($"Deficiency {defId} not found.");

            objectiveElementId = deficiency.ObjectiveElementId;
            hospitalId = deficiency.HospitalId;
            frameworkId = deficiency.FrameworkId;
        }

        if (context.OrganizationComplianceObligationId is Guid oblId && oblId != Guid.Empty)
        {
            var obligation = await _db.OrganizationComplianceObligations.AsNoTracking()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(o => o.Id == oblId, cancellationToken)
                ?? throw new InvalidOperationException($"Obligation {oblId} not found.");

            objectiveElementId = obligation.ObjectiveElementId;
            hospitalId = obligation.HospitalId;
            frameworkId = obligation.FrameworkId;
        }

        if (context.ObjectiveElementId is Guid oeId && oeId != Guid.Empty)
        {
            objectiveElementId = oeId;
        }

        if (context.TaskId is Guid tid && tid != Guid.Empty)
        {
            var task = await _db.ComplianceTasks.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == tid, cancellationToken)
                ?? throw new InvalidOperationException($"Task {tid} not found.");

            taskId = task.Id;
            objectiveElementId = objectiveElementId == Guid.Empty ? task.ObjectiveElementId : objectiveElementId;
            hospitalId = hospitalId == Guid.Empty ? task.HospitalId : hospitalId;
            departmentName = string.IsNullOrWhiteSpace(task.DepartmentName) ? null : task.DepartmentName.Trim();

            if (frameworkId == Guid.Empty)
            {
                frameworkId = await (
                    from s in _db.ComplianceStandards.AsNoTracking()
                    join ch in _db.ComplianceChapters.AsNoTracking() on s.ChapterId equals ch.Id
                    where s.Id == task.ComplianceStandardId
                    select ch.FrameworkId).FirstOrDefaultAsync(cancellationToken);
            }
        }

        if (context.JourneyId is Guid journeyId && journeyId != Guid.Empty)
        {
            journey = await _journeyRepository.GetByIdAsync(journeyId, cancellationToken)
                ?? throw new InvalidOperationException($"Compliance journey {journeyId} not found.");

            if (journey.TenantId != context.TenantId)
            {
                throw new UnauthorizedAccessException($"Access denied to journey {journeyId}.");
            }

            hospitalId = hospitalId == Guid.Empty ? journey.HospitalId : hospitalId;
            frameworkId = frameworkId == Guid.Empty ? journey.FrameworkId : frameworkId;

            if (objectiveElementId == Guid.Empty)
            {
                throw new InvalidOperationException("ObjectiveElementId is required when registering via journey.");
            }

            var ensuredTask = await _taskEnsure.EnsureComplianceTaskForObjectiveElementAsync(
                journey,
                objectiveElementId,
                context.UserId,
                cancellationToken);

            taskId = ensuredTask.Id;
            departmentName ??= string.IsNullOrWhiteSpace(ensuredTask.DepartmentName)
                ? null
                : ensuredTask.DepartmentName.Trim();
        }
        else if (objectiveElementId != Guid.Empty && taskId == Guid.Empty)
        {
            journey = await _journeyRepository.GetByHospitalAndFrameworkAsync(hospitalId, frameworkId, cancellationToken);
            if (journey != null)
            {
                var ensuredTask = await _taskEnsure.EnsureComplianceTaskForObjectiveElementAsync(
                    journey,
                    objectiveElementId,
                    context.UserId,
                    cancellationToken);
                taskId = ensuredTask.Id;
                departmentName ??= string.IsNullOrWhiteSpace(ensuredTask.DepartmentName)
                    ? null
                    : ensuredTask.DepartmentName.Trim();
            }
        }

        if (objectiveElementId == Guid.Empty || hospitalId == Guid.Empty || frameworkId == Guid.Empty || taskId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Could not resolve objective element, hospital, framework, and task for evidence registration.");
        }

        return new ResolvedEvidenceTarget(
            taskId,
            objectiveElementId,
            hospitalId,
            frameworkId,
            journey,
            departmentName);
    }

    private static async Task EnsureJourneyWritableAsync(ComplianceJourney? journey, CancellationToken cancellationToken)
    {
        if (journey == null)
        {
            return;
        }

        if (journey.Status >= ComplianceLifecycleState.Submitted)
        {
            throw new InvalidOperationException("Evidence is locked after submission of the compliance journey.");
        }

        await Task.CompletedTask;
    }

    private static string BuildStorageRelativePath(ResolvedEvidenceTarget resolved, Guid evidenceId)
    {
        if (resolved.Journey != null)
        {
            return Path.Combine(
                "App_Data",
                "evidence_uploads",
                resolved.Journey.Id.ToString("N"),
                resolved.ObjectiveElementId.ToString("N"),
                evidenceId.ToString("N"));
        }

        return Path.Combine(
            "App_Data",
            "evidence_uploads",
            resolved.HospitalId.ToString("N"),
            resolved.ObjectiveElementId.ToString("N"),
            evidenceId.ToString("N"));
    }

    private static string BuildEvidenceMetadataJson(string? contentType, string originalFileName, string? userMetadata)
    {
        var payload = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["contentType"] = contentType ?? "application/octet-stream",
            ["originalFileName"] = originalFileName
        };

        if (!string.IsNullOrWhiteSpace(userMetadata))
        {
            payload["userMetadata"] = userMetadata;
        }

        return JsonSerializer.Serialize(payload);
    }

    private static string ResolveSourceLabel(EvidenceRegistrationContext context)
    {
        if (context.TaskId is Guid t && t != Guid.Empty && context.JourneyId == null)
        {
            return "Task";
        }

        if (context.JourneyId != null)
        {
            return "Journey";
        }

        return "Registration";
    }

    private sealed record ResolvedEvidenceTarget(
        Guid TaskId,
        Guid ObjectiveElementId,
        Guid HospitalId,
        Guid FrameworkId,
        ComplianceJourney? Journey,
        string? DepartmentName);
}
