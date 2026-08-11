using ComplianceService.Application.Contracts;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ComplianceService.Application.Services;

public sealed class ComplianceTaskEnsureService : IComplianceTaskEnsureService
{
    private readonly ComplianceDbContext _db;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ComplianceTaskEnsureService> _logger;

    public ComplianceTaskEnsureService(
        ComplianceDbContext db,
        IUnitOfWork unitOfWork,
        ILogger<ComplianceTaskEnsureService> logger)
    {
        _db = db;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ComplianceTask> EnsureComplianceTaskForObjectiveElementAsync(
        ComplianceJourney journey,
        Guid objectiveElementId,
        Guid assignedUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(journey);
        if (assignedUserId == Guid.Empty)
        {
            throw new ArgumentException("A valid user id is required to assign the compliance task.", nameof(assignedUserId));
        }

        var oe = await _db.ObjectiveElements
            .Include(o => o.ComplianceStandard!)
            .ThenInclude(s => s!.Chapter)
            .FirstOrDefaultAsync(
                o => o.Id == objectiveElementId
                     && o.ComplianceStandard!.Chapter.FrameworkId == journey.FrameworkId,
                cancellationToken);

        if (oe == null)
        {
            throw new InvalidOperationException(
                "The objective element is not part of this journey's framework.");
        }

        if (!oe.IsApplicable)
        {
            throw new InvalidOperationException(
                "The objective element is not applicable for this framework and cannot receive evidence.");
        }

        var assignee = assignedUserId.ToString();
        if (string.IsNullOrWhiteSpace(assignee))
        {
            throw new InvalidOperationException("Assigned user id cannot be empty.");
        }

        var existing = await _db.ComplianceTasks
            .FirstOrDefaultAsync(
                t => t.HospitalId == journey.HospitalId && t.ObjectiveElementId == objectiveElementId,
                cancellationToken);

        var oeDepartmentLabel = NormalizeOeDepartmentLabel(oe.Department);

        if (existing != null)
        {
            var changed = false;
            if (string.IsNullOrWhiteSpace(existing.AssignedToUserId))
            {
                existing.AssignedToUserId = assignee;
                changed = true;
            }

            // Backfill denormalized department label from master OE metadata when task was created before DepartmentName was populated.
            if (string.IsNullOrWhiteSpace(existing.DepartmentName) && oeDepartmentLabel != null)
            {
                existing.DepartmentName = oeDepartmentLabel;
                changed = true;
            }

            if (changed)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return existing;
        }

        var enrollmentDate = journey.EnrollmentDate ?? DateTime.UtcNow;
        var chapter = oe.ComplianceStandard!.Chapter;
        var dueDate = enrollmentDate.AddDays(30);
        var weight = oe.Weight > 0 ? oe.Weight : (chapter.Weight > 0 ? chapter.Weight : 1.0m);

        var task = new ComplianceTask
        {
            Id = Guid.NewGuid(),
            HospitalId = journey.HospitalId,
            ComplianceStandardId = oe.ComplianceStandardId,
            ObjectiveElementId = oe.Id,
            ControlId = null,
            DepartmentName = oeDepartmentLabel,
            AssignedToUserId = assignee,
            DueDate = dueDate,
            Status = ComplianceTaskStatus.Pending,
            Title = oe.Title,
            MaturityState = null,
            Explainability = null,
            Criticality = MapClassificationToCriticality(oe.Classification),
            Priority = ComplianceTaskPriority.Medium,
            Weight = weight,
            CreatedAt = DateTime.UtcNow,
            RowVersion = new byte[8]
        };

        await _db.ComplianceTasks.AddAsync(task, cancellationToken);
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsSqlServerDuplicateKeyException(ex))
        {
            _db.Entry(task).State = EntityState.Detached;
            _logger.LogInformation(
                ex,
                "ComplianceTask create raced for HospitalId={HospitalId} ObjectiveElementId={ObjectiveElementId}; loading existing.",
                journey.HospitalId,
                objectiveElementId);

            var raced = await _db.ComplianceTasks
                .FirstOrDefaultAsync(
                    t => t.HospitalId == journey.HospitalId && t.ObjectiveElementId == objectiveElementId,
                    cancellationToken);

            if (raced == null)
            {
                throw;
            }

            var racedChanged = false;
            if (string.IsNullOrWhiteSpace(raced.AssignedToUserId))
            {
                raced.AssignedToUserId = assignee;
                racedChanged = true;
            }

            if (string.IsNullOrWhiteSpace(raced.DepartmentName) && oeDepartmentLabel != null)
            {
                raced.DepartmentName = oeDepartmentLabel;
                racedChanged = true;
            }

            if (racedChanged)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return raced;
        }

        return task;
    }

    /// <summary>Objective elements store department as a free-text label (master-data enrichment), not a hospital department Guid.</summary>
    private static string? NormalizeOeDepartmentLabel(string? department)
    {
        if (string.IsNullOrWhiteSpace(department))
        {
            return null;
        }

        return department.Trim();
    }

    private static TaskCriticalityLevel MapClassificationToCriticality(ObjectiveElementClassification classification)
    {
        return classification switch
        {
            ObjectiveElementClassification.Core => TaskCriticalityLevel.High,
            ObjectiveElementClassification.Commitment => TaskCriticalityLevel.Medium,
            ObjectiveElementClassification.Achievement => TaskCriticalityLevel.Medium,
            ObjectiveElementClassification.Excellence => TaskCriticalityLevel.Low,
            _ => TaskCriticalityLevel.Medium
        };
    }

    private static bool IsSqlServerDuplicateKeyException(DbUpdateException ex)
    {
        for (var e = ex.InnerException; e != null; e = e.InnerException)
        {
            if (e is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
            {
                return true;
            }
        }

        var msg = ex.InnerException?.Message ?? ex.Message;
        return msg.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
               || msg.Contains("duplicate key", StringComparison.OrdinalIgnoreCase);
    }
}
