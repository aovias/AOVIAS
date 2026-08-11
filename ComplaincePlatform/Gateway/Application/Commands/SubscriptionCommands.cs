using Gateway.DTOs;
using MediatR;
using Shared.Infrastructure.DTO;

namespace Gateway.Application.Commands;

public sealed record CreateSubscriptionCommand(CreateSubscriptionRequestDto Request)
    : IRequest<ApiResponse<OrganizationSubscriptionSummaryDto>>;

public sealed record UpgradeSubscriptionCommand(UpgradeSubscriptionRequestDto Request)
    : IRequest<ApiResponse<OrganizationSubscriptionSummaryDto>>;

