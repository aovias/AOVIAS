using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Application.Services;

/// <summary>
/// OE ownership matrix: department + primary owner + due date per hospital OE.
/// </summary>
public sealed class ObjectiveElementOwnershipAppService
{
    private readonly ComplianceDbContext _db;
    private readonly ILogger<ObjectiveElementOwnershipAppService> _logger;
    private readonly IComplianceNotificationPublisher _notifications;

    public ObjectiveElementOwnershipAppService(
        ComplianceDbContext db,
        ILogger<ObjectiveElementOwnershipAppService> logger,
        IComplianceNotificationPublisher notifications)
    {
        _db = db;
        _logger = logger;
        _notifications = notifications;
    }

    public async Task<IReadOnlyList<ObjectiveElementOwnershipMatrixItemDto>> ListMatrixAsync(
        Guid hospitalId,
        Guid? frameworkId = null,
        CancellationToken cancellationToken = default)
    {
        if (hospitalId == Guid.Empty)
        {
            throw new ArgumentException("HospitalId is required.", nameof(hospitalId));
        }

        var oeQuery =
            from oe in _db.ObjectiveElements.AsNoTracking()
            join std in _db.ComplianceStandards.AsNoTracking() on oe.ComplianceStandardId equals std.Id
            join ch in _db.ComplianceChapters.AsNoTracking() on std.ChapterId equals ch.Id
            select new { oe, FrameworkId = ch.FrameworkId };

        if (frameworkId.HasValue && frameworkId.Value != Guid.Empty)
        {
            oeQuery = oeQuery.Where(x => x.FrameworkId == frameworkId.Value);
        }

        var objectiveElements = await oeQuery
            .OrderBy(x => x.oe.Code)
            .ToListAsync(cancellationToken);

        var oeIds = objectiveElements.Select(x => x.oe.Id).ToList();

        var ownerships = await _db.ObjectiveElementOwnerships
            .AsNoTracking()
            .Include(o => o.Department)
            .Where(o => o.HospitalId == hospitalId && oeIds.Contains(o.ObjectiveElementId))
            .ToListAsync(cancellationToken);

        var ownershipByOe = ownerships
            .GroupBy(o => o.ObjectiveElementId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(o => o.Status == ObjectiveElementOwnershipStatus.Active)
                    .ThenByDescending(o => o.UpdatedAt)
                    .First());

        var journeyStatuses = await _db.ComplianceJourneys
            .AsNoTracking()
            .Where(j => j.HospitalId == hospitalId)
            .Select(j => new { j.FrameworkId, Status = j.Status.ToString() })
            .ToListAsync(cancellationToken);

        var journeyByFramework = journeyStatuses
            .GroupBy(j => j.FrameworkId)
            .ToDictionary(g => g.Key, g => g.First().Status);

        var result = new List<ObjectiveElementOwnershipMatrixItemDto>(objectiveElements.Count);
        foreach (var row in objectiveElements)
        {
            ownershipByOe.TryGetValue(row.oe.Id, out var ownership);
            journeyByFramework.TryGetValue(row.FrameworkId, out var journeyStatus);

            result.Add(new ObjectiveElementOwnershipMatrixItemDto
            {
                OwnershipId = ownership?.Id,
                ObjectiveElementId = row.oe.Id,
                ObjectiveElementCode = row.oe.Code ?? string.Empty,
                ObjectiveElementTitle = row.oe.Title ?? string.Empty,
                FrameworkId = row.FrameworkId,
                DepartmentId = ownership?.DepartmentId,
                DepartmentName = ownership?.Department?.Name,
                PrimaryOwnerUserId = ownership?.PrimaryOwnerUserId,
                DueDate = ownership?.DueDate,
                Status = ownership?.Status.ToString() ?? "Unassigned",
                JourneyStatus = journeyStatus
            });
        }

        return result;
    }

