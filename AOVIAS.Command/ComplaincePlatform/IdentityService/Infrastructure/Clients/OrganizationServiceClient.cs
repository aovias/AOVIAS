using IdentityService.Application.Contracts;
using IdentityService.Application.DTOs;
using Shared.Infrastructure.DTO;
using System.Text.Json;

namespace IdentityService.Infrastructure.Clients;

public class OrganizationServiceClient : IOrganizationServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OrganizationServiceClient> _logger;

    public OrganizationServiceClient(
        HttpClient httpClient,
        ILogger<OrganizationServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<UserAuthContextDto> GetUserAuthContextAsync(
        Guid userId,
        Guid? hospitalId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"/internal/users/{userId}/auth-context";
            if (hospitalId.HasValue && hospitalId.Value != Guid.Empty)
                url += $"?hospitalId={hospitalId.Value}";

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("OrganizationService auth-context returned {StatusCode} for user {UserId}", response.StatusCode, userId);
                return EmptyContext();
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<UserAuthContextDto>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return apiResponse?.Success == true && apiResponse.Data != null
                ? apiResponse.Data
                : EmptyContext();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling OrganizationService auth-context for user {UserId}", userId);
            return EmptyContext();
        }
    }

    public async Task<(Guid organizationId, Guid hospitalId)> GetUserOrganizationAndHospitalAsync(Guid userId)
    {
        var ctx = await GetUserAuthContextAsync(userId);
        return (ctx.OrganizationId, ctx.HospitalId);
    }

    private static UserAuthContextDto EmptyContext() => new()
    {
        OrganizationId = Guid.Empty,
        HospitalId = Guid.Empty,
        MembershipId = Guid.Empty,
        DepartmentId = null,
        Role = string.Empty
    };
}
