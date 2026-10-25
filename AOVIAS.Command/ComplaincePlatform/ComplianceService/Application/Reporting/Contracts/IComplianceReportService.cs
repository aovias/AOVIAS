using ComplianceService.Application.Reporting.Dtos;
using ComplianceService.Application.Reporting.Export;

namespace ComplianceService.Application.Reporting.Contracts;

public interface IComplianceReportService
{
    Task<ComplianceReportDto> GetReportAsync(ReportFilter filter, CancellationToken cancellationToken = default);
    ReportExportModel BuildExportModel(ComplianceReportDto report, string? organizationName, string? frameworkName);
}
