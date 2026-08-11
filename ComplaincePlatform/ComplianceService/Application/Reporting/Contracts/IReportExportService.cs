using ComplianceService.Application.Reporting.Export;

namespace ComplianceService.Application.Reporting.Contracts;

public interface IReportExportService
{
    (byte[] Content, string ContentType, string FileName) Export(ReportExportModel model, ReportExportFormat format);
}
