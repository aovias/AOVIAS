using ComplianceService.Application.Reporting.Dtos;

namespace ComplianceService.Application.Reporting;

internal static class ReportCacheKeys
{
    public static string Build(string reportType, ReportFilter filter) =>
        $"report:{reportType}:{filter.HospitalId:N}:{filter.FrameworkId:N}:{filter.ChapterId:N}:{filter.DepartmentId:N}:{filter.Severity}:{filter.TaskStatus}:{filter.FromDate:O}:{filter.ToDate:O}:{filter.OrganizationId:N}";
}
