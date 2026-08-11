using WorkforceService.Application.Contracts;
using WorkforceService.Application.DTOs;
using WorkforceService.Domain.Aggregates;
using WorkforceService.Domain.Entities;
using WorkforceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.DTO;
using Shared.Infrastructure.Services;
using Shared.Logging;
using Shared.Logging.Audit;

namespace WorkforceService.Application.Services;

public class WorkforceAuditService : IWorkforceAuditService
{
    private readonly WorkforceDbContext _db;
    private readonly IAuditLogger _auditLogger;

    public WorkforceAuditService(WorkforceDbContext db, IAuditLogger auditLogger)
    {
        _db = db;
        _auditLogger = auditLogger;
    }

    public async Task RecordAsync(Guid employmentId, string eventCode, string action, Guid? actorUserId, string? oldValue, string? newValue, string? correlationId, CancellationToken cancellationToken = default)
    {
        _db.WorkforceAuditLogs.Add(new WorkforceAuditLog
        {
            EmploymentId = employmentId,
            ActorUserId = actorUserId,
            EventCode = eventCode,
            Action = action,
            OldValue = oldValue,
            NewValue = newValue,
            CorrelationId = correlationId
        });
        await _db.SaveChangesAsync(cancellationToken);
        _auditLogger.Success(eventCode, action, targetType: "Employment", targetId: employmentId.ToString(), actorId: actorUserId?.ToString());
    }

