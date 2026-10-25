using System.Text.Json;
using ComplianceService.Application.Commands;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.Exceptions;
using Microsoft.Extensions.Logging;
using ComplianceService.Application.DTOs;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Services;

public sealed class ObjectiveJourneyAppService : IObjectiveJourneyService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ComplianceDbContext _db;
    private readonly IComplianceJourneyRepository _journeyRepository;
    private readonly IUserContextService _userContext;
    private readonly IJourneyWorkspaceService _journeyWorkspace;
    private readonly ComplianceCommandService _complianceCommandService;
    private readonly IComplianceSnapshotRecalculationQueue _snapshotRecalculationQueue;
    private readonly IStageUnlockingService _stageUnlocking;
    private readonly IObligationProjectionService _obligationProjection;
    private readonly ILogger<ObjectiveJourneyAppService> _logger;

    public ObjectiveJourneyAppService(
        ComplianceDbContext db,
        IComplianceJourneyRepository journeyRepository,
        IUserContextService userContext,
        IJourneyWorkspaceService journeyWorkspace,
        ComplianceCommandService complianceCommandService,
        IComplianceSnapshotRecalculationQueue snapshotRecalculationQueue,
        IStageUnlockingService stageUnlocking,
        IObligationProjectionService obligationProjection,
        ILogger<ObjectiveJourneyAppService> logger)
    {
        _db = db;
        _journeyRepository = journeyRepository;
        _userContext = userContext;
        _journeyWorkspace = journeyWorkspace;
        _complianceCommandService = complianceCommandService;
        _snapshotRecalculationQueue = snapshotRecalculationQueue;
        _stageUnlocking = stageUnlocking;
        _obligationProjection = obligationProjection;
        _logger = logger;
    }

    public async Task<ObjectiveJourneyStateDto> GetAsync(
        Guid journeyId,
        Guid objectiveElementId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var journey = await EnsureJourneyAccessAsync(journeyId, tenantId, cancellationToken);

        var row = await FindWizardRowAsync(
            journeyId,
            objectiveElementId,
            journey.TenantId,
            asNoTracking: true,
            cancellationToken);

        if (row == null)
        {
            return DefaultState(journey, journeyId, objectiveElementId);
        }

        var payload = DeserializePayload(row.WizardPayloadJson);
        return Map(row, payload);
    }

    public async Task<ObjectiveJourneyStateDto> UpsertDraftAsync(
        Guid journeyId,
        Guid objectiveElementId,
        Guid tenantId,
        SaveObjectiveJourneyDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Payload);

        var journey = await EnsureJourneyAccessAsync(journeyId, tenantId, cancellationToken);

        await EnforceStageGateAsync(journeyId, objectiveElementId, cancellationToken);

        OeWizardPayloadValidator.ValidateAndNormalize(request.Payload);

        var incomingCanon = OeWizardPayloadCanonicalizer.Canonicalize(request.Payload);
        OeWizardPayloadCanonicalizer.EnsurePayloadSizeWithinLimit(incomingCanon);

        // SqlServerRetryingExecutionStrategy requires user transactions to run inside ExecuteAsync.
        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable,
                cancellationToken);

            try
            {
                var entity = await FindWizardRowAsync(
                    journeyId,
                    objectiveElementId,
                    journey.TenantId,
                    asNoTracking: false,
                    cancellationToken);

                if (entity == null)
                {
                    entity = new ObjectiveJourney
                    {
                        Id = Guid.NewGuid(),
                        TenantId = journey.TenantId,
                        HospitalId = journey.HospitalId,
                        ComplianceJourneyId = journeyId,
                        ObjectiveElementId = objectiveElementId,
                        WizardPayloadJson = incomingCanon,
                        CurrentStepIndex = request.Payload.StepIndex,
                        Status = ComputeDraftStatus(request.Payload),
                        LastUpdatedAt = DateTime.UtcNow,
                        CompletedAt = null
                    };
                    _db.ObjectiveJourneys.Add(entity);
                    await _db.SaveChangesAsync(cancellationToken);
                    await tx.CommitAsync(cancellationToken);

                    _snapshotRecalculationQueue.EnqueueHospitalFramework(journey.HospitalId, journey.FrameworkId);

                    var createdPayload = DeserializePayload(entity.WizardPayloadJson);
                    return Map(entity, createdPayload);
                }

                if (entity.Status == ObjectiveJourneyStatus.Completed)
                {
                    throw new InvalidOperationException("WIZARD_COMPLETED");
                }

                if (!string.IsNullOrWhiteSpace(request.RowVersion))
                {
                    var expected = TryDecodeRowVersion(request.RowVersion);
                    if (expected == null || !entity.RowVersion.AsSpan().SequenceEqual(expected))
                    {
                        throw new InvalidOperationException("VERSION_CONFLICT");
                    }
                }

                var storedCanon = OeWizardPayloadCanonicalizer.Canonicalize(DeserializePayload(entity.WizardPayloadJson));
                if (string.Equals(storedCanon, incomingCanon, StringComparison.Ordinal))
                {
                    await tx.CommitAsync(cancellationToken);
                    var unchangedPayload = DeserializePayload(entity.WizardPayloadJson);
                    return Map(entity, unchangedPayload);
                }

                entity.WizardPayloadJson = incomingCanon;
                entity.CurrentStepIndex = request.Payload.StepIndex;
                entity.Status = ComputeDraftStatus(request.Payload);
                entity.LastUpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);

                _ = ProjectObligationAfterJourneyChangeAsync(
                    journey.HospitalId,
                    objectiveElementId,
                    cancellationToken);

                var updatedPayload = DeserializePayload(entity.WizardPayloadJson);
                return Map(entity, updatedPayload);
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    public async Task<ObjectiveJourneyStateDto> CompleteAsync(
        Guid journeyId,
        Guid objectiveElementId,
        Guid tenantId,
        Guid userId,
        IReadOnlyList<string> roles,
        CompleteObjectiveJourneyRequest request,
        OperationalMode? operationalMode,
        string? auditLifecycleMode,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Payload);

        var journey = await EnsureJourneyAccessAsync(journeyId, tenantId, cancellationToken);

        await EnforceStageGateAsync(journeyId, objectiveElementId, cancellationToken);

        OeWizardPayloadValidator.ValidateForCompletion(request.Payload);

        var incomingCanon = OeWizardPayloadCanonicalizer.Canonicalize(request.Payload);
        OeWizardPayloadCanonicalizer.EnsurePayloadSizeWithinLimit(incomingCanon);

        var entity = await FindWizardRowAsync(
            journeyId,
            objectiveElementId,
            journey.TenantId,
            asNoTracking: false,
            cancellationToken);

        if (entity == null)
        {
            entity = new ObjectiveJourney
            {
                Id = Guid.NewGuid(),
                TenantId = journey.TenantId,
                HospitalId = journey.HospitalId,
                ComplianceJourneyId = journeyId,
                ObjectiveElementId = objectiveElementId,
                WizardPayloadJson = incomingCanon,
                CurrentStepIndex = request.Payload.StepIndex,
                Status = ObjectiveJourneyStatus.InProgress,
                LastUpdatedAt = DateTime.UtcNow,
                CompletedAt = null
            };
            _db.ObjectiveJourneys.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);
        }

        if (entity.Status == ObjectiveJourneyStatus.Completed)
        {
            throw new InvalidOperationException("WIZARD_COMPLETED");
        }

        if (!string.IsNullOrWhiteSpace(request.RowVersion))
        {
            var expected = TryDecodeRowVersion(request.RowVersion);
            if (expected == null || !entity.RowVersion.AsSpan().SequenceEqual(expected))
            {
                throw new InvalidOperationException("VERSION_CONFLICT");
            }
        }

        var patch = OeWizardCompletionMapper.ToPatchRequest(request.Payload);
        await _journeyWorkspace.PatchObjectiveElementAsync(
            journeyId,
            objectiveElementId,
            tenantId,
            userId,
            roles,
            patch,
            operationalMode,
            auditLifecycleMode,
            cancellationToken);

        var taskId = request.ComplianceTaskId;
        if (taskId == null || taskId == Guid.Empty)
        {
            var task = await _db.ComplianceTasks.AsNoTracking()
                .FirstOrDefaultAsync(
                    t => t.HospitalId == journey.HospitalId && t.ObjectiveElementId == objectiveElementId,
                    cancellationToken);
            taskId = task?.Id;
        }

        if (taskId is { } tid && tid != Guid.Empty)
        {
            await _complianceCommandService.CompleteTaskAsync(
                journey.HospitalId,
                journey.FrameworkId,
                tid,
                cancellationToken);
        }

        entity.WizardPayloadJson = incomingCanon;
        entity.CurrentStepIndex = request.Payload.StepIndex;
        entity.Status = ObjectiveJourneyStatus.Completed;
        entity.CompletedAt = DateTime.UtcNow;
        entity.LastUpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        // Update OrganizationComplianceObligation via centralized projection.
        _ = ProjectObligationAfterJourneyChangeAsync(
            journey.HospitalId,
            objectiveElementId,
            cancellationToken);

        // Post-completion: evaluate stage unlock threshold for the OE's classification band.
        // This is fire-and-forget — a failure must not affect the completion response.
        _ = EvaluateStageUnlockFireAndForgetAsync(journeyId, objectiveElementId, cancellationToken);

        var payload = DeserializePayload(entity.WizardPayloadJson);
        return Map(entity, payload);
    }

    /// <summary>
    /// Evaluates maturity stage unlock thresholds after OE wizard completion.
    /// Runs asynchronously after SaveChanges — does not affect the completion response.
    /// </summary>
    private async Task EvaluateStageUnlockFireAndForgetAsync(
        Guid journeyId,
        Guid objectiveElementId,
        CancellationToken cancellationToken)
    {
        try
        {
            // Resolve the OE's classification and journey context in one query.
            var context = await (
                from oe in _db.ObjectiveElements.AsNoTracking()
                join j in _db.ComplianceJourneys.AsNoTracking() on journeyId equals j.Id
                where oe.Id == objectiveElementId
                select new { oe.Classification, j.HospitalId, j.OrganizationId }
            ).FirstOrDefaultAsync(cancellationToken);

            if (context == null) return;

            var results = await _stageUnlocking.EvaluateAndPersistAsync(
                journeyId,
                "oe-wizard-completed",
                _userContext.UserId,
                cancellationToken);

            var unlocked = results.FirstOrDefault(r => r.StateChanged && r.CriteriaMet);
            if (unlocked != null)
            {
                _logger.LogInformation(
                    "Maturity stage threshold met after OE completion: Stage={Stage}, Readiness={Readiness:F1}%, JourneyId={JourneyId}",
                    unlocked.Stage,
                    unlocked.ReadinessPercent,
                    journeyId);
            }

            // Always trigger CMI recalculation so the Compliance Maturity Dashboard stays current.
            _snapshotRecalculationQueue.EnqueueMaturityRecalc(context.HospitalId, context.OrganizationId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Stage unlock evaluation failed (non-fatal) for JourneyId={JourneyId} OeId={OeId}",
                journeyId, objectiveElementId);
        }
    }

    /// <summary>
    /// Projects obligation state after ObjectiveJourney wizard changes.
    /// Runs asynchronously — a failure must not affect the caller response.
    /// </summary>
    private async Task ProjectObligationAfterJourneyChangeAsync(
        Guid hospitalId,
        Guid objectiveElementId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _obligationProjection.ProjectByObjectiveElementAsync(
                hospitalId,
                objectiveElementId,
                ObligationProjectionTriggers.JourneyUpdated,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Obligation projection failed (non-fatal) for OeId={OeId} HospitalId={HospitalId}",
                objectiveElementId, hospitalId);
        }
    }

    /// <summary>
    /// Enforces stage-gate rules: throws <see cref="StageLockedException"/> when the OE belongs
    /// to a maturity stage whose prerequisite threshold has not been met.
    /// Core is always accessible; Commitment/Achievement/Excellence require the prior stage to pass.
    /// </summary>
    private async Task EnforceStageGateAsync(
        Guid journeyId,
        Guid objectiveElementId,
        CancellationToken cancellationToken)
    {
        var oe = await _db.ObjectiveElements
            .AsNoTracking()
            .Where(o => o.Id == objectiveElementId)
            .Select(o => new { o.Classification })
            .FirstOrDefaultAsync(cancellationToken);

        if (oe == null) return;

        if (oe.Classification == ObjectiveElementClassification.Core)
        {
            return;
        }

        var accessible = await _stageUnlocking.IsStageAccessibleAsync(
            journeyId,
            oe.Classification,
            cancellationToken);

        if (accessible)
        {
            return;
        }

        var eval = await _stageUnlocking.GetStageEvaluationAsync(
            journeyId,
            oe.Classification,
            cancellationToken);

        throw new StageLockedException(
            oe.Classification,
            eval?.ReadinessPercent ?? 0,
            eval?.UnlockReadinessPercent ?? 0,
            eval?.BlockingReasons ?? Array.Empty<string>());
    }

    private static ObjectiveJourneyStateDto DefaultState(
        ComplianceJourney journey,
        Guid journeyId,
        Guid objectiveElementId) =>
        new()
        {
            Id = null,
            JourneyId = journeyId,
            ObjectiveElementId = objectiveElementId,
            HospitalId = journey.HospitalId,
            Status = ObjectiveJourneyStatusDto.Draft,
            LastUpdatedAt = DateTime.UtcNow,
            CompletedAt = null,
            RowVersion = string.Empty,
            Payload = EmptyWizardPayload()
        };

    private static OeWizardPersistedState EmptyWizardPayload() =>
        new()
        {
            StepIndex = 0,
            UnderstandAck = false,
            ExecuteReady = false,
            EvidenceAck = false,
            EvalSubmitted = false,
            EvalScore = 3,
            EvalTone = "compliant",
            EvalJustification = null,
            NeedsImprove = false,
            ImproveReady = false,
            CompleteAck = false,
            AuditChecklistChecked = null,
            SavedAt = null
        };

    private async Task<ObjectiveJourney?> FindWizardRowAsync(
        Guid journeyId,
        Guid objectiveElementId,
        Guid expectedTenantId,
        bool asNoTracking,
        CancellationToken cancellationToken)
    {
        IQueryable<ObjectiveJourney> query = _db.ObjectiveJourneys
            .IgnoreQueryFilters()
            .Where(o => o.ComplianceJourneyId == journeyId && o.ObjectiveElementId == objectiveElementId);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        var entity = await query.FirstOrDefaultAsync(cancellationToken);
        if (entity != null && entity.TenantId != expectedTenantId)
        {
            throw new UnauthorizedAccessException("Access denied to wizard state.");
        }

        return entity;
    }

    private async Task<ComplianceJourney> EnsureJourneyAccessAsync(
        Guid journeyId,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var journey = await _journeyRepository.GetByIdAsync(journeyId, cancellationToken)
            ?? throw new InvalidOperationException("Journey not found.");

        if (journey.TenantId != tenantId)
        {
            throw new UnauthorizedAccessException("Access denied to journey.");
        }

        if (_userContext.HospitalId != Guid.Empty && journey.HospitalId != _userContext.HospitalId)
        {
            throw new UnauthorizedAccessException("Hospital scope does not match journey.");
        }

        return journey;
    }


    private static ObjectiveJourneyStatus ComputeDraftStatus(OeWizardPersistedState p)
    {
        var started = p.StepIndex > 0
                      || p.UnderstandAck
                      || p.ExecuteReady
                      || p.EvidenceAck
                      || p.EvalSubmitted
                      || p.CompleteAck
                      || (p.AuditChecklistChecked is { Length: > 0 });
        return started ? ObjectiveJourneyStatus.InProgress : ObjectiveJourneyStatus.Draft;
    }

    private static byte[]? TryDecodeRowVersion(string? base64)
    {
        if (string.IsNullOrWhiteSpace(base64))
        {
            return null;
        }

        try
        {
            return Convert.FromBase64String(base64.Trim());
        }
        catch
        {
            return null;
        }
    }

    private static OeWizardPersistedState DeserializePayload(string json)
    {
        return JsonSerializer.Deserialize<OeWizardPersistedState>(json, JsonOptions)
               ?? throw new InvalidOperationException("Invalid wizard payload JSON.");
    }

    private static ObjectiveJourneyStateDto Map(ObjectiveJourney row, OeWizardPersistedState payload) =>
        new()
        {
            Id = row.Id,
            JourneyId = row.ComplianceJourneyId,
            ObjectiveElementId = row.ObjectiveElementId,
            HospitalId = row.HospitalId,
            Status = ToDto(row.Status),
            LastUpdatedAt = row.LastUpdatedAt,
            CompletedAt = row.CompletedAt,
            RowVersion = Convert.ToBase64String(row.RowVersion),
            Payload = payload
        };

    private static ObjectiveJourneyStatusDto ToDto(ObjectiveJourneyStatus s) => s switch
    {
        ObjectiveJourneyStatus.Draft => ObjectiveJourneyStatusDto.Draft,
        ObjectiveJourneyStatus.InProgress => ObjectiveJourneyStatusDto.InProgress,
        ObjectiveJourneyStatus.Completed => ObjectiveJourneyStatusDto.Completed,
        _ => ObjectiveJourneyStatusDto.Draft
    };
}
