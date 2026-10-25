using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.Reporting.Contracts;
using ComplianceService.Application.Reporting.Dtos;
using ComplianceService.Application.Reporting.Export;
using ComplianceService.Application.Services;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.DTO;

namespace ComplianceService.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reports")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public class ReportingController : ControllerBase
{
    private readonly IReportingService _reportingService;
    private readonly IComplianceReportService _complianceReport;
    private readonly IAuditReportService _auditReport;
    private readonly ITaskReportService _taskReport;
    private readonly IEvidenceReportService _evidenceReport;
    private readonly IReadinessReportService _readinessReport;
    private readonly ICustomReportService _customReport;
    private readonly IReportExportService _exportService;
    private readonly AuditLogService _auditLog;
    private readonly ComplianceDbContext _db;
    private readonly IUserContextService _userContext;
    private readonly ILogger<ReportingController> _logger;

    public ReportingController(
        IReportingService reportingService,
        IComplianceReportService complianceReport,
        IAuditReportService auditReport,
        ITaskReportService taskReport,
        IEvidenceReportService evidenceReport,
        IReadinessReportService readinessReport,
        ICustomReportService customReport,
        IReportExportService exportService,
        AuditLogService auditLog,
        ComplianceDbContext db,
        IUserContextService userContext,
        ILogger<ReportingController> logger)
    {
        _reportingService = reportingService;
        _complianceReport = complianceReport;
        _auditReport = auditReport;
        _taskReport = taskReport;
        _evidenceReport = evidenceReport;
        _readinessReport = readinessReport;
        _customReport = customReport;
        _exportService = exportService;
        _auditLog = auditLog;
        _db = db;
        _userContext = userContext;
        _logger = logger;
    }

    [HttpGet("compliance-score")]
    public async Task<ActionResult<ApiResponse<ComplianceScoreReportDto>>> GetComplianceScoreReport(
        [FromQuery] Guid hospitalId,
        [FromQuery] Guid? frameworkId,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        var requestId = HttpContext.TraceIdentifier;
        if (hospitalId == Guid.Empty)
        {
            return ValidationFailure<ComplianceScoreReportDto>(requestId);
        }

        var report = await _reportingService.GetComplianceScoreReportAsync(hospitalId, frameworkId, from, to, ct);
        return SuccessResponse(ApiResponseCode.COMPLIANCE_SCORE_UPDATED, report, requestId);
    }

    [HttpGet("audit-findings")]
    public async Task<ActionResult<ApiResponse<AuditFindingsReportDto>>> GetAuditFindingsReport(
        [FromQuery] Guid hospitalId,
        [FromQuery] Guid? frameworkId,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        var requestId = HttpContext.TraceIdentifier;
        if (hospitalId == Guid.Empty)
        {
            return ValidationFailure<AuditFindingsReportDto>(requestId);
        }

        var report = await _reportingService.GetAuditFindingsReportAsync(hospitalId, frameworkId, from, to, ct);
        return SuccessResponse(ApiResponseCode.REPORT_GENERATED, report, requestId);
    }

    [HttpGet("nabh-readiness")]
    public async Task<ActionResult<ApiResponse<NabhReadinessReportDto>>> GetNabhReadinessReport(
        [FromQuery] Guid hospitalId,
        CancellationToken ct = default)
    {
        var requestId = HttpContext.TraceIdentifier;
        if (hospitalId == Guid.Empty)
        {
            return ValidationFailure<NabhReadinessReportDto>(requestId);
        }

        var report = await _reportingService.GetNabhReadinessReportAsync(hospitalId, ct);
        return SuccessResponse(ApiResponseCode.REPORT_GENERATED, report, requestId);
    }

    [HttpGet("compliance")]
    public async Task<ActionResult<ApiResponse<ComplianceReportDto>>> GetComplianceReport(
        [FromQuery] Guid? hospitalId,
        [FromQuery] Guid? frameworkId,
        [FromQuery] Guid? chapterId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken ct = default)
    {
        var requestId = HttpContext.TraceIdentifier;
        var hid = ResolveHospitalId(hospitalId);
        if (hid == Guid.Empty)
        {
            return ValidationFailure<ComplianceReportDto>(requestId);
        }

        var filter = BuildFilter(hid, frameworkId, chapterId, null, null, null, fromDate, toDate);
        var report = await _complianceReport.GetReportAsync(filter, ct);
        await LogReportAccessAsync("ComplianceReport", hid, ct);
        return SuccessResponse(ApiResponseCode.REPORT_GENERATED, report, requestId);
    }

