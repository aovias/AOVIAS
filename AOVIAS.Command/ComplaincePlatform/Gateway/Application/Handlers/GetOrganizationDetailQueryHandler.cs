using Gateway.Application.Queries;
using Gateway.DTOs;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;
using Shared.Infrastructure.DTO;

namespace Gateway.Application.Handlers;

// Temporary DTOs to match OrganizationService response
internal class OrganizationServiceDetailDto
{
    public Guid TenantId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string OrganizationCode { get; set; } = string.Empty;
    public string OrganizationType { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? TimeZoneId { get; set; }
    public string? LanguageCode { get; set; }
    public List<OrganizationServiceHospitalDto> Hospitals { get; set; } = new();
    public List<OrganizationServiceMemberDto> Members { get; set; } = new();
    public DateTime CreatedOn { get; set; }
}

internal class OrganizationServiceHospitalDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Type { get; set; }
    public int? BedCapacity { get; set; }
    public string? CountryCode { get; set; }
    public string? StateCode { get; set; }
    public bool IsOnboarded { get; set; }
    public string? NABHLevel { get; set; }
    public DateTime CreatedAt { get; set; }
}

internal class OrganizationServiceMemberDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string? Designation { get; set; }
    public Guid? ReportsToUserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? AlternatePhoneNumber { get; set; }
    public string? ProfilePicUrl { get; set; }
    public string? Preferences { get; set; }
    public DateTime CreatedAt { get; set; }
}

// DTOs for deserializing ComplianceService batch journey response
internal class ComplianceJourneyBatchItemDto
{
    public Guid HospitalId { get; set; }
    public List<ComplianceJourneyItemDto> Journeys { get; set; } = new();
}

