using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Commands;

public record CreateProfessionalProfileCommand(string ApiVersion, object RequestBody) : IRequest<IActionResult>;
public record UpdateProfessionalProfileCommand(string ApiVersion, Guid ProfileId, object RequestBody) : IRequest<IActionResult>;
public record DeleteProfessionalProfileCommand(string ApiVersion, Guid ProfileId) : IRequest<IActionResult>;
public record AddQualificationCommand(string ApiVersion, Guid ProfileId, object RequestBody) : IRequest<IActionResult>;
public record AddRegistrationCommand(string ApiVersion, Guid ProfileId, object RequestBody) : IRequest<IActionResult>;
public record AddLicenseCommand(string ApiVersion, Guid ProfileId, object RequestBody) : IRequest<IActionResult>;
public record AddCertificationCommand(string ApiVersion, Guid ProfileId, object RequestBody) : IRequest<IActionResult>;
public record AddCompetencyCommand(string ApiVersion, Guid ProfileId, object RequestBody) : IRequest<IActionResult>;
public record GrantClinicalPrivilegeCommand(string ApiVersion, Guid ProfileId, object RequestBody) : IRequest<IActionResult>;
public record SubmitCredentialingCommand(string ApiVersion, Guid ProfileId, object RequestBody) : IRequest<IActionResult>;
public record AddTrainingRecordCommand(string ApiVersion, Guid ProfileId, object RequestBody) : IRequest<IActionResult>;
