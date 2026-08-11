using ComplianceService.Application.Contracts;

using ComplianceService.Domain;

using ComplianceService.Domain.Enums;

using ComplianceService.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;



namespace ComplianceService.Application.Services;



/// <summary>

/// Default implementation of IComplianceTaskGenerationService.

/// Generates ComplianceTask rows for all applicable Objective Elements of the journey's framework.

/// </summary>

public class ComplianceTaskGenerationService : IComplianceTaskGenerationService

{

    private readonly IComplianceJourneyRepository _journeyRepository;

    private readonly ComplianceDbContext _dbContext;

    private readonly IComplianceTaskRepository _taskRepository;

    private readonly IUnitOfWork _unitOfWork;

    private readonly ILogger<ComplianceTaskGenerationService> _logger;



    public ComplianceTaskGenerationService(

        IComplianceJourneyRepository journeyRepository,

        ComplianceDbContext dbContext,

        IComplianceTaskRepository taskRepository,

        IUnitOfWork unitOfWork,

        ILogger<ComplianceTaskGenerationService> logger)

    {

        _journeyRepository = journeyRepository;

        _dbContext = dbContext;

        _taskRepository = taskRepository;

        _unitOfWork = unitOfWork;

        _logger = logger;

    }



    public async Task GenerateTasksForJourneyAsync(
        Guid journeyId,
        Guid creatingUserId,
        CancellationToken cancellationToken = default)

    {

        var journey = await _journeyRepository.GetByIdAsync(journeyId, cancellationToken);

        if (journey == null)

        {

            throw new InvalidOperationException($"Compliance journey {journeyId} not found");

        }

        var assignee = creatingUserId.ToString();
        if (string.IsNullOrWhiteSpace(assignee))
        {
            throw new InvalidOperationException("creatingUserId must resolve to a non-empty assignee string.");
        }

        _logger.LogInformation(

            "Generating compliance tasks for journey {JourneyId}, HospitalId={HospitalId}, FrameworkId={FrameworkId}",

            journeyId, journey.HospitalId, journey.FrameworkId);



        var objectiveElements = await _dbContext.ObjectiveElements

            .Include(oe => oe.ComplianceStandard)

                .ThenInclude(s => s!.Chapter)

            .Where(oe => oe.ComplianceStandard.Chapter.FrameworkId == journey.FrameworkId && oe.IsApplicable)

            .ToListAsync(cancellationToken);



        if (!objectiveElements.Any())

        {

            _logger.LogWarning(

                "No applicable objective elements found for FrameworkId={FrameworkId}. No tasks generated for journey {JourneyId}.",

                journey.FrameworkId, journeyId);

            return;

        }



        var oeIds = objectiveElements.Select(oe => oe.Id).ToList();

        var tasksForOe = await _dbContext.ComplianceTasks
            .Where(t => t.HospitalId == journey.HospitalId && oeIds.Contains(t.ObjectiveElementId))
            .ToListAsync(cancellationToken);

        var backfillCount = 0;
        foreach (var t in tasksForOe)
        {
            if (string.IsNullOrWhiteSpace(t.AssignedToUserId))
            {
                t.AssignedToUserId = assignee;
                backfillCount++;
            }
        }

        if (backfillCount > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var existingOeIds = tasksForOe
            .Select(t => t.ObjectiveElementId)
            .ToHashSet();

        var enrollmentDate = journey.EnrollmentDate ?? DateTime.UtcNow;

        var createdCount = 0;

        foreach (var oe in objectiveElements)
        {
            if (existingOeIds.Contains(oe.Id))
            {
                continue;
            }

            var chapter = oe.ComplianceStandard!.Chapter;

            var dueDate = enrollmentDate.AddDays(30);

            var weight = oe.Weight > 0 ? oe.Weight :

                (chapter.Weight > 0 ? chapter.Weight : 1.0m);

            var task = new ComplianceTask

            {

                Id = Guid.NewGuid(),

                HospitalId = journey.HospitalId,

                ComplianceStandardId = oe.ComplianceStandardId,

                ObjectiveElementId = oe.Id,

                ControlId = null,

                DepartmentName = string.IsNullOrWhiteSpace(oe.Department) ? null : oe.Department.Trim(),

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

            await _taskRepository.AddAsync(task);

            createdCount++;

        }

        if (createdCount > 0)

        {

            await _unitOfWork.SaveChangesAsync(cancellationToken);

        }

        var skippedExisting = objectiveElements.Count - createdCount;

        _logger.LogInformation(

            "Compliance tasks sync for journey {JourneyId}: created {Created}, skipped existing {SkippedExisting}, framework OE count {Total}",

            journeyId,

            createdCount,

            skippedExisting,

            objectiveElements.Count);

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

}

