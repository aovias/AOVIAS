using ComplianceService.Application.Reporting.Dtos;
using ComplianceService.Application.Reporting.Export;

namespace ComplianceService.Application.Reporting.Contracts;

public interface IAuditReportService
{
    Task<AuditReportDto> GetReportAsync(ReportFilter filter, CancellationToken cancellationToken = default);
    ReportExportModel BuildExportModel(AuditReportDto report, string? organizationName, string? frameworkName);
}
