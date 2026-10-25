using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.TaskExecution;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Application.Services;

public sealed class TaskExecutionService : ITaskExecutionService
{
    private readonly IComplianceTaskAppService _taskAppService;
    private readonly ITaskQueryService _taskQuery;
    private readonly ComplianceCommandService _commandService;
    private readonly IEvidenceRegistrationService _evidenceRegistration;
    private readonly AuditLogService _auditLog;
    private readonly IUserContextService _userContext;
    private readonly ComplianceDbContext _db;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDeficiencyTaskLifecycleService _deficiencyLifecycle;
    private readonly IObligationProjectionService _obligationProjection;
    private readonly IComplianceEventEmitter _eventEmitter;

    public TaskExecutionService(
        IComplianceTaskAppService taskAppService,
        ITaskQueryService taskQuery,
        ComplianceCommandService commandService,
        IEvidenceRegistrationService evidenceRegistration,
        AuditLogService auditLog,
        IUserContextService userContext,
        ComplianceDbContext db,
        IUnitOfWork unitOfWork,
        IDeficiencyTaskLifecycleService deficiencyLifecycle,
        IObligationProjectionService obligationProjection,
        IComplianceEventEmitter eventEmitter)
    {
        _taskAppService = taskAppService;
        _taskQuery = taskQuery;
        _commandService = commandService;
        _evidenceRegistration = evidenceRegistration;
        _auditLog = auditLog;
        _userContext = userContext;
        _db = db;
        _unitOfWork = unitOfWork;
        _deficiencyLifecycle = deficiencyLifecycle;
        _obligationProjection = obligationProjection;
        _eventEmitter = eventEmitter;
    }

    public async Task<TaskDetailResponseDto> CreateAsync(
        Guid frameworkId,
        CreateTaskExecutionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var create = new CreateComplianceTaskRequest
        {
            ObjectiveElementId = request.ObjectiveElementId,
            ComplianceStandardId = request.ComplianceStandardId,
            AssignedToUserId = request.AssignedToUserId,
            DepartmentId = request.DepartmentId,
            DepartmentName = request.DepartmentName,
            DueDate = request.DueDate,
            Title = request.Title,
            Description = request.Description,
            Criticality = request.Criticality,
            Priority = request.Priority,
            ProgramId = request.ProgramId,
            Category = request.Category,
            TaskKind = request.TaskKind,
            BlockedByTaskId = request.BlockedByTaskId
        };

        var created = await _taskAppService.CreateAsync(create, cancellationToken);
        await _auditLog.LogEntityCreatedAsync(
            ComplianceTaskAudit.EntityType,
            created.Id,
            _userContext.UserId,
            new { frameworkId },
            cancellationToken);

        var detail = await _taskQuery.GetDetailAsync(created.Id, frameworkId, cancellationToken);
        return detail ?? throw new InvalidOperationException("Task was created but could not be loaded.");
    }