    [HttpGet("audit")]
    public async Task<ActionResult<ApiResponse<AuditReportDto>>> GetAuditReport(
        [FromQuery] Guid? hospitalId,
        [FromQuery] Guid? frameworkId,
        [FromQuery] Guid? chapterId,
        [FromQuery] Guid? departmentId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken ct = default)
    {
        var requestId = HttpContext.TraceIdentifier;
        var hid = ResolveHospitalId(hospitalId);
        if (hid == Guid.Empty)
        {
            return ValidationFailure<AuditReportDto>(requestId);
        }

        var filter = BuildFilter(hid, frameworkId, chapterId, departmentId, null, null, fromDate, toDate);
        var report = await _auditReport.GetReportAsync(filter, ct);
        await LogReportAccessAsync("AuditReport", hid, ct);
        return SuccessResponse(ApiResponseCode.REPORT_GENERATED, report, requestId);
    }

    [HttpGet("tasks")]
    public async Task<ActionResult<ApiResponse<TaskReportDto>>> GetTaskReport(
        [FromQuery] Guid? hospitalId,
        [FromQuery] Guid? frameworkId,
        [FromQuery] Guid? departmentId,
        [FromQuery] string? status,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken ct = default)
    {
        var requestId = HttpContext.TraceIdentifier;
        var hid = ResolveHospitalId(hospitalId);
        if (hid == Guid.Empty)
        {
            return ValidationFailure<TaskReportDto>(requestId);
        }

        var filter = BuildFilter(hid, frameworkId, null, departmentId, null, status, fromDate, toDate);
        var report = await _taskReport.GetReportAsync(filter, ct);
        await LogReportAccessAsync("TaskReport", hid, ct);
        return SuccessResponse(ApiResponseCode.REPORT_GENERATED, report, requestId);
    }

    [HttpGet("evidence")]
    public async Task<ActionResult<ApiResponse<EvidenceReportDto>>> GetEvidenceReport(
        [FromQuery] Guid? hospitalId,
        [FromQuery] Guid? frameworkId,
        [FromQuery] Guid? chapterId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken ct = default)
    {
        var requestId = HttpContext.TraceIdentifier;
        var hid = ResolveHospitalId(hospitalId);
        if (hid == Guid.Empty)
        {
            return ValidationFailure<EvidenceReportDto>(requestId);
        }

        var filter = BuildFilter(hid, frameworkId, chapterId, null, null, null, fromDate, toDate);
        var report = await _evidenceReport.GetReportAsync(filter, ct);
        await LogReportAccessAsync("EvidenceReport", hid, ct);
        return SuccessResponse(ApiResponseCode.REPORT_GENERATED, report, requestId);
    }

    [HttpGet("readiness")]
    public async Task<ActionResult<ApiResponse<ReadinessReportDto>>> GetReadinessReport(
        [FromQuery] Guid? hospitalId,
        [FromQuery] Guid? frameworkId,
        [FromQuery] Guid? organizationId,
        CancellationToken ct = default)
    {
        var requestId = HttpContext.TraceIdentifier;
        var hid = ResolveHospitalId(hospitalId);
        if (hid == Guid.Empty)
        {
            return ValidationFailure<ReadinessReportDto>(requestId);
        }

        var filter = BuildFilter(hid, frameworkId, null, null, null, null, null, null);
        filter = filter with { OrganizationId = organizationId ?? _userContext.OrganizationId };
        var report = await _readinessReport.GetReportAsync(filter, ct);
        await LogReportAccessAsync("ReadinessReport", hid, ct);
        return SuccessResponse(ApiResponseCode.REPORT_GENERATED, report, requestId);
    }

