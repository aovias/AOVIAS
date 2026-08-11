using System.Text.Json;
using WorkforceService.Application.Contracts;
using Shared.Infrastructure.DTO;

namespace WorkforceService.Infrastructure.Clients;

public class PersonServiceClient : IPersonServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PersonServiceClient> _logger;

    public PersonServiceClient(HttpClient httpClient, ILogger<PersonServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<PersonRefDto?> GetPersonAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/internal/persons/{personId}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("PersonService returned {Status} for {PersonId}", response.StatusCode, personId);
            return null;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var api = JsonSerializer.Deserialize<ApiResponse<PersonPayload>>(body, JsonOptions);
        if (api?.Success != true || api.Data == null) return null;

        return new PersonRefDto
        {
            PersonId = api.Data.Id,
            SearchName = api.Data.SearchName ?? api.Data.DisplayName ?? string.Empty,
            DisplayName = api.Data.DisplayName
        };
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private sealed class PersonPayload
    {
        public Guid Id { get; set; }
        public string? SearchName { get; set; }
        public string? DisplayName { get; set; }
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

    public async Task<bool> ValidateHospitalAsync(Guid hospitalId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/internal/hospitals/{hospitalId}", cancellationToken);
        if (!response.IsSuccessStatusCode)
            _logger.LogWarning("OrganizationService hospital validation failed {HospitalId} {Status}", hospitalId, response.StatusCode);
        return response.IsSuccessStatusCode;
    }
}