    public async Task<TaskDetailResponseDto?> PatchAsync(
        Guid frameworkId,
        Guid taskId,
        PatchTaskExecutionRequestDto request,
        byte[]? ifMatchRowVersion,
        CancellationToken cancellationToken = default)
    {
        var exists = await _taskQuery.ResolveTaskFrameworkAsync(taskId, frameworkId, cancellationToken);
        if (exists == null)
        {
            return null;
        }

        var patch = new PatchComplianceTaskRequest
        {
            AssignedToUserId = request.AssignedToUserId,
            DepartmentId = request.DepartmentId,
            DepartmentName = request.DepartmentName,
            DueDate = request.DueDate,
            Title = request.Title,
            Description = request.Description,
            Criticality = request.Criticality,
            Priority = request.Priority,
            Status = request.Status,
            Category = request.Category,
            TaskKind = request.TaskKind,
            BlockedByTaskId = request.BlockedByTaskId
        };

        try
        {
            var before = await _db.ComplianceTasks
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

            var updated = await _taskAppService.PatchAsync(taskId, patch, ifMatchRowVersion, cancellationToken);
            if (updated == null)
            {
                return null;
            }

            await _auditLog.LogEntityUpdatedAsync(
                ComplianceTaskAudit.EntityType,
                taskId,
                _userContext.UserId,
                patch,
                cancellationToken);

            var after = await _db.ComplianceTasks
                .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

            if (after != null)
            {
                await _deficiencyLifecycle.SyncFromTaskAsync(after, before?.Status, cancellationToken);

                if (patch.AssignedToUserId != null)
                {
                    var isReassign = before != null
                        && !string.IsNullOrWhiteSpace(before.AssignedToUserId)
                        && !string.Equals(before.AssignedToUserId, after.AssignedToUserId, StringComparison.OrdinalIgnoreCase);

                    if (isReassign)
                    {
                        await _db.ComplianceTaskOwnershipHistories.AddAsync(
                            new ComplianceTaskOwnershipHistory
                            {
                                Id = Guid.NewGuid(),
                                TaskId = taskId,
                                HospitalId = after.HospitalId,
                                PreviousAssigneeUserId = before!.AssignedToUserId,
                                NewAssigneeUserId = after.AssignedToUserId,
                                ChangedByUserId = _userContext.UserId,
                                Reason = "patch",
                                ChangedAt = DateTime.UtcNow,
                                CreatedAt = DateTime.UtcNow,
                                RowVersion = new byte[8]
                            },
                            cancellationToken);
                        after.OverdueEscalatedAt = null;
                    }

                    await _eventEmitter.EmitAsync(
                        isReassign
                            ? Application.Events.ComplianceEventType.TaskReassigned
                            : Application.Events.ComplianceEventType.TaskAssigned,
                        taskId,
                        nameof(ComplianceTask),
                        _userContext.OrganizationId,
                        _userContext.UserId,
                        new { after.ObjectiveElementId, after.SourceDeficiencyId, previousOwner = before?.AssignedToUserId },
                        cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }

                var trigger = before?.Status == ComplianceTaskStatus.Completed
                              && after.Status != ComplianceTaskStatus.Completed
                    ? ObligationProjectionTriggers.TaskReopened
                    : ObligationProjectionTriggers.TaskAssigned;

                await _obligationProjection.ProjectByObjectiveElementAsync(
                    after.HospitalId,
                    after.ObjectiveElementId,
                    trigger,
                    cancellationToken);
            }

            return await _taskQuery.GetDetailAsync(taskId, frameworkId, cancellationToken);
        }
        catch (InvalidOperationException ex) when (string.Equals(ex.Message, "CONCURRENCY_CONFLICT", StringComparison.Ordinal))
        {
            throw;
        }
    }

    public async Task CompleteAsync(
        Guid frameworkId,
        Guid taskId,
        byte[]? ifMatchRowVersion,
        CancellationToken cancellationToken = default)
    {
        var exists = await _taskQuery.ResolveTaskFrameworkAsync(taskId, frameworkId, cancellationToken);
        if (exists == null)
        {
            throw new InvalidOperationException("Task not found.");
        }

        var hospitalId = _userContext.HospitalId;
        var task = await _db.ComplianceTasks
            .FirstOrDefaultAsync(t => t.Id == taskId && t.HospitalId == hospitalId, cancellationToken)
            ?? throw new InvalidOperationException("Task not found.");

        if (ifMatchRowVersion != null && !RowVersionCodec.Matches(task.RowVersion, ifMatchRowVersion))
        {
            throw new InvalidOperationException("CONCURRENCY_CONFLICT");
        }

        EnsureAssigneeCanComplete(task);

        ComplianceTask? blocker = null;
        if (task.BlockedByTaskId is { } blockerId && blockerId != Guid.Empty)
        {
            blocker = await _db.ComplianceTasks.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == blockerId && t.HospitalId == hospitalId, cancellationToken);
            var blockReason = TaskDependencyGate.GetBlockReason(task, blocker);
            if (blockReason != null)
            {
                throw new InvalidOperationException(blockReason);
            }
        }

        await _commandService.CompleteTaskAsync(hospitalId, exists.ResolvedFrameworkId, taskId, cancellationToken);

        var completedTask = await _db.ComplianceTasks.FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);
        if (completedTask != null)
        {
            await _deficiencyLifecycle.SyncFromTaskAsync(completedTask, ComplianceTaskStatus.InProgress, cancellationToken);
        }