    [HttpPost("custom")]
    public async Task<ActionResult<ApiResponse<CustomReportResultDto>>> PostCustomReport(
        [FromBody] CustomReportRequest request,
        [FromQuery] Guid? hospitalId,
        CancellationToken ct = default)
    {
        var requestId = HttpContext.TraceIdentifier;
        var hid = ResolveHospitalId(hospitalId);
        if (hid == Guid.Empty)
        {
            return ValidationFailure<CustomReportResultDto>(requestId);
        }

        var filter = BuildFilter(hid, request.FrameworkId, request.ChapterId, request.DepartmentId, request.Severity, null, request.FromDate, request.ToDate);
        var report = await _customReport.GenerateAsync(filter, request, ct);
        await LogReportAccessAsync("CustomReport", hid, ct);
        return SuccessResponse(ApiResponseCode.REPORT_GENERATED, report, requestId);
    }

    [HttpGet("compliance/export")]
    public Task<IActionResult> ExportCompliance(
        [FromQuery] string format,
        [FromQuery] Guid? hospitalId,
        [FromQuery] Guid? frameworkId,
        [FromQuery] Guid? chapterId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken ct = default) =>
        ExportReportAsync("compliance", format, hospitalId, frameworkId, chapterId, null, null, null, fromDate, toDate, ct);

    [HttpGet("audit/export")]
    public Task<IActionResult> ExportAudit(
        [FromQuery] string format,
        [FromQuery] Guid? hospitalId,
        [FromQuery] Guid? frameworkId,
        [FromQuery] Guid? chapterId,
        [FromQuery] Guid? departmentId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken ct = default) =>
        ExportReportAsync("audit", format, hospitalId, frameworkId, chapterId, departmentId, null, null, fromDate, toDate, ct);

    [HttpGet("tasks/export")]
    public Task<IActionResult> ExportTasks(
        [FromQuery] string format,
        [FromQuery] Guid? hospitalId,
        [FromQuery] Guid? frameworkId,
        [FromQuery] Guid? departmentId,
        [FromQuery] string? status,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken ct = default) =>
        ExportReportAsync("tasks", format, hospitalId, frameworkId, null, departmentId, null, status, fromDate, toDate, ct);

    [HttpGet("evidence/export")]
    public Task<IActionResult> ExportEvidence(
        [FromQuery] string format,
        [FromQuery] Guid? hospitalId,
        [FromQuery] Guid? frameworkId,
        [FromQuery] Guid? chapterId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken ct = default) =>
        ExportReportAsync("evidence", format, hospitalId, frameworkId, chapterId, null, null, null, fromDate, toDate, ct);

    [HttpGet("readiness/export")]
    public Task<IActionResult> ExportReadiness(
        [FromQuery] string format,
        [FromQuery] Guid? hospitalId,
        [FromQuery] Guid? frameworkId,
        CancellationToken ct = default) =>
        ExportReportAsync("readiness", format, hospitalId, frameworkId, null, null, null, null, null, null, ct);

    [HttpPost("custom/export")]
    public async Task<IActionResult> ExportCustom(
        [FromBody] CustomReportRequest request,
        [FromQuery] string format,
        [FromQuery] Guid? hospitalId,
        CancellationToken ct = default)
    {
        var hid = ResolveHospitalId(hospitalId);
        if (hid == Guid.Empty)
        {
            return BadRequest("hospitalId is required.");
        }

        if (!TryParseFormat(format, out var exportFormat))
        {
            return BadRequest("format must be pdf, excel, or csv.");
        }

        var filter = BuildFilter(hid, request.FrameworkId, request.ChapterId, request.DepartmentId, request.Severity, null, request.FromDate, request.ToDate);
        var report = await _customReport.GenerateAsync(filter, request, ct);
        var (orgName, fwName) = await ResolveLabelsAsync(request.FrameworkId ?? filter.FrameworkId, ct);
        var model = _customReport.BuildExportModel(report, orgName, fwName);
        await LogReportAccessAsync("CustomReportExport", hid, ct);
        return FileExport(model, exportFormat);
    }

