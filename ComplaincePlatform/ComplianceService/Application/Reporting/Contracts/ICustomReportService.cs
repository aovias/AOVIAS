using ComplianceService.Application.Reporting.Dtos;
using ComplianceService.Application.Reporting.Export;

namespace ComplianceService.Application.Reporting.Contracts;

public interface ICustomReportService
{
    Task<CustomReportResultDto> GenerateAsync(
        ReportFilter filter,
        CustomReportRequest request,
        CancellationToken cancellationToken = default);

    ReportExportModel BuildExportModel(CustomReportResultDto report, string? organizationName, string? frameworkName);
}
