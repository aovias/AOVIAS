using ComplianceService.Application.Contracts;
using ComplianceService.Application.TaskExecution;
using ComplianceService.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Application.BackgroundJobs;

/// <summary>
/// Materializes due <see cref="Domain.RecurringTaskDefinition"/> rows into <see cref="Domain.ComplianceTask"/> occurrences.
/// </summary>
public sealed class RecurringTaskMaterializationJob
{
    private readonly ComplianceDbContext _db;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RecurringTaskMaterializationJob> _logger;

    public RecurringTaskMaterializationJob(
        ComplianceDbContext db,
        IUnitOfWork unitOfWork,
        ILogger<RecurringTaskMaterializationJob> logger)
    {
        _db = db;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
    public async Task MaterializeDueOccurrencesAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        _logger.LogInformation("Starting RecurringTaskMaterializationJob at {UtcNow:o}", utcNow);

        var candidates = await _db.RecurringTaskDefinitions
            .Where(d => d.IsActive)
            .Where(d => d.NextOccurrenceDate == null || d.NextOccurrenceDate <= utcNow)
            .OrderBy(d => d.NextOccurrenceDate)
            .Take(500)
            .ToListAsync(cancellationToken);

        var created = 0;
        foreach (var def in candidates)
        {
            if (!RecurringTaskMaterializer.IsDue(def, utcNow))
            {
                continue;
            }

            var occurrence = (def.NextOccurrenceDate ?? def.StartDate).Date;
            var taskId = Guid.NewGuid();
            var task = RecurringTaskMaterializer.MaterializeOccurrence(def, occurrence, taskId);
            await _db.ComplianceTasks.AddAsync(task, cancellationToken);
            RecurringTaskMaterializer.AdvanceDefinition(def, occurrence, taskId, utcNow);
            created++;
        }

        if (created > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation(
            "RecurringTaskMaterializationJob completed. Candidates={Candidates}, Created={Created}",
            candidates.Count,
            created);
    }
}
