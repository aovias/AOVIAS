using ProfessionalService.Application.DTOs;
using ProfessionalService.Domain.Enums;

namespace ProfessionalService.Application.Contracts;

public interface IProfessionalProfileAppService
{
    Task<ProfessionalProfileDto> CreateAsync(CreateProfessionalProfileRequest request, CancellationToken cancellationToken = default);
    Task<ProfessionalProfileDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProfessionalProfileDto?> GetByEmploymentIdAsync(Guid employmentId, CancellationToken cancellationToken = default);
    Task<ProfessionalProfileDto?> GetByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default);
    Task<PagedResultDto<ProfessionalListItemDto>> ListAsync(int page, int pageSize, Guid? categoryValueId, Guid? specialtyValueId, CancellationToken cancellationToken = default);
    Task<PagedResultDto<ProfessionalListItemDto>> SearchAsync(string query, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ProfessionalProfileDto?> UpdateAsync(Guid id, UpdateProfessionalProfileRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, Guid? actorUserId, CancellationToken cancellationToken = default);
    Task<QualificationDto?> AddQualificationAsync(Guid profileId, AddQualificationRequest request, CancellationToken cancellationToken = default);
    Task<RegistrationDto?> AddRegistrationAsync(Guid profileId, AddRegistrationRequest request, CancellationToken cancellationToken = default);
    Task<LicenseDto?> AddLicenseAsync(Guid profileId, AddLicenseRequest request, CancellationToken cancellationToken = default);
    Task<CertificationDto?> AddCertificationAsync(Guid profileId, AddCertificationRequest request, CancellationToken cancellationToken = default);
    Task<CompetencyDto?> AddCompetencyAsync(Guid profileId, AddCompetencyRequest request, CancellationToken cancellationToken = default);
    Task<ClinicalPrivilegeDto?> GrantPrivilegeAsync(Guid profileId, GrantClinicalPrivilegeRequest request, CancellationToken cancellationToken = default);
    Task<CredentialingRecordDto?> SubmitCredentialingAsync(Guid profileId, SubmitCredentialingRequest request, CancellationToken cancellationToken = default);
    Task<CredentialingRecordDto?> ApproveCredentialingAsync(Guid profileId, Guid recordId, ApproveCredentialingRequest request, CancellationToken cancellationToken = default);
    Task<CredentialingRecordDto?> RejectCredentialingAsync(Guid profileId, Guid recordId, RejectCredentialingRequest request, CancellationToken cancellationToken = default);
    Task<TrainingRecordDto?> AddTrainingAsync(Guid profileId, AddTrainingRecordRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProfessionalTimelineDto>> GetTimelineAsync(Guid profileId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RenewalDashboardItemDto>> GetRenewalsAsync(int withinDays, Guid? organizationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LookupValueDto>> GetLookupsAsync(string categoryCode, CancellationToken cancellationToken = default);
}

public interface IProfessionalAuditService
{
    Task RecordAsync(Guid profileId, string eventCode, string action, Guid? actorUserId, string? oldValue, string? newValue, string? correlationId, CancellationToken cancellationToken = default);
    Task RecordTimelineAsync(Guid profileId, string eventType, string summary, Guid? actorUserId, string? correlationId, CancellationToken cancellationToken = default);
    Task RecordHistoryAsync(Guid profileId, string eventType, string? reason, string? oldJson, string? newJson, Guid? actorUserId, CancellationToken cancellationToken = default);
}

public interface IWorkforceServiceClient
{
    Task<EmploymentRefDto?> GetEmploymentAsync(Guid employmentId, CancellationToken cancellationToken = default);
}

public interface IOrganizationServiceClient
{
    Task<bool> ValidateDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default);
}
