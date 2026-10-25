using ComplianceService.Application.DTOs;

namespace ComplianceService.Application.Contracts;

public interface IReportingService
{
    Task<ComplianceScoreReportDto> GetComplianceScoreReportAsync(
        Guid hospitalId,
        Guid? frameworkId,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default);

    Task<AuditFindingsReportDto> GetAuditFindingsReportAsync(
        Guid hospitalId,
        Guid? frameworkId,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default);

    Task<NabhReadinessReportDto> GetNabhReadinessReportAsync(
        Guid hospitalId,
        CancellationToken cancellationToken = default);
}

