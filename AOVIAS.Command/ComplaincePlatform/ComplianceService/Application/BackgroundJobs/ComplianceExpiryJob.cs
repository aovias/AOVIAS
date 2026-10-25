using ComplianceService.Application.Contracts;
using ComplianceService.Application.Services;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.BackgroundJobs;

/// <summary>
/// Background job that processes compliance journeys that have expired.
/// Runs daily to automatically transition Approved journeys to Expired state.
/// Uses ComplianceJourneyService.AutoExpireJourneyAsync to ensure proper lifecycle management.
/// </summary>
public class ComplianceExpiryJob
{
    private readonly IComplianceJourneyRepository _journeyRepository;
    private readonly ComplianceJourneyService _journeyService;
    private readonly ILogger<ComplianceExpiryJob> _logger;

    public ComplianceExpiryJob(
        IComplianceJourneyRepository journeyRepository,
        ComplianceJourneyService journeyService,
        ILogger<ComplianceExpiryJob> logger)
    {
        _journeyRepository = journeyRepository;
        _journeyService = journeyService;
        _logger = logger;
    }

    /// <summary>
    /// Processes all compliance journeys that have expired.
    /// Called by Hangfire on a recurring schedule (daily).
    /// </summary>
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
    public async Task ProcessExpiredJourneysAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting ComplianceExpiryJob - Processing expired compliance journeys");

        try
        {
            var today = DateTime.UtcNow.Date;
            var expiredJourneys = await _journeyRepository.GetExpiringJourneysAsync(today, cancellationToken);

            _logger.LogInformation("Found {Count} compliance journeys that have expired", expiredJourneys.Count);

            var processedCount = 0;
            var errorCount = 0;

            foreach (var journey in expiredJourneys)
            {
                try
                {
                    await ProcessExpiredJourneyAsync(journey, cancellationToken);
                    processedCount++;
                }
                catch (Exception ex)
                {
                    errorCount++;
                    _logger.LogError(ex,
                        "Error processing expired journey {JourneyId} for Hospital {HospitalId}, Framework {FrameworkId}",
                        journey.Id, journey.HospitalId, journey.FrameworkId);
                }
            }

            _logger.LogInformation(
                "ComplianceExpiryJob completed - Processed: {ProcessedCount}, Errors: {ErrorCount}",
                processedCount, errorCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in ComplianceExpiryJob");
            throw; // Re-throw to trigger Hangfire retry mechanism
        }
    }

    private async Task ProcessExpiredJourneyAsync(ComplianceJourney journey, CancellationToken cancellationToken)
    {
        // Pre-validation: Check status and expiry date before calling service
        // (Service will also validate, but this avoids unnecessary service calls)
        if (journey.Status != ComplianceLifecycleState.Approved)
        {
            _logger.LogWarning(
                "Journey {JourneyId} is not in Approved state (current: {Status}), skipping expiry processing",
                journey.Id, journey.Status);
            return;
        }

        if (!journey.ExpiryDate.HasValue || journey.ExpiryDate.Value > DateTime.UtcNow)
        {
            _logger.LogWarning(
                "Journey {JourneyId} expiry date {ExpiryDate} is in the future, skipping",
                journey.Id, journey.ExpiryDate);
            return;
        }

        // Use ComplianceJourneyService to ensure proper lifecycle management,
        // audit history, domain events, and state machine validation
        try
        {
            await _journeyService.AutoExpireJourneyAsync(journey.Id, cancellationToken);

            _logger.LogInformation(
                "Successfully expired journey {JourneyId} for Hospital {HospitalId}, Framework {FrameworkId}",
                journey.Id, journey.HospitalId, journey.FrameworkId);
        }
        catch (InvalidOperationException ex)
        {
            // Business rule violation (e.g., already expired, not Approved, expiry date not reached)
            // Log and continue processing other journeys
            _logger.LogWarning(
                ex,
                "Cannot auto-expire journey {JourneyId}: {Message}",
                journey.Id,
                ex.Message);
        }
    }
}
