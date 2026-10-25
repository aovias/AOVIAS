using System.Net;
using System.Text.Json;
using Gateway.Application.Commands;
using Gateway.Application.Queries;
using Gateway.DTOs;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.DTO;

namespace Gateway.Application.Handlers;

internal sealed class OrganizationServiceHospitalEnvelopeDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
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
    public DateTime? UpdatedAt { get; set; }
}

public sealed class CreateHospitalCommandHandler : IRequestHandler<CreateHospitalCommand, HospitalResponseDto>
{
    private readonly IProxyService _proxyService;
    private readonly ILogger<CreateHospitalCommandHandler> _logger;

    public CreateHospitalCommandHandler(IProxyService proxyService, ILogger<CreateHospitalCommandHandler> logger)
    {
        _proxyService = proxyService;
        _logger = logger;
    }

    public async Task<HospitalResponseDto> Handle(CreateHospitalCommand request, CancellationToken cancellationToken)
    {
        var result = await _proxyService.ProxyToOrganizationServiceAsync(
            endpoint: "/internal/hospitals",
            httpMethod: HttpMethod.Post,
            requestBody: request.Request,
            cancellationToken: cancellationToken);

        if (result is not ContentResult contentResult)
        {
            throw new HttpRequestException("Unexpected response type from OrganizationService.");
        }

        var statusCode = contentResult.StatusCode ?? 0;
        var body = contentResult.Content ?? string.Empty;

        if (statusCode >= (int)HttpStatusCode.BadRequest)
        {
            _logger.LogError("OrganizationService CreateHospital failed. Status={StatusCode}, Body={Body}", statusCode, body);
            throw new HttpRequestException($"OrganizationService returned error status {statusCode}: {body}");
        }

        var envelope = JsonSerializer.Deserialize<ApiResponse<OrganizationServiceHospitalEnvelopeDto>>(
            body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (envelope == null || !envelope.Success || envelope.Data == null)
        {
            throw new InvalidOperationException("Failed to create hospital via OrganizationService.");
        }

        return Map(envelope.Data);
    }

    internal static HospitalResponseDto Map(OrganizationServiceHospitalEnvelopeDto h) => new()
    {
        Id = h.Id,
        OrganizationId = h.OrganizationId,
        Name = h.Name,
        Code = h.Code,
        Address = h.Address,
        Type = h.Type,
        BedCapacity = h.BedCapacity,
        CountryCode = h.CountryCode,
        StateCode = h.StateCode,
        IsOnboarded = h.IsOnboarded,
        NABHLevel = h.NABHLevel,
        CreatedAt = h.CreatedAt,
        UpdatedAt = h.UpdatedAt
    };
}

public sealed class UpdateHospitalCommandHandler : IRequestHandler<UpdateHospitalCommand, HospitalResponseDto>
{
    private readonly IProxyService _proxyService;
    private readonly ILogger<UpdateHospitalCommandHandler> _logger;

    public UpdateHospitalCommandHandler(IProxyService proxyService, ILogger<UpdateHospitalCommandHandler> logger)
    {
        _proxyService = proxyService;
        _logger = logger;
    }

    public async Task<HospitalResponseDto> Handle(UpdateHospitalCommand request, CancellationToken cancellationToken)
    {
        var result = await _proxyService.ProxyToOrganizationServiceAsync(
            endpoint: $"/internal/hospitals/{request.HospitalId}",
            httpMethod: HttpMethod.Put,
            requestBody: request.Request,
            cancellationToken: cancellationToken);

        if (result is not ContentResult contentResult)
        {
            throw new HttpRequestException("Unexpected response type from OrganizationService.");
        }

        var statusCode = contentResult.StatusCode ?? 0;
        var body = contentResult.Content ?? string.Empty;

        if (statusCode == (int)HttpStatusCode.NotFound)
        {
            throw new KeyNotFoundException($"Hospital with ID {request.HospitalId} was not found.");
        }

        if (statusCode >= (int)HttpStatusCode.BadRequest)
        {
            _logger.LogError("OrganizationService UpdateHospital failed. Status={StatusCode}, Body={Body}", statusCode, body);
            throw new HttpRequestException($"OrganizationService returned error status {statusCode}: {body}");
        }

        var envelope = JsonSerializer.Deserialize<ApiResponse<OrganizationServiceHospitalEnvelopeDto>>(
            body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (envelope == null || !envelope.Success || envelope.Data == null)
        {
            throw new InvalidOperationException("Failed to update hospital via OrganizationService.");
        }

        return CreateHospitalCommandHandler.Map(envelope.Data);
    }
}

public sealed class GetHospitalQueryHandler : IRequestHandler<GetHospitalQuery, HospitalResponseDto>
{
    private readonly IProxyService _proxyService;
    private readonly ILogger<GetHospitalQueryHandler> _logger;

