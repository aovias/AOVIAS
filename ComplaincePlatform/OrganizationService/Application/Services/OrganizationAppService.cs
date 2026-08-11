using Microsoft.EntityFrameworkCore;
using OrganizationService.Application.Contracts;
using OrganizationService.Application.DTOs;
using OrganizationService.Domain.Entities;
using OrganizationService.Infrastructure.Persistence;
using Shared.Infrastructure.Domain;
using Shared.Infrastructure.DTO;
using Shared.Infrastructure.Services;

namespace OrganizationService.Application.Services
{
    public class OrganizationAppService
    {
        private readonly IOrganizationRepository _repo;
        private readonly IUnitOfWork _uow;
        private readonly IPlatformIdGenerator _idGen;
        private readonly OrganizationDbContext _dbContext;

        public OrganizationAppService(
            IOrganizationRepository repo,
            IUnitOfWork uow,
            IPlatformIdGenerator idGen,
            OrganizationDbContext dbContext)
        {
            _repo = repo;
            _uow = uow;
            _idGen = idGen;
            _dbContext = dbContext;
        }

        public async Task<Organization> CreateAsync(CreateOrganizationDto dto, Guid userId)
        {
            var platformId = await _idGen.GenerateAsync("ORG");
            var org = new Organization
            {
                Id = Guid.NewGuid(),
                PlatformId = platformId,
                OrganizationCode = OrganizationCodeHelper.DeriveFromPlatformId(platformId),
                Name = dto.Name,
                Type = dto.Type,
                Address = dto.Address,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(org);
            _dbContext.OrganizationSettings.Add(new OrganizationSettings
            {
                OrganizationId = org.Id,
                TimeZoneId = OrganizationSettingsDefaults.DefaultTimeZoneId,
                LanguageCode = OrganizationSettingsDefaults.DefaultLanguageCode,
                WorkingDaysJson = OrganizationSettingsDefaults.DefaultWorkingDaysJson,
                WorkingHoursStart = OrganizationSettingsDefaults.DefaultWorkingHoursStart,
                WorkingHoursEnd = OrganizationSettingsDefaults.DefaultWorkingHoursEnd,
                RowVersion = Array.Empty<byte>()
            });
            await _uow.SaveChangesAsync();
            return org;
        }

        public async Task<Organization> CreateInternalAsync(CreateOrganizationDto dto)
        {
            // Check if organization with same name already exists
            var existing = await _repo.Query()
                .FirstOrDefaultAsync(o => o.Name == dto.Name);
            if (existing != null)
            {
                throw new InvalidOperationException($"Organization with name '{dto.Name}' already exists.");
            }

            var platformId = await _idGen.GenerateAsync("ORG");
            var org = new Organization
            {
                Id = Guid.NewGuid(),
                PlatformId = platformId,
                OrganizationCode = OrganizationCodeHelper.DeriveFromPlatformId(platformId),
                Name = dto.Name,
                Type = dto.Type,
                Address = dto.Address,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(org);
            _dbContext.OrganizationSettings.Add(new OrganizationSettings
            {
                OrganizationId = org.Id,
                TimeZoneId = OrganizationSettingsDefaults.DefaultTimeZoneId,
                LanguageCode = OrganizationSettingsDefaults.DefaultLanguageCode,
                WorkingDaysJson = OrganizationSettingsDefaults.DefaultWorkingDaysJson,
                WorkingHoursStart = OrganizationSettingsDefaults.DefaultWorkingHoursStart,
                WorkingHoursEnd = OrganizationSettingsDefaults.DefaultWorkingHoursEnd,
                RowVersion = Array.Empty<byte>()
            });
            await _uow.SaveChangesAsync();
            return org;
        }

        /// <summary>
        /// Idempotent per-org defaults for signup (settings). Safe to call twice.
        /// Framework enrollment remains in the onboarding wizard.
        /// </summary>
        public async Task<EnsureOrganizationDefaultsResult> EnsureDefaultsAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            var orgExists = await _dbContext.Organizations.AnyAsync(o => o.Id == organizationId, cancellationToken);
            if (!orgExists)
            {
                throw new InvalidOperationException($"Organization {organizationId} was not found.");
            }

            var settingsCreated = false;
            var settings = await _dbContext.OrganizationSettings
                .FirstOrDefaultAsync(s => s.OrganizationId == organizationId, cancellationToken);
            if (settings == null)
            {
                _dbContext.OrganizationSettings.Add(new OrganizationSettings
                {
                    OrganizationId = organizationId,
                    TimeZoneId = OrganizationSettingsDefaults.DefaultTimeZoneId,
                    LanguageCode = OrganizationSettingsDefaults.DefaultLanguageCode,
                    WorkingDaysJson = OrganizationSettingsDefaults.DefaultWorkingDaysJson,
                    WorkingHoursStart = OrganizationSettingsDefaults.DefaultWorkingHoursStart,
                    WorkingHoursEnd = OrganizationSettingsDefaults.DefaultWorkingHoursEnd,
                    RowVersion = Array.Empty<byte>()
                });
                settingsCreated = true;
                await _uow.SaveChangesAsync();
            }

            return new EnsureOrganizationDefaultsResult(organizationId, settingsCreated, AlreadyHadSettings: !settingsCreated);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var org = await _repo.GetAsync(id);
            if (org == null)
            {
                return false;
            }

            _repo.Remove(org);
            await _uow.SaveChangesAsync();
            return true;
        }

        public async Task<OrganizationDetailResponseDto?> GetOrganizationDetailAsync(Guid organizationId, CancellationToken cancellationToken = default)
        {
            var organization = await _dbContext.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId, cancellationToken);

            if (organization == null)
            {
                return null;
            }

            var hospitals = await _dbContext.Hospitals
                .Where(h => h.OrganizationId == organizationId)
                .Select(h => new HospitalDetailResponseDto
                {
                    Id = h.Id,
                    Name = h.Name,
                    Code = h.Code,
                    Address = h.Address,
                    Type = h.Type,
                    BedCapacity = h.BedCapacity,
                    CountryCode = h.CountryCode,
                    StateCode = h.StateCode,
                    IsOnboarded = h.IsOnboarded,
                    NABHLevel = h.NABHLevel,
                    CreatedAt = h.CreatedAt
                })
                .ToListAsync(cancellationToken);

            var members = await (
                from m in _dbContext.Members.Where(x => x.OrganizationId == organizationId)
                join md in _dbContext.MasterDepartments on m.DepartmentId equals md.Id into mdJoin
                from md in mdJoin.DefaultIfEmpty()
                select new MemberDetailResponseDto
                {
                    Id = m.Id,
                    UserId = m.UserId,
                    DepartmentId = m.DepartmentId,
                    DepartmentName = md != null ? md.Name : null,
                    Designation = m.Designation,
                    ReportsToUserId = m.ReportsToUserId,
                    Role = m.Role,
                    FirstName = m.FirstName,
                    LastName = m.LastName,
                    Email = m.Email,
                    PhoneNumber = m.PhoneNumber,
                    AlternatePhoneNumber = m.AlternatePhoneNumber,
                    ProfilePicUrl = m.ProfilePicUrl,
                    Preferences = m.Preferences,
                    CreatedAt = m.CreatedAt
                }).ToListAsync(cancellationToken);

            var settings = await _dbContext.OrganizationSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.OrganizationId == organizationId, cancellationToken);

            return new OrganizationDetailResponseDto
            {
                TenantId = organization.Id,
                OrganizationName = organization.Name,
                OrganizationCode = string.IsNullOrWhiteSpace(organization.OrganizationCode)
                    ? organization.PlatformId
                    : organization.OrganizationCode,
                OrganizationType = organization.Type,
                LogoUrl = settings?.LogoStoragePath == null
                    ? null
                    : $"/api/v1/organizations/{organization.Id}/settings/logo",
                TimeZoneId = settings?.TimeZoneId ?? OrganizationSettingsDefaults.DefaultTimeZoneId,
                LanguageCode = settings?.LanguageCode ?? OrganizationSettingsDefaults.DefaultLanguageCode,
                Hospitals = hospitals,
                Members = members,
                CreatedOn = organization.CreatedAt
            };
        }
    }

}
