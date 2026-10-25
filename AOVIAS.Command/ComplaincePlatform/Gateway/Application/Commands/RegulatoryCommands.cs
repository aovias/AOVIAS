using Gateway.DTOs;
using MediatR;
using Shared.Infrastructure.DTO;

namespace Gateway.Application.Commands;

/// <summary>
/// Command to update primary jurisdiction of an organization.
/// Business rules (e.g., preventing changes when active accreditations exist)
/// are enforced by ComplianceService; Gateway validates the contract and proxies the request.
/// </summary>
public sealed record UpdateJurisdictionCommand(
    Guid OrgId,
    UpdateJurisdictionRequestDto Request,
    string TraceId)
    : IRequest<Shared.Infrastructure.DTO.ApiResponse<UpdateJurisdictionResponseDto>>;

/// <summary>
/// Command to activate a new accreditation for an organization.
/// Includes idempotency key to prevent duplicate activations on retries.
/// </summary>
public sealed record ActivateAccreditationCommand(
    Guid OrgId,
    ActivateAccreditationRequestDto Request,
    string TraceId,
    string? IdempotencyKey = null)
    : IRequest<Shared.Infrastructure.DTO.ApiResponse<ActivateAccreditationResponseDto>>;

/// <summary>
/// Command to suspend an accreditation.
/// Includes RowVersion/ETag for optimistic concurrency control to prevent race conditions.
/// </summary>
public sealed record SuspendAccreditationCommand(
    Guid OrgId,
    Guid AccreditationId,
    SuspendAccreditationRequestDto Request,
    string TraceId,
    string? IfMatch = null)
    : IRequest<Shared.Infrastructure.DTO.ApiResponse<AccreditationLifecycleChangeResponseDto>>;

/// <summary>
/// Command to archive an accreditation.
/// Includes RowVersion/ETag for optimistic concurrency control to prevent race conditions.
/// </summary>
public sealed record ArchiveAccreditationCommand(
    Guid OrgId,
    Guid AccreditationId,
    string TraceId,
    string? IfMatch = null,
    string? CurrentStatus = null)
    : IRequest<Shared.Infrastructure.DTO.ApiResponse<AccreditationLifecycleChangeResponseDto>>;

/// <summary>
/// Command to trigger background risk recalculation for an organization.
/// Includes idempotency key to prevent duplicate job scheduling when UI retries.
/// </summary>
public sealed record RecalculateRiskCommand(
    Guid OrgId,
    string TraceId,
    string? IdempotencyKey = null)
    : IRequest<Shared.Infrastructure.DTO.ApiResponse<RiskRecalculationResponseDto>>;

