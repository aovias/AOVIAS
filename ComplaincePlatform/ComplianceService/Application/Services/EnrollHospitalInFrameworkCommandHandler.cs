using ComplianceService.Application.Authorization;
using ComplianceService.Application.Commands;
using ComplianceService.Application.Contracts;
using Shared.Infrastructure.DTO;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ComplianceService.Application.Services;

/// <summary>
/// Handler for EnrollHospitalInFrameworkCommand.
/// Orchestrates the enrollment process using the aggregate root pattern.
/// </summary>
public class EnrollHospitalInFrameworkCommandHandler
{
    private readonly IHospitalComplianceRepository _hospitalComplianceRepository;
    private readonly IFrameworkRepository _frameworkRepository;
    private readonly IDomainEventDispatcher _domainEventDispatcher;
    private readonly ComplianceDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IObligationSynchronizationService _obligationSynchronizationService;
    private readonly IHospitalAccessAuthorizer _hospitalAccessAuthorizer;
    private readonly IHospitalAccessCallerResolver _hospitalAccessCallerResolver;
    private readonly IHospitalAccessContext _hospitalAccessContext;
    private readonly ILogger<EnrollHospitalInFrameworkCommandHandler> _logger;

    public EnrollHospitalInFrameworkCommandHandler(
        IHospitalComplianceRepository hospitalComplianceRepository,
        IFrameworkRepository frameworkRepository,
        IDomainEventDispatcher domainEventDispatcher,
        ComplianceDbContext dbContext,
        IUnitOfWork unitOfWork,
        IObligationSynchronizationService obligationSynchronizationService,
        IHospitalAccessAuthorizer hospitalAccessAuthorizer,
        IHospitalAccessCallerResolver hospitalAccessCallerResolver,
        IHospitalAccessContext hospitalAccessContext,
        ILogger<EnrollHospitalInFrameworkCommandHandler> logger)
    {
        _hospitalComplianceRepository = hospitalComplianceRepository;
        _frameworkRepository = frameworkRepository;
        _domainEventDispatcher = domainEventDispatcher;
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _obligationSynchronizationService = obligationSynchronizationService;
        _hospitalAccessAuthorizer = hospitalAccessAuthorizer;
        _hospitalAccessCallerResolver = hospitalAccessCallerResolver;
        _hospitalAccessContext = hospitalAccessContext;
        _logger = logger;
    }

    public async Task<EnrollFrameworkResponseDto> HandleAsync(
        EnrollHospitalInFrameworkCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling enrollment: HospitalId={HospitalId}, FrameworkId={FrameworkId}, RequestedBy={RequestedByUserId}",
            command.HospitalId, command.FrameworkId, command.RequestedByUserId);

        var access = await ResolveHospitalAccessAsync(command.HospitalId, cancellationToken);
        if (!access.IsAllowed)
        {
            throw new HospitalAccessException(access);
        }

        var organizationId = access.OrganizationId;

        // Validate framework exists and is active
        var frameworks = await _frameworkRepository.GetAllActiveFrameworksAsync(cancellationToken);
        var framework = frameworks.FirstOrDefault(f => f.FrameworkId == command.FrameworkId);
        
        if (framework == null)
        {
            _logger.LogWarning("Framework {FrameworkId} not found or not active", command.FrameworkId);
            throw new InvalidOperationException($"Framework with ID {command.FrameworkId} not found or is not active.");
        }

        if (!framework.IsActive)
        {
            _logger.LogWarning("Framework {FrameworkId} is not active", command.FrameworkId);
            throw new InvalidOperationException($"Framework with ID {command.FrameworkId} is not active.");
        }

        // Catalog lifecycle: enroll only when Status == Active (in addition to IsActive sync)
        if (!string.Equals(framework.Status, nameof(Domain.Enums.FrameworkCatalogStatus.Active), StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Framework {FrameworkId} catalog status is {Status}, enrollment rejected",
                command.FrameworkId, framework.Status);
            throw new InvalidOperationException(
                $"Framework with ID {command.FrameworkId} cannot be enrolled when status is '{framework.Status}'. Status must be Active.");
        }

