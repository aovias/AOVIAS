using EnterpriseDirectoryService.Application.Contracts;
using EnterpriseDirectoryService.Application.Directory.Configuration;
using EnterpriseDirectoryService.Application.Directory.Security;
using EnterpriseDirectoryService.Infrastructure.Clients;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace EnterpriseDirectoryService.Application.Directory.Profiles;

public interface IDirectoryProfileComposer
{
    Task<EnterpriseDirectoryProfileDto?> ComposeAsync(Guid id, GovernedDirectoryContext auth, CancellationToken ct = default);
}

public sealed class DirectoryProfileComposer(
    IPersonSourceClient personClient,
    IWorkforceSourceClient workforceClient,
    IProfessionalSourceClient professionalClient,
    IGovernanceSourceClient governanceClient,
    IMemoryCache cache,
    IOptions<DirectoryOptions> options) : IDirectoryProfileComposer
{
    public async Task<EnterpriseDirectoryProfileDto?> ComposeAsync(Guid id, GovernedDirectoryContext auth, CancellationToken ct = default)
    {
        var cacheKey = $"profile:{id}";
        if (cache.TryGetValue(cacheKey, out EnterpriseDirectoryProfileDto? cached))
            return ApplyMasks(cached!, auth);

        var person = await personClient.GetPersonAsync(id, ct);
        Guid personId = id;
        if (person is null)
        {
            var orgIds = await workforceClient.ListOrganizationIdsAsync(ct);
            foreach (var orgId in orgIds)
            {
                var employment = (await workforceClient.ListEmploymentsAsync(orgId, ct)).FirstOrDefault(e => e.Id == id);
                if (employment is null) continue;
                personId = employment.PersonId;
                person = await personClient.GetPersonAsync(personId, ct);
                break;
            }
        }
        if (person is null) return null;

        var orgId2 = person.OrganizationId ?? auth.RowLevelFilter.OrganizationId ?? Guid.Empty;
        var employments = (await workforceClient.ListEmploymentsAsync(orgId2, ct)).Where(e => e.PersonId == personId).ToList();
        var profile = (await professionalClient.ListProfilesAsync(orgId2, ct)).FirstOrDefault(p => p.PersonId == personId);
        var governance = await governanceClient.GetAssigneesAsync(personId, ct);

        var dto = new EnterpriseDirectoryProfileDto
        {
            PersonId = personId,
            EmploymentId = employments.FirstOrDefault()?.Id,
            DisplayName = person.PreferredName ?? person.LegalName ?? "Unknown",
            PreferredName = person.PreferredName,
            PlatformId = person.PlatformId,
            Email = person.Email,
            Phone = person.Phone,
            Employments = employments.Select(e => new EmploymentProfileSectionDto
            {
                EmploymentId = e.Id,
                EmployeeNumber = e.EmployeeNumber,
                DepartmentName = e.DepartmentName,
                HospitalName = e.HospitalName,
                Designation = e.Designation,
                PositionTitle = e.PositionTitle,
                EmploymentStatus = e.EmploymentStatus,
                ManagerName = e.ManagerName
            }).ToList(),
            Professional = profile is null ? null : new ProfessionalProfileSectionDto
            {
                ProfileId = profile.Id,
                ProfessionalNumber = profile.ProfessionalNumber,
                Category = profile.Category,
                Skills = profile.Skills.Select(s => s.SkillName).ToList(),
                Licenses = profile.Licenses.Select(MapCredential).ToList(),
                Certifications = profile.Certifications.Select(MapCredential).ToList()
            },
            Responsibilities = governance.Responsibilities.Select(r => new GovernanceAssignmentDto { ResponsibilityName = r.ResponsibilityName, Role = r.Role }).ToList(),
            Committees = governance.Committees.Select(c => new CommitteeMembershipDto { CommitteeName = c.CommitteeName, Role = c.Role }).ToList()
        };

        cache.Set(cacheKey, dto, TimeSpan.FromSeconds(options.Value.ProfileCacheTtlSeconds));
        return ApplyMasks(dto, auth);
    }

    private static CredentialSummaryDto MapCredential(SourceCredentialDto c) => new()
    {
        Name = c.Name,
        Number = c.Number,
        Status = c.Status,
        ExpiryDate = c.ExpiryDate
    };

    private static EnterpriseDirectoryProfileDto ApplyMasks(EnterpriseDirectoryProfileDto dto, GovernedDirectoryContext auth)
    {
        if (auth.ContactReadable && auth.CredentialReadable) return dto;
        ProfessionalProfileSectionDto? professional = null;
        if (dto.Professional is not null)
        {
            professional = new ProfessionalProfileSectionDto
            {
                ProfileId = dto.Professional.ProfileId,
                ProfessionalNumber = auth.CredentialReadable ? dto.Professional.ProfessionalNumber : Mask(dto.Professional.ProfessionalNumber),
                Category = dto.Professional.Category,
                Skills = dto.Professional.Skills,
                Licenses = dto.Professional.Licenses.Select(l => new CredentialSummaryDto
                {
                    Name = l.Name, Status = l.Status, ExpiryDate = l.ExpiryDate,
                    Number = auth.CredentialReadable ? l.Number : Mask(l.Number)
                }).ToList(),
                Certifications = dto.Professional.Certifications.Select(c => new CredentialSummaryDto
                {
                    Name = c.Name, Status = c.Status, ExpiryDate = c.ExpiryDate,
                    Number = auth.CredentialReadable ? c.Number : Mask(c.Number)
                }).ToList()
            };
        }
        return new EnterpriseDirectoryProfileDto
        {
            PersonId = dto.PersonId,
            EmploymentId = dto.EmploymentId,
            DisplayName = dto.DisplayName,
            PreferredName = dto.PreferredName,
            PlatformId = dto.PlatformId,
            Email = auth.ContactReadable ? dto.Email : Mask(dto.Email),
            Phone = auth.ContactReadable ? dto.Phone : Mask(dto.Phone),
            Employments = dto.Employments,
            Professional = professional,
            Responsibilities = dto.Responsibilities,
            Committees = dto.Committees
        };
    }

    private static string? Mask(string? v) => string.IsNullOrEmpty(v) ? v : "***";
}
