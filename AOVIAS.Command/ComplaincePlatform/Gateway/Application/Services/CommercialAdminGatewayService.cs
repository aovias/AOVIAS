using System.Security.Claims;
using System.Text.Json;
using Gateway.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Auth;

namespace Gateway.Application.Services;

/// <summary>CRP Phase 4 — shared helpers for commercial-admin Gateway façades.</summary>
public sealed class CommercialAdminGatewayService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly IProxyService _proxy;
    private readonly IHttpContextAccessor _http;
    private readonly ILogger<CommercialAdminGatewayService> _logger;

    public CommercialAdminGatewayService(
        IProxyService proxy,
        IHttpContextAccessor http,
        ILogger<CommercialAdminGatewayService> logger)
    {
        _proxy = proxy;
        _http = http;
        _logger = logger;
    }

    public ClaimsPrincipal User => _http.HttpContext?.User ?? new ClaimsPrincipal();

    public string TraceId => _http.HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString("N");

    public Guid? OperatorUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    public string OperatorEmail()
        => User.FindFirstValue(ClaimTypes.Email)
           ?? User.FindFirstValue("email")
           ?? User.Identity?.Name
           ?? string.Empty;

    public IReadOnlyList<string> OperatorRoles()
        => User.FindAll(ClaimTypes.Role).Select(c => c.Value)
            .Concat(User.FindAll("role").Select(c => c.Value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    public bool HasPermission(string permission)
        => CommercialPermissions.AnyRoleHasPermission(OperatorRoles(), permission);

    public IActionResult? ForbidUnless(string permission)
        => HasPermission(permission) ? null : new ForbidResult();

    public static string Serialize(object? value)
        => value == null ? "null" : JsonSerializer.Serialize(value, JsonOpts);

    public async Task RecordAuditAsync(
        Guid? organizationId,
        string targetType,
        string? targetId,
        string action,
        object? previous,
        object? next,
        string reason,
        CancellationToken cancellationToken)
    {
        try
        {
            await _proxy.ProxyToOrganizationServiceAsync(
                "/internal/commercial-admin/audit",
                HttpMethod.Post,
                new
                {
                    OrganizationId = organizationId,
                    OperatorUserId = OperatorUserId(),
                    OperatorEmail = OperatorEmail(),
                    TargetType = targetType,
                    TargetId = targetId,
                    Action = action,
                    PreviousJson = Serialize(previous),
                    NewJson = Serialize(next),
                    Reason = reason,
                    CorrelationId = TraceId
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist commercial admin audit for {Action}", action);
        }
    }

    public Task<IActionResult> ProxyOrgAsync(string path, HttpMethod method, object? body, CancellationToken ct)
        => _proxy.ProxyToOrganizationServiceAsync(path, method, body, ct);

    public Task<IActionResult> ProxyIdentityAsync(string path, HttpMethod method, object? body, CancellationToken ct)
        => _proxy.ProxyToIdentityServiceAsync(path, method, body, ct);
}
