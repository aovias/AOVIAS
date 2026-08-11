using System.Net;
using System.Text.Json;
using Gateway.Application.Queries;
using Gateway.DTOs;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.DTO;

namespace Gateway.Application.Handlers
{
    public class GetDepartmentsQueryHandler
        : IRequestHandler<GetDepartmentsQuery, ApiResponse<IReadOnlyList<DepartmentDto>>>
    {
        private readonly IProxyService _proxyService;
        private readonly ILogger<GetDepartmentsQueryHandler> _logger;

        public GetDepartmentsQueryHandler(
            IProxyService proxyService,
            ILogger<GetDepartmentsQueryHandler> logger)
        {
            _proxyService = proxyService;
            _logger = logger;
        }

        public async Task<ApiResponse<IReadOnlyList<DepartmentDto>>> Handle(
            GetDepartmentsQuery request,
            CancellationToken cancellationToken)
        {
            var endpoint = "/api/v1/common/departments";

            var result = await _proxyService.ProxyToOrganizationServiceAsync(
                endpoint: endpoint,
                httpMethod: HttpMethod.Get,
                requestBody: null,
                cancellationToken: cancellationToken);

            if (result is ContentResult contentResult)
            {
                var statusCode = contentResult.StatusCode ?? (int)HttpStatusCode.OK;

                if (statusCode >= 400)
                {
                    _logger.LogError(
                        "OrganizationService returned error for departments. Status: {StatusCode}, Response: {Response}",
                        statusCode, contentResult.Content);

                    return ApiResponse<IReadOnlyList<DepartmentDto>>.ErrorResponse(
                        new ApiError
                        {
                            Code = "UPSTREAM_ERROR",
                            Message = "Failed to retrieve departments from OrganizationService.",
                            Status = statusCode,
                            Details = contentResult.Content
                        },
                        request.TraceId);
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                // OrganizationService returns standardized ApiResponse<T> envelope.
                // For backward compatibility, also support raw JSON arrays.
                var json = contentResult.Content ?? "[]";
                List<DepartmentDto> items;

                if (!string.IsNullOrWhiteSpace(json) && json.TrimStart().StartsWith("{", StringComparison.Ordinal))
                {
                    var envelope = JsonSerializer.Deserialize<ApiResponse<List<DepartmentDto>>>(json, options);
                    items = envelope?.Data ?? new List<DepartmentDto>();
                }
                else
                {
                    items = JsonSerializer.Deserialize<List<DepartmentDto>>(json, options) ?? new List<DepartmentDto>();
                }

                return ApiResponse<IReadOnlyList<DepartmentDto>>.SuccessResponse(items, request.TraceId);
            }

            if (result is ObjectResult objectResult)
            {
                _logger.LogError(
                    "OrganizationService proxy returned error for departments. Status: {StatusCode}, Value: {Value}",
                    objectResult.StatusCode,
                    objectResult.Value);

                return ApiResponse<IReadOnlyList<DepartmentDto>>.ErrorResponse(
                    new ApiError
                    {
                        Code = "UPSTREAM_UNAVAILABLE",
                        Message = "OrganizationService is unavailable.",
                        Status = objectResult.StatusCode ?? (int)HttpStatusCode.BadGateway,
                        Details = objectResult.Value?.ToString()
                    },
                    request.TraceId);
            }

            _logger.LogError("Unexpected response type when retrieving departments: {Type}", result?.GetType().Name ?? "null");

            return ApiResponse<IReadOnlyList<DepartmentDto>>.ErrorResponse(
                ApiError.InternalServerError("Unexpected response from OrganizationService."),
                request.TraceId);
        }
    }

    public class GetServicesQueryHandler
        : IRequestHandler<GetServicesQuery, ApiResponse<IReadOnlyList<ServiceDto>>>
    {
        private readonly IProxyService _proxyService;
        private readonly ILogger<GetServicesQueryHandler> _logger;

        public GetServicesQueryHandler(
            IProxyService proxyService,
            ILogger<GetServicesQueryHandler> logger)
        {
            _proxyService = proxyService;
            _logger = logger;
        }

        public async Task<ApiResponse<IReadOnlyList<ServiceDto>>> Handle(
            GetServicesQuery request,
            CancellationToken cancellationToken)
        {
            var endpoint = request.DepartmentId.HasValue
                ? $"/api/v1/common/services?departmentId={request.DepartmentId.Value}"
                : "/api/v1/common/services";

            var result = await _proxyService.ProxyToOrganizationServiceAsync(
                endpoint: endpoint,
                httpMethod: HttpMethod.Get,
                requestBody: null,
                cancellationToken: cancellationToken);

            if (result is ContentResult contentResult)
            {
                var statusCode = contentResult.StatusCode ?? (int)HttpStatusCode.OK;

                if (statusCode >= 400)
                {
                    _logger.LogError(
                        "OrganizationService returned error for services. Status: {StatusCode}, Response: {Response}",
                        statusCode, contentResult.Content);

                    return ApiResponse<IReadOnlyList<ServiceDto>>.ErrorResponse(
                        new ApiError
                        {
                            Code = "UPSTREAM_ERROR",
                            Message = "Failed to retrieve services from OrganizationService.",
                            Status = statusCode,
                            Details = contentResult.Content
                        },
                        request.TraceId);
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                // OrganizationService returns standardized ApiResponse<T> envelope.
                // For backward compatibility, also support raw JSON arrays.
                var json = contentResult.Content ?? "[]";
                List<ServiceDto> items;

                if (!string.IsNullOrWhiteSpace(json) && json.TrimStart().StartsWith("{", StringComparison.Ordinal))
                {
                    var envelope = JsonSerializer.Deserialize<ApiResponse<List<ServiceDto>>>(json, options);
                    items = envelope?.Data ?? new List<ServiceDto>();
                }
                else
                {
                    items = JsonSerializer.Deserialize<List<ServiceDto>>(json, options) ?? new List<ServiceDto>();
                }

                return ApiResponse<IReadOnlyList<ServiceDto>>.SuccessResponse(items, request.TraceId);
            }

            if (result is ObjectResult objectResult)
            {
                _logger.LogError(
                    "OrganizationService proxy returned error for services. Status: {StatusCode}, Value: {Value}",
                    objectResult.StatusCode,
                    objectResult.Value);

                return ApiResponse<IReadOnlyList<ServiceDto>>.ErrorResponse(
                    new ApiError
                    {
                        Code = "UPSTREAM_UNAVAILABLE",
                        Message = "OrganizationService is unavailable.",
                        Status = objectResult.StatusCode ?? (int)HttpStatusCode.BadGateway,
                        Details = objectResult.Value?.ToString()
                    },
                    request.TraceId);
            }

            _logger.LogError("Unexpected response type when retrieving services: {Type}", result?.GetType().Name ?? "null");

            return ApiResponse<IReadOnlyList<ServiceDto>>.ErrorResponse(
                ApiError.InternalServerError("Unexpected response from OrganizationService."),
                request.TraceId);
        }
    }

    public class GetMasterDepartmentsQueryHandler
        : IRequestHandler<GetMasterDepartmentsQuery, ApiResponse<IReadOnlyList<MasterDepartmentDto>>>
    {
        private readonly IProxyService _proxyService;
        private readonly ILogger<GetMasterDepartmentsQueryHandler> _logger;

        public GetMasterDepartmentsQueryHandler(
            IProxyService proxyService,
            ILogger<GetMasterDepartmentsQueryHandler> logger)
        {
            _proxyService = proxyService;
            _logger = logger;
        }

        public async Task<ApiResponse<IReadOnlyList<MasterDepartmentDto>>> Handle(
            GetMasterDepartmentsQuery request,
            CancellationToken cancellationToken)
        {
            var endpoint = "/api/v1/common/master-departments";

            var result = await _proxyService.ProxyToOrganizationServiceAsync(
                endpoint: endpoint,
                httpMethod: HttpMethod.Get,
                requestBody: null,
                cancellationToken: cancellationToken);

            if (result is ContentResult contentResult)
            {
                var statusCode = contentResult.StatusCode ?? (int)HttpStatusCode.OK;

                if (statusCode >= 400)
                {
                    _logger.LogError(
                        "OrganizationService returned error for master departments. Status: {StatusCode}, Response: {Response}",
                        statusCode, contentResult.Content);

                    return ApiResponse<IReadOnlyList<MasterDepartmentDto>>.ErrorResponse(
                        new ApiError
                        {
                            Code = "UPSTREAM_ERROR",
                            Message = "Failed to retrieve master departments from OrganizationService.",
                            Status = statusCode,
                            Details = contentResult.Content
                        },
                        request.TraceId);
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                // OrganizationService returns standardized ApiResponse<T> envelope.
                // For backward compatibility, also support raw JSON arrays.
                var json = contentResult.Content ?? "[]";
                List<MasterDepartmentDto> items;

                if (!string.IsNullOrWhiteSpace(json) && json.TrimStart().StartsWith("{", StringComparison.Ordinal))
                {
                    var envelope = JsonSerializer.Deserialize<ApiResponse<List<MasterDepartmentDto>>>(json, options);
                    items = envelope?.Data ?? new List<MasterDepartmentDto>();
                }
                else
                {
                    items = JsonSerializer.Deserialize<List<MasterDepartmentDto>>(json, options) ?? new List<MasterDepartmentDto>();
                }

                return ApiResponse<IReadOnlyList<MasterDepartmentDto>>.SuccessResponse(items, request.TraceId);
            }

            if (result is ObjectResult objectResult)
            {
                _logger.LogError(
                    "OrganizationService proxy returned error for master departments. Status: {StatusCode}, Value: {Value}",
                    objectResult.StatusCode,
                    objectResult.Value);

                return ApiResponse<IReadOnlyList<MasterDepartmentDto>>.ErrorResponse(
                    new ApiError
                    {
                        Code = "UPSTREAM_UNAVAILABLE",
                        Message = "OrganizationService is unavailable.",
                        Status = objectResult.StatusCode ?? (int)HttpStatusCode.BadGateway,
                        Details = objectResult.Value?.ToString()
                    },
                    request.TraceId);
            }

            _logger.LogError("Unexpected response type when retrieving master departments: {Type}", result?.GetType().Name ?? "null");

            return ApiResponse<IReadOnlyList<MasterDepartmentDto>>.ErrorResponse(
                ApiError.InternalServerError("Unexpected response from OrganizationService."),
                request.TraceId);
        }
    }
}