    public async Task RecordTimelineAsync(Guid employmentId, string eventType, string summary, Guid? actorUserId, string? oldJson, string? newJson, string? correlationId, CancellationToken cancellationToken = default)
    {
        _db.WorkforceTimeline.Add(new WorkforceTimelineEntry
        {
            EmploymentId = employmentId,
            EventType = eventType,
            Summary = summary,
            ActorUserId = actorUserId,
            OldValuesJson = oldJson,
            NewValuesJson = newJson,
            CorrelationId = correlationId
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RecordHistoryAsync(Guid employmentId, string eventType, string? reason, string? oldJson, string? newJson, Guid? actorUserId, CancellationToken cancellationToken = default)
    {
        _db.EmploymentHistories.Add(new EmploymentHistory
        {
            EmploymentId = employmentId,
            EventType = eventType,
            Reason = reason,
            PreviousValueJson = oldJson,
            NewValueJson = newJson,
            ActorUserId = actorUserId
        });
        await _db.SaveChangesAsync(cancellationToken);
    }
}

public class EmploymentAppService : IEmploymentAppService
{
    private readonly WorkforceDbContext _db;
    private readonly IPlatformIdGenerator _idGenerator;
    private readonly IWorkforceAuditService _audit;
    private readonly IPersonServiceClient _personClient;
    private readonly IOrganizationServiceClient _orgClient;
    private readonly IHttpContextAccessor _http;

    public EmploymentAppService(
        WorkforceDbContext db,
        IPlatformIdGenerator idGenerator,
        IWorkforceAuditService audit,
        IPersonServiceClient personClient,
        IOrganizationServiceClient orgClient,
        IHttpContextAccessor http)
    {
        _db = db;
        _idGenerator = idGenerator;
        _audit = audit;
        _personClient = personClient;
        _orgClient = orgClient;
        _http = http;
    }

    public async Task<EmploymentDto> CreateAsync(CreateEmploymentRequest request, CancellationToken cancellationToken = default)
    {
        var person = await _personClient.GetPersonAsync(request.PersonId, cancellationToken)
            ?? throw new InvalidOperationException("Person not found.");

        if (!await _orgClient.ValidateHospitalAsync(request.HospitalId, cancellationToken))
            throw new InvalidOperationException("Hospital not found.");

        var duplicate = await _db.Employments.AnyAsync(e =>
            !e.IsDeleted && e.PersonId == request.PersonId &&
            e.OrganizationId == request.OrganizationId && e.HospitalId == request.HospitalId, cancellationToken);
        if (duplicate)
            throw new InvalidOperationException("Active employment already exists for this person at this organization and hospital.");

        var employment = new Employment
        {
            Id = Guid.NewGuid(),
            EmploymentNumber = await _idGenerator.GenerateAsync("EMP", cancellationToken),
            PersonId = request.PersonId,
            OrganizationId = request.OrganizationId,
            HospitalId = request.HospitalId,
            UserId = request.UserId,
            OrganizationMemberId = request.OrganizationMemberId,
            EmploymentTypeValueId = request.EmploymentTypeValueId,
            EmploymentStatusValueId = request.EmploymentStatusValueId,
            PersonSearchName = person.SearchName,
            JoiningDate = request.JoiningDate ?? DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = request.CreatedByUserId,
            Availability = new AvailabilityProfile()
        };

        _db.Employments.Add(employment);
        _db.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateId = employment.Id,
            OccurredOn = DateTime.UtcNow,
            Type = "EmploymentCreated",
            Payload = $"{{\"employmentId\":\"{employment.Id}\"}}"
        });
        await _db.SaveChangesAsync(cancellationToken);

        var cid = CorrelationId();
        await _audit.RecordAsync(employment.Id, "EMPLOYMENT_CREATED", "CreateEmployment", request.CreatedByUserId, null, employment.EmploymentNumber, cid, cancellationToken);
        await _audit.RecordTimelineAsync(employment.Id, "Created", "Employment created", request.CreatedByUserId, null, employment.EmploymentNumber, cid, cancellationToken);
        await _audit.RecordHistoryAsync(employment.Id, "Joined", null, null, employment.EmploymentNumber, request.CreatedByUserId, cancellationToken);

        return (await GetByIdAsync(employment.Id, cancellationToken))!;
    }

    public async Task<IntegrationUpsertResponse> UpsertFromIntegrationAsync(IntegrationUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var employeeNumber = GetField(request.Fields, "employeeNumber", "EmployeeNumber", "ExternalEmployeeNumber")
            ?? request.ExternalId
            ?? throw new InvalidOperationException("EmployeeNumber is required for employment upsert.");
        var personId = ParseGuid(GetField(request.Fields, "personId", "PersonId"))
            ?? throw new InvalidOperationException("PersonId is required for employment upsert.");
        var hospitalId = ParseGuid(GetField(request.Fields, "hospitalId", "HospitalId"))
            ?? throw new InvalidOperationException("HospitalId is required for employment upsert.");

        var employment = await _db.Employments.FirstOrDefaultAsync(e =>
            !e.IsDeleted &&
            e.OrganizationId == request.OrganizationId &&
            e.EmploymentNumber == employeeNumber, cancellationToken);

        if (employment is null)
        {
            var created = await CreateAsync(new CreateEmploymentRequest
            {
                PersonId = personId,
                OrganizationId = request.OrganizationId,
                HospitalId = hospitalId,
                JoiningDate = ParseDate(GetField(request.Fields, "joiningDate", "JoiningDate"))
            }, cancellationToken);

            var entity = await _db.Employments.FirstAsync(e => e.Id == created.Id, cancellationToken);
            if (!string.Equals(entity.EmploymentNumber, employeeNumber, StringComparison.Ordinal))
            {
                entity.EmploymentNumber = employeeNumber;
                await _db.SaveChangesAsync(cancellationToken);
            }

            return new IntegrationUpsertResponse { EntityId = created.Id, WasCreated = true };
        }

        var person = await _personClient.GetPersonAsync(employment.PersonId, cancellationToken);
        if (person != null) employment.PersonSearchName = person.SearchName;
        employment.PersonId = personId;
        employment.HospitalId = hospitalId;
        employment.JoiningDate = ParseDate(GetField(request.Fields, "joiningDate", "JoiningDate")) ?? employment.JoiningDate;
        employment.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.RecordAsync(employment.Id, "EMPLOYMENT_INTEGRATION_UPSERT", "IntegrationUpsert", null, null, employment.EmploymentNumber, CorrelationId(), cancellationToken);
        return new IntegrationUpsertResponse { EntityId = employment.Id, WasCreated = false };
    }

    public Task<bool> RollbackIntegrationAsync(Guid organizationId, Guid employmentId, CancellationToken cancellationToken = default)
    {
        _ = organizationId;
        return DeleteAsync(employmentId, null, cancellationToken);
    }

    private static string? GetField(Dictionary<string, string?> fields, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (fields.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }
        return null;
    }

    private static Guid? ParseGuid(string? value) =>
        Guid.TryParse(value, out var id) ? id : null;

    private static DateTime? ParseDate(string? value) =>
        DateTime.TryParse(value, out var dt) ? dt : null;

    public async Task<EmploymentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var e = await LoadEmploymentQuery().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
        return e == null ? null : MapToDto(e);
    }

