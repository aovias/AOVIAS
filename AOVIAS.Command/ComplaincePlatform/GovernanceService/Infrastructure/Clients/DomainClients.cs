using System.Net.Http.Json;
using System.Text.Json;

namespace GovernanceService.Infrastructure.Clients;

public interface IPersonServiceClient
{
    Task<bool> ExistsAsync(Guid personId, CancellationToken ct = default);
}

public interface IWorkforceServiceClient
{
    Task<bool> EmploymentExistsAsync(Guid employmentId, CancellationToken ct = default);
    Task<Guid?> GetManagerEmploymentIdAsync(Guid employmentId, CancellationToken ct = default);
}

public interface IOrganizationServiceClient
{
    Task<bool> DepartmentExistsAsync(Guid departmentId, CancellationToken ct = default);
}

internal sealed class EmploymentApiDto
{
    public Guid Id { get; set; }
    public List<ReportingRelationshipApiDto> ReportingRelationships { get; set; } = new();
}

internal sealed class ReportingRelationshipApiDto
{
    public Guid ManagerEmploymentId { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

public class PersonServiceClient(HttpClient http) : IPersonServiceClient
{
    public async Task<bool> ExistsAsync(Guid personId, CancellationToken ct = default)
    {
        var response = await http.GetAsync($"/internal/person/{personId}", ct);
        return response.IsSuccessStatusCode;
    }
}

public class WorkforceServiceClient(HttpClient http) : IWorkforceServiceClient
{
    public async Task<bool> EmploymentExistsAsync(Guid employmentId, CancellationToken ct = default)
    {
        var response = await http.GetAsync($"/internal/employment/{employmentId}", ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<Guid?> GetManagerEmploymentIdAsync(Guid employmentId, CancellationToken ct = default)
    {
        var dto = await DeserializeDataAsync<EmploymentApiDto>(await http.GetAsync($"/internal/employment/{employmentId}", ct), ct);
        if (dto == null) return null;
        var now = DateTime.UtcNow;
        var rel = dto.ReportingRelationships.FirstOrDefault(r => r.EffectiveTo == null || r.EffectiveTo > now);
        return rel?.ManagerEmploymentId;
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

public class OrganizationServiceClient(HttpClient http) : IOrganizationServiceClient
{
    public async Task<bool> DepartmentExistsAsync(Guid departmentId, CancellationToken ct = default)
    {
        var response = await http.GetAsync($"/internal/departments/{departmentId}", ct);
        return response.IsSuccessStatusCode;
    }
}

public interface IAssigneeValidationService
{
    Task ValidateAsync(string assigneeType, Guid assigneeId, CancellationToken ct = default);
}

public class AssigneeValidationService(
    IPersonServiceClient personClient,
    IWorkforceServiceClient workforceClient,
    IOrganizationServiceClient organizationClient) : IAssigneeValidationService
{
    public async Task ValidateAsync(string assigneeType, Guid assigneeId, CancellationToken ct = default)
    {
        var exists = assigneeType switch
        {
            "Person" => await personClient.ExistsAsync(assigneeId, ct),
            "Employment" => await workforceClient.EmploymentExistsAsync(assigneeId, ct),
            "Department" => await organizationClient.DepartmentExistsAsync(assigneeId, ct),
            "Position" or "ProfessionalCategory" or "Organization" or "Hospital" or "Committee" or "Role" or "User" => true,
            _ => throw new InvalidOperationException($"Unknown assignee type: {assigneeType}")
        };
        if (!exists) throw new InvalidOperationException($"Assignee not found: {assigneeType}/{assigneeId}");
    }
}
