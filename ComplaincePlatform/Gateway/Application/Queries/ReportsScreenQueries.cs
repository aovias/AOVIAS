using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public record GetComplianceScreenReportQuery(
    string ApiVersion,
    Guid? HospitalId,
    Guid? FrameworkId,
    Guid? ChapterId,
    DateTime? FromDate,
    DateTime? ToDate) : IRequest<IActionResult>;

public record GetAuditScreenReportQuery(
    string ApiVersion,
    Guid? HospitalId,
    Guid? FrameworkId,
    Guid? ChapterId,
    Guid? DepartmentId,
    DateTime? FromDate,
    DateTime? ToDate) : IRequest<IActionResult>;

public record GetTaskScreenReportQuery(
    string ApiVersion,
    Guid? HospitalId,
    Guid? FrameworkId,
    Guid? DepartmentId,
    string? Status,
    DateTime? FromDate,
    DateTime? ToDate) : IRequest<IActionResult>;

public record GetEvidenceScreenReportQuery(
    string ApiVersion,
    Guid? HospitalId,
    Guid? FrameworkId,
    Guid? ChapterId,
    DateTime? FromDate,
    DateTime? ToDate) : IRequest<IActionResult>;

public record GetReadinessScreenReportQuery(
    string ApiVersion,
    Guid? HospitalId,
    Guid? FrameworkId,
    Guid? OrganizationId) : IRequest<IActionResult>;

public record PostCustomScreenReportCommand(
    string ApiVersion,
    Guid? HospitalId,
    object Body) : IRequest<IActionResult>;

public record ExportCustomScreenReportCommand(
    string ApiVersion,
    string Format,
    Guid? HospitalId,
    object Body) : IRequest<IActionResult>;

public record ExportScreenReportQuery(
    string ApiVersion,
    string ReportType,
    string Format,
    Guid? HospitalId,
    Guid? FrameworkId,
    Guid? ChapterId,
    Guid? DepartmentId,
    string? Status,
    string? Severity,
    DateTime? FromDate,
    DateTime? ToDate) : IRequest<IActionResult>;
