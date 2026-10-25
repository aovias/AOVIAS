using PersonService.Application.Commands;
using PersonService.Application.Contracts;
using PersonService.Application.DTOs;
using PersonService.Application.Queries;
using MediatR;

namespace PersonService.Application.Handlers;

public class CreatePersonCommandHandler : IRequestHandler<CreatePersonCommand, InternalCreatePersonResponse>
{
    private readonly IPersonAppService _service;
    public CreatePersonCommandHandler(IPersonAppService service) => _service = service;
    public Task<InternalCreatePersonResponse> Handle(CreatePersonCommand request, CancellationToken cancellationToken)
        => _service.CreateAsync(request.Request, cancellationToken);
}

public class UpdatePersonCommandHandler : IRequestHandler<UpdatePersonCommand, PersonDto?>
{
    private readonly IPersonAppService _service;
    public UpdatePersonCommandHandler(IPersonAppService service) => _service = service;
    public Task<PersonDto?> Handle(UpdatePersonCommand request, CancellationToken cancellationToken)
        => _service.UpdateAsync(request.Id, request.Request, cancellationToken);
}

public class DeletePersonCommandHandler : IRequestHandler<DeletePersonCommand, bool>
{
    private readonly IPersonAppService _service;
    public DeletePersonCommandHandler(IPersonAppService service) => _service = service;
    public Task<bool> Handle(DeletePersonCommand request, CancellationToken cancellationToken)
        => _service.DeleteAsync(request.Id, request.ActorUserId, cancellationToken);
}

public class GetPersonByIdQueryHandler : IRequestHandler<GetPersonByIdQuery, PersonDto?>
{
    private readonly IPersonAppService _service;
    public GetPersonByIdQueryHandler(IPersonAppService service) => _service = service;
    public Task<PersonDto?> Handle(GetPersonByIdQuery request, CancellationToken cancellationToken)
        => _service.GetByIdAsync(request.Id, cancellationToken);
}

public class ListPersonsQueryHandler : IRequestHandler<ListPersonsQuery, PagedResultDto<PersonListItemDto>>
{
    private readonly IPersonAppService _service;
    public ListPersonsQueryHandler(IPersonAppService service) => _service = service;
    public Task<PagedResultDto<PersonListItemDto>> Handle(ListPersonsQuery request, CancellationToken cancellationToken)
        => _service.ListAsync(request.Page, request.PageSize, request.Status, cancellationToken);
}

public class SearchPersonsQueryHandler : IRequestHandler<SearchPersonsQuery, PagedResultDto<PersonListItemDto>>
{
    private readonly IPersonAppService _service;
    public SearchPersonsQueryHandler(IPersonAppService service) => _service = service;
    public Task<PagedResultDto<PersonListItemDto>> Handle(SearchPersonsQuery request, CancellationToken cancellationToken)
        => _service.SearchAsync(request.Query, request.Page, request.PageSize, cancellationToken);
}

public class GetPersonContactsQueryHandler : IRequestHandler<GetPersonContactsQuery, IReadOnlyList<PersonContactDto>>
{
    private readonly IPersonAppService _service;
    public GetPersonContactsQueryHandler(IPersonAppService service) => _service = service;
    public Task<IReadOnlyList<PersonContactDto>> Handle(GetPersonContactsQuery request, CancellationToken cancellationToken)
        => _service.GetContactsAsync(request.PersonId, cancellationToken);
}

public class GetPersonAddressesQueryHandler : IRequestHandler<GetPersonAddressesQuery, IReadOnlyList<PersonAddressDto>>
{
    private readonly IPersonAppService _service;
    public GetPersonAddressesQueryHandler(IPersonAppService service) => _service = service;
    public Task<IReadOnlyList<PersonAddressDto>> Handle(GetPersonAddressesQuery request, CancellationToken cancellationToken)
        => _service.GetAddressesAsync(request.PersonId, cancellationToken);
}

public class GetPersonTimelineQueryHandler : IRequestHandler<GetPersonTimelineQuery, IReadOnlyList<PersonTimelineDto>>
{
    private readonly IPersonAppService _service;
    public GetPersonTimelineQueryHandler(IPersonAppService service) => _service = service;
    public Task<IReadOnlyList<PersonTimelineDto>> Handle(GetPersonTimelineQuery request, CancellationToken cancellationToken)
        => _service.GetTimelineAsync(request.PersonId, cancellationToken);
}

public class GetPersonLookupsQueryHandler : IRequestHandler<GetPersonLookupsQuery, IReadOnlyList<LookupValueDto>>
{
    private readonly IPersonAppService _service;
    public GetPersonLookupsQueryHandler(IPersonAppService service) => _service = service;
    public Task<IReadOnlyList<LookupValueDto>> Handle(GetPersonLookupsQuery request, CancellationToken cancellationToken)
        => _service.GetLookupsAsync(request.CategoryCode, cancellationToken);
}

public class AddPersonDocumentCommandHandler : IRequestHandler<AddPersonDocumentCommand, PersonDocumentDto?>
{
    private readonly IPersonAppService _service;
    public AddPersonDocumentCommandHandler(IPersonAppService service) => _service = service;
    public Task<PersonDocumentDto?> Handle(AddPersonDocumentCommand request, CancellationToken cancellationToken)
        => _service.AddDocumentAsync(request.PersonId, request.Document, cancellationToken);
}

public class RemovePersonDocumentCommandHandler : IRequestHandler<RemovePersonDocumentCommand, bool>
{
    private readonly IPersonAppService _service;
    public RemovePersonDocumentCommandHandler(IPersonAppService service) => _service = service;
    public Task<bool> Handle(RemovePersonDocumentCommand request, CancellationToken cancellationToken)
        => _service.RemoveDocumentAsync(request.PersonId, request.DocumentId, request.ActorUserId, cancellationToken);
}
