using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ComplianceService.Application.Authorization;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.DTO;

namespace ComplianceService.Infrastructure.Services;

public interface IOrganizationServiceClient : IHospitalDirectory
{
    Task<IReadOnlyList<OrganizationHospitalDto>> GetHospitalsAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<OrganizationBusinessCalendarDto?> GetBusinessCalendarAsync(Guid organizationId, int? year = null, CancellationToken cancellationToken = default);
}

public sealed class OrganizationServiceClient : IOrganizationServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OrganizationServiceClient> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public OrganizationServiceClient(HttpClient httpClient, ILogger<OrganizationServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<OrganizationHospitalDto>> GetHospitalsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/internal/organizations/{organizationId}/hospitals", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "OrganizationService returned {StatusCode} while fetching hospitals for organization {OrganizationId}",
                    response.StatusCode,
                    organizationId);
                return Array.Empty<OrganizationHospitalDto>();
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var envelope = JsonSerializer.Deserialize<ApiResponse<List<OrganizationHospitalDto>>>(content, JsonOptions);
            return envelope?.Data ?? new List<OrganizationHospitalDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve hospitals for organization {OrganizationId}", organizationId);
            return Array.Empty<OrganizationHospitalDto>();
        }
    }

    public async Task<OrganizationBusinessCalendarDto?> GetBusinessCalendarAsync(
        Guid organizationId,
        int? year = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var path = $"/internal/organizations/{organizationId}/business-calendar";
            if (year.HasValue)
            {
                path += $"?year={year.Value}";
            }

            var response = await _httpClient.GetAsync(path, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "OrganizationService returned {StatusCode} while fetching business calendar for organization {OrganizationId}",
                    response.StatusCode,
                    organizationId);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var envelope = JsonSerializer.Deserialize<ApiResponse<OrganizationBusinessCalendarDto>>(content, JsonOptions);
            return envelope?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve business calendar for organization {OrganizationId}", organizationId);
            return null;
        }
    }

    public async Task<HospitalLookupResult> GetHospitalAsync(Guid hospitalId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/internal/hospitals/{hospitalId:D}", cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return HospitalLookupResult.NotFound();
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "OrganizationService returned {StatusCode} while fetching hospital {HospitalId}",
                    response.StatusCode,
                    hospitalId);
                return HospitalLookupResult.Unavailable();
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var envelope = JsonSerializer.Deserialize<ApiResponse<OrganizationHospitalRecordDto>>(content, JsonOptions);
            var data = envelope?.Data;
            if (data is null || data.Id == Guid.Empty || data.OrganizationId == Guid.Empty)
            {
                return HospitalLookupResult.NotFound();
            }

            return HospitalLookupResult.Found(new OrganizationHospitalRecord(
                data.Id,
                data.OrganizationId,
                data.Status ?? "Active",
                data.IsDeleted,
                data.IsOnboarded));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve hospital {HospitalId} from OrganizationService", hospitalId);
            return HospitalLookupResult.Unavailable();
        }
    }
}

internal sealed class OrganizationHospitalRecordDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string? Status { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsOnboarded { get; set; }
}

public sealed record OrganizationHospitalDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public bool IsOnboarded { get; init; }
    public string? NabhLevel { get; init; }
}

public sealed class OrganizationBusinessCalendarDto
{
    public Guid OrganizationId { get; init; }
    public string TimeZoneId { get; init; } = string.Empty;
    public IReadOnlyList<string> WorkingDays { get; init; } = Array.Empty<string>();
    public OrganizationWorkingHoursDto WorkingHours { get; init; } = new();
    public IReadOnlyList<OrganizationHolidayDto> Holidays { get; init; } = Array.Empty<OrganizationHolidayDto>();
}

public sealed class OrganizationWorkingHoursDto
{
    public string Start { get; init; } = "09:00";
    public string End { get; init; } = "18:00";
}

public sealed class OrganizationHolidayDto
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public DateOnly Date { get; init; }
    public string Name { get; init; } = string.Empty;
}
