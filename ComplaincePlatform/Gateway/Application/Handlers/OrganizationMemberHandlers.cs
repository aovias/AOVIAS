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

internal sealed class OrganizationServiceMemberEnvelopeDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
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

public sealed class CreateOrganizationMemberCommandHandler : IRequestHandler<CreateOrganizationMemberCommand, OrganizationMemberResponseDto>
{
    private readonly IProxyService _proxyService;
    public CreateOrganizationMemberCommandHandler(IProxyService proxyService) => _proxyService = proxyService;

    public async Task<OrganizationMemberResponseDto> Handle(CreateOrganizationMemberCommand request, CancellationToken cancellationToken)
    {
        var result = await _proxyService.ProxyToOrganizationServiceAsync(
            endpoint: "/internal/organization-members",
            httpMethod: HttpMethod.Post,
            requestBody: request.Request,
            cancellationToken: cancellationToken);

        return await ParseMemberEnvelope(result, "create");
    }

    internal static OrganizationMemberResponseDto Map(OrganizationServiceMemberEnvelopeDto member) => new()
    {
        Id = member.Id,
        OrganizationId = member.OrganizationId,
        UserId = member.UserId,
        DepartmentId = member.DepartmentId,
        DepartmentName = member.DepartmentName,
        Designation = member.Designation,
        ReportsToUserId = member.ReportsToUserId,
        Role = member.Role,
        FirstName = member.FirstName,
        LastName = member.LastName,
        Email = member.Email,
        PhoneNumber = member.PhoneNumber,
        AlternatePhoneNumber = member.AlternatePhoneNumber,
        ProfilePicUrl = member.ProfilePicUrl,
        Preferences = member.Preferences,
        CreatedAt = member.CreatedAt
    };

    internal static async Task<OrganizationMemberResponseDto> ParseMemberEnvelope(IActionResult result, string action)
    {
        if (result is not ContentResult contentResult)
        {
            throw new HttpRequestException("Unexpected response type from OrganizationService.");
        }

        var statusCode = contentResult.StatusCode ?? 0;
        var body = contentResult.Content ?? string.Empty;
        if (statusCode == (int)HttpStatusCode.NotFound)
        {
            throw new KeyNotFoundException("Organization member was not found.");
        }
        if (statusCode >= (int)HttpStatusCode.BadRequest)
        {
            throw new HttpRequestException($"OrganizationService {action} organization member failed with status {statusCode}: {body}");
        }

        var envelope = JsonSerializer.Deserialize<ApiResponse<OrganizationServiceMemberEnvelopeDto>>(
            body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (envelope?.Success != true || envelope.Data == null)
        {
            throw new InvalidOperationException($"Failed to {action} organization member via OrganizationService.");
        }

        return Map(envelope.Data);
    }
}

public sealed class UpdateOrganizationMemberCommandHandler : IRequestHandler<UpdateOrganizationMemberCommand, OrganizationMemberResponseDto>
{
    private readonly IProxyService _proxyService;
    public UpdateOrganizationMemberCommandHandler(IProxyService proxyService) => _proxyService = proxyService;

    public async Task<OrganizationMemberResponseDto> Handle(UpdateOrganizationMemberCommand request, CancellationToken cancellationToken)
    {
        var result = await _proxyService.ProxyToOrganizationServiceAsync(
            endpoint: $"/internal/organization-members/{request.MemberId}",
            httpMethod: HttpMethod.Put,
            requestBody: request.Request,
            cancellationToken: cancellationToken);

        return await CreateOrganizationMemberCommandHandler.ParseMemberEnvelope(result, "update");
    }
}

public sealed class GetOrganizationMemberQueryHandler : IRequestHandler<GetOrganizationMemberQuery, OrganizationMemberResponseDto>
{
    private readonly IProxyService _proxyService;
    public GetOrganizationMemberQueryHandler(IProxyService proxyService) => _proxyService = proxyService;

    public async Task<OrganizationMemberResponseDto> Handle(GetOrganizationMemberQuery request, CancellationToken cancellationToken)
    {
        var result = await _proxyService.ProxyToOrganizationServiceAsync(
            endpoint: $"/internal/organization-members/{request.MemberId}",
            httpMethod: HttpMethod.Get,
            requestBody: null,
            cancellationToken: cancellationToken);

        return await CreateOrganizationMemberCommandHandler.ParseMemberEnvelope(result, "get");
    }
}

public sealed class ListOrganizationMembersQueryHandler : IRequestHandler<ListOrganizationMembersQuery, List<OrganizationMemberResponseDto>>
{
    private readonly IProxyService _proxyService;
    public ListOrganizationMembersQueryHandler(IProxyService proxyService) => _proxyService = proxyService;

    public async Task<List<OrganizationMemberResponseDto>> Handle(ListOrganizationMembersQuery request, CancellationToken cancellationToken)
    {
        var result = await _proxyService.ProxyToOrganizationServiceAsync(
            endpoint: $"/internal/organizations/{request.OrganizationId}/organization-members",
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
            throw new HttpRequestException($"OrganizationService list organization members failed with status {statusCode}: {body}");
        }

        var envelope = JsonSerializer.Deserialize<ApiResponse<List<OrganizationServiceMemberEnvelopeDto>>>(
            body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (envelope?.Success != true || envelope.Data == null)
        {
            return new List<OrganizationMemberResponseDto>();
        }

        return envelope.Data.Select(CreateOrganizationMemberCommandHandler.Map).ToList();
    }
}