    public GetHospitalQueryHandler(IProxyService proxyService, ILogger<GetHospitalQueryHandler> logger)
    {
        _proxyService = proxyService;
        _logger = logger;
    }

    public async Task<HospitalResponseDto> Handle(GetHospitalQuery request, CancellationToken cancellationToken)
    {
        var result = await _proxyService.ProxyToOrganizationServiceAsync(
            endpoint: $"/internal/hospitals/{request.HospitalId}",
            httpMethod: HttpMethod.Get,
            requestBody: null,
            cancellationToken: cancellationToken);

        if (result is not ContentResult contentResult)
        {
            throw new HttpRequestException("Unexpected response type from OrganizationService.");
        }

        var statusCode = contentResult.StatusCode ?? 0;
        var body = contentResult.Content ?? string.Empty;

        if (statusCode == (int)HttpStatusCode.NotFound)
        {
            throw new KeyNotFoundException($"Hospital with ID {request.HospitalId} was not found.");
        }

        if (statusCode >= (int)HttpStatusCode.BadRequest)
        {
            _logger.LogError("OrganizationService GetHospital failed. Status={StatusCode}, Body={Body}", statusCode, body);
            throw new HttpRequestException($"OrganizationService returned error status {statusCode}: {body}");
        }

        var envelope = JsonSerializer.Deserialize<ApiResponse<OrganizationServiceHospitalEnvelopeDto>>(
            body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (envelope == null || !envelope.Success || envelope.Data == null)
        {
            throw new InvalidOperationException("Failed to retrieve hospital from OrganizationService.");
        }

        return CreateHospitalCommandHandler.Map(envelope.Data);
    }
}

public sealed class ListHospitalsByOrganizationQueryHandler
    : IRequestHandler<ListHospitalsByOrganizationQuery, IReadOnlyList<HospitalResponseDto>>
{
    private readonly IProxyService _proxyService;
    private readonly ILogger<ListHospitalsByOrganizationQueryHandler> _logger;

    public ListHospitalsByOrganizationQueryHandler(
        IProxyService proxyService,
        ILogger<ListHospitalsByOrganizationQueryHandler> logger)
    {
        _proxyService = proxyService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<HospitalResponseDto>> Handle(
        ListHospitalsByOrganizationQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _proxyService.ProxyToOrganizationServiceAsync(
            endpoint: $"/internal/organizations/{request.OrganizationId}/hospitals",
            httpMethod: HttpMethod.Get,
            requestBody: null,
            cancellationToken: cancellationToken);

        if (result is not ContentResult contentResult)
        {
            throw new HttpRequestException("Unexpected response type from OrganizationService.");
        }

        var statusCode = contentResult.StatusCode ?? 0;
        var body = contentResult.Content ?? string.Empty;

        if (statusCode >= (int)HttpStatusCode.BadRequest)
        {
            _logger.LogError(
                "OrganizationService ListHospitals failed. Status={StatusCode}, Body={Body}",
                statusCode,
                body);
            throw new HttpRequestException($"OrganizationService returned error status {statusCode}: {body}");
        }

        var envelope = JsonSerializer.Deserialize<ApiResponse<List<OrganizationServiceHospitalEnvelopeDto>>>(
            body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (envelope == null || !envelope.Success || envelope.Data == null)
        {
            return Array.Empty<HospitalResponseDto>();
        }

        return envelope.Data.Select(CreateHospitalCommandHandler.Map).ToList();
    }
}

