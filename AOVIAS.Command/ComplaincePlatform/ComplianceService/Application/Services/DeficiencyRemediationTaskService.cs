using ComplianceService.Application.Contracts;
using ComplianceService.Application.Events;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ComplianceService.Application.Services;

/// <summary>
/// Idempotent remediation task provisioning for identified deficiencies.
/// </summary>
public sealed class DeficiencyRemediationTaskService : IDeficiencyRemediationTaskService
{
    private const string SystemAssignee = "deficiency-engine";

    private readonly ComplianceDbContext _db;
    private readonly IDeficiencyRepository _deficiencyRepository;
    private readonly IDeficiencyTaskLifecycleService _lifecycleService;
    private readonly IAssignmentRoutingService _routingService;
    private readonly IComplianceEventEmitter _eventEmitter;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeficiencyRemediationTaskService> _logger;

    public DeficiencyRemediationTaskService(
        ComplianceDbContext db,
        IDeficiencyRepository deficiencyRepository,
        IDeficiencyTaskLifecycleService lifecycleService,
        IAssignmentRoutingService routingService,
        IComplianceEventEmitter eventEmitter,
        IUnitOfWork unitOfWork,
        ILogger<DeficiencyRemediationTaskService> logger)
    {
        _db = db;
        _deficiencyRepository = deficiencyRepository;
        _lifecycleService = lifecycleService;
        _routingService = routingService;
        _eventEmitter = eventEmitter;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Guid?> EnsureRemediationTaskAsync(
        Guid deficiencyId,
        CancellationToken cancellationToken = default,
        bool runAutoRouting = true)
    {
        if (deficiencyId == Guid.Empty)
        {
            return null;
        }

        var deficiency = await _deficiencyRepository.GetByIdAsync(deficiencyId, cancellationToken);
        if (deficiency == null)
        {
            _logger.LogWarning("DeficiencyRemediation: Deficiency {DeficiencyId} not found.", deficiencyId);
            return null;
        }

        if (deficiency.Status == DeficiencyStatus.Closed)
        {
            return null;
        }

        var existingLinked = await _db.ComplianceTasks
            .FirstOrDefaultAsync(
                t => t.SourceDeficiencyId == deficiencyId
                     && t.Status != ComplianceTaskStatus.Completed,
                cancellationToken);

        if (existingLinked != null)
        {
            DeficiencyRemediationTaskComposer.ApplyMetadata(existingLinked, deficiency);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            if (runAutoRouting)
            {
                await _routingService.TryAutoAssignDeficiencyAsync(deficiencyId, forceAutoAssign: false, ct: cancellationToken);
            }

            return existingLinked.Id;
        }

        var openOeTask = await _db.ComplianceTasks
            .FirstOrDefaultAsync(
                t => t.HospitalId == deficiency.HospitalId
                     && t.ObjectiveElementId == deficiency.ObjectiveElementId
                     && t.Status != ComplianceTaskStatus.Completed
                     && t.SourceDeficiencyId == null,
                cancellationToken);

        if (openOeTask != null)
        {
            openOeTask.SourceDeficiencyId = deficiencyId;
            DeficiencyRemediationTaskComposer.ApplyMetadata(openOeTask, deficiency);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _lifecycleService.SyncFromTaskAsync(openOeTask, null, cancellationToken);
            if (runAutoRouting)
            {
                await _routingService.TryAutoAssignDeficiencyAsync(deficiencyId, forceAutoAssign: false, ct: cancellationToken);
            }

            _logger.LogInformation(
                "DeficiencyRemediation: Linked existing task {TaskId} to deficiency {DeficiencyId}.",
                openOeTask.Id,
                deficiencyId);

            return openOeTask.Id;
        }

        var oe = await _db.ObjectiveElements
            .Include(o => o.ComplianceStandard!)
            .ThenInclude(s => s!.Chapter)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == deficiency.ObjectiveElementId, cancellationToken);

        if (oe == null || !oe.IsApplicable)
        {
            _logger.LogWarning(
                "DeficiencyRemediation: Objective element {OeId} not found or not applicable.",
                deficiency.ObjectiveElementId);
            return null;
        }

        var chapter = oe.ComplianceStandard!.Chapter;
        var weight = oe.Weight > 0 ? oe.Weight : (chapter.Weight > 0 ? chapter.Weight : 1.0m);
        var assignee = !string.IsNullOrWhiteSpace(deficiency.AssignedTo)
            ? deficiency.AssignedTo.Trim()
            : SystemAssignee;

        var task = new ComplianceTask
        {
            Id = Guid.NewGuid(),
            HospitalId = deficiency.HospitalId,
            ComplianceStandardId = oe.ComplianceStandardId,
            ObjectiveElementId = oe.Id,
            SourceDeficiencyId = deficiencyId,
            AssignedToUserId = assignee,
            DueDate = deficiency.DueDate ?? DateTime.UtcNow.Date.AddDays(
                deficiency.Severity switch
                {
                    DeficiencySeverity.Critical => 7,
                    DeficiencySeverity.Major    => 14,
                    DeficiencySeverity.Moderate => 21,
                    _                           => 30
                }),
            Status = ComplianceTaskStatus.Pending,
            Title = DeficiencyRemediationTaskComposer.BuildTitle(deficiency),
            Description = DeficiencyRemediationTaskComposer.BuildDescription(deficiency),
            MaturityStage = deficiency.MaturityLevel,
            Criticality = DeficiencyRemediationTaskComposer.MapSeverityToCriticality(deficiency.Severity),
            Priority = DeficiencyRemediationTaskComposer.MapSeverityToPriority(deficiency.Severity),
            Weight = weight,
            CreatedAt = DateTime.UtcNow,
            RowVersion = new byte[8]
        };

        await _db.ComplianceTasks.AddAsync(task, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _eventEmitter.EmitAsync(
            ComplianceEventType.TaskCreated,
            task.Id,
            nameof(ComplianceTask),
            deficiency.OrganizationId,
            null,
            new
            {
                task.HospitalId,
                task.ObjectiveElementId,
                task.SourceDeficiencyId,
                deficiencyId,
                deficiency.Severity,
                deficiency.DeficiencyType,
                AutoCreated = true
            },
            cancellationToken);

        if (deficiency.Severity == DeficiencySeverity.Critical)
        {
            await _eventEmitter.EmitAsync(
                ComplianceEventType.DeficiencyCreated,
                deficiencyId,
                nameof(ComplianceDeficiency),
                deficiency.OrganizationId,
                null,
                new
                {
                    deficiency.HospitalId,
                    deficiency.FrameworkId,
                    deficiency.Severity,
                    taskId = task.Id
                },
                cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _lifecycleService.SyncFromTaskAsync(task, null, cancellationToken);

        if (runAutoRouting)
        {
            await _routingService.TryAutoAssignDeficiencyAsync(deficiencyId, forceAutoAssign: false, ct: cancellationToken);
        }

        _logger.LogInformation(
            "DeficiencyRemediation: Created task {TaskId} for deficiency {DeficiencyId} ({Code}).",
            task.Id,
            deficiencyId,
            deficiency.DeficiencyCode);

        return task.Id;
    }
}

internal static class DeficiencyRemediationTaskComposer
{
    public static string BuildTitle(ComplianceDeficiency deficiency)
    {
        if (deficiency.OriginType == DeficiencyOriginType.AuditFinding
            && !string.IsNullOrWhiteSpace(deficiency.OriginReference))
        {
            return $"Resolve Finding: {deficiency.OriginReference.Trim()}";
        }

        return deficiency.DeficiencyType switch
        {
            DeficiencyType.Evidence       => $"Upload evidence — {TrimTitle(deficiency.Title)}",
            DeficiencyType.Verification   => $"Address verification feedback — {TrimTitle(deficiency.Title)}",
            DeficiencyType.Implementation => $"Implement — {TrimTitle(deficiency.Title)}",
            DeficiencyType.Assessment     => $"Remediate assessment failure — {TrimTitle(deficiency.Title)}",
            DeficiencyType.Maturity       => $"Resolve core blocker — {TrimTitle(deficiency.Title)}",
            _                             => TrimTitle(deficiency.Title)
        };
    }

    public static string BuildDescription(ComplianceDeficiency deficiency)
    {
        var reason = string.IsNullOrWhiteSpace(deficiency.Description)
            ? deficiency.Title
            : deficiency.Description.Trim();

        return $"Remediation task for deficiency {deficiency.DeficiencyCode}. {reason}";
    }

    public static void ApplyMetadata(ComplianceTask task, ComplianceDeficiency deficiency)
    {
        task.SourceDeficiencyId ??= deficiency.Id;
        task.Title = BuildTitle(deficiency);
        task.Description = BuildDescription(deficiency);
        task.Criticality = MapSeverityToCriticality(deficiency.Severity);
        task.Priority = MapSeverityToPriority(deficiency.Severity);
        task.MaturityStage = deficiency.MaturityLevel;

        if (deficiency.DueDate.HasValue)
        {
            task.DueDate = deficiency.DueDate;
        }

        if (!string.IsNullOrWhiteSpace(deficiency.AssignedTo))
        {
            task.AssignedToUserId = deficiency.AssignedTo.Trim();
        }
    }

    public static TaskCriticalityLevel MapSeverityToCriticality(DeficiencySeverity severity) =>
        severity switch
        {
            DeficiencySeverity.Critical => TaskCriticalityLevel.Critical,
            DeficiencySeverity.Major    => TaskCriticalityLevel.High,
            DeficiencySeverity.Moderate => TaskCriticalityLevel.Medium,
            _                           => TaskCriticalityLevel.Low
        };

    public static ComplianceTaskPriority MapSeverityToPriority(DeficiencySeverity severity) =>
        severity switch
        {
            DeficiencySeverity.Critical => ComplianceTaskPriority.Urgent,
            DeficiencySeverity.Major    => ComplianceTaskPriority.High,
            DeficiencySeverity.Moderate => ComplianceTaskPriority.Medium,
            _                           => ComplianceTaskPriority.Low
        };

    private static string TrimTitle(string title) =>
        string.IsNullOrWhiteSpace(title) ? "Compliance remediation" : title.Trim();
}
