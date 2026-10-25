using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public record GetLatestWizardAssessmentQuery(Guid HospitalId, string ApiVersion) : IRequest<IActionResult>;
