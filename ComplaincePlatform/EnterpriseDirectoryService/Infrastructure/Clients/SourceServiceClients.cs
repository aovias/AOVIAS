using System.Net.Http.Json;
using System.Text.Json;
using EnterpriseDirectoryService.Application.Contracts;

namespace EnterpriseDirectoryService.Infrastructure.Clients;

public interface IPersonSourceClient
{
    Task<IReadOnlyList<SourcePersonDto>> ListPersonsAsync(Guid organizationId, CancellationToken ct = default);
    Task<SourcePersonDto?> GetPersonAsync(Guid personId, CancellationToken ct = default);
}

public interface IWorkforceSourceClient
{
    Task<IReadOnlyList<SourceEmploymentDto>> ListEmploymentsAsync(Guid organizationId, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> ListOrganizationIdsAsync(CancellationToken ct = default);
}

public interface IProfessionalSourceClient
{
    Task<IReadOnlyList<SourceProfessionalProfileDto>> ListProfilesAsync(Guid organizationId, CancellationToken ct = default);
}

public interface IGovernanceSourceClient
{
    Task<GovernanceAssigneeBundle> GetAssigneesAsync(Guid personId, CancellationToken ct = default);
}

public sealed class GovernanceAssigneeBundle
{
    public List<SourceGovernanceAssigneeDto> Responsibilities { get; set; } = [];
    public List<SourceCommitteeMemberDto> Committees { get; set; } = [];
}

public sealed class PersonSourceClient(HttpClient http) : IPersonSourceClient
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public async Task<IReadOnlyList<SourcePersonDto>> ListPersonsAsync(Guid organizationId, CancellationToken ct = default)
    {
        try
        {
            var response = await http.GetAsync($"/api/v1/persons?organizationId={organizationId}&pageSize=5000", ct);
            if (!response.IsSuccessStatusCode) return Array.Empty<SourcePersonDto>();
            var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<PagedDto<SourcePersonDto>>>(JsonOpts, ct);
            return envelope?.Data?.Items ?? new List<SourcePersonDto>();
        }
        catch { return Array.Empty<SourcePersonDto>(); }
    }

    public async Task<SourcePersonDto?> GetPersonAsync(Guid personId, CancellationToken ct = default)
    {
        try
        {
            var response = await http.GetAsync($"/api/v1/persons/{personId}", ct);
            if (!response.IsSuccessStatusCode) return null;
            var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<SourcePersonDto>>(JsonOpts, ct);
            return envelope?.Data;
        }
        catch { return null; }
    }
}

public sealed class WorkforceSourceClient(HttpClient http) : IWorkforceSourceClient
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public async Task<IReadOnlyList<SourceEmploymentDto>> ListEmploymentsAsync(Guid organizationId, CancellationToken ct = default)
    {
        try
        {
            var response = await http.GetAsync($"/api/v1/employment?organizationId={organizationId}&pageSize=5000", ct);
            if (!response.IsSuccessStatusCode) return Array.Empty<SourceEmploymentDto>();
            var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<PagedDto<SourceEmploymentDto>>>(JsonOpts, ct);
            return envelope?.Data?.Items ?? new List<SourceEmploymentDto>();
        }
        catch { return Array.Empty<SourceEmploymentDto>(); }
    }

    public async Task<IReadOnlyList<Guid>> ListOrganizationIdsAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await http.GetAsync("/internal/workforce/organizations", ct);
            if (!response.IsSuccessStatusCode) return Array.Empty<Guid>();
            var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<List<Guid>>>(JsonOpts, ct);
            return envelope?.Data ?? new List<Guid>();
        }
        catch { return Array.Empty<Guid>(); }
    }
}

public sealed class ProfessionalSourceClient(HttpClient http) : IProfessionalSourceClient
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public async Task<IReadOnlyList<SourceProfessionalProfileDto>> ListProfilesAsync(Guid organizationId, CancellationToken ct = default)
    {
        try
        {
            var response = await http.GetAsync($"/api/v1/professional-profiles?organizationId={organizationId}&pageSize=5000", ct);
            if (!response.IsSuccessStatusCode) return Array.Empty<SourceProfessionalProfileDto>();
            var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<PagedDto<SourceProfessionalProfileDto>>>(JsonOpts, ct);
            return envelope?.Data?.Items ?? new List<SourceProfessionalProfileDto>();
        }
        catch { return Array.Empty<SourceProfessionalProfileDto>(); }
    }
}

public sealed class GovernanceSourceClient(HttpClient http) : IGovernanceSourceClient
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public async Task<GovernanceAssigneeBundle> GetAssigneesAsync(Guid personId, CancellationToken ct = default)
    {
        try
        {
            var response = await http.GetAsync($"/internal/governance/assignees?personId={personId}", ct);
            if (!response.IsSuccessStatusCode) return new GovernanceAssigneeBundle();
            var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<GovernanceAssigneeBundle>>(JsonOpts, ct);
            return envelope?.Data ?? new GovernanceAssigneeBundle();
        }
        catch { return new GovernanceAssigneeBundle(); }
    }
}
