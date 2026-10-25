using OrganizationService.Application.Contracts;
using OrganizationService.Application.DTOs;
using Shared.Infrastructure.Domain;
using Shared.Infrastructure.Services;

namespace OrganizationService.Application.Services;

public class HospitalAppService
{
    private readonly IHospitalRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IPlatformIdGenerator _idGen;
    private readonly IMasterDepartmentRepository _masterDepartmentRepository;
    private readonly IHospitalMasterDepartmentRepository _hospitalMasterDepartmentRepository;

    public HospitalAppService(
        IHospitalRepository repo,
        IUnitOfWork uow,
        IPlatformIdGenerator idGen,
        IMasterDepartmentRepository masterDepartmentRepository,
        IHospitalMasterDepartmentRepository hospitalMasterDepartmentRepository)
    {
        _repo = repo;
        _uow = uow;
        _idGen = idGen;
        _masterDepartmentRepository = masterDepartmentRepository;
        _hospitalMasterDepartmentRepository = hospitalMasterDepartmentRepository;
    }

    public async Task<Hospital> CreateAsync(CreateHospitalDto dto)
    {
        // Check if hospital with same code already exists in the organization
        var existing = await _repo.GetByCodeAsync(dto.Code, dto.OrganizationId);
        if (existing != null)
        {
            throw new InvalidOperationException($"Hospital with code '{dto.Code}' already exists in this organization.");
        }

        var hospital = new Hospital
        {
            Id = Guid.NewGuid(),
            OrganizationId = dto.OrganizationId,
            Name = dto.Name,
            Code = dto.Code,
            Address = dto.Address,
            Type = dto.Type,
            BedCapacity = dto.BedCapacity,
            CountryCode = dto.CountryCode,
            StateCode = dto.StateCode,
            IsOnboarded = dto.IsOnboarded ?? false,
            NABHLevel = dto.NABHLevel,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(hospital);
        await _uow.SaveChangesAsync();
        return hospital;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var hospital = await _repo.GetAsync(id);
        if (hospital == null)
        {
            return false;
        }

        _repo.Remove(hospital);
        await _uow.SaveChangesAsync();
        return true;
    }

    public async Task<Hospital?> GetAsync(Guid id)
    {
        return await _repo.GetAsync(id);
    }

    public async Task<List<Hospital>> ListByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _repo.ListByOrganizationAsync(organizationId, cancellationToken);
    }

    public async Task<Hospital?> UpdateAsync(Guid id, UpdateHospitalDto dto)
    {
        var hospital = await _repo.GetAsync(id);
        if (hospital == null)
        {
            return null;
        }

        hospital.Name = dto.Name;
        hospital.Code = dto.Code;
        hospital.Address = dto.Address;
        hospital.Type = dto.Type;
        hospital.BedCapacity = dto.BedCapacity;
        hospital.CountryCode = dto.CountryCode;
        hospital.StateCode = dto.StateCode;
        hospital.IsOnboarded = dto.IsOnboarded;
        hospital.NABHLevel = dto.NABHLevel;

        _repo.Update(hospital);
        await _uow.SaveChangesAsync();
        return hospital;
    }

    /// <summary>
    /// Applies onboarding wizard profile: hospital fields, bed capacity range, wizard version, and master department selection.
    /// </summary>
    public async Task<Hospital?> ApplyWizardProfileAsync(Guid hospitalId, HospitalWizardProfileDto dto, CancellationToken cancellationToken = default)
    {
        var hospital = await _repo.GetAsync(hospitalId);
        if (hospital == null)
        {
            return null;
        }

        if (hospital.OrganizationId != dto.OrganizationId)
        {
            throw new InvalidOperationException("Hospital does not belong to the specified organization.");
        }

        var distinctMasterIds = dto.DepartmentMasterIds.Distinct().ToList();
        if (distinctMasterIds.Count > 0)
        {
            var foundCount = await _masterDepartmentRepository.CountExistingIdsAsync(distinctMasterIds, cancellationToken);
            if (foundCount != distinctMasterIds.Count)
            {
                throw new InvalidOperationException("One or more department IDs are not valid master departments.");
            }
        }

        hospital.Type = dto.HospitalType;
        hospital.BedCapacityRange = dto.BedCapacityRange;
        hospital.CountryCode = dto.CountryCode;
        hospital.StateCode = dto.StateCode;
        hospital.IsOnboarded = dto.MarkOnboarded;
        hospital.WizardVersion = dto.WizardVersion;

        await _hospitalMasterDepartmentRepository.ReplaceHospitalSelectionsAsync(hospitalId, distinctMasterIds, cancellationToken);

        _repo.Update(hospital);
        await _uow.SaveChangesAsync();
        return hospital;
    }
}

public class CreateHospitalDto
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Type { get; set; }
    public int? BedCapacity { get; set; }
    public string? CountryCode { get; set; }
    public string? StateCode { get; set; }
    public bool? IsOnboarded { get; set; }
    public string? NABHLevel { get; set; }
}

public class UpdateHospitalDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Type { get; set; }
    public int? BedCapacity { get; set; }
    public string? CountryCode { get; set; }
    public string? StateCode { get; set; }
    public bool IsOnboarded { get; set; }
    public string? NABHLevel { get; set; }
}