        await _auditLog.LogEntityUpdatedAsync(
            ComplianceTaskAudit.EntityType,
            taskId,
            _userContext.UserId,
            new { action = "Completed" },
            cancellationToken);
    }

    public async Task<TaskDetailResponseDto?> ReassignAsync(
        Guid frameworkId,
        Guid taskId,
        ReassignTaskRequestDto request,
        byte[]? ifMatchRowVersion,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.AssignedToUserId))
        {
            throw new ArgumentException("AssignedToUserId is required.", nameof(request));
        }

        var exists = await _taskQuery.ResolveTaskFrameworkAsync(taskId, frameworkId, cancellationToken);
        if (exists == null)
        {
            return null;
        }

        var hospitalId = _userContext.HospitalId;
        var task = await _db.ComplianceTasks
            .FirstOrDefaultAsync(t => t.Id == taskId && t.HospitalId == hospitalId, cancellationToken);
        if (task == null)
        {
            return null;
        }

        if (ifMatchRowVersion != null && !RowVersionCodec.Matches(task.RowVersion, ifMatchRowVersion))
        {
            throw new InvalidOperationException("CONCURRENCY_CONFLICT");
        }

        if (task.Status == ComplianceTaskStatus.Completed)
        {
            throw new InvalidOperationException("Completed tasks cannot be reassigned.");
        }

        var previous = task.AssignedToUserId;
        var next = request.AssignedToUserId.Trim();
        if (string.Equals(previous, next, StringComparison.OrdinalIgnoreCase))
        {
            return await _taskQuery.GetDetailAsync(taskId, frameworkId, cancellationToken);
        }

        task.AssignedToUserId = next;
        // Clear overdue escalation stamp so a newly assigned owner gets a fresh signal if still overdue.
        task.OverdueEscalatedAt = null;

        await _db.ComplianceTaskOwnershipHistories.AddAsync(
            new ComplianceTaskOwnershipHistory
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                HospitalId = hospitalId,
                PreviousAssigneeUserId = previous,
                NewAssigneeUserId = next,
                ChangedByUserId = _userContext.UserId,
                Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim(),
                ChangedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                RowVersion = new byte[8]
            },
            cancellationToken);

        await _eventEmitter.EmitAsync(
            Application.Events.ComplianceEventType.TaskReassigned,
            taskId,
            nameof(ComplianceTask),
            _userContext.OrganizationId,
            _userContext.UserId,
            new
            {
                task.ObjectiveElementId,
                task.SourceDeficiencyId,
                previousOwner = previous,
                newOwner = next,
                reason = request.Reason
            },
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLog.LogEntityUpdatedAsync(
            ComplianceTaskAudit.EntityType,
            taskId,
            _userContext.UserId,
            new { action = "Reassigned", previous, next, request.Reason },
            cancellationToken);

        await _deficiencyLifecycle.SyncFromTaskAsync(task, task.Status, cancellationToken);
        await _obligationProjection.ProjectByObjectiveElementAsync(
            task.HospitalId,
            task.ObjectiveElementId,
            ObligationProjectionTriggers.TaskAssigned,
            cancellationToken);

        return await _taskQuery.GetDetailAsync(taskId, frameworkId, cancellationToken);
    }

    public async Task<TaskCommentDto> AddCommentAsync(
        Guid frameworkId,
        Guid taskId,
        AddTaskCommentRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Body))
        {
            throw new ArgumentException("Body is required.", nameof(request));
        }

        var ctx = await _taskQuery.ResolveTaskFrameworkAsync(taskId, frameworkId, cancellationToken)
            ?? throw new InvalidOperationException("Task not found.");

        var comment = new ComplianceTaskComment
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            HospitalId = _userContext.HospitalId,
            FrameworkId = ctx.ResolvedFrameworkId,
            AuthorUserId = _userContext.UserId,
            Body = request.Body.Trim(),
            CreatedAt = DateTime.UtcNow,
            RowVersion = new byte[8]
        };

        await _db.ComplianceTaskComments.AddAsync(comment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new TaskCommentDto
        {
            Id = comment.Id,
            TaskId = comment.TaskId,
            AuthorUserId = comment.AuthorUserId,
            Body = comment.Body,
            CreatedAt = comment.CreatedAt
        };
    }

    public async Task<Guid> UploadEvidenceAsync(
        Guid frameworkId,
        Guid taskId,
        HttpRequest httpRequest,
        CancellationToken cancellationToken = default)
    {
        var ctx = await _taskQuery.ResolveTaskFrameworkAsync(taskId, frameworkId, cancellationToken)
            ?? throw new InvalidOperationException("Task not found.");

        if (!httpRequest.HasFormContentType)
        {
            throw new InvalidOperationException("Multipart form required.");
        }

        var form = await httpRequest.ReadFormAsync(cancellationToken);
        var file = ResolveUploadedFile(form);
        var evidenceType = form["evidenceType"].ToString();
        if (string.IsNullOrWhiteSpace(evidenceType))
        {
            evidenceType = form["EvidenceType"].ToString();
        }

        var name = form["name"].ToString();
        if (string.IsNullOrEmpty(name))
        {
            name = form["Name"].ToString();
        }

        var title = form["title"].ToString();
        if (string.IsNullOrEmpty(title))
        {
            title = form["Title"].ToString();
        }

        var metadata = form["metadata"].ToString();
        if (string.IsNullOrEmpty(metadata))
        {
            metadata = form["Metadata"].ToString();
        }

        if (file is null || file.Length == 0)
        {
            throw new InvalidOperationException("A non-empty file is required.");
        }

        if (string.IsNullOrWhiteSpace(evidenceType))
        {
            throw new InvalidOperationException("evidenceType is required.");
        }

        await using var stream = file.OpenReadStream();
        var result = await _evidenceRegistration.RegisterFromUploadAsync(
            new EvidenceRegistrationUploadRequest
            {
                Context = EvidenceRegistrationContext.ForTask(
                    taskId,
                    _userContext.OrganizationId,
                    _userContext.UserId),
                FileStream = stream,
                FileName = file.FileName,
                ContentType = file.ContentType,
                EvidenceType = evidenceType.Trim(),
                DisplayName = string.IsNullOrWhiteSpace(name) ? null : name,
                Title = string.IsNullOrWhiteSpace(title) ? null : title,
                Metadata = string.IsNullOrWhiteSpace(metadata) ? null : metadata
            },
            cancellationToken);

        return result.EvidenceId;
    }

    private static IFormFile? ResolveUploadedFile(IFormCollection form)
    {
        var primary = form.Files["file"];
        if (primary is { Length: > 0 })
        {
            return primary;
        }

        foreach (var key in new[] { "upload", "evidence", "files" })
        {
            var f = form.Files[key];
            if (f is { Length: > 0 })
            {
                return f;
            }
        }

        return form.Files.FirstOrDefault(f => f.Length > 0);
    }

    /// <summary>Only the assignee may complete a task when an assignee is set.</summary>
    private void EnsureAssigneeCanComplete(ComplianceTask task)
    {
        if (string.IsNullOrWhiteSpace(task.AssignedToUserId))
        {
            return;
        }

        var userId = _userContext.UserId;
        if (userId == Guid.Empty)
        {
            throw new InvalidOperationException("Only the assigned user can complete this task.");
        }

        if (!string.Equals(task.AssignedToUserId.Trim(), userId.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only the assigned user can complete this task.");
        }
    }
}
