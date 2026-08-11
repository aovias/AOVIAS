using ComplianceService.Application.DTOs;

namespace ComplianceService.Application.Contracts;

public interface IDataMigrationValidationService
{
    Task<DataMigrationValidationReportDto> ValidateAsync(CancellationToken cancellationToken = default);
}
