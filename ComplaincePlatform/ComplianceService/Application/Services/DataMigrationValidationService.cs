using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Application.Services;

public sealed class DataMigrationValidationService : IDataMigrationValidationService
{
    private readonly ComplianceDbContext _dbContext;

    public DataMigrationValidationService(ComplianceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DataMigrationValidationReportDto> ValidateAsync(CancellationToken cancellationToken = default)
    {
        var tasksWithoutOe = await _dbContext.ComplianceTasks
            .AsNoTracking()
            .CountAsync(t => t.ObjectiveElementId == Guid.Empty, cancellationToken);

        var evidenceWithoutTask = await _dbContext.Evidences
            .AsNoTracking()
            .CountAsync(e => e.TaskId == Guid.Empty, cancellationToken);

        var findingsWithoutScopeLinkage = await _dbContext.Findings
            .AsNoTracking()
            .CountAsync(f =>
                (f.Scope == FindingScope.Audit && (!f.ComplianceJourneyId.HasValue || !f.ObjectiveElementId.HasValue)) ||
                (f.Scope == FindingScope.Organization && f.TenantId == Guid.Empty),
                cancellationToken);

        var findingsWithInvalidComplianceState = await _dbContext.Findings
            .AsNoTracking()
            .CountAsync(f =>
                f.ComplianceStatus != ComplianceEvaluationStatus.FullyCompliant &&
                f.ComplianceStatus != ComplianceEvaluationStatus.PartiallyCompliant &&
                f.ComplianceStatus != ComplianceEvaluationStatus.NonCompliant &&
                f.ComplianceStatus != ComplianceEvaluationStatus.NotApplicable,
                cancellationToken);

        var suggestions = new List<string>
        {
            "Auto-fix: map findings with missing status to NonCompliant and mark remarks with migration note.",
            "Manual-fix: task rows with empty ObjectiveElementId need business review and OE mapping.",
            "Manual-fix: evidence rows without TaskId require remapping before cutover."
        };

        var hasCritical = tasksWithoutOe > 0 || evidenceWithoutTask > 0 || findingsWithoutScopeLinkage > 0;

        return new DataMigrationValidationReportDto
        {
            TasksWithoutObjectiveElementId = tasksWithoutOe,
            EvidenceWithoutTaskId = evidenceWithoutTask,
            FindingsWithoutScopeLinkage = findingsWithoutScopeLinkage,
            FindingsWithInvalidComplianceState = findingsWithInvalidComplianceState,
            HasCriticalIssues = hasCritical,
            SuggestedFixes = suggestions
        };
    }
}
