using ComplianceService.Application.DTOs;
using ComplianceService.Application.Contracts;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Governance;

namespace ComplianceService.Application.Services;

public sealed class DepartmentOwnershipAppService
{
    private readonly ComplianceDbContext _db;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGovernanceClient? _governanceClient;
    private readonly ILogger<DepartmentOwnershipAppService> _logger;

    public DepartmentOwnershipAppService(
        ComplianceDbContext db,
        IUnitOfWork unitOfWork,
        ILogger<DepartmentOwnershipAppService> logger,
        IGovernanceClient? governanceClient = null)
    {
        _db = db;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _governanceClient = governanceClient;
    }

    public async Task<IReadOnlyList<DepartmentOwnershipDto>> ListAsync(
        Guid organizationId,
        Guid? hospitalId = null,
        CancellationToken ct = default)
    {
        var query = _db.DepartmentOwnerships
            .AsNoTracking()
            .Include(o => o.Department)
            .Where(o => o.OrganizationId == organizationId);

        if (hospitalId.HasValue)
        {
            query = query.Where(o => o.HospitalId == null || o.HospitalId == hospitalId);
        }

        var rows = await query.OrderBy(o => o.Department!.Name).ToListAsync(ct);
        var result = new List<DepartmentOwnershipDto>();
        foreach (var row in rows)
        {
            var dto = Map(row);
            await EnrichFromGovernanceAsync(dto, ct);
            result.Add(dto);
        }
        return result;
    }

    /// <summary>Pilot: resolve department owners from GovernanceService when local row is absent.</summary>
    public async Task<IReadOnlyList<GovernanceOwnerDto>> GetGovernanceOwnersAsync(Guid departmentId, CancellationToken ct = default)
    {
        if (_governanceClient == null) return Array.Empty<GovernanceOwnerDto>();
        try
        {
            var owners = await _governanceClient.GetOwnersAsync(new OwnerLookupRequest
            {
                ResourceType = "Department",
                ResourceId = departmentId,
                ResponsibilityTypeCode = "ProcessOwner"
            }, ct);
            return owners.Owners;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Governance owner lookup failed for department {DepartmentId}", departmentId);
            return Array.Empty<GovernanceOwnerDto>();
        }
    }

    private async Task EnrichFromGovernanceAsync(DepartmentOwnershipDto dto, CancellationToken ct)
    {
        var govOwners = await GetGovernanceOwnersAsync(dto.DepartmentId, ct);
        if (govOwners.Count == 0) return;
        var primary = govOwners.FirstOrDefault(o => o.Priority.Equals("Primary", StringComparison.OrdinalIgnoreCase));
        if (primary != null && string.IsNullOrWhiteSpace(dto.PrimaryOwnerUserId))
            dto.PrimaryOwnerUserId = primary.AssigneeId.ToString();
        dto.GovernanceOwners = govOwners;
    }

    public async Task<DepartmentOwnershipDto> UpsertAsync(
        UpsertDepartmentOwnershipRequestDto request,
        CancellationToken ct = default)
    {
        var dept = await _db.Departments
            .FirstOrDefaultAsync(d => d.Id == request.DepartmentId && d.OrganizationId == request.OrganizationId, ct)
            ?? throw new InvalidOperationException("Department not found for organization.");

        if (!dept.IsActive)
        {
            throw new InvalidOperationException(
                "Cannot assign ownership to an inactive department.");
        }

        var existing = await _db.DepartmentOwnerships
            .FirstOrDefaultAsync(o =>
                o.OrganizationId == request.OrganizationId
                && o.HospitalId == request.HospitalId
                && o.DepartmentId == request.DepartmentId
                && o.Status == DepartmentOwnershipStatus.Active,
                ct);

        if (existing != null)
        {
            existing.PrimaryOwnerUserId = request.PrimaryOwnerUserId.Trim();
            existing.BackupOwnerUserId = string.IsNullOrWhiteSpace(request.BackupOwnerUserId)
                ? null
                : request.BackupOwnerUserId.Trim();
            existing.EffectiveFrom = request.EffectiveFrom ?? existing.EffectiveFrom;
            if (!string.IsNullOrWhiteSpace(request.Status)
                && Enum.TryParse<DepartmentOwnershipStatus>(request.Status, ignoreCase: true, out var status))
            {
                existing.Status = status;
            }
        }
        else
        {
            existing = new DepartmentOwnership
            {
                Id = Guid.NewGuid(),
                OrganizationId = request.OrganizationId,
                HospitalId = request.HospitalId,
                DepartmentId = request.DepartmentId,
                PrimaryOwnerUserId = request.PrimaryOwnerUserId.Trim(),
                BackupOwnerUserId = string.IsNullOrWhiteSpace(request.BackupOwnerUserId)
                    ? null
                    : request.BackupOwnerUserId.Trim(),
                EffectiveFrom = request.EffectiveFrom ?? DateTime.UtcNow,
                Status = DepartmentOwnershipStatus.Active,
                CreatedAt = DateTime.UtcNow,
                RowVersion = new byte[8],
            };
            await _db.DepartmentOwnerships.AddAsync(existing, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        await _db.Entry(existing).Reference(o => o.Department).LoadAsync(ct);
        return Map(existing);
    }

    public async Task<IReadOnlyList<DepartmentLabelMappingDto>> GetLabelMappingsAsync(CancellationToken ct = default)
    {
        if (!await _db.DepartmentLabelMappings.AnyAsync(ct))
        {
            foreach (var (label, code, role) in DepartmentLabelCatalog.Mappings)
            {
                await _db.DepartmentLabelMappings.AddAsync(new DepartmentLabelMapping
                {
                    Id = Guid.NewGuid(),
                    OeDepartmentLabel = label,
                    NormalizedCode = code,
                    DefaultResponsibleRole = role,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    RowVersion = new byte[8],
                }, ct);
            }

            await _unitOfWork.SaveChangesAsync(ct);
        }

        return await _db.DepartmentLabelMappings
            .AsNoTracking()
            .Where(m => m.IsActive)
            .OrderBy(m => m.OeDepartmentLabel)
            .Select(m => new DepartmentLabelMappingDto
            {
                OeDepartmentLabel = m.OeDepartmentLabel,
                NormalizedCode = m.NormalizedCode,
                DefaultResponsibleRole = m.DefaultResponsibleRole,
            })
            .ToListAsync(ct);
    }

    private static DepartmentOwnershipDto Map(DepartmentOwnership o) =>
        new()
        {
            Id = o.Id,
            OrganizationId = o.OrganizationId,
            HospitalId = o.HospitalId,
            DepartmentId = o.DepartmentId,
            DepartmentCode = o.Department?.Code ?? string.Empty,
            DepartmentName = o.Department?.Name ?? string.Empty,
            PrimaryOwnerUserId = o.PrimaryOwnerUserId,
            BackupOwnerUserId = o.BackupOwnerUserId,
            EffectiveFrom = o.EffectiveFrom,
            EffectiveTo = o.EffectiveTo,
            Status = o.Status.ToString(),
        };
}
