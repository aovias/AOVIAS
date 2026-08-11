using Gateway.Application.Commands;
using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Handlers;

public sealed class ListPersonsQueryHandler : IRequestHandler<ListPersonsQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    public ListPersonsQueryHandler(IProxyService proxy) => _proxy = proxy;
    public Task<IActionResult> Handle(ListPersonsQuery request, CancellationToken cancellationToken)
    {
        var q = $"?page={request.Page}&pageSize={request.PageSize}";
        if (!string.IsNullOrWhiteSpace(request.Status))
            q += $"&status={Uri.EscapeDataString(request.Status)}";
        return _proxy.ProxyToPersonServiceAsync($"/api/v{request.ApiVersion}/persons{q}", HttpMethod.Get, cancellationToken: cancellationToken);
    }
}

public sealed class SearchPersonsQueryHandler : IRequestHandler<SearchPersonsQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    public SearchPersonsQueryHandler(IProxyService proxy) => _proxy = proxy;
    public Task<IActionResult> Handle(SearchPersonsQuery request, CancellationToken cancellationToken)
    {
        var q = $"?q={Uri.EscapeDataString(request.Query)}&page={request.Page}&pageSize={request.PageSize}";
        return _proxy.ProxyToPersonServiceAsync($"/api/v{request.ApiVersion}/persons/search{q}", HttpMethod.Get, cancellationToken: cancellationToken);
    }
}

public sealed class GetPersonByIdQueryHandler : IRequestHandler<GetPersonByIdQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    public GetPersonByIdQueryHandler(IProxyService proxy) => _proxy = proxy;
    public Task<IActionResult> Handle(GetPersonByIdQuery request, CancellationToken cancellationToken)
        => _proxy.ProxyToPersonServiceAsync($"/api/v{request.ApiVersion}/persons/{request.PersonId}", HttpMethod.Get, cancellationToken: cancellationToken);
}

public sealed class CreatePersonCommandHandler : IRequestHandler<CreatePersonCommand, IActionResult>
{
    private readonly IProxyService _proxy;
    public CreatePersonCommandHandler(IProxyService proxy) => _proxy = proxy;
    public Task<IActionResult> Handle(CreatePersonCommand request, CancellationToken cancellationToken)
        => _proxy.ProxyToPersonServiceAsync($"/api/v{request.ApiVersion}/persons", HttpMethod.Post, request.RequestBody, cancellationToken: cancellationToken);
}

public sealed class UpdatePersonCommandHandler : IRequestHandler<UpdatePersonCommand, IActionResult>
{
    private readonly IProxyService _proxy;
    public UpdatePersonCommandHandler(IProxyService proxy) => _proxy = proxy;
    public Task<IActionResult> Handle(UpdatePersonCommand request, CancellationToken cancellationToken)
        => _proxy.ProxyToPersonServiceAsync($"/api/v{request.ApiVersion}/persons/{request.PersonId}", HttpMethod.Put, request.RequestBody, cancellationToken: cancellationToken);
}

public sealed class DeletePersonCommandHandler : IRequestHandler<DeletePersonCommand, IActionResult>
{
    private readonly IProxyService _proxy;
    public DeletePersonCommandHandler(IProxyService proxy) => _proxy = proxy;
    public Task<IActionResult> Handle(DeletePersonCommand request, CancellationToken cancellationToken)
        => _proxy.ProxyToPersonServiceAsync($"/api/v{request.ApiVersion}/persons/{request.PersonId}", HttpMethod.Delete, cancellationToken: cancellationToken);
}

public sealed class GetPersonContactsQueryHandler : IRequestHandler<GetPersonContactsQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    public GetPersonContactsQueryHandler(IProxyService proxy) => _proxy = proxy;
    public Task<IActionResult> Handle(GetPersonContactsQuery request, CancellationToken cancellationToken)
        => _proxy.ProxyToPersonServiceAsync($"/api/v{request.ApiVersion}/persons/{request.PersonId}/contacts", HttpMethod.Get, cancellationToken: cancellationToken);
}

public sealed class GetPersonAddressesQueryHandler : IRequestHandler<GetPersonAddressesQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    public GetPersonAddressesQueryHandler(IProxyService proxy) => _proxy = proxy;
    public Task<IActionResult> Handle(GetPersonAddressesQuery request, CancellationToken cancellationToken)
        => _proxy.ProxyToPersonServiceAsync($"/api/v{request.ApiVersion}/persons/{request.PersonId}/addresses", HttpMethod.Get, cancellationToken: cancellationToken);
}

public sealed class GetPersonTimelineQueryHandler : IRequestHandler<GetPersonTimelineQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    public GetPersonTimelineQueryHandler(IProxyService proxy) => _proxy = proxy;
    public Task<IActionResult> Handle(GetPersonTimelineQuery request, CancellationToken cancellationToken)
        => _proxy.ProxyToPersonServiceAsync($"/api/v{request.ApiVersion}/persons/{request.PersonId}/timeline", HttpMethod.Get, cancellationToken: cancellationToken);
}

public sealed class GetPersonLookupsQueryHandler : IRequestHandler<GetPersonLookupsQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    public GetPersonLookupsQueryHandler(IProxyService proxy) => _proxy = proxy;
    public Task<IActionResult> Handle(GetPersonLookupsQuery request, CancellationToken cancellationToken)
        => _proxy.ProxyToPersonServiceAsync($"/api/v{request.ApiVersion}/persons/lookups/{Uri.EscapeDataString(request.Category)}", HttpMethod.Get, cancellationToken: cancellationToken);
}

public sealed class AddPersonDocumentCommandHandler : IRequestHandler<AddPersonDocumentCommand, IActionResult>
{
    private readonly IProxyService _proxy;
    public AddPersonDocumentCommandHandler(IProxyService proxy) => _proxy = proxy;
    public Task<IActionResult> Handle(AddPersonDocumentCommand request, CancellationToken cancellationToken)
        => _proxy.ProxyToPersonServiceAsync($"/api/v{request.ApiVersion}/persons/{request.PersonId}/documents", HttpMethod.Post, request.RequestBody, cancellationToken: cancellationToken);
}

public sealed class RemovePersonDocumentCommandHandler : IRequestHandler<RemovePersonDocumentCommand, IActionResult>
{
    private readonly IProxyService _proxy;
    public RemovePersonDocumentCommandHandler(IProxyService proxy) => _proxy = proxy;
    public Task<IActionResult> Handle(RemovePersonDocumentCommand request, CancellationToken cancellationToken)
        => _proxy.ProxyToPersonServiceAsync($"/api/v{request.ApiVersion}/persons/{request.PersonId}/documents/{request.DocumentId}", HttpMethod.Delete, cancellationToken: cancellationToken);
}
