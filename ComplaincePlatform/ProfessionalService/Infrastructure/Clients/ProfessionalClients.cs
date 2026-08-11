using System.Text.Json;
using ProfessionalService.Application.Contracts;
using ProfessionalService.Application.DTOs;
using Shared.Infrastructure.DTO;

namespace ProfessionalService.Infrastructure.Clients;

public class WorkforceServiceClient : IWorkforceServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WorkforceServiceClient> _logger;

    public WorkforceServiceClient(HttpClient httpClient, ILogger<WorkforceServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<EmploymentRefDto?> GetEmploymentAsync(Guid employmentId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/internal/employment/{employmentId}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("WorkforceService returned {Status} for employment {EmploymentId}", response.StatusCode, employmentId);
            return null;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var api = JsonSerializer.Deserialize<ApiResponse<EmploymentPayload>>(body, JsonOptions);
        if (api?.Success != true || api.Data == null) return null;

        return new EmploymentRefDto
        {
            EmploymentId = api.Data.Id,
            PersonId = api.Data.PersonId,
            OrganizationId = api.Data.OrganizationId,
            HospitalId = api.Data.HospitalId,
            PersonSearchName = api.Data.PersonSearchName ?? string.Empty
        };
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private sealed class EmploymentPayload
    {
        public Guid Id { get; set; }
        public Guid PersonId { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid HospitalId { get; set; }
        public string? PersonSearchName { get; set; }
    }
}

public class OrganizationServiceClient : IOrganizationServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OrganizationServiceClient> _logger;

    public OrganizationServiceClient(HttpClient httpClient, ILogger<OrganizationServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> ValidateDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/internal/master-departments/{departmentId}", cancellationToken);
        if (!response.IsSuccessStatusCode)
            _logger.LogWarning("OrganizationService department validation failed {DepartmentId} {Status}", departmentId, response.StatusCode);
        return response.IsSuccessStatusCode;
    }
}
