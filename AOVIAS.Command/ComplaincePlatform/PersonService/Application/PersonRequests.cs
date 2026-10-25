using PersonService.Application.DTOs;
using MediatR;

namespace PersonService.Application.Commands;

public record CreatePersonCommand(CreatePersonRequest Request) : IRequest<InternalCreatePersonResponse>;
public record UpdatePersonCommand(Guid Id, UpdatePersonRequest Request) : IRequest<PersonDto?>;
public record DeletePersonCommand(Guid Id, Guid? ActorUserId) : IRequest<bool>;
public record AddPersonDocumentCommand(Guid PersonId, PersonDocumentDto Document) : IRequest<PersonDocumentDto?>;
public record RemovePersonDocumentCommand(Guid PersonId, Guid DocumentId, Guid? ActorUserId) : IRequest<bool>;
