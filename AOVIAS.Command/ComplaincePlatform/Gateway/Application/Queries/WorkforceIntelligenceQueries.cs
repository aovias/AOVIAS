using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public record GetIntelligenceDashboardQuery(string ApiVersion, Guid OrganizationId, Guid? HospitalId, Guid? DepartmentId, DateTime? AsOf) : IRequest<IActionResult>;
public record GetIntelligenceHeadcountQuery(string ApiVersion, Guid OrganizationId, Guid? HospitalId, Guid? DepartmentId, DateTime? AsOf) : IRequest<IActionResult>;
public record GetIntelligenceSkillsQuery(string ApiVersion, Guid OrganizationId, Guid? HospitalId, Guid? DepartmentId, DateTime? AsOf) : IRequest<IActionResult>;
public record GetIntelligenceCompetenciesQuery(string ApiVersion, Guid OrganizationId, Guid? HospitalId, Guid? DepartmentId, DateTime? AsOf) : IRequest<IActionResult>;
public record GetIntelligenceTrainingQuery(string ApiVersion, Guid OrganizationId, Guid? HospitalId, Guid? DepartmentId, DateTime? AsOf) : IRequest<IActionResult>;
public record GetIntelligenceLicensesQuery(string ApiVersion, Guid OrganizationId, Guid? HospitalId, Guid? DepartmentId, DateTime? AsOf) : IRequest<IActionResult>;
public record GetIntelligenceCertificationsQuery(string ApiVersion, Guid OrganizationId, Guid? HospitalId, Guid? DepartmentId, DateTime? AsOf) : IRequest<IActionResult>;
public record GetIntelligenceForecastQuery(string ApiVersion, Guid OrganizationId, string ForecastType) : IRequest<IActionResult>;
public record GetIntelligenceRecommendationsQuery(string ApiVersion, Guid OrganizationId) : IRequest<IActionResult>;
public record SearchIntelligenceWorkforceQuery(string ApiVersion, Guid OrganizationId, string Query, int Page, int PageSize) : IRequest<IActionResult>;
public record GetIntelligenceOrganizationQuery(string ApiVersion, Guid OrganizationId, DateTime? AsOf) : IRequest<IActionResult>;
public record GetIntelligenceResponsibilitiesQuery(string ApiVersion, Guid OrganizationId, DateTime? AsOf) : IRequest<IActionResult>;
public record GetIntelligenceCommitteesQuery(string ApiVersion, Guid OrganizationId) : IRequest<IActionResult>;
public record GetIntelligenceAvailabilityQuery(string ApiVersion, Guid OrganizationId, Guid? HospitalId) : IRequest<IActionResult>;
public record RefreshIntelligenceDashboardQuery(string ApiVersion, Guid OrganizationId) : IRequest<IActionResult>;
