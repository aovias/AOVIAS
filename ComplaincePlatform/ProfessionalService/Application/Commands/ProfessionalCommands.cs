using MediatR;
using ProfessionalService.Application.DTOs;

namespace ProfessionalService.Application.Commands;

public record CreateProfessionalProfileCommand(CreateProfessionalProfileRequest Request) : IRequest<ProfessionalProfileDto>;
public record UpdateProfessionalProfileCommand(Guid Id, UpdateProfessionalProfileRequest Request) : IRequest<ProfessionalProfileDto?>;
public record DeleteProfessionalProfileCommand(Guid Id, Guid? ActorUserId) : IRequest<bool>;
public record AddQualificationCommand(Guid ProfileId, AddQualificationRequest Request) : IRequest<QualificationDto?>;
public record AddRegistrationCommand(Guid ProfileId, AddRegistrationRequest Request) : IRequest<RegistrationDto?>;
public record AddLicenseCommand(Guid ProfileId, AddLicenseRequest Request) : IRequest<LicenseDto?>;
public record AddCertificationCommand(Guid ProfileId, AddCertificationRequest Request) : IRequest<CertificationDto?>;
public record AddCompetencyCommand(Guid ProfileId, AddCompetencyRequest Request) : IRequest<CompetencyDto?>;
public record GrantClinicalPrivilegeCommand(Guid ProfileId, GrantClinicalPrivilegeRequest Request) : IRequest<ClinicalPrivilegeDto?>;
public record SubmitCredentialingCommand(Guid ProfileId, SubmitCredentialingRequest Request) : IRequest<CredentialingRecordDto?>;
public record ApproveCredentialingCommand(Guid ProfileId, Guid RecordId, ApproveCredentialingRequest Request) : IRequest<CredentialingRecordDto?>;
public record RejectCredentialingCommand(Guid ProfileId, Guid RecordId, RejectCredentialingRequest Request) : IRequest<CredentialingRecordDto?>;
public record AddTrainingRecordCommand(Guid ProfileId, AddTrainingRecordRequest Request) : IRequest<TrainingRecordDto?>;