    public async Task<ObjectiveElementOwnershipMatrixItemDto> UpsertAsync(
        Guid hospitalId,
        UpsertObjectiveElementOwnershipRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (hospitalId == Guid.Empty)
        {
            throw new ArgumentException("HospitalId is required.", nameof(hospitalId));
        }

        if (request.ObjectiveElementId == Guid.Empty || request.DepartmentId == Guid.Empty
            || request.FrameworkId == Guid.Empty || request.OrganizationId == Guid.Empty)
        {
            throw new ArgumentException("OrganizationId, FrameworkId, ObjectiveElementId, and DepartmentId are required.");
        }

        if (string.IsNullOrWhiteSpace(request.PrimaryOwnerUserId))
        {
            throw new ArgumentException("PrimaryOwnerUserId is required.");
        }

        var oeExists = await (
            from oe in _db.ObjectiveElements.AsNoTracking()
            join std in _db.ComplianceStandards.AsNoTracking() on oe.ComplianceStandardId equals std.Id
            join ch in _db.ComplianceChapters.AsNoTracking() on std.ChapterId equals ch.Id
            where oe.Id == request.ObjectiveElementId && ch.FrameworkId == request.FrameworkId
            select oe.Id).AnyAsync(cancellationToken);

        if (!oeExists)
        {
            throw new InvalidOperationException(
                "Objective element not found for the specified framework.");
        }

        var department = await _db.Departments
            .FirstOrDefaultAsync(
                d => d.Id == request.DepartmentId && d.OrganizationId == request.OrganizationId,
                cancellationToken)
            ?? throw new InvalidOperationException("Department not found for organization.");

        if (!department.IsActive)
        {
            throw new InvalidOperationException(
                "Cannot assign ownership to an inactive department.");
        }

        var existing = await _db.ObjectiveElementOwnerships
            .FirstOrDefaultAsync(
                o => o.HospitalId == hospitalId && o.ObjectiveElementId == request.ObjectiveElementId,
                cancellationToken);

        var now = DateTime.UtcNow;
        var status = ObjectiveElementOwnershipStatus.Active;
        if (!string.IsNullOrWhiteSpace(request.Status)
            && Enum.TryParse<ObjectiveElementOwnershipStatus>(request.Status, ignoreCase: true, out var parsed))
        {
            status = parsed;
        }

        if (existing != null)
        {
            existing.OrganizationId = request.OrganizationId;
            existing.FrameworkId = request.FrameworkId;
            existing.DepartmentId = request.DepartmentId;
            existing.PrimaryOwnerUserId = request.PrimaryOwnerUserId.Trim();
            existing.DueDate = request.DueDate;
            existing.Status = status;
            existing.UpdatedAt = now;
        }
        else
        {
            existing = new ObjectiveElementOwnership
            {
                Id = Guid.NewGuid(),
                OrganizationId = request.OrganizationId,
                HospitalId = hospitalId,
                FrameworkId = request.FrameworkId,
                ObjectiveElementId = request.ObjectiveElementId,
                DepartmentId = request.DepartmentId,
                PrimaryOwnerUserId = request.PrimaryOwnerUserId.Trim(),
                DueDate = request.DueDate,
                Status = status,
                CreatedAt = now,
                UpdatedAt = now
            };
            await _db.ObjectiveElementOwnerships.AddAsync(existing, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Upserted OE ownership HospitalId={HospitalId} OE={ObjectiveElementId} Dept={DepartmentId}",
            hospitalId, request.ObjectiveElementId, request.DepartmentId);

        var oeMeta = await _db.ObjectiveElements.AsNoTracking()
            .Where(o => o.Id == existing.ObjectiveElementId)
            .Select(o => new { o.Code, o.Title })
            .FirstOrDefaultAsync(cancellationToken);

        await _notifications.PublishAsync(new ComplianceNotificationMessage
        {
            EventType = "oe.ownership.assigned",
            Title = "OE ownership assigned",
            Body =
                $"Objective element {(oeMeta?.Code ?? existing.ObjectiveElementId.ToString())} assigned to {department.Name}",
            HospitalId = hospitalId,
            OrganizationId = request.OrganizationId,
            RecipientUserId = existing.PrimaryOwnerUserId,
            Data = new Dictionary<string, string>
            {
                ["objectiveElementId"] = existing.ObjectiveElementId.ToString(),
                ["departmentId"] = existing.DepartmentId.ToString(),
                ["frameworkId"] = existing.FrameworkId.ToString(),
            },
        }, cancellationToken);

        return new ObjectiveElementOwnershipMatrixItemDto
        {
            OwnershipId = existing.Id,
            ObjectiveElementId = existing.ObjectiveElementId,
            ObjectiveElementCode = oeMeta?.Code ?? string.Empty,
            ObjectiveElementTitle = oeMeta?.Title ?? string.Empty,
            FrameworkId = existing.FrameworkId,
            DepartmentId = existing.DepartmentId,
            DepartmentName = department.Name,
            PrimaryOwnerUserId = existing.PrimaryOwnerUserId,
            DueDate = existing.DueDate,
            Status = existing.Status.ToString()
        };
    }

    public async Task ClearAsync(
        Guid hospitalId,
        Guid objectiveElementId,
        CancellationToken cancellationToken = default)
    {
        if (hospitalId == Guid.Empty)
        {
            throw new ArgumentException("HospitalId is required.", nameof(hospitalId));
        }

        if (objectiveElementId == Guid.Empty)
        {
            throw new ArgumentException("ObjectiveElementId is required.", nameof(objectiveElementId));
        }

        var existing = await _db.ObjectiveElementOwnerships
            .FirstOrDefaultAsync(
                o => o.HospitalId == hospitalId && o.ObjectiveElementId == objectiveElementId,
                cancellationToken);

        if (existing == null)
        {
            return;
        }

        _db.ObjectiveElementOwnerships.Remove(existing);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Cleared OE ownership HospitalId={HospitalId} OE={ObjectiveElementId}",
            hospitalId, objectiveElementId);
    }
}
