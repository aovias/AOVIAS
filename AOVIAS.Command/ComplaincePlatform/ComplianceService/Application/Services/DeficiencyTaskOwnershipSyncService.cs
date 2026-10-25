using ComplianceService.Application.Contracts;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ComplianceService.Application.Services;

/// <summary>Sprint 9B Phase 0 — primary remediation task is ownership authority.</summary>
public sealed class DeficiencyTaskOwnershipSyncService : IDeficiencyTaskOwnershipSyncService
{
    public const string SystemAssignee = "deficiency-engine";

    private readonly ComplianceDbContext _db;
    private readonly IDeficiencyRepository _deficiencyRepository;
    private readonly IServiceProvider _serviceProvider;
    private readonly IDeficiencyAuditTrailService _auditTrail;
    private readonly IComplianceEventEmitter _eventEmitter;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeficiencyTaskOwnershipSyncService> _logger;

    public DeficiencyTaskOwnershipSyncService(
        ComplianceDbContext db,
        IDeficiencyRepository deficiencyRepository,
        IServiceProvider serviceProvider,
        IDeficiencyAuditTrailService auditTrail,
        IComplianceEventEmitter eventEmitter,
        IUnitOfWork unitOfWork,
        ILogger<DeficiencyTaskOwnershipSyncService> logger)
    {
        _db = db;
        _deficiencyRepository = deficiencyRepository;
        _serviceProvider = serviceProvider;
        _auditTrail = auditTrail;
        _eventEmitter = eventEmitter;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ComplianceTask?> GetPrimaryRemediationTaskAsync(Guid deficiencyId, CancellationToken ct = default)
    {
        return await _db.ComplianceTasks
            .Where(t => t.SourceDeficiencyId == deficiencyId && t.Status != ComplianceTaskStatus.Completed)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task SyncDeficiencyOwnerFromTaskAsync(
        ComplianceTask task,
        string? updatedBy = null,
        CancellationToken ct = default)
    {
        if (task.SourceDeficiencyId is not { } deficiencyId || deficiencyId == Guid.Empty)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(task.AssignedToUserId) || task.AssignedToUserId == SystemAssignee)
        {
            return;
        }

        var deficiency = await _deficiencyRepository.GetByIdAsync(deficiencyId, ct);
        if (deficiency == null || deficiency.Status is DeficiencyStatus.Closed or DeficiencyStatus.Cancelled)
        {
            return;
        }

        var primary = await GetPrimaryRemediationTaskAsync(deficiencyId, ct);
        if (primary == null || primary.Id != task.Id)
        {
            return;
        }

        var previousOwner = deficiency.AssignedTo;
        if (string.Equals(previousOwner, task.AssignedToUserId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        deficiency.Assign(task.AssignedToUserId, task.DueDate, updatedBy ?? "task-ownership-sync");
        if (!string.IsNullOrWhiteSpace(task.DepartmentName))
        {
            deficiency.UpdateOwnershipMetadata(task.DepartmentName, task.DueDate, updatedBy);
        }

        await _auditTrail.LogAsync(
            deficiencyId,
            string.IsNullOrWhiteSpace(previousOwner) ? "Assigned" : "OwnerChanged",
            null,
            updatedBy ?? "System",
            null,
            ct);

        await _eventEmitter.EmitAsync(
            Application.Events.ComplianceEventType.DeficiencyOwnerChanged,
            deficiencyId,
            nameof(ComplianceDeficiency),
            deficiency.OrganizationId,
            ParseUserId(updatedBy),
            new { previousOwner, newOwner = task.AssignedToUserId, taskId = task.Id },
            ct);

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogDebug(
            "OwnershipSync: Task {TaskId} → deficiency {DeficiencyId} owner '{Owner}'.",
            task.Id, deficiencyId, task.AssignedToUserId);
    }

    public async Task SyncTaskOwnerFromDeficiencyAsync(
        ComplianceDeficiency deficiency,
        string? updatedBy = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(deficiency.AssignedTo))
        {
            return;
        }

        var taskId = await RemediationTasks.EnsureRemediationTaskAsync(deficiency.Id, ct, runAutoRouting: false);
        if (!taskId.HasValue)
        {
            return;
        }

        var task = await _db.ComplianceTasks.FirstOrDefaultAsync(t => t.Id == taskId.Value, ct);
        if (task == null)
        {
            return;
        }

        var previousOwner = task.AssignedToUserId;
        task.AssignedToUserId = deficiency.AssignedTo.Trim();
        if (deficiency.DueDate.HasValue)
        {
            task.DueDate = deficiency.DueDate;
        }

        if (!string.IsNullOrWhiteSpace(deficiency.Department))
        {
            task.DepartmentName = deficiency.Department;
        }

        if (!string.Equals(previousOwner, task.AssignedToUserId, StringComparison.OrdinalIgnoreCase))
        {
            await _eventEmitter.EmitAsync(
                Application.Events.ComplianceEventType.TaskReassigned,
                task.Id,
                nameof(ComplianceTask),
                deficiency.OrganizationId,
                ParseUserId(updatedBy),
                new { previousOwner, newOwner = task.AssignedToUserId, deficiencyId = deficiency.Id },
                ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task SetPrimaryOwnerAsync(
        Guid deficiencyId,
        string ownerUserId,
        DateTime? dueDate = null,
        string? departmentName = null,
        Guid? departmentId = null,
        string? updatedBy = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(ownerUserId) || ownerUserId == SystemAssignee)
        {
            return;
        }

        var taskId = await RemediationTasks.EnsureRemediationTaskAsync(deficiencyId, ct, runAutoRouting: false);
        if (!taskId.HasValue)
        {
            return;
        }

        var task = await _db.ComplianceTasks.FirstOrDefaultAsync(t => t.Id == taskId.Value, ct)
            ?? throw new InvalidOperationException($"Primary task {taskId} not found.");

        var previousOwner = task.AssignedToUserId;
        task.AssignedToUserId = ownerUserId.Trim();
        if (dueDate.HasValue)
        {
            task.DueDate = dueDate;
        }

        if (departmentId.HasValue)
        {
            task.DepartmentId = departmentId;
        }

        if (!string.IsNullOrWhiteSpace(departmentName))
        {
            task.DepartmentName = departmentName;
        }

        await _unitOfWork.SaveChangesAsync(ct);
        await SyncDeficiencyOwnerFromTaskAsync(task, updatedBy, ct);

        if (!string.Equals(previousOwner, ownerUserId, StringComparison.OrdinalIgnoreCase))
        {
            var deficiency = await _deficiencyRepository.GetByIdAsync(deficiencyId, ct);
            await _eventEmitter.EmitAsync(
                Application.Events.ComplianceEventType.TaskAssigned,
                task.Id,
                nameof(ComplianceTask),
                deficiency?.OrganizationId ?? Guid.Empty,
                ParseUserId(updatedBy),
                new { deficiencyId, autoRouted = true },
                ct);
        }
    }

    private IDeficiencyRemediationTaskService RemediationTasks =>
        _serviceProvider.GetRequiredService<IDeficiencyRemediationTaskService>();

    private static Guid? ParseUserId(string? value) =>
        Guid.TryParse(value, out var id) ? id : null;
}
