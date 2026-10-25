using ComplianceService.Application.Reporting.Dtos;
using ComplianceService.Application.Reporting.Export;

namespace ComplianceService.Application.Reporting.Contracts;

public interface IEvidenceReportService
{
    Task<EvidenceReportDto> GetReportAsync(ReportFilter filter, CancellationToken cancellationToken = default);
    ReportExportModel BuildExportModel(EvidenceReportDto report, string? organizationName, string? frameworkName);
}
