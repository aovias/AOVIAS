using Gateway.Application.Commands;
using Gateway.Application.Queries;
using Gateway.DTOs;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.DTO;
using System.Net;
using System.Text.Json;

namespace Gateway.Application.Handlers;

public class GetCurrentSubscriptionQueryHandler
    : IRequestHandler<GetCurrentSubscriptionQuery, ApiResponse<OrganizationSubscriptionSummaryDto>>
{
    private readonly IProxyService _proxyService;

    public GetCurrentSubscriptionQueryHandler(IProxyService proxyService)
    {
        _proxyService = proxyService;
    }

    public async Task<ApiResponse<OrganizationSubscriptionSummaryDto>> Handle(
        GetCurrentSubscriptionQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _proxyService.ProxyToOrganizationServiceAsync(
            endpoint: $"/internal/subscriptions/current?organizationId={request.OrganizationId}",
            httpMethod: HttpMethod.Get,
            requestBody: null,
            cancellationToken: cancellationToken);

        if (result is not ContentResult contentResult)
        {
            return ApiResponse<OrganizationSubscriptionSummaryDto>.ErrorResponse(
                new ApiError
                {
                    Code = "UPSTREAM_UNEXPECTED_RESPONSE",
                    Message = "Unexpected response from OrganizationService",
                    Status = (int)HttpStatusCode.BadGateway
                },
                string.Empty);
        }

        if (contentResult.StatusCode == (int)HttpStatusCode.NotFound)
        {
            return ApiResponse<OrganizationSubscriptionSummaryDto>.ErrorResponse(
                new ApiError
                {
                    Code = "SUBSCRIPTION_NOT_FOUND",
                    Message = "No active subscription found",
                    Status = (int)HttpStatusCode.NotFound
                },
                string.Empty);
        }

        if (contentResult.StatusCode != (int)HttpStatusCode.OK || string.IsNullOrEmpty(contentResult.Content))
        {
            return ApiResponse<OrganizationSubscriptionSummaryDto>.ErrorResponse(
                new ApiError
                {
                    Code = "UPSTREAM_ERROR",
                    Message = $"Failed to get current subscription (status {contentResult.StatusCode})",
                    Status = (int)HttpStatusCode.BadGateway,
                    Details = contentResult.Content
                },
                string.Empty);
        }

        var element = JsonSerializer.Deserialize<JsonElement>(contentResult.Content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var response = MapToSummaryDto(element);
        return ApiResponse<OrganizationSubscriptionSummaryDto>.SuccessResponse(response, string.Empty);
    }

    internal static OrganizationSubscriptionSummaryDto MapToSummaryDto(JsonElement element)
    {
        var plan = new SubscriptionPlanDto();
        if (element.TryGetProperty("subscriptionPlanId", out var planIdProp))
        {
            plan.Id = planIdProp.GetGuid();
        }

        return new OrganizationSubscriptionSummaryDto
        {
            SubscriptionId = element.GetProperty("id").GetGuid(),
            OrganizationId = element.GetProperty("organizationId").GetGuid(),
            StartsAt = element.GetProperty("startsAt").GetDateTime(),
            EndsAt = element.TryGetProperty("endsAt", out var endsAtProp) && endsAtProp.ValueKind != JsonValueKind.Null
                ? endsAtProp.GetDateTime()
                : (DateTime?)null,
            IsTrial = element.GetProperty("isTrial").GetBoolean(),
            IsActive = element.GetProperty("isActive").GetBoolean(),
            CurrentUserCount = element.GetProperty("currentUserCount").GetInt32(),
            CurrentFrameworkCount = element.GetProperty("currentFrameworkCount").GetInt32(),
            Plan = plan
        };
    }
}

public class CreateSubscriptionCommandHandler
    : IRequestHandler<CreateSubscriptionCommand, ApiResponse<OrganizationSubscriptionSummaryDto>>
{
    private readonly IProxyService _proxyService;

    public CreateSubscriptionCommandHandler(IProxyService proxyService)
    {
        _proxyService = proxyService;
    }

    public async Task<ApiResponse<OrganizationSubscriptionSummaryDto>> Handle(
        CreateSubscriptionCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _proxyService.ProxyToOrganizationServiceAsync(
            endpoint: "/internal/subscriptions",
            httpMethod: HttpMethod.Post,
            requestBody: new
            {
                request.Request.OrganizationId,
                SubscriptionPlanId = request.Request.SubscriptionPlanId,
                request.Request.IsTrial
            },
            cancellationToken: cancellationToken);

        if (result is not ContentResult contentResult ||
            string.IsNullOrEmpty(contentResult.Content) ||
            contentResult.StatusCode is null)
        {
            return ApiResponse<OrganizationSubscriptionSummaryDto>.ErrorResponse(
                new ApiError
                {
                    Code = "UPSTREAM_ERROR",
                    Message = "Failed to assign subscription",
                    Status = (int)HttpStatusCode.BadGateway
                },
                string.Empty);
        }

        if (contentResult.StatusCode >= 400)
        {
            return ApiResponse<OrganizationSubscriptionSummaryDto>.ErrorResponse(
                new ApiError
                {
                    Code = "UPSTREAM_ERROR",
                    Message = contentResult.Content,
                    Status = contentResult.StatusCode.Value
                },
                string.Empty);
        }

        var element = JsonSerializer.Deserialize<JsonElement>(contentResult.Content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var response = GetCurrentSubscriptionQueryHandler.MapToSummaryDto(element);
        return ApiResponse<OrganizationSubscriptionSummaryDto>.SuccessResponse(response, string.Empty);
    }
}

public class UpgradeSubscriptionCommandHandler
    : IRequestHandler<UpgradeSubscriptionCommand, ApiResponse<OrganizationSubscriptionSummaryDto>>
{
    private readonly IProxyService _proxyService;

    public UpgradeSubscriptionCommandHandler(IProxyService proxyService)
    {
        _proxyService = proxyService;
    }

    public async Task<ApiResponse<OrganizationSubscriptionSummaryDto>> Handle(
        UpgradeSubscriptionCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _proxyService.ProxyToOrganizationServiceAsync(
            endpoint: "/internal/subscriptions/upgrade",
            httpMethod: HttpMethod.Patch,
            requestBody: new
            {
                request.Request.OrganizationId,
                TargetPlanId = request.Request.TargetPlanId
            },
            cancellationToken: cancellationToken);

        if (result is not ContentResult contentResult ||
            string.IsNullOrEmpty(contentResult.Content) ||
            contentResult.StatusCode is null)
        {
            return ApiResponse<OrganizationSubscriptionSummaryDto>.ErrorResponse(
                new ApiError
                {
                    Code = "UPSTREAM_ERROR",
                    Message = "Failed to upgrade subscription",
                    Status = (int)HttpStatusCode.BadGateway
                },
                string.Empty);
        }

        if (contentResult.StatusCode >= 400)
        {
            return ApiResponse<OrganizationSubscriptionSummaryDto>.ErrorResponse(
                new ApiError
                {
                    Code = "UPSTREAM_ERROR",
                    Message = contentResult.Content,
                    Status = contentResult.StatusCode.Value
                },
                string.Empty);
        }

        var element = JsonSerializer.Deserialize<JsonElement>(contentResult.Content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var response = GetCurrentSubscriptionQueryHandler.MapToSummaryDto(element);
        return ApiResponse<OrganizationSubscriptionSummaryDto>.SuccessResponse(response, string.Empty);
    }
}