    private async Task<IActionResult> ExportReportAsync(
        string reportType,
        string format,
        Guid? hospitalId,
        Guid? frameworkId,
        Guid? chapterId,
        Guid? departmentId,
        string? severity,
        string? taskStatus,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken ct)
    {
        var hid = ResolveHospitalId(hospitalId);
        if (hid == Guid.Empty)
        {
            return BadRequest("hospitalId is required.");
        }

        if (!TryParseFormat(format, out var exportFormat))
        {
            return BadRequest("format must be pdf, excel, or csv.");
        }

        var filter = BuildFilter(hid, frameworkId, chapterId, departmentId, severity, taskStatus, fromDate, toDate);
        var (orgName, fwName) = await ResolveLabelsAsync(frameworkId, ct);
        ReportExportModel model;

        switch (reportType)
        {
            case "compliance":
                model = _complianceReport.BuildExportModel(await _complianceReport.GetReportAsync(filter, ct), orgName, fwName);
                break;
            case "audit":
                model = _auditReport.BuildExportModel(await _auditReport.GetReportAsync(filter, ct), orgName, fwName);
                break;
            case "tasks":
                model = _taskReport.BuildExportModel(await _taskReport.GetReportAsync(filter, ct), orgName, fwName);
                break;
            case "evidence":
                model = _evidenceReport.BuildExportModel(await _evidenceReport.GetReportAsync(filter, ct), orgName, fwName);
                break;
            case "readiness":
                model = _readinessReport.BuildExportModel(await _readinessReport.GetReportAsync(filter, ct), orgName, fwName);
                break;
            default:
                return NotFound();
        }

        await LogReportAccessAsync($"{reportType}Export", hid, ct);
        return FileExport(model, exportFormat);
    }

    private IActionResult FileExport(ReportExportModel model, ReportExportFormat format)
    {
        var (content, contentType, fileName) = _exportService.Export(model, format);
        return File(content, contentType, fileName);
    }

    private Guid ResolveHospitalId(Guid? hospitalId) =>
        hospitalId is { } id && id != Guid.Empty ? id : _userContext.HospitalId;

    private static ReportFilter BuildFilter(
        Guid hospitalId,
        Guid? frameworkId,
        Guid? chapterId,
        Guid? departmentId,
        string? severity,
        string? taskStatus,
        DateTime? fromDate,
        DateTime? toDate) =>
        new()
        {
            HospitalId = hospitalId,
            FrameworkId = frameworkId,
            ChapterId = chapterId,
            DepartmentId = departmentId,
            Severity = severity,
            TaskStatus = taskStatus,
            FromDate = fromDate,
            ToDate = toDate
        };

    private async Task<(string OrgName, string FrameworkName)> ResolveLabelsAsync(Guid? frameworkId, CancellationToken ct)
    {
        var orgName = _userContext.OrganizationId.ToString();
        var fwName = string.Empty;
        if (frameworkId.HasValue)
        {
            fwName = await _db.ComplianceFrameworks.AsNoTracking()
                .Where(f => f.Id == frameworkId.Value)
                .Select(f => f.Name)
                .FirstOrDefaultAsync(ct) ?? string.Empty;
        }

        return (orgName, fwName);
    }

    private async Task LogReportAccessAsync(string reportType, Guid hospitalId, CancellationToken ct)
    {
        try
        {
            await _auditLog.LogEntityCreatedAsync(
                "Report",
                hospitalId,
                _userContext.UserId,
                new { reportType, exported = reportType.Contains("Export", StringComparison.Ordinal) },
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write report audit log for {ReportType}", reportType);
        }
    }

    private static bool TryParseFormat(string? format, out ReportExportFormat exportFormat)
    {
        exportFormat = ReportExportFormat.Csv;
        if (string.IsNullOrWhiteSpace(format))
        {
            return false;
        }

        switch (format.Trim().ToLowerInvariant())
        {
            case "csv":
                exportFormat = ReportExportFormat.Csv;
                return true;
            case "excel":
            case "xlsx":
                exportFormat = ReportExportFormat.Excel;
                return true;
            case "pdf":
                exportFormat = ReportExportFormat.Pdf;
                return true;
            default:
                return false;
        }
    }

    private ActionResult<ApiResponse<T>> ValidationFailure<T>(string requestId)
    {
        var response = ApiResponseFactory.Failure<T>(ApiResponseCode.VALIDATION_FAILED, requestId);
        var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
        return StatusCode(meta.HttpStatus, response);
    }

    private ActionResult<ApiResponse<T>> SuccessResponse<T>(ApiResponseCode code, T data, string requestId)
    {
        var success = ApiResponseFactory.Success(code, data, requestId);
        var successMeta = ApiResponseCodes.Registry[code];
        return StatusCode(successMeta.HttpStatus, success);
    }
}