    public async Task<PagedResultDto<EmploymentListItemDto>> ListAsync(int page, int pageSize, Guid? organizationId, Guid? hospitalId, Guid? statusValueId, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var q = _db.Employments.AsNoTracking().Where(e => !e.IsDeleted);
        if (organizationId.HasValue) q = q.Where(e => e.OrganizationId == organizationId);
        if (hospitalId.HasValue) q = q.Where(e => e.HospitalId == hospitalId);
        if (statusValueId.HasValue) q = q.Where(e => e.EmploymentStatusValueId == statusValueId);

        var total = await q.CountAsync(cancellationToken);
        var items = await q.OrderBy(e => e.PersonSearchName).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(e => new EmploymentListItemDto
            {
                Id = e.Id,
                EmploymentNumber = e.EmploymentNumber,
                PersonSearchName = e.PersonSearchName,
                PersonId = e.PersonId,
                OrganizationId = e.OrganizationId,
                HospitalId = e.HospitalId,
                JoiningDate = e.JoiningDate,
                PrimaryPositionName = e.PositionAssignments.Where(p => !p.IsDeleted && p.IsPrimary && (p.EffectiveTo == null || p.EffectiveTo > DateTime.UtcNow))
                    .Select(p => p.Position!.PositionName).FirstOrDefault()
            }).ToListAsync(cancellationToken);

        return new PagedResultDto<EmploymentListItemDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<PagedResultDto<EmploymentListItemDto>> SearchAsync(string query, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var term = query.Trim().ToUpperInvariant();
        var q = _db.Employments.AsNoTracking().Where(e => !e.IsDeleted && (
            e.PersonSearchName.Contains(term) || e.EmploymentNumber.Contains(term)));

        var total = await q.CountAsync(cancellationToken);
        var items = await q.OrderBy(e => e.PersonSearchName).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(e => new EmploymentListItemDto
            {
                Id = e.Id,
                EmploymentNumber = e.EmploymentNumber,
                PersonSearchName = e.PersonSearchName,
                PersonId = e.PersonId,
                OrganizationId = e.OrganizationId,
                HospitalId = e.HospitalId,
                JoiningDate = e.JoiningDate
            }).ToListAsync(cancellationToken);

        return new PagedResultDto<EmploymentListItemDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<EmploymentDto?> UpdateAsync(Guid id, UpdateEmploymentRequest request, CancellationToken cancellationToken = default)
    {
        var employment = await _db.Employments.FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);
        if (employment == null || !employment.CanMutate()) return null;

        var person = await _personClient.GetPersonAsync(employment.PersonId, cancellationToken);
        if (person != null) employment.PersonSearchName = person.SearchName;

        employment.EmploymentTypeValueId = request.EmploymentTypeValueId ?? employment.EmploymentTypeValueId;
        employment.EmploymentStatusValueId = request.EmploymentStatusValueId ?? employment.EmploymentStatusValueId;
        employment.JoiningDate = request.JoiningDate ?? employment.JoiningDate;
        employment.ConfirmationDate = request.ConfirmationDate ?? employment.ConfirmationDate;
        employment.ProbationEndDate = request.ProbationEndDate ?? employment.ProbationEndDate;
        employment.SeparationDate = request.SeparationDate ?? employment.SeparationDate;
        employment.NoticePeriodDays = request.NoticePeriodDays ?? employment.NoticePeriodDays;
        employment.UserId = request.UserId ?? employment.UserId;
        employment.UpdatedAt = DateTime.UtcNow;
        employment.UpdatedByUserId = request.CreatedByUserId;

        await _db.SaveChangesAsync(cancellationToken);
        var cid = CorrelationId();
        await _audit.RecordAsync(id, "EMPLOYMENT_UPDATED", "UpdateEmployment", request.CreatedByUserId, null, null, cid, cancellationToken);
        await _audit.RecordTimelineAsync(id, "Updated", "Employment updated", request.CreatedByUserId, null, null, cid, cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid? actorUserId, CancellationToken cancellationToken = default)
    {
        var employment = await _db.Employments.FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);
        if (employment == null) return false;
        employment.SoftDelete(actorUserId);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.RecordAsync(id, "EMPLOYMENT_DELETED", "DeleteEmployment", actorUserId, null, null, CorrelationId(), cancellationToken);
        return true;
    }

    public async Task<PositionAssignmentDto?> AssignPositionAsync(Guid employmentId, AssignPositionRequest request, CancellationToken cancellationToken = default)
    {
        var employment = await _db.Employments.Include(e => e.PositionAssignments)
            .FirstOrDefaultAsync(e => e.Id == employmentId && !e.IsDeleted, cancellationToken);
        if (employment == null || !employment.CanMutate()) return null;

        var position = await _db.Positions.FirstOrDefaultAsync(p => p.Id == request.PositionId && !p.IsDeleted, cancellationToken);
        if (position == null) throw new InvalidOperationException("Position not found.");

        if (request.IsPrimary)
        {
            foreach (var active in employment.PositionAssignments.Where(a => a.IsPrimary && a.IsActive(request.EffectiveFrom)))
                active.EffectiveTo = request.EffectiveFrom;
        }

        var assignment = new PositionAssignment
        {
            Id = Guid.NewGuid(),
            EmploymentId = employmentId,
            PositionId = request.PositionId,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            IsPrimary = request.IsPrimary,
            CreatedAt = DateTime.UtcNow
        };
        _db.PositionAssignments.Add(assignment);
        _db.OutboxMessages.Add(new OutboxMessage { Id = Guid.NewGuid(), AggregateId = employmentId, OccurredOn = DateTime.UtcNow, Type = "PositionAssigned", Payload = $"{{\"positionId\":\"{request.PositionId}\"}}" });
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.RecordTimelineAsync(employmentId, "PositionAssigned", $"Assigned position {position.PositionName}", request.ActorUserId, null, position.PositionName, CorrelationId(), cancellationToken);
        await _audit.RecordHistoryAsync(employmentId, "PositionChanged", null, null, position.PositionName, request.ActorUserId, cancellationToken);

        return new PositionAssignmentDto
        {
            Id = assignment.Id,
            PositionId = position.Id,
            PositionName = position.PositionName,
            EffectiveFrom = assignment.EffectiveFrom,
            EffectiveTo = assignment.EffectiveTo,
            IsPrimary = assignment.IsPrimary,
            IsActive = assignment.IsActive()
        };
    }

    public async Task<DepartmentAssignmentDto?> AssignDepartmentAsync(Guid employmentId, AssignDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        var employment = await _db.Employments.Include(e => e.DepartmentAssignments)
            .FirstOrDefaultAsync(e => e.Id == employmentId && !e.IsDeleted, cancellationToken);
        if (employment == null || !employment.CanMutate()) return null;

        if (request.IsPrimary)
        {
            foreach (var active in employment.DepartmentAssignments.Where(a => a.IsPrimary && a.IsActive(request.EffectiveFrom)))
                active.EffectiveTo = request.EffectiveFrom;
        }

        var assignment = new DepartmentAssignment
        {
            Id = Guid.NewGuid(),
            EmploymentId = employmentId,
            MasterDepartmentId = request.MasterDepartmentId,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            IsPrimary = request.IsPrimary,
            AllocationPercentage = request.AllocationPercentage,
            IsTemporary = request.IsTemporary,
            CreatedAt = DateTime.UtcNow
        };
        _db.DepartmentAssignments.Add(assignment);
        _db.OutboxMessages.Add(new OutboxMessage { Id = Guid.NewGuid(), AggregateId = employmentId, OccurredOn = DateTime.UtcNow, Type = "DepartmentAssigned", Payload = $"{{\"departmentId\":\"{request.MasterDepartmentId}\"}}" });
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.RecordTimelineAsync(employmentId, "DepartmentAssigned", "Department assignment updated", request.ActorUserId, null, request.MasterDepartmentId.ToString(), CorrelationId(), cancellationToken);
        return new DepartmentAssignmentDto
        {
            Id = assignment.Id,
            MasterDepartmentId = assignment.MasterDepartmentId,
            EffectiveFrom = assignment.EffectiveFrom,
            EffectiveTo = assignment.EffectiveTo,
            IsPrimary = assignment.IsPrimary,
            AllocationPercentage = assignment.AllocationPercentage,
            IsTemporary = assignment.IsTemporary,
            IsActive = assignment.IsActive()
        };
    }

    public async Task<ReportingRelationshipDto?> AssignManagerAsync(Guid employmentId, AssignManagerRequest request, CancellationToken cancellationToken = default)
    {
        if (employmentId == request.ManagerEmploymentId)
            throw new InvalidOperationException("Employment cannot report to itself.");

        var employment = await _db.Employments.FirstOrDefaultAsync(e => e.Id == employmentId && !e.IsDeleted, cancellationToken);
        if (employment == null || !employment.CanMutate()) return null;

        if (!await _db.Employments.AnyAsync(e => e.Id == request.ManagerEmploymentId && !e.IsDeleted, cancellationToken))
            throw new InvalidOperationException("Manager employment not found.");

        if (await WouldCreateReportingCycleAsync(employmentId, request.ManagerEmploymentId, cancellationToken))
            throw new InvalidOperationException("Reporting assignment would create a cycle.");

        var rel = new ReportingRelationship
        {
            Id = Guid.NewGuid(),
            EmploymentId = employmentId,
            ManagerEmploymentId = request.ManagerEmploymentId,
            RelationshipTypeValueId = request.RelationshipTypeValueId,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            CreatedAt = DateTime.UtcNow
        };
        _db.ReportingRelationships.Add(rel);
        _db.OutboxMessages.Add(new OutboxMessage { Id = Guid.NewGuid(), AggregateId = employmentId, OccurredOn = DateTime.UtcNow, Type = "ManagerAssigned", Payload = $"{{\"managerEmploymentId\":\"{request.ManagerEmploymentId}\"}}" });
        await _db.SaveChangesAsync(cancellationToken);

        var manager = await _db.Employments.AsNoTracking().FirstAsync(e => e.Id == request.ManagerEmploymentId, cancellationToken);
        await _audit.RecordTimelineAsync(employmentId, "ManagerAssigned", "Reporting relationship assigned", request.ActorUserId, null, manager.PersonSearchName, CorrelationId(), cancellationToken);

        return new ReportingRelationshipDto
        {
            Id = rel.Id,
            ManagerEmploymentId = rel.ManagerEmploymentId,
            ManagerPersonSearchName = manager.PersonSearchName,
            RelationshipTypeValueId = rel.RelationshipTypeValueId,
            EffectiveFrom = rel.EffectiveFrom,
            EffectiveTo = rel.EffectiveTo,
            IsActive = rel.IsActive()
        };
    }

    public async Task<EmploymentDto?> TransferAsync(Guid employmentId, TransferEmploymentRequest request, CancellationToken cancellationToken = default)
    {
        var employment = await _db.Employments.FirstOrDefaultAsync(e => e.Id == employmentId && !e.IsDeleted, cancellationToken);
        if (employment == null || !employment.CanMutate()) return null;

        _db.Transfers.Add(new TransferRecord
        {
            Id = Guid.NewGuid(),
            EmploymentId = employmentId,
            TransferKind = request.TransferKind,
            TargetOrganizationId = request.TargetOrganizationId,
            TargetHospitalId = request.TargetHospitalId,
            TargetMasterDepartmentId = request.TargetMasterDepartmentId,
            TargetPositionId = request.TargetPositionId,
            EffectiveDate = request.EffectiveDate,
            ReasonValueId = request.ReasonValueId,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        });

        if (request.TargetOrganizationId.HasValue) employment.OrganizationId = request.TargetOrganizationId.Value;
        if (request.TargetHospitalId.HasValue) employment.HospitalId = request.TargetHospitalId.Value;
        employment.UpdatedAt = DateTime.UtcNow;

        if (request.TargetPositionId.HasValue)
            await AssignPositionAsync(employmentId, new AssignPositionRequest { PositionId = request.TargetPositionId.Value, EffectiveFrom = request.EffectiveDate, IsPrimary = true, ActorUserId = request.ActorUserId }, cancellationToken);
        if (request.TargetMasterDepartmentId.HasValue)
            await AssignDepartmentAsync(employmentId, new AssignDepartmentRequest { MasterDepartmentId = request.TargetMasterDepartmentId.Value, EffectiveFrom = request.EffectiveDate, IsPrimary = true, ActorUserId = request.ActorUserId }, cancellationToken);

        _db.OutboxMessages.Add(new OutboxMessage { Id = Guid.NewGuid(), AggregateId = employmentId, OccurredOn = DateTime.UtcNow, Type = "EmploymentTransferred", Payload = "{}" });
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.RecordTimelineAsync(employmentId, "Transferred", $"Transfer: {request.TransferKind}", request.ActorUserId, null, null, CorrelationId(), cancellationToken);
        await _audit.RecordHistoryAsync(employmentId, "Transferred", request.Notes, null, request.TransferKind, request.ActorUserId, cancellationToken);
        return await GetByIdAsync(employmentId, cancellationToken);
    }

    public async Task<EmploymentDto?> PromoteAsync(Guid employmentId, PromoteEmploymentRequest request, CancellationToken cancellationToken = default)
    {
        var employment = await _db.Employments.Include(e => e.PositionAssignments).FirstOrDefaultAsync(e => e.Id == employmentId && !e.IsDeleted, cancellationToken);
        if (employment == null || !employment.CanMutate()) return null;

        var previous = employment.PositionAssignments.FirstOrDefault(p => p.IsPrimary && p.IsActive(request.EffectiveDate))?.PositionId;
        _db.Promotions.Add(new PromotionRecord
        {
            Id = Guid.NewGuid(),
            EmploymentId = employmentId,
            PreviousPositionId = previous,
            NewPositionId = request.NewPositionId,
            EffectiveDate = request.EffectiveDate,
            ReasonValueId = request.ReasonValueId,
            Notes = request.Notes,
            GradeValueId = request.GradeValueId,
            BandValueId = request.BandValueId,
            CreatedAt = DateTime.UtcNow
        });

        await AssignPositionAsync(employmentId, new AssignPositionRequest { PositionId = request.NewPositionId, EffectiveFrom = request.EffectiveDate, IsPrimary = true, ActorUserId = request.ActorUserId }, cancellationToken);
        _db.OutboxMessages.Add(new OutboxMessage { Id = Guid.NewGuid(), AggregateId = employmentId, OccurredOn = DateTime.UtcNow, Type = "EmploymentPromoted", Payload = "{}" });
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.RecordTimelineAsync(employmentId, "Promoted", "Employment promoted", request.ActorUserId, null, null, CorrelationId(), cancellationToken);
        await _audit.RecordHistoryAsync(employmentId, "Promoted", request.Notes, previous?.ToString(), request.NewPositionId.ToString(), request.ActorUserId, cancellationToken);
        return await GetByIdAsync(employmentId, cancellationToken);
    }

    public async Task<IReadOnlyList<EmploymentHistoryDto>> GetHistoryAsync(Guid employmentId, CancellationToken cancellationToken = default)
        => await _db.EmploymentHistories.AsNoTracking().Where(h => h.EmploymentId == employmentId).OrderByDescending(h => h.OccurredAt)
            .Select(h => new EmploymentHistoryDto { Id = h.Id, OccurredAt = h.OccurredAt, EventType = h.EventType, Reason = h.Reason, PreviousValueJson = h.PreviousValueJson, NewValueJson = h.NewValueJson })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<WorkforceTimelineDto>> GetTimelineAsync(Guid employmentId, CancellationToken cancellationToken = default)
        => await _db.WorkforceTimeline.AsNoTracking().Where(t => t.EmploymentId == employmentId).OrderByDescending(t => t.OccurredAt)
            .Select(t => new WorkforceTimelineDto { Id = t.Id, OccurredAt = t.OccurredAt, EventType = t.EventType, Summary = t.Summary })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<LookupValueDto>> GetLookupsAsync(string categoryCode, CancellationToken cancellationToken = default)
        => await _db.WorkforceLookupValues.AsNoTracking().Where(v => v.IsActive && v.Category!.Code == categoryCode).OrderBy(v => v.SortOrder)
            .Select(v => new LookupValueDto { Id = v.Id, Code = v.Code, Name = v.Name, SortOrder = v.SortOrder }).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PositionDto>> GetPositionsAsync(Guid organizationId, CancellationToken cancellationToken = default)
        => await _db.Positions.AsNoTracking().Where(p => p.OrganizationId == organizationId && !p.IsDeleted && p.IsActive)
            .Select(p => new PositionDto { Id = p.Id, OrganizationId = p.OrganizationId, HospitalId = p.HospitalId, PositionCode = p.PositionCode, PositionName = p.PositionName, IsClinical = p.IsClinical, IsAdministrative = p.IsAdministrative, IsLeadership = p.IsLeadership })
            .ToListAsync(cancellationToken);

    public async Task<OrgChartNodeDto?> GetOrgChartAsync(Guid organizationId, Guid? hospitalId, Guid? rootEmploymentId, CancellationToken cancellationToken = default)
    {
        var employments = await _db.Employments.AsNoTracking()
            .Where(e => !e.IsDeleted && e.OrganizationId == organizationId && (!hospitalId.HasValue || e.HospitalId == hospitalId))
            .Include(e => e.PositionAssignments.Where(p => !p.IsDeleted)).ThenInclude(p => p.Position)
            .Include(e => e.ReportingRelationships.Where(r => !r.IsDeleted))
            .ToListAsync(cancellationToken);

        if (employments.Count == 0) return null;

        OrgChartNodeDto Build(Guid id)
        {
            var e = employments.First(x => x.Id == id);
            var primaryPos = e.PositionAssignments.FirstOrDefault(p => p.IsPrimary && p.IsActive())?.Position?.PositionName;
            var reports = employments.Where(x => x.ReportingRelationships.Any(r => r.IsActive() && r.ManagerEmploymentId == id)).Select(x => Build(x.Id)).ToList();
            return new OrgChartNodeDto { EmploymentId = e.Id, EmploymentNumber = e.EmploymentNumber, PersonSearchName = e.PersonSearchName, PositionName = primaryPos, Reports = reports };
        }

        var root = rootEmploymentId ?? employments.FirstOrDefault(e => !employments.Any(x => x.ReportingRelationships.Any(r => r.IsActive() && r.EmploymentId == e.Id)))?.Id
            ?? employments[0].Id;
        return Build(root);
    }

    public async Task<IReadOnlyList<EmploymentListItemDto>> GetManagerTeamAsync(Guid managerEmploymentId, CancellationToken cancellationToken = default)
    {
        var teamIds = await _db.ReportingRelationships.AsNoTracking()
            .Where(r => !r.IsDeleted && r.ManagerEmploymentId == managerEmploymentId && (r.EffectiveTo == null || r.EffectiveTo > DateTime.UtcNow))
            .Select(r => r.EmploymentId).ToListAsync(cancellationToken);

        return await _db.Employments.AsNoTracking().Where(e => teamIds.Contains(e.Id) && !e.IsDeleted)
            .Select(e => new EmploymentListItemDto { Id = e.Id, EmploymentNumber = e.EmploymentNumber, PersonSearchName = e.PersonSearchName, PersonId = e.PersonId, OrganizationId = e.OrganizationId, HospitalId = e.HospitalId, JoiningDate = e.JoiningDate })
            .ToListAsync(cancellationToken);
    }

    private async Task<bool> WouldCreateReportingCycleAsync(Guid employmentId, Guid managerEmploymentId, CancellationToken cancellationToken)
    {
        var visited = new HashSet<Guid> { employmentId };
        var current = managerEmploymentId;
        while (true)
        {
            if (!visited.Add(current)) return true;
            var next = await _db.ReportingRelationships.AsNoTracking()
                .Where(r => !r.IsDeleted && r.EmploymentId == current && (r.EffectiveTo == null || r.EffectiveTo > DateTime.UtcNow))
                .Select(r => r.ManagerEmploymentId).FirstOrDefaultAsync(cancellationToken);
            if (next == Guid.Empty) return false;
            current = next;
        }
    }

    private IQueryable<Employment> LoadEmploymentQuery() =>
        _db.Employments.AsNoTracking()
            .Include(e => e.PositionAssignments.Where(p => !p.IsDeleted)).ThenInclude(p => p.Position)
            .Include(e => e.DepartmentAssignments.Where(d => !d.IsDeleted))
            .Include(e => e.ReportingRelationships.Where(r => !r.IsDeleted))
            .Include(e => e.Contracts.Where(c => !c.IsDeleted))
            .Include(e => e.Availability);

    private static EmploymentDto MapToDto(Employment e) => new()
    {
        Id = e.Id,
        EmploymentNumber = e.EmploymentNumber,
        PersonId = e.PersonId,
        OrganizationId = e.OrganizationId,
        HospitalId = e.HospitalId,
        UserId = e.UserId,
        OrganizationMemberId = e.OrganizationMemberId,
        EmploymentTypeValueId = e.EmploymentTypeValueId,
        EmploymentStatusValueId = e.EmploymentStatusValueId,
        PersonSearchName = e.PersonSearchName,
        JoiningDate = e.JoiningDate,
        ConfirmationDate = e.ConfirmationDate,
        ProbationEndDate = e.ProbationEndDate,
        SeparationDate = e.SeparationDate,
        NoticePeriodDays = e.NoticePeriodDays,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
        PositionAssignments = e.PositionAssignments.Select(p => new PositionAssignmentDto
        {
            Id = p.Id, PositionId = p.PositionId, PositionName = p.Position?.PositionName,
            EffectiveFrom = p.EffectiveFrom, EffectiveTo = p.EffectiveTo, IsPrimary = p.IsPrimary, IsActive = p.IsActive()
        }).ToList(),
        DepartmentAssignments = e.DepartmentAssignments.Select(d => new DepartmentAssignmentDto
        {
            Id = d.Id, MasterDepartmentId = d.MasterDepartmentId, EffectiveFrom = d.EffectiveFrom, EffectiveTo = d.EffectiveTo,
            IsPrimary = d.IsPrimary, AllocationPercentage = d.AllocationPercentage, IsTemporary = d.IsTemporary, IsActive = d.IsActive()
        }).ToList(),
        ReportingRelationships = e.ReportingRelationships.Select(r => new ReportingRelationshipDto
        {
            Id = r.Id, ManagerEmploymentId = r.ManagerEmploymentId, RelationshipTypeValueId = r.RelationshipTypeValueId,
            EffectiveFrom = r.EffectiveFrom, EffectiveTo = r.EffectiveTo, IsActive = r.IsActive()
        }).ToList(),
        Contracts = e.Contracts.Select(c => new EmploymentContractDto { Id = c.Id, ContractTypeValueId = c.ContractTypeValueId, ContractNumber = c.ContractNumber, EffectiveFrom = c.EffectiveFrom, ExpiryDate = c.ExpiryDate, Status = c.Status }).ToList(),
        Availability = e.Availability == null ? null : new AvailabilityProfileDto { WorkingDaysJson = e.Availability.WorkingDaysJson, WorkingHoursJson = e.Availability.WorkingHoursJson, ShiftPlaceholder = e.Availability.ShiftPlaceholder, LeaveStatus = e.Availability.LeaveStatus }
    };

    private string? CorrelationId() =>
        _http.HttpContext?.Request.Headers["X-Correlation-Id"].FirstOrDefault()
        ?? _http.HttpContext?.Request.Headers["X-Correlation-ID"].FirstOrDefault()
        ?? _http.HttpContext?.TraceIdentifier;
}
