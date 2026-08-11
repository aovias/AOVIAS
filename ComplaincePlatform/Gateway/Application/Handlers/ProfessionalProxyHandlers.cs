using Gateway.Application.Commands;
using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Handlers;

public sealed class ListProfessionalProfilesQueryHandler : IRequestHandler<ListProfessionalProfilesQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    public ListProfessionalProfilesQueryHandler(IProxyService proxy) => _proxy = proxy;
    public Task<IActionResult> Handle(ListProfessionalProfilesQuery request, CancellationToken cancellationToken)
    {
        var q = $"?page={request.Page}&pageSize={request.PageSize}";
        if (request.CategoryValueId.HasValue) q += $"&categoryValueId={request.CategoryValueId}";
        if (request.SpecialtyValueId.HasValue) q += $"&specialtyValueId={request.SpecialtyValueId}";
        return _proxy.ProxyToProfessionalServiceAsync($"/api/v{request.ApiVersion}/professional-profiles{q}", HttpMethod.Get, cancellationToken: cancellationToken);
    }
}

public sealed class SearchProfessionalProfilesQueryHandler : IRequestHandler<SearchProfessionalProfilesQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    public SearchProfessionalProfilesQueryHandler(IProxyService proxy) => _proxy = proxy;
    public Task<IActionResult> Handle(SearchProfessionalProfilesQuery request, CancellationToken cancellationToken)
    {
        var q = $"?q={Uri.EscapeDataString(request.Query)}&page={request.Page}&pageSize={request.PageSize}";
        return _proxy.ProxyToProfessionalServiceAsync($"/api/v{request.ApiVersion}/professional-profiles/search{q}", HttpMethod.Get, cancellationToken: cancellationToken);
    }
}

public sealed class GetProfessionalProfileByIdQueryHandler : IRequestHandler<GetProfessionalProfileByIdQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    public GetProfessionalProfileByIdQueryHandler(IProxyService proxy) => _proxy = proxy;
    public Task<IActionResult> Handle(GetProfessionalProfileByIdQuery request, CancellationToken cancellationToken)
        => _proxy.ProxyToProfessionalServiceAsync($"/api/v{request.ApiVersion}/professional-profiles/{request.ProfileId}", HttpMethod.Get, cancellationToken: cancellationToken);
}

public sealed class GetProfessionalTimelineQueryHandler : IRequestHandler<GetProfessionalTimelineQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    public GetProfessionalTimelineQueryHandler(IProxyService proxy) => _proxy = proxy;
    public Task<IActionResult> Handle(GetProfessionalTimelineQuery request, CancellationToken cancellationToken)
        => _proxy.ProxyToProfessionalServiceAsync($"/api/v{request.ApiVersion}/professional-profiles/{request.ProfileId}/timeline", HttpMethod.Get, cancellationToken: cancellationToken);
}

public sealed class GetProfessionalRenewalsQueryHandler : IRequestHandler<GetProfessionalRenewalsQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    public GetProfessionalRenewalsQueryHandler(IProxyService proxy) => _proxy = proxy;
    public Task<IActionResult> Handle(GetProfessionalRenewalsQuery request, CancellationToken cancellationToken)
    {
        var q = $"?withinDays={request.WithinDays}";
        if (request.OrganizationId.HasValue) q += $"&organizationId={request.OrganizationId}";
        return _proxy.ProxyToProfessionalServiceAsync($"/api/v{request.ApiVersion}/professional-profiles/renewals{q}", HttpMethod.Get, cancellationToken: cancellationToken);
    }
}

public sealed class GetProfessionalLookupsQueryHandler : IRequestHandler<GetProfessionalLookupsQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    public GetProfessionalLookupsQueryHandler(IProxyService proxy) => _proxy = proxy;
    public Task<IActionResult> Handle(GetProfessionalLookupsQuery request, CancellationToken cancellationToken)
        => _proxy.ProxyToProfessionalServiceAsync($"/api/v{request.ApiVersion}/professional-profiles/lookups/{Uri.EscapeDataString(request.Category)}", HttpMethod.Get, cancellationToken: cancellationToken);
}

public sealed class CreateProfessionalProfileCommandHandler : IRequestHandler<CreateProfessionalProfileCommand, IActionResult>
{
    private readonly IProxyService _proxy;
    public CreateProfessionalProfileCommandHandler(IProxyService proxy) => _proxy = proxy;
    public Task<IActionResult> Handle(CreateProfessionalProfileCommand request, CancellationToken cancellationToken)
        => _proxy.ProxyToProfessionalServiceAsync($"/api/v{request.ApiVersion}/professional-profiles", HttpMethod.Post, request.RequestBody, cancellationToken: cancellationToken);
}

public sealed class UpdateProfessionalProfileCommandHandler : IRequestHandler<UpdateProfessionalProfileCommand, IActionResult>
{
    private readonly IProxyService _proxy;
    public UpdateProfessionalProfileCommandHandler(IProxyService proxy) => _proxy = proxy;
    public Task<IActionResult> Handle(UpdateProfessionalProfileCommand request, CancellationToken cancellationToken)
        => _proxy.ProxyToProfessionalServiceAsync($"/api/v{request.ApiVersion}/professional-profiles/{request.ProfileId}", HttpMethod.Put, request.RequestBody, cancellationToken: cancellationToken);
}

