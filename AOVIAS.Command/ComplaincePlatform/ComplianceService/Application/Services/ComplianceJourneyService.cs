using ComplianceService.Application.Contracts;
using ComplianceService.Application.Dashboard.Summary;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.Events;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.Domain.Enums;
using System.Text.Json;

namespace ComplianceService.Application.Services;

/// <summary>
/// Application service for managing compliance journey state transitions.
    /// Orchestrates business logic, role validation and audit trail, and ensures
    /// that all lifecycle mutations are centralized here.
/// </summary>
public class ComplianceJourneyService
{
    private readonly IComplianceJourneyRepository _journeyRepository;
    private readonly IComplianceJourneyHistoryRepository _historyRepository;
    private readonly IFrameworkConfigurationRepository _frameworkConfigRepository;
    private readonly IComplianceAuditFindingRepository _auditFindingRepository;
    private readonly IFrameworkJourneyPolicyFactory _policyFactory;
    private readonly IComplianceTaskGenerationService _taskGenerationService;
    private readonly ComplianceDbContext _dbContext;
    private readonly IDomainEventDispatcher _domainEventDispatcher;
    private readonly IComplianceEventEmitter _eventEmitter;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IComplianceScoreService _complianceScore;
    private readonly IDashboardSummaryCache _dashboardSummaryCache;
    private readonly ILogger<ComplianceJourneyService> _logger;

    public ComplianceJourneyService(
        IComplianceJourneyRepository journeyRepository,
        IComplianceJourneyHistoryRepository historyRepository,
        IFrameworkConfigurationRepository frameworkConfigRepository,
        IComplianceAuditFindingRepository auditFindingRepository,
        IFrameworkJourneyPolicyFactory policyFactory,
        IComplianceTaskGenerationService taskGenerationService,
        ComplianceDbContext dbContext,
        IDomainEventDispatcher domainEventDispatcher,
        IComplianceEventEmitter eventEmitter,
        IUnitOfWork unitOfWork,
        IComplianceScoreService complianceScore,
        IDashboardSummaryCache dashboardSummaryCache,
        ILogger<ComplianceJourneyService> logger)
    {
        _journeyRepository = journeyRepository;
        _historyRepository = historyRepository;
        _frameworkConfigRepository = frameworkConfigRepository;
        _auditFindingRepository = auditFindingRepository;
        _policyFactory = policyFactory;
        _taskGenerationService = taskGenerationService;
        _dbContext = dbContext;
        _domainEventDispatcher = domainEventDispatcher;
        _eventEmitter = eventEmitter;
        _unitOfWork = unitOfWork;
        _complianceScore = complianceScore;
        _dashboardSummaryCache = dashboardSummaryCache;
        _logger = logger;
    }

    /// <summary>
    /// Returns detailed information about a compliance journey, including
    /// current status, valid next states, key dates and scores.
    /// Enforces tenant and hospital isolation.
    /// </summary>
    public async Task<JourneyDetailsDto> GetJourneyDetailsAsync(
        Guid complianceJourneyId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var journey = await _journeyRepository.GetByIdAsync(complianceJourneyId, cancellationToken);
        if (journey == null)
        {
            throw new InvalidOperationException($"Compliance journey {complianceJourneyId} not found");
        }

        // Enforce tenant isolation
        if (journey.TenantId != tenantId)
        {
            throw new UnauthorizedAccessException(
                $"Access denied: Journey {complianceJourneyId} does not belong to tenant {tenantId}");
        }

        var currentStatus = journey.Status;
        var validNextStates = Shared.Infrastructure.Domain.ComplianceLifecycleStateMachine
            .GetValidNextStates(currentStatus);

        // SurveillanceDue: placeholder check for NABH-like frameworks.
        // In future, derive from NABH policy (e.g., based on ApprovalDate/ExpiryDate).
        bool? surveillanceDue = null;
        var frameworkName = journey.Framework?.Name ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(frameworkName) &&
            frameworkName.StartsWith("NABH", StringComparison.OrdinalIgnoreCase))
        {
            surveillanceDue = false; // TODO: implement real surveillance logic
        }

        var renewalWindowOpen =
            journey.RenewalWindowStartDate.HasValue &&
            DateTime.UtcNow >= journey.RenewalWindowStartDate.Value;

        // Progress percentage comes from the canonical single source of truth (live OE-based
        // aggregation, 0-100) so the journey header matches Dashboard, Sidebar and Reports.
        var progressPercentage = await _complianceScore.GetJourneyScoreAsync(
            journey.FrameworkId, journey.Id, cancellationToken);

        // RiskIndex = RiskScore (can be enhanced with weighted calculation later)
        var riskIndex = journey.RiskScore;

        // TargetAccreditationDate: Use ExpiryDate if approved, otherwise calculate from validity period
        DateTime? targetAccreditationDate = null;
        if (journey.Status == ComplianceLifecycleState.Approved && journey.ExpiryDate.HasValue)
        {
            targetAccreditationDate = journey.ExpiryDate;
        }
        else if (journey.EnrollmentDate.HasValue && journey.ValidityPeriodInMonths > 0)
        {
            targetAccreditationDate = journey.EnrollmentDate.Value.AddMonths(journey.ValidityPeriodInMonths);
        }

