using Gateway.Application.Queries;
using Gateway.DTOs;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Gateway.Application.Handlers;

/// <summary>
/// Proxies the hospital dashboard request to ComplianceService and normalizes the response for the UI.
/// </summary>
public sealed class GetHospitalDashboardQueryHandler : IRequestHandler<GetHospitalDashboardQuery, HospitalDashboardDto>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IProxyService _proxyService;
    private readonly ILogger<GetHospitalDashboardQueryHandler> _logger;

    public GetHospitalDashboardQueryHandler(
        IProxyService proxyService,
        ILogger<GetHospitalDashboardQueryHandler> logger)
    {
        _proxyService = proxyService;
        _logger = logger;
    }

    public async Task<HospitalDashboardDto> Handle(GetHospitalDashboardQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving hospital dashboard for hospital {HospitalId}", request.HospitalId);

        try
        {
            var endpoint = $"/api/v1/dashboard/hospital?hospitalId={request.HospitalId}";
            var result = await _proxyService.ProxyToComplianceServiceAsync(
                endpoint: endpoint,
                httpMethod: HttpMethod.Get,
                requestBody: null,
                cancellationToken: cancellationToken);

            if (result is ContentResult contentResult)
            {
                if (contentResult.StatusCode == (int)HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedAccessException("Unauthorized access to ComplianceService");
                }

                if (contentResult.StatusCode == (int)HttpStatusCode.Forbidden)
                {
                    throw new UnauthorizedAccessException("Hospital access denied");
                }

                if (contentResult.StatusCode >= 500)
                {
                    _logger.LogError(
                        "ComplianceService returned error {StatusCode} for hospital {HospitalId}. Response: {Response}",
                        contentResult.StatusCode,
                        request.HospitalId,
                        contentResult.Content);
                    throw new InvalidOperationException(
                        $"ComplianceService error: {contentResult.Content ?? "Internal server error"}");
                }

                if (contentResult.StatusCode == (int)HttpStatusCode.OK)
                {
                    if (string.IsNullOrEmpty(contentResult.Content))
                    {
                        _logger.LogWarning(
                            "ComplianceService returned an empty payload for hospital dashboard {HospitalId}",
                            request.HospitalId);
                        return new HospitalDashboardDto();
                    }

                    var envelope = JsonSerializer.Deserialize<ApiResponse<BackendHospitalDashboardDto>>(
                        contentResult.Content,
                        JsonOptions);

                    if (envelope?.Data == null)
                    {
                        _logger.LogWarning(
                            "ComplianceService did not return hospital dashboard data for {HospitalId}. Payload: {Payload}",
                            request.HospitalId,
                            contentResult.Content);
                        throw new InvalidOperationException("ComplianceService did not return hospital dashboard data.");
                    }

                    _logger.LogInformation(
                        "Successfully retrieved hospital dashboard for hospital {HospitalId}",
                        request.HospitalId);
                    return MapToDto(envelope.Data);
                }
            }

            if (result is StatusCodeResult statusCodeResult)
            {
                if (statusCodeResult.StatusCode == (int)HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedAccessException("Unauthorized access to ComplianceService");
                }

                if (statusCodeResult.StatusCode == (int)HttpStatusCode.Forbidden)
                {
                    throw new UnauthorizedAccessException("Hospital access denied");
                }

                if (statusCodeResult.StatusCode >= 500)
                {
                    _logger.LogError(
                        "ComplianceService returned {StatusCode} for hospital dashboard request {HospitalId}",
                        statusCodeResult.StatusCode,
                        request.HospitalId);
                    throw new InvalidOperationException("ComplianceService internal server error");
                }
            }

            if (result is ObjectResult objectResult)
            {
                _logger.LogError(
                    "ProxyService returned ObjectResult with status {StatusCode} for hospital {HospitalId}. Value: {Value}",
                    objectResult.StatusCode,
                    request.HospitalId,
                    objectResult.Value != null ? JsonSerializer.Serialize(objectResult.Value) : "null");

                if (objectResult.StatusCode == (int)HttpStatusCode.BadGateway)
                {
                    throw new InvalidOperationException("ComplianceService is unavailable.");
                }

                if (objectResult.StatusCode >= 500)
                {
                    throw new InvalidOperationException("ComplianceService internal server error");
                }
            }

            _logger.LogWarning(
                "Unexpected response type from ComplianceService for hospital {HospitalId}. Response type: {ResponseType}",
                request.HospitalId,
                result?.GetType().Name ?? "null");
            throw new InvalidOperationException(
                $"Unexpected response from ComplianceService. Response type: {result?.GetType().Name ?? "null"}");
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving hospital dashboard for hospital {HospitalId}", request.HospitalId);
            throw;
        }
    }

    private static HospitalDashboardDto MapToDto(BackendHospitalDashboardDto data)
    {
        return new HospitalDashboardDto
        {
            HospitalId = data.HospitalId.ToString(),
            ComplianceScore = data.ComplianceScore,
            RiskIndex = data.RiskIndex,
            DepartmentBreakdown = (data.DepartmentBreakdown ?? new List<BackendDepartmentComplianceDto>())
                .Select(MapDepartment)
                .ToList(),
            ChapterBreakdown = (data.ChapterBreakdown ?? new List<BackendChapterComplianceDto>())
                .Select(MapChapter)
                .ToList()
        };
    }

    private static DepartmentComplianceDto MapDepartment(BackendDepartmentComplianceDto dept)
        => new()
        {
            Department = dept.Department,
            ComplianceScore = dept.ComplianceScore,
            TotalTasks = dept.TotalTasks,
            CompletedTasks = dept.CompletedTasks
        };

    private static ChapterComplianceDto MapChapter(BackendChapterComplianceDto chapter)
        => new()
        {
            ChapterId = chapter.ChapterId.ToString(),
            ChapterCode = chapter.ChapterCode,
            ChapterName = chapter.ChapterName,
            ComplianceScore = chapter.ComplianceScore,
            TotalTasks = chapter.TotalTasks,
            CompletedTasks = chapter.CompletedTasks
        };

    private sealed class BackendHospitalDashboardDto
    {
        public Guid HospitalId { get; set; }
        public decimal ComplianceScore { get; set; }
        public decimal RiskIndex { get; set; }
        public List<BackendDepartmentComplianceDto> DepartmentBreakdown { get; set; } = new();
        public List<BackendChapterComplianceDto> ChapterBreakdown { get; set; } = new();
    }

    private sealed class BackendDepartmentComplianceDto
    {
        public string Department { get; set; } = string.Empty;
        public decimal ComplianceScore { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
    }

    private sealed class BackendChapterComplianceDto
    {
        public Guid ChapterId { get; set; }
        public string ChapterCode { get; set; } = string.Empty;
        public string ChapterName { get; set; } = string.Empty;
        public decimal ComplianceScore { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
    }
}
