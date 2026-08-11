using ComplianceService.Application.Contracts;
using ComplianceService.Application.Services;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.BackgroundJobs;

/// <summary>
/// Background job that processes compliance journeys entering the renewal window.
/// Runs daily to automatically transition Approved journeys to RenewalInProgress state.
/// Uses ComplianceJourneyService.OpenRenewalWindowAsync to ensure proper lifecycle management.
/// </summary>
public class ComplianceRenewalWindowJob
{
    private readonly IComplianceJourneyRepository _journeyRepository;
    private readonly ComplianceJourneyService _journeyService;
    private readonly ILogger<ComplianceRenewalWindowJob> _logger;

    public ComplianceRenewalWindowJob(
        IComplianceJourneyRepository journeyRepository,
        ComplianceJourneyService journeyService,
        ILogger<ComplianceRenewalWindowJob> logger)
    {
        _journeyRepository = journeyRepository;
        _journeyService = journeyService;
        _logger = logger;
    }

    /// <summary>
    /// Processes all compliance journeys entering the renewal window.
    /// Called by Hangfire on a recurring schedule (daily).
    /// </summary>
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
    public async Task ProcessRenewalWindowJourneysAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting ComplianceRenewalWindowJob - Processing journeys entering renewal window");

        try
        {
            var today = DateTime.UtcNow.Date;
            var renewalWindowJourneys = await _journeyRepository.GetJourneysInRenewalWindowAsync(today, cancellationToken);

            _logger.LogInformation("Found {Count} compliance journeys entering renewal window", renewalWindowJourneys.Count);

            var processedCount = 0;
            var errorCount = 0;

            foreach (var journey in renewalWindowJourneys)
            {
                try
                {
                    await ProcessRenewalWindowJourneyAsync(journey, cancellationToken);
                    processedCount++;
                }
                catch (Exception ex)
                {
                    errorCount++;
                    _logger.LogError(ex,
                        "Error processing renewal window journey {JourneyId} for Hospital {HospitalId}, Framework {FrameworkId}",
                        journey.Id, journey.HospitalId, journey.FrameworkId);
                }
            }

            _logger.LogInformation(
                "ComplianceRenewalWindowJob completed - Processed: {ProcessedCount}, Errors: {ErrorCount}",
                processedCount, errorCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in ComplianceRenewalWindowJob");
            throw; // Re-throw to trigger Hangfire retry mechanism
        }
    }

    private async Task ProcessRenewalWindowJourneyAsync(ComplianceJourney journey, CancellationToken cancellationToken)
    {
        // Pre-validation: Check status and renewal window date before calling service
        // (Service will also validate, but this avoids unnecessary service calls)
        if (journey.Status != ComplianceLifecycleState.Approved)
        {
            _logger.LogWarning(
                "Journey {JourneyId} is not in Approved state (current: {Status}), skipping renewal window processing",
                journey.Id, journey.Status);
            return;
        }

        if (!journey.RenewalWindowStartDate.HasValue || journey.RenewalWindowStartDate.Value > DateTime.UtcNow)
        {
            _logger.LogWarning(
                "Journey {JourneyId} renewal window start date {RenewalWindowStartDate} is in the future, skipping",
                journey.Id, journey.RenewalWindowStartDate);
            return;
        }

        // Use ComplianceJourneyService to ensure proper lifecycle management,
        // audit history, domain events, and state machine validation
        try
        {
            await _journeyService.OpenRenewalWindowAsync(journey.Id, cancellationToken);

            _logger.LogInformation(
                "Successfully opened renewal window for journey {JourneyId} for Hospital {HospitalId}, Framework {FrameworkId}",
                journey.Id, journey.HospitalId, journey.FrameworkId);
        }
        catch (InvalidOperationException ex)
        {
            // Business rule violation (e.g., already RenewalInProgress, not Approved, renewal window not reached)
            // Log and continue processing other journeys
            _logger.LogWarning(
                ex,
                "Cannot open renewal window for journey {JourneyId}: {Message}",
                journey.Id,
                ex.Message);
        }
    }
}
