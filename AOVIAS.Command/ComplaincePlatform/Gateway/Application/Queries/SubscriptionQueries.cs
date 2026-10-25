using Gateway.DTOs;
using MediatR;
using Shared.Infrastructure.DTO;

namespace Gateway.Application.Queries;

public sealed record GetCurrentSubscriptionQuery(Guid OrganizationId)
    : IRequest<ApiResponse<OrganizationSubscriptionSummaryDto>>;