        var existingEnrollments = await _frameworkRepository.GetHospitalFrameworksAsync(command.HospitalId, cancellationToken);
        var alreadyEnrolled = existingEnrollments.Any(e => e.FrameworkId == command.FrameworkId);
        
        if (alreadyEnrolled)
        {
            _logger.LogWarning(
                "Hospital {HospitalId} is already enrolled in framework {FrameworkId}",
                command.HospitalId, command.FrameworkId);
            throw new InvalidOperationException(
                $"Hospital {command.HospitalId} is already enrolled in framework {command.FrameworkId}.");
        }

        // Check if aggregate already exists (rehydration scenario)
        // For new enrollments, LoadAsync may fail if no tasks/chapters exist yet
        HospitalComplianceAggregate aggregate;
        try
        {
            aggregate = await _hospitalComplianceRepository.LoadAsync(
                command.HospitalId,
                command.FrameworkId,
                cancellationToken);
            
            _logger.LogInformation(
                "Rehydrated existing aggregate for HospitalId={HospitalId}, FrameworkId={FrameworkId}",
                command.HospitalId, command.FrameworkId);
        }
        catch (InvalidOperationException ex)
        {
            // Aggregate doesn't exist or framework has no chapters yet
            // For new enrollment, create a new aggregate
            // The aggregate will be populated with framework chapters when tasks are created later
            aggregate = new HospitalComplianceAggregate(command.HospitalId, command.FrameworkId);
            _logger.LogInformation(
                "Created new aggregate for HospitalId={HospitalId}, FrameworkId={FrameworkId}. Note: {Message}",
                command.HospitalId, command.FrameworkId, ex.Message);
        }

        // Execute domain method
        aggregate.Enroll(command.RequestedByUserId);

        // Save aggregate (persists state changes)
        await _hospitalComplianceRepository.SaveAsync(aggregate, cancellationToken);

        // Collect domain events
        var domainEvents = aggregate.DomainEvents.ToList();

        // Clear domain events from aggregate
        aggregate.ClearDomainEvents();

        // Dispatch domain events to Outbox
        if (domainEvents.Any())
        {
            await _domainEventDispatcher.DispatchAsync(domainEvents, cancellationToken);
        }

        // Save changes (aggregate state + outbox messages in same transaction)
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Successfully enrolled HospitalId={HospitalId} in FrameworkId={FrameworkId} OrganizationId={OrganizationId}. Raised {EventCount} domain events.",
            command.HospitalId, command.FrameworkId, organizationId, domainEvents.Count);

        // Sync OrganizationComplianceObligation records for every ObjectiveElement in the framework.
        // Idempotent — safe on re-enrollment. Runs after the enrollment transaction commits.
        try
        {
            await _obligationSynchronizationService.SyncForOrganizationAsync(
                organizationId: organizationId,
                hospitalId: command.HospitalId,
                tenantId: organizationId,
                frameworkId: command.FrameworkId,
                ct: cancellationToken);
        }
        catch (Exception ex)
        {
            // Log but do not fail enrollment if obligation sync has a transient error.
            // Obligations can be re-synced via the internal API.
            _logger.LogWarning(ex,
                "Obligation sync failed after enrollment for HospitalId={HospitalId} FrameworkId={FrameworkId}. " +
                "Obligations can be re-synced via POST /internal/v1/obligations/sync",
                command.HospitalId, command.FrameworkId);
        }

        return new EnrollFrameworkResponseDto
        {
            HospitalId = command.HospitalId,
            FrameworkId = command.FrameworkId,
            // Lifecycle is owned by ComplianceJourney. At enrollment time we are always in Enrolled state.
            Status = "Enrolled",
            EnrolledOn = DateTime.UtcNow
        };
    }

    private async Task<HospitalAccessResult> ResolveHospitalAccessAsync(
        Guid hospitalId,
        CancellationToken cancellationToken)
    {
        if (_hospitalAccessContext.Current is { RequestedHospitalId: var authorizedId } current
            && authorizedId == hospitalId)
        {
            return current;
        }

        return await _hospitalAccessAuthorizer.AuthorizeAsync(
            hospitalId,
            _hospitalAccessCallerResolver.Resolve(),
            cancellationToken);
    }
}
