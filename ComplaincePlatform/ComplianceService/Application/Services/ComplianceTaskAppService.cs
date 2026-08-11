using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.Events;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ComplianceService.Application.Services;

public sealed class ComplianceTaskAppService : IComplianceTaskAppService
{
    private readonly ComplianceDbContext _db;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserContextService _userContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IComplianceEventEmitter _eventEmitter;
    private readonly ILogger<ComplianceTaskAppService> _logger;

    public ComplianceTaskAppService(
        ComplianceDbContext db,
        IUnitOfWork unitOfWork,
        IUserContextService userContext,
        IHttpContextAccessor httpContextAccessor,
        IComplianceEventEmitter eventEmitter,
        ILogger<ComplianceTaskAppService> logger)
    {
        _db = db;
        _unitOfWork = unitOfWork;
        _userContext = userContext;
        _httpContextAccessor = httpContextAccessor;
        _eventEmitter = eventEmitter;
        _logger = logger;
    }

    public async Task<ComplianceTaskDetailDto> CreateAsync(CreateComplianceTaskRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ObjectiveElementId == Guid.Empty)
        {
            throw new ArgumentException("ObjectiveElementId is required.", nameof(request));
        }

        if (request.ComplianceStandardId == Guid.Empty)
        {
            throw new ArgumentException("ComplianceStandardId is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.AssignedToUserId))
        {
            throw new ArgumentException("AssignedToUserId is required.", nameof(request));
        }

        if (request.DepartmentId == null || request.DepartmentId == Guid.Empty)
        {
            throw new ArgumentException("DepartmentId is required for execution tasks.", nameof(request));
        }

        var hospitalId = _userContext.HospitalId;

        var oe = await _db.ObjectiveElements
            .Include(o => o.ComplianceStandard!)
            .ThenInclude(s => s!.Chapter)
            .FirstOrDefaultAsync(o => o.Id == request.ObjectiveElementId, cancellationToken)
            ?? throw new InvalidOperationException("Objective element was not found.");

        if (oe.ComplianceStandardId != request.ComplianceStandardId)
        {
            throw new InvalidOperationException("Objective element does not belong to the specified compliance standard.");
        }

        if (!oe.IsApplicable)
        {
            throw new InvalidOperationException("Objective element is not applicable and cannot be tasked.");
        }

        var exists = await _db.ComplianceTasks
            .AnyAsync(
                t => t.HospitalId == hospitalId
                     && t.ObjectiveElementId == request.ObjectiveElementId
                     && t.SourceDeficiencyId == null,
                cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException(
                "A compliance task already exists for this hospital and objective element.");
        }

        var chapter = oe.ComplianceStandard!.Chapter;
        var weight = oe.Weight > 0 ? oe.Weight : (chapter.Weight > 0 ? chapter.Weight : 1.0m);

        var task = new ComplianceTask
        {
            Id = Guid.NewGuid(),
            HospitalId = hospitalId,
            ComplianceStandardId = request.ComplianceStandardId,
            ObjectiveElementId = request.ObjectiveElementId,
            ProgramId = request.ProgramId,
            DepartmentId = request.DepartmentId,
            DepartmentName = string.IsNullOrWhiteSpace(request.DepartmentName) ? null : request.DepartmentName.Trim(),
            AssignedToUserId = request.AssignedToUserId.Trim(),
            DueDate = request.DueDate,
            Status = ComplianceTaskStatus.Pending,
            Title = string.IsNullOrWhiteSpace(request.Title) ? oe.Title : request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Criticality = request.Criticality,
            Priority = request.Priority ?? ComplianceTaskPriority.Medium,
            Category = request.Category ?? TaskCategory.Compliance,
            TaskKind = request.TaskKind ?? TaskKind.Standard,
            BlockedByTaskId = request.BlockedByTaskId is { } bid && bid != Guid.Empty ? bid : null,
            Weight = weight,
            CreatedAt = DateTime.UtcNow,
            RowVersion = new byte[8]
        };

        await _db.ComplianceTasks.AddAsync(task, cancellationToken);
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _eventEmitter.EmitAsync(
                ComplianceEventType.TaskCreated,
                task.Id,
                nameof(ComplianceTask),
                _userContext.OrganizationId,
                _userContext.UserId,
                new
                {
                    task.HospitalId,
                    task.ObjectiveElementId,
                    task.DepartmentId,
                    task.Status,
                    task.DueDate
                },
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsDuplicateHospitalObjective(ex))
        {
            _logger.LogWarning(ex, "Duplicate compliance task for hospital {HospitalId} and OE {ObjectiveElementId}", hospitalId, request.ObjectiveElementId);
            throw new InvalidOperationException(
                "A compliance task already exists for this hospital and objective element.");
        }

        return (await GetByIdAsync(task.Id, cancellationToken))!;
    }

    public async Task<IReadOnlyList<ComplianceTaskListItemDto>> ListAsync(
        Guid? frameworkId,
        CancellationToken cancellationToken,
        TaskCategory? category = null,
        TaskKind? taskKind = null)
    {
        var hospitalId = _userContext.HospitalId;

        var query =
            from t in _db.ComplianceTasks.AsNoTracking()
            join oe in _db.ObjectiveElements.AsNoTracking() on t.ObjectiveElementId equals oe.Id
            join s in _db.ComplianceStandards.AsNoTracking() on t.ComplianceStandardId equals s.Id
            join ch in _db.ComplianceChapters.AsNoTracking() on s.ChapterId equals ch.Id
            where t.HospitalId == hospitalId
                  && (frameworkId == null || frameworkId == Guid.Empty || ch.FrameworkId == frameworkId)
                  && (category == null || t.Category == category)
                  && (taskKind == null || t.TaskKind == taskKind)
            select new { t, oe, s };

        var rows = await query
            .OrderBy(x => x.s.Code)
            .ThenBy(x => x.oe.Code)
            .ToListAsync(cancellationToken);

        return rows.ConvertAll(x => new ComplianceTaskListItemDto
        {
            Id = x.t.Id,
            HospitalId = x.t.HospitalId,
            ProgramId = x.t.ProgramId,
            ComplianceStandardId = x.t.ComplianceStandardId,
            ObjectiveElementId = x.t.ObjectiveElementId,
            DepartmentId = x.t.DepartmentId,
            DepartmentName = x.t.DepartmentName,
            AssignedToUserId = x.t.AssignedToUserId,
            AssignedToDisplayName = ResolveAssigneeDisplayName(x.t.AssignedToUserId),
            DueDate = x.t.DueDate,
            Status = x.t.Status,
            Title = x.t.Title,
            Description = x.t.Description,
            Criticality = x.t.Criticality,
            Priority = x.t.Priority,
            Category = x.t.Category,
            TaskKind = x.t.TaskKind,
            BlockedByTaskId = x.t.BlockedByTaskId,
            Weight = x.t.Weight,
            ObjectiveElementCode = x.oe.Code,
            ObjectiveElementName = x.oe.Title,
            StandardCode = x.s.Code,
            CreatedAt = x.t.CreatedAt
        });
    }

    public async Task<ComplianceTaskDetailDto?> GetByIdAsync(Guid taskId, CancellationToken cancellationToken)
    {
        if (taskId == Guid.Empty)
        {
            return null;
        }

        var hospitalId = _userContext.HospitalId;

        var row = await (
            from t in _db.ComplianceTasks.AsNoTracking()
            join oe in _db.ObjectiveElements.AsNoTracking() on t.ObjectiveElementId equals oe.Id
            join s in _db.ComplianceStandards.AsNoTracking() on t.ComplianceStandardId equals s.Id
            where t.Id == taskId && t.HospitalId == hospitalId
            select new { t, oe, s }).FirstOrDefaultAsync(cancellationToken);

        if (row == null)
        {
            return null;
        }

        return new ComplianceTaskDetailDto
        {
            Id = row.t.Id,
            HospitalId = row.t.HospitalId,
            ProgramId = row.t.ProgramId,
            ComplianceStandardId = row.t.ComplianceStandardId,
            ObjectiveElementId = row.t.ObjectiveElementId,
            DepartmentId = row.t.DepartmentId,
            DepartmentName = row.t.DepartmentName,
            AssignedToUserId = row.t.AssignedToUserId,
            AssignedToDisplayName = ResolveAssigneeDisplayName(row.t.AssignedToUserId),
            DueDate = row.t.DueDate,
            Status = row.t.Status,
            Title = row.t.Title,
            Description = row.t.Description,
            Criticality = row.t.Criticality,
            Priority = row.t.Priority,
            Category = row.t.Category,
            TaskKind = row.t.TaskKind,
            BlockedByTaskId = row.t.BlockedByTaskId,
            Weight = row.t.Weight,
            ObjectiveElementCode = row.oe.Code,
            ObjectiveElementName = row.oe.Title,
            StandardCode = row.s.Code,
            CreatedAt = row.t.CreatedAt,
            MaturityState = row.t.MaturityState
        };
    }

    public Task<ComplianceTaskDetailDto?> PatchAsync(Guid taskId, PatchComplianceTaskRequest request, CancellationToken cancellationToken) =>
        PatchAsync(taskId, request, ifMatchRowVersion: null, cancellationToken);

    public async Task<ComplianceTaskDetailDto?> PatchAsync(Guid taskId, PatchComplianceTaskRequest request, byte[]? ifMatchRowVersion, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (taskId == Guid.Empty)
        {
            return null;
        }

        if (request.Status == ComplianceTaskStatus.Completed)
        {
            throw new InvalidOperationException(
                "Status cannot be set to Completed via this operation. Use the complete-task endpoint.");
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
            throw new InvalidOperationException("Completed tasks cannot be modified.");
        }

        if (request.AssignedToUserId != null)
        {
            if (string.IsNullOrWhiteSpace(request.AssignedToUserId))
            {
                throw new ArgumentException("AssignedToUserId cannot be empty.", nameof(request));
            }

            task.AssignedToUserId = request.AssignedToUserId.Trim();
        }

        if (request.DepartmentId != null)
        {
            if (request.DepartmentId == Guid.Empty)
            {
                throw new ArgumentException("DepartmentId cannot be empty when provided.", nameof(request));
            }

            task.DepartmentId = request.DepartmentId;
        }

        if (request.DepartmentName != null)
        {
            task.DepartmentName = string.IsNullOrWhiteSpace(request.DepartmentName) ? null : request.DepartmentName.Trim();
        }

        if (request.DueDate != null)
        {
            task.DueDate = request.DueDate;
        }

        if (request.Title != null)
        {
            task.Title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim();
        }

        if (request.Description != null)
        {
            task.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        }

        if (request.Criticality.HasValue)
        {
            task.Criticality = request.Criticality.Value;
        }

        if (request.Priority.HasValue)
        {
            task.Priority = request.Priority.Value;
        }

        if (request.Category.HasValue)
        {
            task.Category = request.Category.Value;
        }

        if (request.TaskKind.HasValue)
        {
            task.TaskKind = request.TaskKind.Value;
        }

        if (request.BlockedByTaskId.HasValue)
        {
            task.BlockedByTaskId = request.BlockedByTaskId.Value == Guid.Empty
                ? null
                : request.BlockedByTaskId.Value;
        }

        if (request.Status.HasValue)
        {
            if (request.Status.Value == ComplianceTaskStatus.Completed)
            {
                throw new InvalidOperationException(
                    "Status cannot be set to Completed via this operation. Use the complete-task endpoint.");
            }

            task.Status = request.Status.Value;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(taskId, cancellationToken);
    }

    private string? ResolveAssigneeDisplayName(string assignedToUserId)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        if (string.IsNullOrEmpty(sub) || !string.Equals(sub, assignedToUserId, StringComparison.Ordinal))
        {
            return null;
        }

        return user.FindFirstValue("name")
               ?? user.FindFirstValue(ClaimTypes.Name)
               ?? user.FindFirstValue("preferred_username")
               ?? user.FindFirstValue(ClaimTypes.Email);
    }

    private static bool IsDuplicateHospitalObjective(DbUpdateException ex)
    {
        for (var e = ex.InnerException; e != null; e = e.InnerException)
        {
            if (e is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
            {
                return true;
            }
        }

        var msg = ex.InnerException?.Message ?? ex.Message;
        return msg.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
               || msg.Contains("duplicate key", StringComparison.OrdinalIgnoreCase);
    }
}
