using ComplianceService.Application.Reporting.Dtos;
using ComplianceService.Application.Reporting.Export;

namespace ComplianceService.Application.Reporting.Contracts;

public interface IReadinessReportService
{
    Task<ReadinessReportDto> GetReportAsync(ReportFilter filter, CancellationToken cancellationToken = default);
    ReportExportModel BuildExportModel(ReadinessReportDto report, string? organizationName, string? frameworkName);
}
