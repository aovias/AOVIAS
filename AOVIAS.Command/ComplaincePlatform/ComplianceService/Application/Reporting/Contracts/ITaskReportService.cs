using ComplianceService.Application.Reporting.Dtos;
using ComplianceService.Application.Reporting.Export;

namespace ComplianceService.Application.Reporting.Contracts;

public interface ITaskReportService
{
    Task<TaskReportDto> GetReportAsync(ReportFilter filter, CancellationToken cancellationToken = default);
    ReportExportModel BuildExportModel(TaskReportDto report, string? organizationName, string? frameworkName);
}
