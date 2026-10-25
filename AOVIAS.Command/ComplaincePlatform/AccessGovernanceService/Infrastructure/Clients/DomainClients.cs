using AccessGovernanceService.Application.Contracts;
using System.Net.Http.Json;
using System.Text.Json;

namespace AccessGovernanceService.Infrastructure.Clients;

internal sealed class EmploymentApiDto
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid HospitalId { get; set; }
    public string? EmploymentStatusName { get; set; }
    public List<DepartmentAssignmentApiDto> DepartmentAssignments { get; set; } = new();
}

internal sealed class DepartmentAssignmentApiDto
{
    public Guid? DepartmentId { get; set; }
    public bool IsPrimary { get; set; }
}

internal sealed class ProfessionalProfileApiDto
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public Guid? ProfessionalCategoryValueId { get; set; }
    public List<RegistrationApiDto> Registrations { get; set; } = new();
    public List<LicenseApiDto> Licenses { get; set; } = new();
    public List<ClinicalPrivilegeApiDto> ClinicalPrivileges { get; set; } = new();
}

internal sealed class RegistrationApiDto { public string Status { get; set; } = string.Empty; }
internal sealed class LicenseApiDto { public string Status { get; set; } = string.Empty; }
internal sealed class ClinicalPrivilegeApiDto { public string PrivilegeName { get; set; } = string.Empty; public string Status { get; set; } = string.Empty; }

internal sealed class AuthContextApiDto
{
    public Guid OrganizationId { get; set; }
    public Guid HospitalId { get; set; }
    public Guid MembershipId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string Role { get; set; } = string.Empty;
}

public class OrganizationServiceClient(HttpClient http) : IOrganizationServiceClient
{
    public async Task<MembershipContextDto?> GetMembershipContextAsync(Guid userId, Guid? organizationId = null, CancellationToken cancellationToken = default)
    {
        var url = organizationId.HasValue
            ? $"/internal/users/{userId}/auth-context?organizationId={organizationId}"
            : $"/internal/users/{userId}/auth-context";
        var dto = await DeserializeDataAsync<AuthContextApiDto>(await http.GetAsync(url, cancellationToken), cancellationToken);
        if (dto == null || dto.MembershipId == Guid.Empty)
            return null;
        return new MembershipContextDto
        {
            OrganizationId = dto.OrganizationId,
            HospitalId = dto.HospitalId,
            MembershipId = dto.MembershipId,
            DepartmentId = dto.DepartmentId,
            Role = dto.Role ?? string.Empty,
            MembershipType = dto.Role ?? "Member",
            MembershipStatus = "Active"
        };
    }

    private static async Task<T?> DeserializeDataAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode) return default;
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        if (doc.RootElement.TryGetProperty("data", out var data))
            return data.Deserialize<T>();
        return doc.RootElement.Deserialize<T>();
    }
}

public class WorkforceServiceClient(HttpClient http) : IWorkforceServiceClient
{
    public async Task<EmploymentContextDto?> GetEmploymentAsync(Guid employmentId, CancellationToken cancellationToken = default)
    {
        var dto = await DeserializeDataAsync<EmploymentApiDto>(await http.GetAsync($"/internal/employment/{employmentId}", cancellationToken), cancellationToken);
        if (dto == null) return null;
        var dept = dto.DepartmentAssignments.FirstOrDefault(d => d.IsPrimary) ?? dto.DepartmentAssignments.FirstOrDefault();
        return new EmploymentContextDto
        {
            Id = dto.Id,
            PersonId = dto.PersonId,
            OrganizationId = dto.OrganizationId,
            HospitalId = dto.HospitalId,
            DepartmentId = dept?.DepartmentId,
            Status = dto.EmploymentStatusName ?? "Unknown"
        };
    }

    private static async Task<T?> DeserializeDataAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode) return default;
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        if (doc.RootElement.TryGetProperty("data", out var data))
            return data.Deserialize<T>();
        return doc.RootElement.Deserialize<T>();
    }
}

public class ProfessionalServiceClient(HttpClient http) : IProfessionalServiceClient
{
    public async Task<ProfessionalContextDto?> GetByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        var dto = await DeserializeDataAsync<ProfessionalProfileApiDto>(await http.GetAsync($"/internal/professional/by-person/{personId}", cancellationToken), cancellationToken);
        return dto == null ? null : Map(dto);
    }

    public async Task<ProfessionalContextDto?> GetByProfileIdAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        var dto = await DeserializeDataAsync<ProfessionalProfileApiDto>(await http.GetAsync($"/internal/professional/{profileId}", cancellationToken), cancellationToken);
        return dto == null ? null : Map(dto);
    }

    private static ProfessionalContextDto Map(ProfessionalProfileApiDto dto) => new()
    {
        ProfileId = dto.Id,
        PersonId = dto.PersonId,
        Category = dto.ProfessionalCategoryValueId?.ToString() ?? "Unknown",
        HasActiveRegistration = dto.Registrations.Any(r => string.Equals(r.Status, "Active", StringComparison.OrdinalIgnoreCase)),
        HasActiveLicense = dto.Licenses.Any(l => string.Equals(l.Status, "Active", StringComparison.OrdinalIgnoreCase)),
        ClinicalPrivileges = dto.ClinicalPrivileges.Where(p => string.Equals(p.Status, "Active", StringComparison.OrdinalIgnoreCase)).Select(p => p.PrivilegeName).ToList()
    };

    private static async Task<T?> DeserializeDataAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode) return default;
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        if (doc.RootElement.TryGetProperty("data", out var data))
            return data.Deserialize<T>();
        return doc.RootElement.Deserialize<T>();
    }
}

public class IdentityServiceClient(HttpClient http) : IIdentityServiceClient
{
    public async Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var response = await http.GetAsync($"/internal/users/{userId}/exists", cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
