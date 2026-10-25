using MediatR;
using ProfessionalService.Application.Commands;
using ProfessionalService.Application.Contracts;
using ProfessionalService.Application.DTOs;
using ProfessionalService.Application.Queries;

namespace ProfessionalService.Application.Handlers;

public class ProfessionalCommandHandlers :
    IRequestHandler<CreateProfessionalProfileCommand, ProfessionalProfileDto>,
    IRequestHandler<UpdateProfessionalProfileCommand, ProfessionalProfileDto?>,
    IRequestHandler<DeleteProfessionalProfileCommand, bool>,
    IRequestHandler<AddQualificationCommand, QualificationDto?>,
    IRequestHandler<AddRegistrationCommand, RegistrationDto?>,
    IRequestHandler<AddLicenseCommand, LicenseDto?>,
    IRequestHandler<AddCertificationCommand, CertificationDto?>,
    IRequestHandler<AddCompetencyCommand, CompetencyDto?>,
    IRequestHandler<GrantClinicalPrivilegeCommand, ClinicalPrivilegeDto?>,
    IRequestHandler<SubmitCredentialingCommand, CredentialingRecordDto?>,
    IRequestHandler<ApproveCredentialingCommand, CredentialingRecordDto?>,
    IRequestHandler<RejectCredentialingCommand, CredentialingRecordDto?>,
    IRequestHandler<AddTrainingRecordCommand, TrainingRecordDto?>
{
    private readonly IProfessionalProfileAppService _service;
    public ProfessionalCommandHandlers(IProfessionalProfileAppService service) => _service = service;

    public Task<ProfessionalProfileDto> Handle(CreateProfessionalProfileCommand request, CancellationToken cancellationToken) => _service.CreateAsync(request.Request, cancellationToken);
    public Task<ProfessionalProfileDto?> Handle(UpdateProfessionalProfileCommand request, CancellationToken cancellationToken) => _service.UpdateAsync(request.Id, request.Request, cancellationToken);
    public Task<bool> Handle(DeleteProfessionalProfileCommand request, CancellationToken cancellationToken) => _service.DeleteAsync(request.Id, request.ActorUserId, cancellationToken);
    public Task<QualificationDto?> Handle(AddQualificationCommand request, CancellationToken cancellationToken) => _service.AddQualificationAsync(request.ProfileId, request.Request, cancellationToken);
    public Task<RegistrationDto?> Handle(AddRegistrationCommand request, CancellationToken cancellationToken) => _service.AddRegistrationAsync(request.ProfileId, request.Request, cancellationToken);
    public Task<LicenseDto?> Handle(AddLicenseCommand request, CancellationToken cancellationToken) => _service.AddLicenseAsync(request.ProfileId, request.Request, cancellationToken);
    public Task<CertificationDto?> Handle(AddCertificationCommand request, CancellationToken cancellationToken) => _service.AddCertificationAsync(request.ProfileId, request.Request, cancellationToken);
    public Task<CompetencyDto?> Handle(AddCompetencyCommand request, CancellationToken cancellationToken) => _service.AddCompetencyAsync(request.ProfileId, request.Request, cancellationToken);
    public Task<ClinicalPrivilegeDto?> Handle(GrantClinicalPrivilegeCommand request, CancellationToken cancellationToken) => _service.GrantPrivilegeAsync(request.ProfileId, request.Request, cancellationToken);
    public Task<CredentialingRecordDto?> Handle(SubmitCredentialingCommand request, CancellationToken cancellationToken) => _service.SubmitCredentialingAsync(request.ProfileId, request.Request, cancellationToken);
    public Task<CredentialingRecordDto?> Handle(ApproveCredentialingCommand request, CancellationToken cancellationToken) => _service.ApproveCredentialingAsync(request.ProfileId, request.RecordId, request.Request, cancellationToken);
    public Task<CredentialingRecordDto?> Handle(RejectCredentialingCommand request, CancellationToken cancellationToken) => _service.RejectCredentialingAsync(request.ProfileId, request.RecordId, request.Request, cancellationToken);
    public Task<TrainingRecordDto?> Handle(AddTrainingRecordCommand request, CancellationToken cancellationToken) => _service.AddTrainingAsync(request.ProfileId, request.Request, cancellationToken);
}

public class ProfessionalQueryHandlers :
    IRequestHandler<GetProfessionalProfileByIdQuery, ProfessionalProfileDto?>,
    IRequestHandler<GetProfessionalProfileByEmploymentQuery, ProfessionalProfileDto?>,
    IRequestHandler<GetProfessionalProfileByPersonQuery, ProfessionalProfileDto?>,
    IRequestHandler<ListProfessionalProfilesQuery, PagedResultDto<ProfessionalListItemDto>>,
    IRequestHandler<SearchProfessionalProfilesQuery, PagedResultDto<ProfessionalListItemDto>>,
    IRequestHandler<GetProfessionalTimelineQuery, IReadOnlyList<ProfessionalTimelineDto>>,
    IRequestHandler<GetRenewalDashboardQuery, IReadOnlyList<RenewalDashboardItemDto>>,
    IRequestHandler<GetProfessionalLookupsQuery, IReadOnlyList<LookupValueDto>>
{
    private readonly IProfessionalProfileAppService _service;
    public ProfessionalQueryHandlers(IProfessionalProfileAppService service) => _service = service;

    public Task<ProfessionalProfileDto?> Handle(GetProfessionalProfileByIdQuery request, CancellationToken cancellationToken) => _service.GetByIdAsync(request.Id, cancellationToken);
    public Task<ProfessionalProfileDto?> Handle(GetProfessionalProfileByEmploymentQuery request, CancellationToken cancellationToken) => _service.GetByEmploymentIdAsync(request.EmploymentId, cancellationToken);
    public Task<ProfessionalProfileDto?> Handle(GetProfessionalProfileByPersonQuery request, CancellationToken cancellationToken) => _service.GetByPersonIdAsync(request.PersonId, cancellationToken);
    public Task<PagedResultDto<ProfessionalListItemDto>> Handle(ListProfessionalProfilesQuery request, CancellationToken cancellationToken) => _service.ListAsync(request.Page, request.PageSize, request.CategoryValueId, request.SpecialtyValueId, cancellationToken);
    public Task<PagedResultDto<ProfessionalListItemDto>> Handle(SearchProfessionalProfilesQuery request, CancellationToken cancellationToken) => _service.SearchAsync(request.Query, request.Page, request.PageSize, cancellationToken);
    public Task<IReadOnlyList<ProfessionalTimelineDto>> Handle(GetProfessionalTimelineQuery request, CancellationToken cancellationToken) => _service.GetTimelineAsync(request.ProfileId, cancellationToken);
    public Task<IReadOnlyList<RenewalDashboardItemDto>> Handle(GetRenewalDashboardQuery request, CancellationToken cancellationToken) => _service.GetRenewalsAsync(request.WithinDays, request.OrganizationId, cancellationToken);
    public Task<IReadOnlyList<LookupValueDto>> Handle(GetProfessionalLookupsQuery request, CancellationToken cancellationToken) => _service.GetLookupsAsync(request.Category, cancellationToken);
}
