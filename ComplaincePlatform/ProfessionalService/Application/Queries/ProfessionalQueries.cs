using MediatR;
using ProfessionalService.Application.DTOs;

namespace ProfessionalService.Application.Queries;

public record GetProfessionalProfileByIdQuery(Guid Id) : IRequest<ProfessionalProfileDto?>;
public record GetProfessionalProfileByEmploymentQuery(Guid EmploymentId) : IRequest<ProfessionalProfileDto?>;
public record GetProfessionalProfileByPersonQuery(Guid PersonId) : IRequest<ProfessionalProfileDto?>;
public record ListProfessionalProfilesQuery(int Page, int PageSize, Guid? CategoryValueId, Guid? SpecialtyValueId) : IRequest<PagedResultDto<ProfessionalListItemDto>>;
public record SearchProfessionalProfilesQuery(string Query, int Page, int PageSize) : IRequest<PagedResultDto<ProfessionalListItemDto>>;
public record GetProfessionalTimelineQuery(Guid ProfileId) : IRequest<IReadOnlyList<ProfessionalTimelineDto>>;
public record GetRenewalDashboardQuery(int WithinDays, Guid? OrganizationId) : IRequest<IReadOnlyList<RenewalDashboardItemDto>>;
public record GetProfessionalLookupsQuery(string Category) : IRequest<IReadOnlyList<LookupValueDto>>;
