using ComplianceService.Application.Contracts;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.BackgroundJobs;

/// <summary>
/// Background job that escalates compliance journeys with high risk scores or unresolved audit findings.
/// Runs daily to identify and escalate journeys requiring attention.
/// </summary>
public class ComplianceEscalationJob
{
    private readonly IComplianceJourneyRepository _journeyRepository;
    private readonly IComplianceAuditFindingRepository _auditFindingRepository;
    private readonly ComplianceDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ComplianceEscalationJob> _logger;

    public ComplianceEscalationJob(
        IComplianceJourneyRepository journeyRepository,
        IComplianceAuditFindingRepository auditFindingRepository,
        ComplianceDbContext dbContext,
        IUnitOfWork unitOfWork,
        ILogger<ComplianceEscalationJob> logger)
    {
        _journeyRepository = journeyRepository;
        _auditFindingRepository = auditFindingRepository;
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Processes all compliance journeys and escalates those requiring attention.
    /// Called by Hangfire on a recurring schedule (daily).
    /// </summary>
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
    public async Task ProcessEscalationsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting ComplianceEscalationJob - Processing compliance escalations");

        try
        {
            // Get all active journeys (not Draft, Suspended, Expired, Rejected)
            var activeStatuses = new[]
            {
                ComplianceLifecycleState.Enrolled,
                ComplianceLifecycleState.GapAssessment,
                ComplianceLifecycleState.InProgress,
                ComplianceLifecycleState.Submitted,
                ComplianceLifecycleState.AuditScheduled,
                ComplianceLifecycleState.AuditCompleted,
                ComplianceLifecycleState.Approved,
                ComplianceLifecycleState.RenewalInProgress
            };

            var escalatedCount = 0;
            var errorCount = 0;

            foreach (var status in activeStatuses)
            {
                var journeys = await _journeyRepository.GetByStatusAsync(status, cancellationToken);

                foreach (var journey in journeys)
                {
                    try
                    {
                        var shouldEscalate = await CheckAndEscalateAsync(journey, cancellationToken);
                        if (shouldEscalate)
                        {
                            escalatedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        _logger.LogError(ex,
                            "Error processing escalation for journey {JourneyId} for Hospital {HospitalId}, Framework {FrameworkId}",
                            journey.Id, journey.HospitalId, journey.FrameworkId);
                    }
                }
            }

            _logger.LogInformation(
                "ComplianceEscalationJob completed - Escalated: {EscalatedCount}, Errors: {ErrorCount}",
                escalatedCount, errorCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in ComplianceEscalationJob");
            throw; // Re-throw to trigger Hangfire retry mechanism
        }
    }

    private async Task<bool> CheckAndEscalateAsync(ComplianceJourney journey, CancellationToken cancellationToken)
    {
        var shouldEscalate = false;
        var escalationReasons = new List<string>();

        // Check risk score (threshold: 50)
        if (journey.RiskScore >= 50)
        {
            shouldEscalate = true;
            escalationReasons.Add($"High risk score: {journey.RiskScore}");
        }

        // Check for overdue audit findings
        var unresolvedFindings = await _auditFindingRepository.GetUnresolvedByJourneyIdAsync(journey.Id, cancellationToken);
        var overdueFindings = unresolvedFindings
            .Where(f => f.DueDate.HasValue && f.DueDate.Value < DateTime.UtcNow)
            .ToList();

        if (overdueFindings.Any())
        {
            shouldEscalate = true;
            escalationReasons.Add($"{overdueFindings.Count} overdue audit findings");
        }

        // Check if global corrective action deadline for the journey has been exceeded
        if (journey.CorrectiveActionDeadline.HasValue &&
            DateTime.UtcNow > journey.CorrectiveActionDeadline.Value &&
            unresolvedFindings.Any())
        {
            shouldEscalate = true;
            escalationReasons.Add("Corrective action deadline exceeded for unresolved findings");
        }

        // Check for unresolved major NCs
        var hasUnresolvedMajorNc = await _auditFindingRepository.HasUnresolvedMajorNcAsync(journey.Id, cancellationToken);
        if (hasUnresolvedMajorNc)
        {
            shouldEscalate = true;
            escalationReasons.Add("Unresolved major non-conformities");
        }

        if (shouldEscalate)
        {
            // Recalculate risk score based on current findings
            var majorNcCount = await _auditFindingRepository.GetMajorNcCountAsync(journey.Id, cancellationToken);
            var minorNcCount = await _auditFindingRepository.GetMinorNcCountAsync(journey.Id, cancellationToken);
            
            var newRiskScore = journey.CalculateRiskScore(majorNcCount, minorNcCount);
            journey.UpdateRiskScore(newRiskScore);

            await _journeyRepository.UpdateAsync(journey, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogWarning(
                "Escalated journey {JourneyId} for Hospital {HospitalId}, Framework {FrameworkId}. Reasons: {Reasons}",
                journey.Id, journey.HospitalId, journey.FrameworkId, string.Join(", ", escalationReasons));
        }

        return shouldEscalate;
    }
}