internal class ComplianceJourneyItemDto
{
    public Guid JourneyId { get; set; }
    /// <summary>Framework the journey enrolls in — see <c>HospitalJourneyItemDto.FrameworkId</c>.</summary>
    public Guid FrameworkId { get; set; }
    public string FrameworkCode { get; set; } = string.Empty;
    public string FrameworkName { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

/// <summary>
/// Handler for GetOrganizationDetailQuery that orchestrates calls to OrganizationService using ProxyService.
/// </summary>
public class GetOrganizationDetailQueryHandler : IRequestHandler<GetOrganizationDetailQuery, OrganizationDetailDto>
{
    private readonly IProxyService _proxyService;
    private readonly ILogger<GetOrganizationDetailQueryHandler> _logger;

    public GetOrganizationDetailQueryHandler(
        IProxyService proxyService,
        ILogger<GetOrganizationDetailQueryHandler> logger)
    {
        _proxyService = proxyService;
        _logger = logger;
    }

    public async Task<OrganizationDetailDto> Handle(GetOrganizationDetailQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving organization detail for tenant {TenantId}", request.TenantId);

        try
        {
            var endpoint = $"/api/v1/organizations/{request.TenantId}/detail";
            
            var result = await _proxyService.ProxyToOrganizationServiceAsync(
                endpoint: endpoint,
                httpMethod: HttpMethod.Get,
                requestBody: null,
                cancellationToken: cancellationToken);

            if (result is ContentResult contentResult)
            {
                var statusCode = contentResult.StatusCode ?? 0;
                
                // Handle error status codes
                if (statusCode == (int)HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Organization not found for tenant {TenantId}", request.TenantId);
                    throw new KeyNotFoundException($"Organization with ID {request.TenantId} was not found.");
                }

                if (statusCode == (int)HttpStatusCode.Unauthorized || statusCode == (int)HttpStatusCode.Forbidden)
                {
                    _logger.LogError("Authorization failed when calling OrganizationService for tenant {TenantId}. Status: {StatusCode}, Response: {Response}", 
                        request.TenantId, statusCode, contentResult.Content);
                    throw new UnauthorizedAccessException($"Access denied when retrieving organization detail for tenant {request.TenantId}.");
                }

                if (statusCode >= 400)
                {
                    _logger.LogError("OrganizationService returned error for tenant {TenantId}. Status: {StatusCode}, Response: {Response}", 
                        request.TenantId, statusCode, contentResult.Content);
                    throw new HttpRequestException($"OrganizationService returned error status {statusCode}: {contentResult.Content}");
                }

                // Handle successful response
                if (statusCode == (int)HttpStatusCode.OK && !string.IsNullOrEmpty(contentResult.Content))
                {
                    try
                    {
                        // OrganizationService wraps responses in standardized ApiResponse<T> envelope.
                        // Deserialize envelope and extract the underlying DTO.
                        var envelope = JsonSerializer.Deserialize<ApiResponse<OrganizationServiceDetailDto>>(
                            contentResult.Content,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (envelope == null || !envelope.Success || envelope.Data == null)
                        {
                            _logger.LogWarning("Failed to deserialize organization detail envelope for tenant {TenantId}. Response: {Response}",
                                request.TenantId, contentResult.Content);
                            throw new InvalidOperationException("Failed to retrieve organization detail from OrganizationService.");
                        }

                        var orgServiceDto = envelope.Data;

                        // Map to Gateway DTO
                        var organizationDetail = new OrganizationDetailDto
                        {
                            TenantId = orgServiceDto.TenantId,
                            OrganizationName = orgServiceDto.OrganizationName,
                            OrganizationCode = orgServiceDto.OrganizationCode,
                            OrganizationType = orgServiceDto.OrganizationType,
                            LogoUrl = orgServiceDto.LogoUrl,
                            TimeZoneId = orgServiceDto.TimeZoneId,
                            LanguageCode = orgServiceDto.LanguageCode,
                            CreatedOn = orgServiceDto.CreatedOn,
                            Hospitals = orgServiceDto.Hospitals?.Select(h => new HospitalDetailDto
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
                            }).ToList() ?? new List<HospitalDetailDto>(),
                            Members = orgServiceDto.Members?.Select(m => new MemberDto
                            {
                                Id = m.Id,
                                UserId = m.UserId,
                                DepartmentId = m.DepartmentId,
                                DepartmentName = m.DepartmentName,
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
                            }).ToList() ?? new List<MemberDto>()
                        };

                        // Enrich hospitals with journey data from ComplianceService (batch call)
                        await EnrichHospitalsWithJourneysAsync(organizationDetail.Hospitals, cancellationToken);

                        _logger.LogInformation("Successfully retrieved organization detail for tenant {TenantId}", request.TenantId);
                        return organizationDetail;
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "JSON deserialization error for tenant {TenantId}. Response: {Response}", 
                            request.TenantId, contentResult.Content);
                        throw new InvalidOperationException($"Failed to parse response from OrganizationService: {ex.Message}");
                    }
                }
            }

            // Handle ObjectResult (error responses from ProxyService)
            if (result is ObjectResult objectResult)
            {
                _logger.LogError("OrganizationService proxy returned error for tenant {TenantId}. Status: {StatusCode}, Response: {Response}", 
                    request.TenantId, objectResult.StatusCode, 
                    objectResult.Value != null ? JsonSerializer.Serialize(objectResult.Value) : "null");
                throw new HttpRequestException($"OrganizationService is unavailable or returned an error: {objectResult.Value}");
            }

            _logger.LogError("Unexpected response type from OrganizationService for tenant {TenantId}. Type: {Type}", 
                request.TenantId, result?.GetType().Name ?? "null");
            throw new InvalidOperationException("Unexpected response from OrganizationService.");
        }
        catch (KeyNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving organization detail for tenant {TenantId}", request.TenantId);
            throw;
        }
    }

    /// <summary>
    /// Calls ComplianceService batch endpoint to fetch journeys for all hospitals
    /// and maps them into each HospitalDetailDto.Journey.
    /// Gracefully degrades: if the call fails, hospitals keep Journey = [].
    /// </summary>
    private async Task EnrichHospitalsWithJourneysAsync(
        List<HospitalDetailDto> hospitals,
        CancellationToken cancellationToken)
    {
        if (hospitals == null || hospitals.Count == 0) return;

        var hospitalIds = hospitals.Select(h => h.Id).ToList();

        try
        {
            var batchResult = await _proxyService.ProxyToComplianceServiceAsync(
                endpoint: "/api/v1/journeys/by-hospitals",
                httpMethod: HttpMethod.Post,
                requestBody: new { HospitalIds = hospitalIds },
                cancellationToken: cancellationToken);

            if (batchResult is ContentResult batchContent
                && batchContent.StatusCode == 200
                && !string.IsNullOrEmpty(batchContent.Content))
            {
                var envelope = JsonSerializer.Deserialize<ApiResponse<List<ComplianceJourneyBatchItemDto>>>(
                    batchContent.Content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (envelope?.Success == true && envelope.Data != null)
                {
                    var lookup = envelope.Data.ToDictionary(x => x.HospitalId, x => x.Journeys);

                    foreach (var hospital in hospitals)
                    {
                        if (lookup.TryGetValue(hospital.Id, out var journeys))
                        {
                            hospital.Journey = journeys.Select(j => new JourneyDto
                            {
                                JourneyId = j.JourneyId,
                                FrameworkId = j.FrameworkId,
                                FrameworkCode = j.FrameworkCode,
                                FrameworkName = j.FrameworkName,
                                StartDate = j.StartDate,
                                ExpiryDate = j.ExpiryDate
                            }).ToList();
                        }
                        // else: hospital.Journey stays as default new() — empty list
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Graceful degradation: log warning, hospitals keep Journey = []
            _logger.LogWarning(ex,
                "Failed to enrich hospitals with journey data. Hospitals will have empty Journey arrays.");
        }
    }
}