        return new JourneyDetailsDto
        {
            JourneyId = journey.Id,
            HospitalId = journey.HospitalId,
            FrameworkId = journey.FrameworkId,
            FrameworkName = frameworkName,
            CurrentState = currentStatus.ToString(),
            CurrentStatus = currentStatus,
            ValidNextStates = validNextStates,
            StartDate = journey.EnrollmentDate,
            TargetAccreditationDate = targetAccreditationDate,
            ExpiryDate = journey.ExpiryDate,
            ProgressPercentage = progressPercentage,
            ReadinessScore = progressPercentage,
            RiskScore = journey.RiskScore,
            RiskIndex = riskIndex,
            SurveillanceDue = surveillanceDue,
            RenewalWindowOpen = renewalWindowOpen
        };
    }

    /// <summary>
    /// Returns the valid next states for a journey after applying core state
    /// machine rules, framework policy, and role-based restrictions.
    /// Enforces tenant isolation.
    /// </summary>
    public async Task<List<ComplianceLifecycleState>> GetValidNextStatesAsync(
        Guid complianceJourneyId,
        Guid userId,
        IReadOnlyList<string> roles,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var roleForPolicy = roles.FirstOrDefault() ?? string.Empty;
        var journey = await _journeyRepository.GetByIdAsync(complianceJourneyId, cancellationToken);
        if (journey == null)
        {
            throw new InvalidOperationException($"Compliance journey {complianceJourneyId} not found");
        }

        // Enforce tenant isolation
        if (journey.TenantId != tenantId)
        {
            throw new UnauthorizedAccessException(
                $"Access denied: Journey {complianceJourneyId} does not belong to tenant {tenantId}");
        }

        var currentStatus = journey.Status;

        // Base candidates from core state machine
        var candidates = Shared.Infrastructure.Domain.ComplianceLifecycleStateMachine
            .GetValidNextStates(currentStatus);

        // Resolve framework-specific policy
        var frameworkCode = journey.Framework?.Name ?? "DEFAULT";
        var policy = _policyFactory.GetPolicy(frameworkCode);

        var allowed = new List<ComplianceLifecycleState>();

        foreach (var candidate in candidates)
        {
            // Skip no-op transitions
            if (candidate == currentStatus)
            {
                continue;
            }

            // First, let policy decide
            if (!policy.CanTransition(currentStatus, candidate, roleForPolicy, journey, out _))
            {
                continue;
            }

            // Then apply role-based hard rules
            try
            {
                ValidateRoleForTransition(currentStatus, candidate, roles);
                allowed.Add(candidate);
            }
            catch
            {
                // Role not allowed; exclude from list
            }
        }

        return allowed;
    }

    /// <summary>
    /// Gets all chapters for a compliance journey with completion percentages.
    /// Enforces tenant isolation.
    /// </summary>
    public async Task<List<JourneyChapterDto>> GetJourneyChaptersAsync(
        Guid complianceJourneyId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var journey = await _journeyRepository.GetByIdAsync(complianceJourneyId, cancellationToken);
        if (journey == null)
        {
            throw new InvalidOperationException($"Compliance journey {complianceJourneyId} not found");
        }

        // Enforce tenant isolation
        if (journey.TenantId != tenantId)
        {
            throw new UnauthorizedAccessException(
                $"Access denied: Journey {complianceJourneyId} does not belong to tenant {tenantId}");
        }

        // Get all chapters for the framework
        var chapters = await _dbContext.ComplianceChapters
            .AsNoTracking()
            .Where(c => c.FrameworkId == journey.FrameworkId)
            .OrderBy(c => c.Sequence)
            .ToListAsync(cancellationToken);

        var mandatoryOes = await _dbContext.ObjectiveElements
            .AsNoTracking()
            .Include(oe => oe.ComplianceStandard)
                .ThenInclude(s => s.Chapter)
            .Where(oe => oe.IsMandatory && oe.IsApplicable)
            .Where(oe => oe.ComplianceStandard.Chapter.FrameworkId == journey.FrameworkId)
            .ToListAsync(cancellationToken);

        var verifiedOeIds = await GetVerifiedObjectiveElementIdsForHospitalFrameworkAsync(
            journey.HospitalId,
            journey.FrameworkId,
            cancellationToken);

        var chapterDtos = new List<JourneyChapterDto>();
        foreach (var chapter in chapters)
        {
            var chapterOes = mandatoryOes.Where(oe => oe.ComplianceStandard.ChapterId == chapter.Id).ToList();
            var total = chapterOes.Count;
            var completed = chapterOes.Count(oe => verifiedOeIds.Contains(oe.Id));

            var completionPercentage = total > 0
                ? (decimal)completed / total * 100
                : 0;

            chapterDtos.Add(new JourneyChapterDto
            {
                ChapterId = chapter.Id,
                ChapterCode = chapter.Code,
                ChapterName = chapter.Name,
                Sequence = chapter.Sequence,
                CompletionPercentage = Math.Round(completionPercentage, 2),
                TotalObjectiveElements = total,
                CompletedObjectiveElements = completed
            });
        }

        return chapterDtos;
    }

    /// <summary>
    /// Gets non-conformance summary for a compliance journey.
    /// Enforces tenant isolation.
    /// </summary>
    public async Task<NonConformanceSummaryDto> GetNonConformanceSummaryAsync(
        Guid complianceJourneyId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var journey = await _journeyRepository.GetByIdAsync(complianceJourneyId, cancellationToken);
        if (journey == null)
        {
            throw new InvalidOperationException($"Compliance journey {complianceJourneyId} not found");
        }

        // Enforce tenant isolation
        if (journey.TenantId != tenantId)
        {
            throw new UnauthorizedAccessException(
                $"Access denied: Journey {complianceJourneyId} does not belong to tenant {tenantId}");
        }

        // Get all audit findings for this journey
        var findings = await _auditFindingRepository.GetByJourneyIdAsync(complianceJourneyId, cancellationToken);

        var summary = new NonConformanceSummaryDto
        {
            TotalCount = findings.Count,
            MajorCount = findings.Count(f => f.Type == NonConformanceType.MajorNC),
            MinorCount = findings.Count(f => f.Type == NonConformanceType.MinorNC),
            ObservationCount = findings.Count(f => f.Type == NonConformanceType.Observation),
            OFICount = findings.Count(f => f.Type == NonConformanceType.OFI),
            UnresolvedCount = findings.Count(f => !f.IsResolved)
        };

        return summary;
    }

    /// <summary>
    /// Creates a new compliance journey for the given hospital and framework.
    /// Initializes the journey in Draft state using framework configuration.
    /// </summary>
    public async Task<ComplianceJourney> CreateJourneyAsync(
        Guid tenantId,
        Guid organizationId,
        Guid hospitalId,
        Guid frameworkId,
        Guid creatingUserId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Creating compliance journey for HospitalId={HospitalId}, FrameworkId={FrameworkId}, Tenant={TenantId}, Org={OrganizationId}",
            hospitalId, frameworkId, tenantId, organizationId);

        // Idempotent create: reuse existing journey for this hospital+framework (lookup ignores tenant filter; see repository).
        var existing = await _journeyRepository.GetByHospitalAndFrameworkAsync(hospitalId, frameworkId, cancellationToken);
        if (existing != null)
        {
            if (existing.TenantId != tenantId)
            {
                _logger.LogWarning(
                    "Journey {JourneyId} exists for HospitalId={HospitalId}, FrameworkId={FrameworkId} but TenantId {ExistingTenant} does not match request {RequestTenant}",
                    existing.Id, hospitalId, frameworkId, existing.TenantId, tenantId);
                throw new InvalidOperationException(
                    "A compliance journey already exists for this hospital and framework under a different organization.");
            }

            _logger.LogInformation(
                "Compliance journey already exists for HospitalId={HospitalId}, FrameworkId={FrameworkId}; returning existing {JourneyId} (idempotent)",
                hospitalId, frameworkId, existing.Id);
            return existing;
        }

        // Load framework configuration to initialize validity and readiness requirements.
        // Persist FrameworkConfiguration before adding the journey so a concurrent first-time config insert
        // cannot be mis-handled as a journey duplicate (single SaveChanges used to insert both rows).
        var config = await _frameworkConfigRepository.GetOrCreateDefaultAsync(frameworkId, cancellationToken);
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsSqlServerDuplicateKeyException(ex))
        {
            var addedConfigs = _dbContext.ChangeTracker.Entries<FrameworkConfiguration>()
                .Where(e => e.State == EntityState.Added)
                .ToList();
            if (addedConfigs.Count == 0)
            {
                throw;
            }

            _logger.LogWarning(ex,
                "FrameworkConfiguration insert race for FrameworkId={FrameworkId}; detaching pending row and reloading.",
                frameworkId);
            foreach (var entry in addedConfigs)
            {
                entry.State = EntityState.Detached;
            }

            var reloaded = await _frameworkConfigRepository.GetByFrameworkIdAsync(frameworkId, cancellationToken);
            if (reloaded == null)
            {
                throw;
            }

            config = reloaded;
        }

        var journey = new ComplianceJourney(
            tenantId,
            organizationId,
            hospitalId,
            frameworkId,
            config.DefaultValidityMonths,
            config.MinimumReadinessScore);

        await _journeyRepository.AddAsync(journey, cancellationToken);
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsSqlServerDuplicateKeyException(ex))
        {
            // Unique index on (HospitalId, FrameworkId): race or row invisible to filtered query earlier.
            _logger.LogWarning(ex,
                "Duplicate compliance journey insert for HospitalId={HospitalId}, FrameworkId={FrameworkId}; loading existing row.",
                hospitalId, frameworkId);

            _dbContext.Entry(journey).State = EntityState.Detached;

            var raced = await _journeyRepository.GetByHospitalAndFrameworkAsync(hospitalId, frameworkId, cancellationToken);
            if (raced == null || raced.TenantId != tenantId)
            {
                throw;
            }

            journey = raced;
        }

        _logger.LogInformation(
            "Created compliance journey {JourneyId} for HospitalId={HospitalId}, FrameworkId={FrameworkId} with initial status {Status}",
            journey.Id, hospitalId, frameworkId, journey.Status);

        // Generate compliance tasks for this journey (best-effort; do not block journey creation on failures)
        try
        {
            await _taskGenerationService.GenerateTasksForJourneyAsync(journey.Id, creatingUserId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error generating compliance tasks for journey {JourneyId}. Journey remains created but tasks may be incomplete.",
                journey.Id);
        }

        return journey;
    }

    /// <summary>
    /// Transitions a compliance journey to a new state.
    /// Validates business rules, role-based access, creates audit trail,
    /// and dispatches domain events. This is the single entry point for
    /// lifecycle mutations initiated by application/API callers.
    /// Enforces tenant isolation.
    /// </summary>
    public async Task TransitionJourneyAsync(
        Guid complianceJourneyId,
        ComplianceLifecycleState newStatus,
        Guid userId,
        IReadOnlyList<string> roles,
        Guid tenantId,
        string? remarks = null,
        CancellationToken cancellationToken = default)
    {
        var roleForPolicy = roles.FirstOrDefault() ?? string.Empty;
        _logger.LogInformation(
            "Transitioning compliance journey {JourneyId} to {NewStatus} by user {UserId} with roles {Roles}",
            complianceJourneyId, newStatus, userId, string.Join(',', roles));

        var journey = await _journeyRepository.GetByIdAsync(complianceJourneyId, cancellationToken);
        if (journey == null)
        {
            throw new InvalidOperationException($"Compliance journey {complianceJourneyId} not found");
        }

        // Enforce tenant isolation
        if (journey.TenantId != tenantId)
        {
            throw new UnauthorizedAccessException(
                $"Access denied: Journey {complianceJourneyId} does not belong to tenant {tenantId}");
        }

        var oldStatus = journey.Status;

    // Resolve framework code from navigation if available; otherwise use name or default.
    var frameworkCode = journey.Framework?.Name ?? "DEFAULT";
    var policy = _policyFactory.GetPolicy(frameworkCode);

    // Framework-specific pre-validation
    if (!policy.CanTransition(oldStatus, newStatus, roleForPolicy, journey, out var reason))
    {
        throw new InvalidOperationException(
            reason ?? $"Transition from {oldStatus} to {newStatus} is not allowed by policy for framework '{frameworkCode}'.");
    }

    // Validate state machine (no skipping) and state-specific business rules
        await ValidateTransitionAsync(journey, newStatus, cancellationToken);

    // Validate role-based access for this transition (hardcoded matrix)
    ValidateRoleForTransition(oldStatus, newStatus, roles);

        // Use execution strategy for transaction
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // Transition state (validates and raises domain events)
                journey.TransitionTo(newStatus, userId, remarks);

                // Apply additional framework-driven behavior after transition
                // (e.g., renewal window and reapply dates)
                if (newStatus is ComplianceLifecycleState.Approved or ComplianceLifecycleState.Renewed)
                {
                    var config = await _frameworkConfigRepository.GetOrCreateDefaultAsync(
                        journey.FrameworkId, cancellationToken);

                    if (journey.ExpiryDate.HasValue)
                    {
                        var renewalWindowStart = journey.ExpiryDate.Value
                            .AddDays(-config.RenewalDaysBeforeExpiry);
                        journey.SetRenewalWindowStartDate(renewalWindowStart);
                    }
                }
                else if (newStatus == ComplianceLifecycleState.Rejected)
                {
                    var config = await _frameworkConfigRepository.GetOrCreateDefaultAsync(
                        journey.FrameworkId, cancellationToken);
                    var reapplyDate = DateTime.UtcNow.AddDays(config.ReapplyWaitingDays);
                    journey.SetReapplyAllowedDate(reapplyDate);
                }

                // Create history record with snapshot
                var snapshotJson = CreateSnapshotJson(journey);
                var history = new ComplianceJourneyHistory(
                    journey.Id,
                    oldStatus,
                    newStatus,
                    userId,
                    remarks,
                    snapshotJson);

                // Raise a generic state-changed domain event for downstream consumers
                var stateChangedEvent = new ComplianceStateChangedDomainEvent(
                    journey.TenantId,
                    journey.OrganizationId,
                    journey.HospitalId,
                    journey.FrameworkId,
                    journey.Id,
                    oldStatus,
                    newStatus,
                    userId,
                    roleForPolicy,
                    remarks,
                    DateTime.UtcNow);

                // Use reflection-friendly pattern to add the event when available
                if (journey is AggregateRoot aggregateRoot)
                {
                    // Protected AddDomainEvent is exposed via aggregate root
                    var addMethod = typeof(AggregateRoot)
                        .GetMethod("AddDomainEvent", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    addMethod?.Invoke(aggregateRoot, new object[] { stateChangedEvent });
                }

                await _historyRepository.AddAsync(history, cancellationToken);

                // Framework-specific post-transition hook
                policy.OnTransition(oldStatus, newStatus, journey);

                // Collect and dispatch domain events
                var domainEvents = journey.DomainEvents.ToList();
                journey.ClearDomainEvents();

                if (domainEvents.Any())
                {
                    await _domainEventDispatcher.DispatchAsync(domainEvents, cancellationToken);
                }

                // Update risk score if needed
                await UpdateRiskScoreAsync(journey, cancellationToken);

                // Save changes
                await _journeyRepository.UpdateAsync(journey, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                _dashboardSummaryCache.Invalidate(journey.TenantId, journey.HospitalId, journey.FrameworkId);

                _logger.LogInformation(
                    "Successfully transitioned compliance journey {JourneyId} from {OldStatus} to {NewStatus}",
                    complianceJourneyId, oldStatus, newStatus);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    /// <summary>
    /// Automatically expires a compliance journey that has reached its expiry date.
    /// This method is called by internal automation endpoints (e.g., ComplianceExpiryJob).
    /// 
    /// Business Rules:
    /// - Only allowed if current status == Approved
    /// - Only allowed if ExpiryDate <= Today (UTC)
    /// - Idempotent: If already Expired, returns without error (no-op)
    /// - Uses system user (Guid.Empty) and "System" role
    /// - Bypasses the guard in ValidateTransitionAsync that prevents Expired transitions
    /// </summary>
    public async Task AutoExpireJourneyAsync(
        Guid complianceJourneyId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Auto-expiring compliance journey {JourneyId}",
            complianceJourneyId);

        var journey = await _journeyRepository.GetByIdAsync(complianceJourneyId, cancellationToken);
        if (journey == null)
        {
            throw new InvalidOperationException($"Compliance journey {complianceJourneyId} not found");
        }

        // Idempotency check: If already Expired, return early (no-op)
        if (journey.Status == ComplianceLifecycleState.Expired)
        {
            _logger.LogInformation(
                "Journey {JourneyId} is already in Expired state, skipping auto-expire (idempotent)",
                complianceJourneyId);
            return;
        }

        // Validate current status is Approved
        if (journey.Status != ComplianceLifecycleState.Approved)
        {
            throw new InvalidOperationException(
                $"Cannot auto-expire journey {complianceJourneyId}: current status is {journey.Status}, but must be Approved.");
        }

        // Validate ExpiryDate <= Today (UTC)
        if (!journey.ExpiryDate.HasValue)
        {
            throw new InvalidOperationException(
                $"Cannot auto-expire journey {complianceJourneyId}: ExpiryDate is not set.");
        }

        var today = DateTime.UtcNow.Date;
        if (journey.ExpiryDate.Value.Date > today)
        {
            throw new InvalidOperationException(
                $"Cannot auto-expire journey {complianceJourneyId}: ExpiryDate ({journey.ExpiryDate.Value:O}) is in the future.");
        }

        // Use system user and role for internal automation
        var systemUserId = Guid.Empty;
        var systemRole = ApplicationRoles.SYSTEM;
        var remarks = $"Automatically expired by system background job. ExpiryDate: {journey.ExpiryDate.Value:O}";

        var oldStatus = journey.Status;

        // Resolve framework code from navigation if available; otherwise use name or default.
        var frameworkCode = journey.Framework?.Name ?? "DEFAULT";
        var policy = _policyFactory.GetPolicy(frameworkCode);

        // Framework-specific pre-validation (but allow Expired transition for system)
        if (!policy.CanTransition(oldStatus, ComplianceLifecycleState.Expired, systemRole, journey, out var reason))
        {
            // Log warning but allow system to proceed (policy may be too restrictive for automation)
            _logger.LogWarning(
                "Framework policy '{FrameworkCode}' does not allow transition from {OldStatus} to Expired, but proceeding with system override. Reason: {Reason}",
                frameworkCode, oldStatus, reason);
        }

        // Use execution strategy for transaction
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // Transition state (bypasses ValidateTransitionAsync guard by calling TransitionTo directly)
                // The domain method will validate the state machine transition
                journey.TransitionTo(ComplianceLifecycleState.Expired, systemUserId, remarks);

                // Create history record with snapshot
                var snapshotJson = CreateSnapshotJson(journey);
                var history = new ComplianceJourneyHistory(
                    journey.Id,
                    oldStatus,
                    ComplianceLifecycleState.Expired,
                    systemUserId,
                    remarks,
                    snapshotJson);

                // Raise a state-changed domain event
                var stateChangedEvent = new ComplianceStateChangedDomainEvent(
                    journey.TenantId,
                    journey.OrganizationId,
                    journey.HospitalId,
                    journey.FrameworkId,
                    journey.Id,
                    oldStatus,
                    ComplianceLifecycleState.Expired,
                    systemUserId,
                    systemRole,
                    remarks,
                    DateTime.UtcNow);

                // Use reflection-friendly pattern to add the event when available
                if (journey is AggregateRoot aggregateRoot)
                {
                    var addMethod = typeof(AggregateRoot)
                        .GetMethod("AddDomainEvent", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    addMethod?.Invoke(aggregateRoot, new object[] { stateChangedEvent });
                }

                await _historyRepository.AddAsync(history, cancellationToken);

                // Framework-specific post-transition hook
                policy.OnTransition(oldStatus, ComplianceLifecycleState.Expired, journey);

                // Collect and dispatch domain events
                var domainEvents = journey.DomainEvents.ToList();
                journey.ClearDomainEvents();

                if (domainEvents.Any())
                {
                    await _domainEventDispatcher.DispatchAsync(domainEvents, cancellationToken);
                }

                // Update risk score (Expired state increases risk)
                await UpdateRiskScoreAsync(journey, cancellationToken);

                // Save changes
                await _journeyRepository.UpdateAsync(journey, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "Successfully auto-expired compliance journey {JourneyId} from {OldStatus} to Expired",
                    complianceJourneyId, oldStatus);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    /// <summary>
    /// Opens the renewal window for a compliance journey that has reached its renewal window start date.
    /// This method is called by internal automation endpoints (e.g., ComplianceRenewalWindowJob).
    /// 
    /// Business Rules:
    /// - Only allowed if current status == Approved
    /// - Only allowed if Today >= RenewalWindowStartDate
    /// - Idempotent: If already RenewalInProgress, returns without error (no-op)
    /// - Uses system user (Guid.Empty) and "System" role
    /// - Unlocks the journey (IsLocked = false) to allow updates for renewal
    /// </summary>
    public async Task OpenRenewalWindowAsync(
        Guid complianceJourneyId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Opening renewal window for compliance journey {JourneyId}",
            complianceJourneyId);

        var journey = await _journeyRepository.GetByIdAsync(complianceJourneyId, cancellationToken);
        if (journey == null)
        {
            throw new InvalidOperationException($"Compliance journey {complianceJourneyId} not found");
        }

        // Idempotency check: If already RenewalInProgress, return early (no-op)
        if (journey.Status == ComplianceLifecycleState.RenewalInProgress)
        {
            _logger.LogInformation(
                "Journey {JourneyId} is already in RenewalInProgress state, skipping renewal window open (idempotent)",
                complianceJourneyId);
            return;
        }

        // Validate current status is Approved
        if (journey.Status != ComplianceLifecycleState.Approved)
        {
            throw new InvalidOperationException(
                $"Cannot open renewal window for journey {complianceJourneyId}: current status is {journey.Status}, but must be Approved.");
        }

        // Validate RenewalWindowStartDate <= Today (UTC)
        if (!journey.RenewalWindowStartDate.HasValue)
        {
            throw new InvalidOperationException(
                $"Cannot open renewal window for journey {complianceJourneyId}: RenewalWindowStartDate is not set.");
        }

        var today = DateTime.UtcNow.Date;
        if (journey.RenewalWindowStartDate.Value.Date > today)
        {
            throw new InvalidOperationException(
                $"Cannot open renewal window for journey {complianceJourneyId}: RenewalWindowStartDate ({journey.RenewalWindowStartDate.Value:O}) is in the future.");
        }

        // Use system user and role for internal automation
        var systemUserId = Guid.Empty;
        var systemRole = ApplicationRoles.SYSTEM;
        var remarks = $"Automatically opened renewal window by system background job. RenewalWindowStartDate: {journey.RenewalWindowStartDate.Value:O}";

        var oldStatus = journey.Status;

        // Resolve framework code from navigation if available; otherwise use name or default.
        var frameworkCode = journey.Framework?.Name ?? "DEFAULT";
        var policy = _policyFactory.GetPolicy(frameworkCode);

        // Framework-specific pre-validation
        if (!policy.CanTransition(oldStatus, ComplianceLifecycleState.RenewalInProgress, systemRole, journey, out var reason))
        {
            // Log warning but allow system to proceed (policy may be too restrictive for automation)
            _logger.LogWarning(
                "Framework policy '{FrameworkCode}' does not allow transition from {OldStatus} to RenewalInProgress, but proceeding with system override. Reason: {Reason}",
                frameworkCode, oldStatus, reason);
        }

        // Use execution strategy for transaction
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // Transition state (domain method will unlock journey and raise domain events)
                journey.TransitionTo(ComplianceLifecycleState.RenewalInProgress, systemUserId, remarks);

                // Create history record with snapshot
                var snapshotJson = CreateSnapshotJson(journey);
                var history = new ComplianceJourneyHistory(
                    journey.Id,
                    oldStatus,
                    ComplianceLifecycleState.RenewalInProgress,
                    systemUserId,
                    remarks,
                    snapshotJson);

                // Raise a state-changed domain event
                var stateChangedEvent = new ComplianceStateChangedDomainEvent(
                    journey.TenantId,
                    journey.OrganizationId,
                    journey.HospitalId,
                    journey.FrameworkId,
                    journey.Id,
                    oldStatus,
                    ComplianceLifecycleState.RenewalInProgress,
                    systemUserId,
                    systemRole,
                    remarks,
                    DateTime.UtcNow);

                // Use reflection-friendly pattern to add the event when available
                if (journey is AggregateRoot aggregateRoot)
                {
                    var addMethod = typeof(AggregateRoot)
                        .GetMethod("AddDomainEvent", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    addMethod?.Invoke(aggregateRoot, new object[] { stateChangedEvent });
                }

                await _historyRepository.AddAsync(history, cancellationToken);

                // Framework-specific post-transition hook
                policy.OnTransition(oldStatus, ComplianceLifecycleState.RenewalInProgress, journey);

                // Collect and dispatch domain events
                var domainEvents = journey.DomainEvents.ToList();
                journey.ClearDomainEvents();

                if (domainEvents.Any())
                {
                    await _domainEventDispatcher.DispatchAsync(domainEvents, cancellationToken);
                }

                // Update risk score (renewal window may affect risk calculation)
                await UpdateRiskScoreAsync(journey, cancellationToken);

                // Save changes
                await _journeyRepository.UpdateAsync(journey, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "Successfully opened renewal window for compliance journey {JourneyId} from {OldStatus} to RenewalInProgress",
                    complianceJourneyId, oldStatus);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    /// <summary>
    /// Returns a coverage report of mandatory objective elements for a compliance journey.
    /// </summary>
    public async Task<MandatoryObjectiveElementCoverageReportDto> GetMandatoryObjectiveElementCoverageAsync(
        Guid complianceJourneyId,
        CancellationToken cancellationToken = default)
    {
        var journey = await _journeyRepository.GetByIdAsync(complianceJourneyId, cancellationToken);
        if (journey == null)
        {
            throw new InvalidOperationException($"Compliance journey {complianceJourneyId} not found");
        }

        var requiredOes = await _dbContext.ObjectiveElements
            .AsNoTracking()
            .Include(oe => oe.ComplianceStandard)
                .ThenInclude(s => s.Chapter)
            .Where(oe => oe.IsMandatory && oe.IsApplicable)
            .Where(oe => oe.ComplianceStandard.Chapter.FrameworkId == journey.FrameworkId)
            .ToListAsync(cancellationToken);

        var requiredIds = requiredOes.Select(oe => oe.Id).ToHashSet();

        var coveredIds = await GetVerifiedObjectiveElementIdsForHospitalFrameworkAsync(
            journey.HospitalId,
            journey.FrameworkId,
            cancellationToken);

        var missing = requiredOes
            .Where(oe => !coveredIds.Contains(oe.Id))
            .Select(oe => new MandatoryObjectiveElementDto
            {
                ObjectiveElementId = oe.Id,
                ChapterCode = oe.ComplianceStandard.Chapter?.Code ?? string.Empty,
                ElementCode = oe.Code,
                Description = oe.Description,
                IsMandatory = oe.IsMandatory,
                Classification = oe.Classification.ToString()
            })
            .ToList();

        return new MandatoryObjectiveElementCoverageReportDto
        {
            ComplianceJourneyId = journey.Id,
            FrameworkId = journey.FrameworkId,
            TotalRequiredObjectiveElements = requiredIds.Count,
            CoveredObjectiveElements = requiredIds.Count(id => coveredIds.Contains(id)),
            MissingObjectiveElements = missing
        };
    }

    private async Task ValidateTransitionAsync(
        ComplianceJourney journey,
        ComplianceLifecycleState newStatus,
        CancellationToken cancellationToken)
    {
        // Guard: prevent direct application transitions to Expired.
        // Expiry must be driven by the background job (ComplianceExpiryJob).
        if (newStatus == ComplianceLifecycleState.Expired)
        {
            throw new InvalidOperationException(
                "Compliance can only move to Expired state via system background job.");
        }

        switch (newStatus)
        {
            case ComplianceLifecycleState.Enrolled:
                // Load framework configuration and ensure journey configuration is initialized
                var enrolledConfig = await _frameworkConfigRepository.GetOrCreateDefaultAsync(
                    journey.FrameworkId, cancellationToken);

                // Apply default validity period and minimum readiness score if not already set
                if (journey.ValidityPeriodInMonths <= 0 || journey.MinimumRequiredScore <= 0)
                {
                    journey.EnsureConfiguration(
                        enrolledConfig.DefaultValidityMonths,
                        enrolledConfig.MinimumReadinessScore);
                }

                if (journey.ValidityPeriodInMonths <= 0)
                {
                    throw new InvalidOperationException("Validity period must be defined before enrollment");
                }

                // Enforce enrollment payment confirmation
                if (!journey.EnrollmentPaymentId.HasValue
                    || !journey.EnrollmentFeeAmount.HasValue
                    || journey.EnrollmentFeeAmount.Value <= 0
                    || !journey.EnrollmentFeePaidOn.HasValue)
                {
                    throw new InvalidOperationException(
                        "Enrollment payment must be confirmed before enrolling in the framework.");
                }
                break;

            case ComplianceLifecycleState.GapAssessment:
                // No additional validation needed
                break;

            case ComplianceLifecycleState.InProgress:
                var hasVerifiedOeEvidence = await _dbContext.ObjectiveElementEvidences
                    .AsNoTracking()
                    .AnyAsync(
                        oee => oee.VerificationStatus == EvidenceVerificationStatus.Verified
                               && oee.Evidence.HospitalId == journey.HospitalId
                               && oee.Evidence.FrameworkId == journey.FrameworkId,
                        cancellationToken);
                if (!hasVerifiedOeEvidence)
                {
                    throw new InvalidOperationException(
                        "Cannot move to InProgress: at least one verified objective-element evidence item is required for the journey.");
                }

                // If re-applying after Rejected, enforce ReapplyAllowedDate
                if (journey.Status == ComplianceLifecycleState.Rejected &&
                    journey.ReapplyAllowedDate.HasValue &&
                    DateTime.UtcNow < journey.ReapplyAllowedDate.Value)
                {
                    throw new InvalidOperationException(
                        $"Cannot re-apply before {journey.ReapplyAllowedDate.Value:O}. ReapplyAllowedDate has not been reached.");
                }
                break;

            case ComplianceLifecycleState.Submitted:
                // Validate readiness score
                if (journey.CurrentReadinessScore < journey.MinimumRequiredScore)
                {
                    throw new InvalidOperationException(
                        $"Cannot submit: ReadinessScore ({journey.CurrentReadinessScore}) is below minimum required ({journey.MinimumRequiredScore})");
                }

                var requiredOeIds = await _dbContext.ObjectiveElements
                    .AsNoTracking()
                    .Where(oe => oe.IsMandatory && oe.IsApplicable)
                    .Where(oe => oe.ComplianceStandard.Chapter.FrameworkId == journey.FrameworkId)
                    .Select(oe => oe.Id)
                    .ToListAsync(cancellationToken);

                if (requiredOeIds.Count > 0)
                {
                    var coveredIds = await GetVerifiedObjectiveElementIdsForHospitalFrameworkAsync(
                        journey.HospitalId,
                        journey.FrameworkId,
                        cancellationToken);

                    if (!requiredOeIds.All(id => coveredIds.Contains(id)))
                    {
                        throw new InvalidOperationException(
                            "Cannot submit: all mandatory objective elements must have verified evidence.");
                    }
                }
                break;

            case ComplianceLifecycleState.AuditScheduled:
                // Validate audit team assigned and audit date defined
                if (!journey.AuditDate.HasValue)
                {
                    throw new InvalidOperationException("Audit date must be defined before scheduling audit.");
                }

                if (string.IsNullOrWhiteSpace(journey.AuditTeamDetails))
                {
                    throw new InvalidOperationException("Audit team details must be defined before scheduling audit.");
                }
                break;

            case ComplianceLifecycleState.AuditCompleted:
                // Validate audit findings recorded
                var findings = await _auditFindingRepository.GetByJourneyIdAsync(journey.Id, cancellationToken);
                if (!findings.Any())
                {
                    throw new InvalidOperationException("Audit findings must be recorded before completing audit");
                }

                // Ensure a corrective action deadline exists; if not, derive from framework configuration
                if (!journey.CorrectiveActionDeadline.HasValue)
                {
                    var config = await _frameworkConfigRepository.GetOrCreateDefaultAsync(
                        journey.FrameworkId, cancellationToken);
                    var deadline = DateTime.UtcNow.AddDays(config.MajorNcResolutionDays);
                    journey.SetCorrectiveActionDeadline(deadline);
                }
                break;

            case ComplianceLifecycleState.Approved:
                // Validate all major non-conformities resolved
                var hasUnresolvedMajorNc = await _auditFindingRepository.HasUnresolvedMajorNcAsync(journey.Id, cancellationToken);
                if (hasUnresolvedMajorNc)
                {
                    throw new InvalidOperationException("All major non-conformities must be resolved before approval");
                }

                // Ensure configuration is present (if journey was created before configuration existed)
                var approvedConfig = await _frameworkConfigRepository.GetOrCreateDefaultAsync(
                    journey.FrameworkId, cancellationToken);
                if (journey.ValidityPeriodInMonths <= 0 || journey.MinimumRequiredScore <= 0)
                {
                    journey.EnsureConfiguration(
                        approvedConfig.DefaultValidityMonths,
                        approvedConfig.MinimumReadinessScore);
                }
                break;

            case ComplianceLifecycleState.Rejected:
                // RejectionReason will be validated in domain method
                break;

            case ComplianceLifecycleState.Suspended:
                // SuspendedReason will be validated in domain method
                break;

            case ComplianceLifecycleState.RenewalInProgress:
                // Manual transition to RenewalInProgress must respect the renewal window.
                if (journey.RenewalWindowStartDate.HasValue &&
                    DateTime.UtcNow < journey.RenewalWindowStartDate.Value)
                {
                    throw new InvalidOperationException(
                        "Cannot start renewal before the renewal window start date.");
                }
                break;
        }
    }

    /// <summary>
    /// Validates that the caller's role is allowed to perform the requested
    /// lifecycle transition. This is a hardcoded matrix for now and can be
    /// replaced by a policy-based configuration later.
    /// </summary>
    private static void ValidateRoleForTransition(
        ComplianceLifecycleState fromStatus,
        ComplianceLifecycleState toStatus,
        IReadOnlyList<string> roles)
    {
        // Minimal role matrix (canonical names — see Shared.Infrastructure.Auth.ApplicationRoles):
        // Draft -> Enrolled: ORG_ADMIN
        // Submitted -> AuditScheduled: AUTHORITY
        // AuditCompleted -> Approved: AUTHORITY
        // Approved -> Suspended: REGULATOR

        bool isAllowed = true;

        if (fromStatus == ComplianceLifecycleState.Draft &&
            toStatus == ComplianceLifecycleState.Enrolled)
        {
            isAllowed = HasAnyRole(roles, ApplicationRoles.ORG_ADMIN);
        }
        else if (fromStatus == ComplianceLifecycleState.Submitted &&
                 toStatus == ComplianceLifecycleState.AuditScheduled)
        {
            isAllowed = HasAnyRole(roles, ApplicationRoles.AUTHORITY);
        }
        else if (fromStatus == ComplianceLifecycleState.AuditCompleted &&
                 toStatus == ComplianceLifecycleState.Approved)
        {
            isAllowed = HasAnyRole(roles, ApplicationRoles.AUTHORITY);
        }
        else if (fromStatus == ComplianceLifecycleState.Approved &&
                 toStatus == ComplianceLifecycleState.Suspended)
        {
            isAllowed = HasAnyRole(roles, ApplicationRoles.REGULATOR);
        }

        if (!isAllowed)
        {
            throw new InvalidOperationException(
                $"Caller roles are not permitted to transition compliance journey from {fromStatus} to {toStatus}.");
        }
    }

    private static bool HasAnyRole(IReadOnlyList<string>? roles, params string[] allowedCanonical)
    {
        if (roles == null || roles.Count == 0)
            return false;

        foreach (var a in allowedCanonical)
        {
            foreach (var r in roles)
            {
                if (string.Equals(r?.Trim(), a, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }

    private async Task UpdateRiskScoreAsync(ComplianceJourney journey, CancellationToken cancellationToken)
    {
        var majorNcCount = await _auditFindingRepository.GetMajorNcCountAsync(journey.Id, cancellationToken);
        var minorNcCount = await _auditFindingRepository.GetMinorNcCountAsync(journey.Id, cancellationToken);
        
        var newRiskScore = journey.CalculateRiskScore(majorNcCount, minorNcCount);
        journey.UpdateRiskScore(newRiskScore);
        await _eventEmitter.EmitAsync(
            ComplianceEventType.RiskUpdated,
            journey.Id,
            nameof(ComplianceJourney),
            journey.TenantId,
            null,
            new
            {
                journey.HospitalId,
                journey.FrameworkId,
                RiskScore = newRiskScore,
                majorNcCount,
                minorNcCount
            },
            cancellationToken);
    }

    private string CreateSnapshotJson(ComplianceJourney journey)
    {
        var snapshot = new
        {
            journey.Id,
            journey.TenantId,
            journey.OrganizationId,
            journey.HospitalId,
            journey.FrameworkId,
            journey.Status,
            journey.EnrollmentDate,
            journey.SubmissionDate,
            journey.ApprovalDate,
            journey.ExpiryDate,
            journey.ValidityPeriodInMonths,
            journey.CurrentReadinessScore,
            journey.RiskScore,
            journey.CertificateNumber,
            journey.IsPublicCertificateVisible,
            journey.IsLocked,
            SnapshotAt = DateTime.UtcNow
        };

        return JsonSerializer.Serialize(snapshot, new JsonSerializerOptions
        {
            WriteIndented = false
        });
    }

    private async Task<HashSet<Guid>> GetVerifiedObjectiveElementIdsForHospitalFrameworkAsync(
        Guid hospitalId,
        Guid frameworkId,
        CancellationToken cancellationToken)
    {
        var list = await _dbContext.ObjectiveElementEvidences
            .AsNoTracking()
            .Where(oee => oee.VerificationStatus == EvidenceVerificationStatus.Verified)
            .Where(oee => oee.Evidence.HospitalId == hospitalId && oee.Evidence.FrameworkId == frameworkId)
            .Select(oee => oee.ObjectiveElementId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return list.ToHashSet();
    }

    /// <summary>
    /// Detects SQL Server duplicate-key violations (2601/2627) or English message fallbacks.
    /// </summary>
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
