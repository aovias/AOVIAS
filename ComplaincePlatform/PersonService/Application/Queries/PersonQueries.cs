using PersonService.Application.DTOs;
using MediatR;

namespace PersonService.Application.Queries;

public record GetPersonByIdQuery(Guid Id) : IRequest<PersonDto?>;
public record ListPersonsQuery(int Page, int PageSize, string? Status) : IRequest<PagedResultDto<PersonListItemDto>>;
public record SearchPersonsQuery(string Query, int Page, int PageSize) : IRequest<PagedResultDto<PersonListItemDto>>;
public record GetPersonContactsQuery(Guid PersonId) : IRequest<IReadOnlyList<PersonContactDto>>;
public record GetPersonAddressesQuery(Guid PersonId) : IRequest<IReadOnlyList<PersonAddressDto>>;
public record GetPersonTimelineQuery(Guid PersonId) : IRequest<IReadOnlyList<PersonTimelineDto>>;
public record GetPersonLookupsQuery(string CategoryCode) : IRequest<IReadOnlyList<LookupValueDto>>;
