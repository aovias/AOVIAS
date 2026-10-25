using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Handlers;

public sealed class ListSlaPoliciesQueryHandler : IRequestHandler<ListSlaPoliciesQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    public ListSlaPoliciesQueryHandler(IProxyService proxy) => _proxy = proxy;

    public Task<IActionResult> Handle(ListSlaPoliciesQuery request, CancellationToken cancellationToken)
    {
        var hospital = request.HospitalId.HasValue ? $"&hospitalId={request.HospitalId}" : string.Empty;
        return _proxy.ProxyToComplianceServiceAsync(
            $"/internal/v1/sla/policies?organizationId={request.OrganizationId}{hospital}",
            HttpMethod.Get, null, null, cancellationToken);
    }
}

public sealed class CreateSlaPolicyCommandHandler : IRequestHandler<CreateSlaPolicyCommand, IActionResult>
{
    private readonly IProxyService _proxy;
    public CreateSlaPolicyCommandHandler(IProxyService proxy) => _proxy = proxy;

    public Task<IActionResult> Handle(CreateSlaPolicyCommand request, CancellationToken cancellationToken) =>
        _proxy.ProxyToComplianceServiceAsync(
            "/internal/v1/sla/policies",
            HttpMethod.Post, request.Body, null, cancellationToken);
}

public sealed class GetDeficiencySlaQueryHandler : IRequestHandler<GetDeficiencySlaQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    public GetDeficiencySlaQueryHandler(IProxyService proxy) => _proxy = proxy;

    public Task<IActionResult> Handle(GetDeficiencySlaQuery request, CancellationToken cancellationToken) =>
        _proxy.ProxyToComplianceServiceAsync(
            $"/internal/v1/sla/deficiencies/{request.DeficiencyId}",
            HttpMethod.Get, null, null, cancellationToken);
}

public sealed class GetDeficiencyEscalationsQueryHandler : IRequestHandler<GetDeficiencyEscalationsQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    public GetDeficiencyEscalationsQueryHandler(IProxyService proxy) => _proxy = proxy;

    public Task<IActionResult> Handle(GetDeficiencyEscalationsQuery request, CancellationToken cancellationToken) =>
        _proxy.ProxyToComplianceServiceAsync(
            $"/internal/v1/sla/deficiencies/{request.DeficiencyId}/escalations",
            HttpMethod.Get, null, null, cancellationToken);
}

public sealed class AcknowledgeEscalationCommandHandler : IRequestHandler<AcknowledgeEscalationCommand, IActionResult>
{
    private readonly IProxyService _proxy;
    public AcknowledgeEscalationCommandHandler(IProxyService proxy) => _proxy = proxy;

    public Task<IActionResult> Handle(AcknowledgeEscalationCommand request, CancellationToken cancellationToken) =>
        _proxy.ProxyToComplianceServiceAsync(
            $"/internal/v1/sla/deficiencies/{request.DeficiencyId}/acknowledge-escalation",
            HttpMethod.Post, request.Body, null, cancellationToken);
}

public sealed class GetSlaMetricsQueryHandler : IRequestHandler<GetSlaMetricsQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    public GetSlaMetricsQueryHandler(IProxyService proxy) => _proxy = proxy;

    public Task<IActionResult> Handle(GetSlaMetricsQuery request, CancellationToken cancellationToken) =>
        _proxy.ProxyToComplianceServiceAsync(
            $"/internal/v1/sla/metrics?organizationId={request.OrganizationId}",
            HttpMethod.Get, null, null, cancellationToken);
}
