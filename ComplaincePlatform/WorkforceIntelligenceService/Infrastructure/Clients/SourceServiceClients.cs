using System.Net.Http.Json;
using System.Text.Json;
using WorkforceIntelligenceService.Application.Contracts;

namespace WorkforceIntelligenceService.Infrastructure.Clients;

public interface IWorkforceSourceClient
{
    Task<IReadOnlyList<SourceEmploymentDto>> ListEmploymentsAsync(Guid organizationId, CancellationToken ct = default);
}

public interface IProfessionalSourceClient
{
    Task<IReadOnlyList<SourceProfessionalProfileDto>> ListProfilesAsync(Guid organizationId, CancellationToken ct = default);
    Task<IReadOnlyList<SourceRenewalItemDto>> GetExpiringAsync(Guid organizationId, int withinDays, CancellationToken ct = default);
}

public interface IGovernanceSourceClient
{
    Task<SourceGovernanceSummaryDto> GetSummaryAsync(Guid organizationId, CancellationToken ct = default);
}

public interface IOrganizationSourceClient
{
    Task<Dictionary<string, int>> GetDepartmentSizesAsync(Guid organizationId, CancellationToken ct = default);
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
        catch
        {
            return Array.Empty<SourceEmploymentDto>();
        }
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
            var items = envelope?.Data?.Items ?? new List<SourceProfessionalProfileDto>();
            foreach (var item in items)
            {
                await HydrateProfileAsync(item, ct);
            }
            return items;
        }
        catch
        {
            return Array.Empty<SourceProfessionalProfileDto>();
        }
    }

    private async Task HydrateProfileAsync(SourceProfessionalProfileDto item, CancellationToken ct)
    {
        try
        {
            var response = await http.GetAsync($"/api/v1/professional-profiles/{item.Id}", ct);
            if (!response.IsSuccessStatusCode) return;
            var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<SourceProfessionalProfileDto>>(JsonOpts, ct);
            if (envelope?.Data is null) return;
            item.Skills = envelope.Data.Skills.Select(s => new SourceSkillDto { SkillName = s.SkillName, ProficiencyLevel = s.ProficiencyLevel, IsCritical = s.IsCritical }).ToList();
            item.Competencies = envelope.Data.Competencies.Select(c => new SourceCompetencyDto { SkillName = c.SkillName, Level = c.Level, AssessmentDate = c.AssessmentDate }).ToList();
            item.TrainingRecords = envelope.Data.TrainingRecords.Select(t => new SourceTrainingDto { CourseName = t.CourseName, IsMandatory = t.IsMandatory, CompletionDate = t.CompletionDate }).ToList();
            item.Licenses = envelope.Data.Licenses;
            item.Certifications = envelope.Data.Certifications;
            item.Qualifications = envelope.Data.Qualifications;
        }
        catch { /* best effort */ }
    }

    public async Task<IReadOnlyList<SourceRenewalItemDto>> GetExpiringAsync(Guid organizationId, int withinDays, CancellationToken ct = default)
    {
        try
        {
            var response = await http.GetAsync($"/internal/professional/expiring?withinDays={withinDays}&organizationId={organizationId}", ct);
            if (!response.IsSuccessStatusCode) return Array.Empty<SourceRenewalItemDto>();
            var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<List<SourceRenewalItemDto>>>(JsonOpts, ct);
            return envelope?.Data ?? new List<SourceRenewalItemDto>();
        }
        catch
        {
            return Array.Empty<SourceRenewalItemDto>();
        }
    }
}

public sealed class GovernanceSourceClient(HttpClient http) : IGovernanceSourceClient
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public async Task<SourceGovernanceSummaryDto> GetSummaryAsync(Guid organizationId, CancellationToken ct = default)
    {
        try
        {
            var response = await http.GetAsync($"/api/v1/governance/analytics/summary?organizationId={organizationId}", ct);
            if (!response.IsSuccessStatusCode) return new SourceGovernanceSummaryDto();
            var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<SourceGovernanceSummaryDto>>(JsonOpts, ct);
            return envelope?.Data ?? new SourceGovernanceSummaryDto();
        }
        catch
        {
            return new SourceGovernanceSummaryDto();
        }
    }
}

public sealed class OrganizationSourceClient(HttpClient http) : IOrganizationSourceClient
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public async Task<Dictionary<string, int>> GetDepartmentSizesAsync(Guid organizationId, CancellationToken ct = default)
    {
        try
        {
            var response = await http.GetAsync($"/api/v1/departments/tree?organizationId={organizationId}", ct);
            if (!response.IsSuccessStatusCode) return new Dictionary<string, int>();
            var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<List<DeptNodeDto>>>(JsonOpts, ct);
            var result = new Dictionary<string, int>();
            foreach (var node in envelope?.Data ?? [])
            {
                result[node.Name ?? node.Id.ToString()] = node.MemberCount ?? 0;
            }
            return result;
        }
        catch
        {
            return new Dictionary<string, int>();
        }
    }

    private sealed class DeptNodeDto
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public int? MemberCount { get; set; }
    }
}

internal sealed class ApiEnvelope<T>
{
    public T? Data { get; set; }
}

internal sealed class PagedDto<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
}