public sealed class DeleteProfessionalProfileCommandHandler : IRequestHandler<DeleteProfessionalProfileCommand, IActionResult>
{
    private readonly IProxyService _proxy;
    public DeleteProfessionalProfileCommandHandler(IProxyService proxy) => _proxy = proxy;
    public Task<IActionResult> Handle(DeleteProfessionalProfileCommand request, CancellationToken cancellationToken)
        => _proxy.ProxyToProfessionalServiceAsync($"/api/v{request.ApiVersion}/professional-profiles/{request.ProfileId}", HttpMethod.Delete, cancellationToken: cancellationToken);
}

public sealed class AddQualificationCommandHandler : IRequestHandler<AddQualificationCommand, IActionResult>
{
    private readonly IProxyService _proxy;
    public AddQualificationCommandHandler(IProxyService proxy) => _proxy = proxy;
    public Task<IActionResult> Handle(AddQualificationCommand request, CancellationToken cancellationToken)
        => _proxy.ProxyToProfessionalServiceAsync($"/api/v{request.ApiVersion}/professional-profiles/{request.ProfileId}/qualifications", HttpMethod.Post, request.RequestBody, cancellationToken: cancellationToken);
}

public sealed class AddRegistrationCommandHandler : IRequestHandler<AddRegistrationCommand, IActionResult>
{
    private readonly IProxyService _proxy;
    public AddRegistrationCommandHandler(IProxyService proxy) => _proxy = proxy;
    public Task<IActionResult> Handle(AddRegistrationCommand request, CancellationToken cancellationToken)
        => _proxy.ProxyToProfessionalServiceAsync($"/api/v{request.ApiVersion}/professional-profiles/{request.ProfileId}/registrations", HttpMethod.Post, request.RequestBody, cancellationToken: cancellationToken);
}

public sealed class AddLicenseCommandHandler : IRequestHandler<AddLicenseCommand, IActionResult>
{
    private readonly IProxyService _proxy;
    public AddLicenseCommandHandler(IProxyService proxy) => _proxy = proxy;
    public Task<IActionResult> Handle(AddLicenseCommand request, CancellationToken cancellationToken)
        => _proxy.ProxyToProfessionalServiceAsync($"/api/v{request.ApiVersion}/professional-profiles/{request.ProfileId}/licenses", HttpMethod.Post, request.RequestBody, cancellationToken: cancellationToken);
}

public sealed class AddCertificationCommandHandler : IRequestHandler<AddCertificationCommand, IActionResult>
{
    private readonly IProxyService _proxy;
    public AddCertificationCommandHandler(IProxyService proxy) => _proxy = proxy;
    public Task<IActionResult> Handle(AddCertificationCommand request, CancellationToken cancellationToken)
        => _proxy.ProxyToProfessionalServiceAsync($"/api/v{request.ApiVersion}/professional-profiles/{request.ProfileId}/certifications", HttpMethod.Post, request.RequestBody, cancellationToken: cancellationToken);
}

public sealed class AddCompetencyCommandHandler : IRequestHandler<AddCompetencyCommand, IActionResult>
{
    private readonly IProxyService _proxy;
    public AddCompetencyCommandHandler(IProxyService proxy) => _proxy = proxy;
    public Task<IActionResult> Handle(AddCompetencyCommand request, CancellationToken cancellationToken)
        => _proxy.ProxyToProfessionalServiceAsync($"/api/v{request.ApiVersion}/professional-profiles/{request.ProfileId}/competencies", HttpMethod.Post, request.RequestBody, cancellationToken: cancellationToken);
}

public sealed class GrantClinicalPrivilegeCommandHandler : IRequestHandler<GrantClinicalPrivilegeCommand, IActionResult>
{
    private readonly IProxyService _proxy;
    public GrantClinicalPrivilegeCommandHandler(IProxyService proxy) => _proxy = proxy;
    public Task<IActionResult> Handle(GrantClinicalPrivilegeCommand request, CancellationToken cancellationToken)
        => _proxy.ProxyToProfessionalServiceAsync($"/api/v{request.ApiVersion}/professional-profiles/{request.ProfileId}/privileges", HttpMethod.Post, request.RequestBody, cancellationToken: cancellationToken);
}

public sealed class SubmitCredentialingCommandHandler : IRequestHandler<SubmitCredentialingCommand, IActionResult>
{
    private readonly IProxyService _proxy;
    public SubmitCredentialingCommandHandler(IProxyService proxy) => _proxy = proxy;
    public Task<IActionResult> Handle(SubmitCredentialingCommand request, CancellationToken cancellationToken)
        => _proxy.ProxyToProfessionalServiceAsync($"/api/v{request.ApiVersion}/professional-profiles/{request.ProfileId}/credentialing", HttpMethod.Post, request.RequestBody, cancellationToken: cancellationToken);
}

public sealed class AddTrainingRecordCommandHandler : IRequestHandler<AddTrainingRecordCommand, IActionResult>
{
    private readonly IProxyService _proxy;
    public AddTrainingRecordCommandHandler(IProxyService proxy) => _proxy = proxy;
    public Task<IActionResult> Handle(AddTrainingRecordCommand request, CancellationToken cancellationToken)
        => _proxy.ProxyToProfessionalServiceAsync($"/api/v{request.ApiVersion}/professional-profiles/{request.ProfileId}/training", HttpMethod.Post, request.RequestBody, cancellationToken: cancellationToken);
}
