using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Handlers;

public sealed class ListOeOwnershipQueryHandler : IRequestHandler<ListOeOwnershipQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    public ListOeOwnershipQueryHandler(IProxyService proxy) => _proxy = proxy;

    public Task<IActionResult> Handle(ListOeOwnershipQuery request, CancellationToken cancellationToken)
    {
        var framework = request.FrameworkId.HasValue && request.FrameworkId.Value != Guid.Empty
            ? $"?frameworkId={request.FrameworkId}"
            : string.Empty;

        return _proxy.ProxyToComplianceServiceAsync(
            $"/api/v{request.ApiVersion}/hospitals/{request.HospitalId}/oe-ownership{framework}",
            HttpMethod.Get, null, null, cancellationToken);
    }
}

public sealed class UpsertOeOwnershipCommandHandler : IRequestHandler<UpsertOeOwnershipCommand, IActionResult>
{
    private readonly IProxyService _proxy;
    public UpsertOeOwnershipCommandHandler(IProxyService proxy) => _proxy = proxy;

    public Task<IActionResult> Handle(UpsertOeOwnershipCommand request, CancellationToken cancellationToken) =>
        _proxy.ProxyToComplianceServiceAsync(
            $"/api/v{request.ApiVersion}/hospitals/{request.HospitalId}/oe-ownership",
            HttpMethod.Put, request.Body, null, cancellationToken);
}

public sealed class ClearOeOwnershipCommandHandler : IRequestHandler<ClearOeOwnershipCommand, IActionResult>
{
    private readonly IProxyService _proxy;
    public ClearOeOwnershipCommandHandler(IProxyService proxy) => _proxy = proxy;

    public Task<IActionResult> Handle(ClearOeOwnershipCommand request, CancellationToken cancellationToken) =>
        _proxy.ProxyToComplianceServiceAsync(
            $"/api/v{request.ApiVersion}/hospitals/{request.HospitalId}/oe-ownership/{request.ObjectiveElementId}",
            HttpMethod.Delete, null, null, cancellationToken);
}
