using Gateway.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public sealed record GetPlatformActivityQuery(
    string ApiVersion,
    Guid OrganizationId,
    string? Search,
    DateTime? From,
    DateTime? To,
    string? Module,
    string? Category,
    string? Action,
    string? UserId,
    string? Status,
    string? Service,
    string? Sort,
    int Page,
    int PageSize) : IRequest<IActionResult>;
