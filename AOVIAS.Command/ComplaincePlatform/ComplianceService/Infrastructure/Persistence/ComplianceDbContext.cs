using ComplianceService.Domain;
using ComplianceService.Domain.Auditor;
using ComplianceService.Domain.Documentation;
using ComplianceService.Domain.Copilot;
using ComplianceService.Application.Contracts;
using ComplianceService.Infrastructure.Copilot.EntityConfigurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Shared.Infrastructure.Domain;
using Shared.Infrastructure.Domain.Enums;
using Shared.Infrastructure.EntityFrameworkCore;
using Shared.Infrastructure.Execution;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ComplianceService.Infrastructure.Persistence;

public class ComplianceDbContext : DbContext
{
    private readonly Guid? _currentTenantId;

    /// <summary>
    /// True when tenant filters are registered unconditionally (fail closed). Controlled by
    /// <c>Tenancy:MandatoryQueryFilters</c>; see the constructor for why it is a flag.
    /// </summary>
    private readonly bool _mandatoryQueryFilters;

    /// <summary>
    /// Tenant used by the global query filters. <see cref="Guid.Empty"/> when unresolved, which
    /// matches no rows.
    ///
    /// <para>
    /// **This must stay a context member, not a local.** EF caches the compiled model per context
    /// type. A filter closing over a local variable bakes that variable's value into the cached
    /// model as a constant — fixed at whatever the first-constructed context happened to hold —
    /// so every later request silently reuses it. Referencing an instance member instead makes EF
    /// parameterise the filter and re-evaluate it per request.
    /// </para>
    /// </summary>
    private Guid TenantScope => _currentTenantId ?? Guid.Empty;

    public ComplianceDbContext(
        DbContextOptions<ComplianceDbContext> options,
        IUserContextService? userContext = null,
        IExecutionContextAccessor? executionContext = null,
        IConfiguration? configuration = null,
        ILogger<ComplianceDbContext>? logger = null)
        : base(options)
    {
        // Resolve the tenant once, for use in global query filters.
        //
        // The execution context comes first: it reads the propagated X-Aovias-Organization-Id
        // header, which is the only source available on internal API-key routes. Those routes carry
        // no user principal, so IUserContextService throws there — and the resulting null tenant is
        // exactly why filter registration used to be skipped, silently disabling isolation on
        // service-to-service calls (ADR-002, risk R-01).
        //
        // IUserContextService remains as the fallback for direct user requests. Its exceptions are
        // still swallowed so migrations and design-time tooling keep working.
        var fromExecutionContext = executionContext?.OrganizationId ?? Guid.Empty;
        if (fromExecutionContext != Guid.Empty)
        {
            _currentTenantId = fromExecutionContext;
        }
        else
        {
            try
            {
                _currentTenantId = userContext?.OrganizationId;
            }
            catch
            {
                _currentTenantId = null;
            }
        }

        // Flagged because it is a one-way behavioural change: with filters always registered, any
        // path that fails to establish a tenant returns no rows instead of everything. That is the
        // correct failure mode, but it must be switchable for one release while telemetry confirms
        // no legitimate path is left without context.
        _mandatoryQueryFilters = configuration?.GetValue("Tenancy:MandatoryQueryFilters", false) ?? false;

        // The signal that matters when rolling mandatory filters into a new environment: a request
        // that resolved no tenant will return no rows and still report success. Surfacing it here
        // means a missing propagation path is visible in logs rather than in a support ticket.
        if (_mandatoryQueryFilters && TenantScope == Guid.Empty && logger is not null)
        {
            ExecutionContextTelemetry.FailClosed(
                logger,
                service: "ComplianceService",
                operation: "DbContext.Construct",
                correlationId: executionContext?.Current?.CorrelationId);
        }
    }
    public DbSet<ComplianceFramework> ComplianceFrameworks { get; set; }
    public DbSet<ComplianceChapter> ComplianceChapters { get; set; }
    public DbSet<Regulation> Regulations { get; set; }
    public DbSet<ComplianceProgram> CompliancePrograms => Set<ComplianceProgram>();
    public DbSet<Control> Controls => Set<Control>();
    public DbSet<ComplianceTask> ComplianceTasks => Set<ComplianceTask>();
    public DbSet<ComplianceTaskComment> ComplianceTaskComments => Set<ComplianceTaskComment>();
    public DbSet<ComplianceTaskOwnershipHistory> ComplianceTaskOwnershipHistories => Set<ComplianceTaskOwnershipHistory>();
    public DbSet<RecurringTaskDefinition> RecurringTaskDefinitions => Set<RecurringTaskDefinition>();
    public DbSet<Evidence> Evidences => Set<Evidence>();
    public DbSet<EvidenceVersion> EvidenceVersions => Set<EvidenceVersion>();
    public DbSet<ComplianceSnapshot> ComplianceSnapshots => Set<ComplianceSnapshot>();
    public DbSet<FrameworkChapterSnapshot> FrameworkChapterSnapshots => Set<FrameworkChapterSnapshot>();
    public DbSet<OrganizationComplianceOverviewSnapshot> OrganizationComplianceOverviewSnapshots => Set<OrganizationComplianceOverviewSnapshot>();
    public DbSet<Risk> Risks => Set<Risk>();
    public DbSet<GovernanceTraceLink> GovernanceTraceLinks => Set<GovernanceTraceLink>();
    public DbSet<PlatformSequence> PlatformSequences { get; set; } = default!;
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    
    // Compliance Journey Engine entities
    public DbSet<ComplianceJourney> ComplianceJourneys => Set<ComplianceJourney>();
    public DbSet<ComplianceJourneyHistory> ComplianceJourneyHistories => Set<ComplianceJourneyHistory>();
    public DbSet<FrameworkConfiguration> FrameworkConfigurations => Set<FrameworkConfiguration>();
    public DbSet<Finding> Findings => Set<Finding>();
    public DbSet<ComplianceAuditFinding> ComplianceAuditFindings => Set<ComplianceAuditFinding>();
    public DbSet<FindingEvidence> FindingEvidences => Set<FindingEvidence>();
    public DbSet<FindingAuditTrail> FindingAuditTrails => Set<FindingAuditTrail>();
    public DbSet<FindingComment> FindingComments => Set<FindingComment>();
    public DbSet<FindingAttachment> FindingAttachments => Set<FindingAttachment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<WizardOnboardingAssessment> WizardOnboardingAssessments => Set<WizardOnboardingAssessment>();

    // Scoring Refactor entities
    public DbSet<ComplianceStandard> ComplianceStandards => Set<ComplianceStandard>();
    public DbSet<ObjectiveElement> ObjectiveElements => Set<ObjectiveElement>();
    public DbSet<ObjectiveElementScore> ObjectiveElementScores => Set<ObjectiveElementScore>();
    public DbSet<ObjectiveElementEvidence> ObjectiveElementEvidences => Set<ObjectiveElementEvidence>();
    public DbSet<EvidenceReviewAuditEntry> EvidenceReviewAuditEntries => Set<EvidenceReviewAuditEntry>();
    public DbSet<ObjectiveElementObservation> ObjectiveElementObservations => Set<ObjectiveElementObservation>();
    public DbSet<ActionPlan> ActionPlans => Set<ActionPlan>();
    public DbSet<RuleConfig> RuleConfigs => Set<RuleConfig>();
    public DbSet<FrameworkChapterDistribution> FrameworkChapterDistributions => Set<FrameworkChapterDistribution>();
    public DbSet<ObjectiveJourney> ObjectiveJourneys => Set<ObjectiveJourney>();

    // Control Center entities
    public DbSet<ComplianceDepartment> Departments => Set<ComplianceDepartment>();
    public DbSet<Capa> Capas => Set<Capa>();
    public DbSet<ScheduledAudit> ScheduledAudits => Set<ScheduledAudit>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<ActivityEvent> ActivityEvents => Set<ActivityEvent>();

    // Obligation Engine entities
    public DbSet<OrganizationComplianceObligation> OrganizationComplianceObligations => Set<OrganizationComplianceObligation>();
    public DbSet<ComplianceDeficiency> ComplianceDeficiencies => Set<ComplianceDeficiency>();
    public DbSet<DeficiencyAuditTrail> DeficiencyAuditTrails => Set<DeficiencyAuditTrail>();
    public DbSet<DepartmentLabelMapping> DepartmentLabelMappings => Set<DepartmentLabelMapping>();
    public DbSet<DepartmentOwnership> DepartmentOwnerships => Set<DepartmentOwnership>();
    public DbSet<ObjectiveElementOwnership> ObjectiveElementOwnerships => Set<ObjectiveElementOwnership>();
    public DbSet<AssignmentRoutingLog> AssignmentRoutingLogs => Set<AssignmentRoutingLog>();
    public DbSet<SlaPolicy> SlaPolicies => Set<SlaPolicy>();
    public DbSet<EscalationPolicy> EscalationPolicies => Set<EscalationPolicy>();
    public DbSet<EscalationLevel> EscalationLevels => Set<EscalationLevel>();
    public DbSet<DeficiencyEscalationHistory> DeficiencyEscalationHistories => Set<DeficiencyEscalationHistory>();
    public DbSet<OrganizationMaturityStageState> OrganizationMaturityStageStates => Set<OrganizationMaturityStageState>();
    public DbSet<MaturityStageUnlockAuditEntry> MaturityStageUnlockAuditEntries => Set<MaturityStageUnlockAuditEntry>();

    public DbSet<AnalyticsDailySnapshot> AnalyticsDailySnapshots => Set<AnalyticsDailySnapshot>();
    public DbSet<DashboardProjection> DashboardProjections => Set<DashboardProjection>();

    // Sprint 13A – Copilot Core
    public DbSet<CopilotRecommendation> CopilotRecommendations => Set<CopilotRecommendation>();
    public DbSet<AiDraft> AiDrafts => Set<AiDraft>();

    // Sprint 13D – Approval Card Framework
    public DbSet<CopilotActionProposal> CopilotActionProposals => Set<CopilotActionProposal>();
    public DbSet<ProposalAuditEvent> CopilotProposalAuditEvents => Set<ProposalAuditEvent>();

    // Sprint 13E – Persona Experience Layer
    public DbSet<CopilotActivityEvent> CopilotActivityEvents => Set<CopilotActivityEvent>();

    // Sprint 13F – Copilot Observability
    public DbSet<CopilotEffectivenessSnapshot> CopilotEffectivenessSnapshots => Set<CopilotEffectivenessSnapshot>();

    // Sprint 14 – External Auditor Workspace
    public DbSet<AuditSession> AuditSessions => Set<AuditSession>();
    public DbSet<AuditObservation> AuditObservations => Set<AuditObservation>();
    public DbSet<AuditComment> AuditComments => Set<AuditComment>();

    // Sprint 15 – Quality Documentation & Policy Management (QDMS)
    public DbSet<ControlledDocument> ControlledDocuments => Set<ControlledDocument>();
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();
    public DbSet<DocumentReview> DocumentReviews => Set<DocumentReview>();
    public DbSet<DocumentApproval> DocumentApprovals => Set<DocumentApproval>();
    public DbSet<DocumentAcknowledgement> DocumentAcknowledgements => Set<DocumentAcknowledgement>();
    public DbSet<DocumentTemplate> DocumentTemplates => Set<DocumentTemplate>();
    public DbSet<DocumentLifecycleAuditEntry> DocumentLifecycleAuditEntries => Set<DocumentLifecycleAuditEntry>();
    public DbSet<DocumentSection> DocumentSections => Set<DocumentSection>();
    public DbSet<DocumentAuthoringComment> DocumentAuthoringComments => Set<DocumentAuthoringComment>();
    public DbSet<DocumentAuthoringCheckpoint> DocumentAuthoringCheckpoints => Set<DocumentAuthoringCheckpoint>();
    public DbSet<DocumentCrossReference> DocumentCrossReferences => Set<DocumentCrossReference>();

    // Sprint 16 – Incident Management & Patient Safety
    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<IncidentInvestigation> IncidentInvestigations => Set<IncidentInvestigation>();
    public DbSet<IncidentRca> IncidentRcas => Set<IncidentRca>();
    public DbSet<PatientFeedback> PatientFeedbacks => Set<PatientFeedback>();
    public DbSet<LessonLearned> LessonsLearned => Set<LessonLearned>();
    public DbSet<IncidentAttachment> IncidentAttachments => Set<IncidentAttachment>();
    public DbSet<IncidentTimelineEntry> IncidentTimelineEntries => Set<IncidentTimelineEntry>();
    public DbSet<IncidentAuditTrail> IncidentAuditTrails => Set<IncidentAuditTrail>();

    // Sprint 17 – Clinical Indicators & Quality Intelligence
    public DbSet<ClinicalIndicator> ClinicalIndicators => Set<ClinicalIndicator>();
    public DbSet<IndicatorMeasurement> IndicatorMeasurements => Set<IndicatorMeasurement>();
    public DbSet<IndicatorThresholdHistory> IndicatorThresholdHistories => Set<IndicatorThresholdHistory>();
    public DbSet<IndicatorAuditTrail> IndicatorAuditTrails => Set<IndicatorAuditTrail>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<PlatformSequence>(entity =>
        {
            entity.ToTable("PlatformSequences");

            entity.HasKey(x => x.Id);

            // 10 was sized for short document prefixes ("CHP", "STD", "MOE"). CAPA numbering scopes
            // its sequence per hospital — `CAPA-{hospitalId:N}` is 37 characters — so every CAPA
            // allocation failed on save with a truncation error, which is why no CAPA could ever be
            // recorded (see NEW-07 / DEF-010). 64 leaves room for other GUID-scoped prefixes.
            entity.Property(x => x.Prefix)
                  .IsRequired()
                  .HasMaxLength(64);

            entity.HasIndex(x => x.Prefix)
                  .IsUnique();

            entity.Property(x => x.CurrentValue)
                  .IsRequired();
        });



        // ComplianceProgram configuration
        builder.Entity<ComplianceProgram>(entity =>
        {
            entity.HasIndex(x => x.PlatformId)
                  .IsUnique();

            entity.Property(x => x.RowVersion)
                  .IsRowVersion();

            entity.Property(x => x.PlatformId)
                  .IsRequired()
                  .HasMaxLength(10);

            entity.Property(x => x.Name)
                  .IsRequired();

            entity.Property(x => x.Explainability)
                  .ConfigureExplainability();

            // Many-to-one relationship with ComplianceFramework
            entity.HasOne(p => p.ComplianceFramework)
                  .WithMany()
                  .HasForeignKey(p => p.ComplianceFrameworkId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Index on ComplianceFrameworkId for query performance
            entity.HasIndex(x => x.ComplianceFrameworkId);

            // HospitalId configuration
            entity.Property(x => x.HospitalId)
                  .IsRequired();

            // Index on HospitalId for query performance
            entity.HasIndex(x => x.HospitalId);
        });

        // AuditLog configuration
        builder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.EntityType)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(x => x.Action)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(x => x.MetadataJson)
                  .IsRequired();

            entity.HasIndex(x => new { x.EntityType, x.EntityId });
            entity.HasIndex(x => x.Timestamp);
        });

        // Control configuration
        builder.Entity<Control>(entity =>
        {
            entity.Property(x => x.RowVersion)
                  .IsRowVersion();

            entity.Property(x => x.Code)
                  .IsRequired();

            entity.Property(x => x.Description)
                  .IsRequired();

            entity.Property(x => x.Phase)
                  .IsRequired()
                  .HasMaxLength(50)
                  .HasDefaultValue("Readiness");

            entity.Property(x => x.Explainability)
                  .ConfigureExplainability();

            // Many-to-one relationship with ComplianceProgram
            entity.HasOne<ComplianceProgram>()
                  .WithMany()
                  .HasForeignKey(x => x.ProgramId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<ObjectiveElement>()
                  .WithMany()
                  .HasForeignKey(x => x.ObjectiveElementId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Index on ProgramId for query performance
            entity.HasIndex(x => x.ProgramId);

            entity.HasIndex(x => x.ObjectiveElementId);

            // Unique constraint on (ProgramId, Code) to prevent duplicate codes per program
            entity.HasIndex(x => new { x.ProgramId, x.Code })
                  .IsUnique();
        });

        // ComplianceTask configuration
        builder.Entity<ComplianceTask>(entity =>
        {
            entity.Property(x => x.RowVersion)
                  .IsRowVersion();

            entity.Property(x => x.AssignedToUserId)
                  .IsRequired();

            entity.Property(x => x.Status)
                  .IsRequired()
                  .HasConversion<int>()
                  .HasDefaultValue(Domain.Enums.ComplianceTaskStatus.Pending);

            entity.Property(x => x.Description)
                  .HasMaxLength(4000);

            entity.Property(x => x.DepartmentName)
                  .HasMaxLength(256);

            entity.Property(x => x.DueDate);

            entity.Property(x => x.ProgramId);

            entity.Property(x => x.DepartmentId);

            entity.HasOne(x => x.Program)
                  .WithMany()
                  .HasForeignKey(x => x.ProgramId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(x => x.ProgramId);

            entity.HasIndex(x => x.DepartmentId);

            entity.Property(x => x.Explainability)
                  .ConfigureExplainability();

            // Configure Criticality enum to be stored as int with default value
            entity.Property(x => x.Criticality)
                  .IsRequired()
                  .HasConversion<int>()
                  .HasDefaultValue(Domain.Enums.TaskCriticalityLevel.Medium);

            // Execution priority (distinct from Criticality scoring field)
            entity.Property(x => x.Priority)
                  .IsRequired()
                  .HasConversion<int>()
                  .HasDefaultValue(Domain.Enums.ComplianceTaskPriority.Medium);

            // Configure Weight with precision decimal(5,2) and default value
            entity.Property(x => x.Weight)
                  .IsRequired()
                  .HasColumnType("decimal(5,2)")
                  .HasDefaultValue(1.0m);

            entity.HasOne(x => x.ObjectiveElement)
                  .WithMany()
                  .HasForeignKey(x => x.ObjectiveElementId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.ComplianceStandard)
                  .WithMany()
                  .HasForeignKey(x => x.ComplianceStandardId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Many-to-one relationship with Control
            entity.HasOne<Control>()
                  .WithMany()
                  .HasForeignKey(x => x.ControlId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.ControlId);
            entity.HasIndex(x => x.ComplianceStandardId);
            entity.HasIndex(x => x.ObjectiveElementId);

            // HospitalId configuration
            entity.Property(x => x.HospitalId)
                  .IsRequired();

            // Index on HospitalId for query performance
            entity.HasIndex(x => x.HospitalId);

            entity.HasIndex(x => new { x.HospitalId, x.ObjectiveElementId });

            entity.HasIndex(x => x.SourceDeficiencyId);

            entity.Property(x => x.Category)
                  .IsRequired()
                  .HasConversion<int>()
                  .HasDefaultValue(Domain.Enums.TaskCategory.Compliance);

            entity.Property(x => x.TaskKind)
                  .IsRequired()
                  .HasConversion<int>()
                  .HasDefaultValue(Domain.Enums.TaskKind.Standard);

            entity.Property(x => x.BlockedByTaskId);
            entity.Property(x => x.RecurringDefinitionId);
            entity.Property(x => x.OverdueEscalatedAt);

            entity.HasIndex(x => x.Category);
            entity.HasIndex(x => x.TaskKind);
            entity.HasIndex(x => x.BlockedByTaskId);
            entity.HasIndex(x => x.RecurringDefinitionId);
            entity.HasIndex(x => new { x.HospitalId, x.DueDate, x.Status });
        });

        builder.Entity<ComplianceTaskOwnershipHistory>(entity =>
        {
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.Property(x => x.PreviousAssigneeUserId).HasMaxLength(450);
            entity.Property(x => x.NewAssigneeUserId).IsRequired().HasMaxLength(450);
            entity.Property(x => x.Reason).HasMaxLength(2000);
            entity.Property(x => x.ChangedAt).IsRequired();
            entity.HasIndex(x => x.TaskId);
            entity.HasIndex(x => new { x.HospitalId, x.TaskId, x.ChangedAt });
        });

        builder.Entity<RecurringTaskDefinition>(entity =>
        {
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.Property(x => x.AssignedToUserId).IsRequired().HasMaxLength(450);
            entity.Property(x => x.Title).IsRequired().HasMaxLength(500);
            entity.Property(x => x.Description).HasMaxLength(4000);
            entity.Property(x => x.DepartmentName).HasMaxLength(256);
            entity.Property(x => x.Category).IsRequired().HasConversion<int>();
            entity.Property(x => x.TaskKind).IsRequired().HasConversion<int>();
            entity.Property(x => x.Criticality).IsRequired().HasConversion<int>();
            entity.Property(x => x.Priority).IsRequired().HasConversion<int>();
            entity.Property(x => x.Frequency).IsRequired().HasConversion<int>();
            entity.Property(x => x.Interval).IsRequired().HasDefaultValue(1);
            entity.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
            entity.HasIndex(x => new { x.HospitalId, x.IsActive, x.NextOccurrenceDate });
            entity.HasIndex(x => x.ObjectiveElementId);
        });

        builder.Entity<ComplianceTaskComment>(entity =>
        {
            entity.ToTable("ComplianceTaskComments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.Property(x => x.Body).IsRequired().HasMaxLength(4000);
            entity.HasIndex(x => x.TaskId);
            entity.HasIndex(x => new { x.HospitalId, x.FrameworkId });
            entity.HasOne(x => x.Task)
                .WithMany()
                .HasForeignKey(x => x.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Evidence configuration
        builder.Entity<Evidence>(entity =>
        {
            entity.Property(x => x.RowVersion)
                  .IsRowVersion();

            entity.Property(x => x.HospitalId)
                  .IsRequired();

            entity.Property(x => x.FrameworkId)
                  .IsRequired();

            entity.Property(x => x.EvidenceType)
                  .IsRequired();

            // New enums stored as ints
            entity.Property(x => x.ArtifactType)
                  .IsRequired()
                  .HasConversion<int>();

            entity.Property(x => x.LifecycleStatus)
                  .IsRequired()
                  .HasConversion<int>();

            // Metadata
            entity.Property(x => x.Title)
                  .HasMaxLength(250);

            entity.Property(x => x.Description);

            entity.Property(x => x.Source)
                  .IsRequired()
                  .HasMaxLength(50);

            entity.Property(x => x.Department)
                  .HasMaxLength(100);

            entity.Property(x => x.Tags);

            entity.Property(x => x.CurrentVersion)
                  .IsRequired()
                  .HasDefaultValue(1);

            entity.Property(x => x.FileUrl)
                  .IsRequired();

            entity.Property(x => x.Hash)
                  .IsRequired()
                  .HasMaxLength(64); // SHA-256 hash

            entity.Property(x => x.UploadedBy)
                  .IsRequired()
                  .HasMaxLength(100);

            // Many-to-one relationship with ComplianceTask (using navigation property)
            entity.HasOne(x => x.Task)
                  .WithMany()
                  .HasForeignKey(x => x.TaskId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Indexes for query performance
            entity.HasIndex(x => x.TaskId);
            entity.HasIndex(x => x.HospitalId);
            entity.HasIndex(x => new { x.HospitalId, x.FrameworkId });
            entity.HasIndex(x => x.ArtifactType);
            entity.HasIndex(x => x.Department);

            entity.Property(x => x.ExpiresAt);
        });

        // EvidenceVersion configuration
        builder.Entity<EvidenceVersion>(entity =>
        {
            entity.ToTable("EvidenceVersions");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.VersionNumber)
                  .IsRequired();

            entity.Property(x => x.FileUrl)
                  .IsRequired();

            entity.Property(x => x.Hash)
                  .IsRequired()
                  .HasMaxLength(64);

            entity.Property(x => x.CreatedAt)
                  .IsRequired();

            entity.HasOne(x => x.Evidence)
                  .WithMany()
                  .HasForeignKey(x => x.EvidenceId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => new { x.EvidenceId, x.VersionNumber })
                  .IsUnique();
        });

        builder.Entity<ComplianceFramework>(entity =>
        {
            entity.ToTable("ComplianceFrameworks");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired();
            entity.Property(x => x.Version).IsRequired();
            entity.Property(x => x.Status)
                  .HasConversion<int>()
                  .HasDefaultValue(Domain.Enums.FrameworkCatalogStatus.Active)
                  .IsRequired();
            entity.Property(x => x.PublishedAt);
            entity.Property(x => x.RetiredAt);
            entity.Property(x => x.ArchivedAt);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => new { x.Name, x.Status });

            // Unique constraint on (Name, Version) to prevent duplicates
            entity.HasIndex(x => new { x.Name, x.Version })
                  .IsUnique();
        });

        builder.Entity<ComplianceChapter>(entity =>
        {
            entity.ToTable("ComplianceChapters");
            entity.HasKey(x => x.Id);

            // Configure Weight with precision decimal(5,2) and default value
            entity.Property(x => x.Weight)
                  .IsRequired()
                  .HasColumnType("decimal(5,2)")
                  .HasDefaultValue(1.0m);

            // Many-to-one relationship with ComplianceFramework
            entity.HasOne(x => x.Framework)
                  .WithMany()
                  .HasForeignKey(x => x.FrameworkId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Index on FrameworkId for query performance
            entity.HasIndex(x => x.FrameworkId);
        });

        builder.Entity<Regulation>(entity =>
        {
            entity.ToTable("Regulations");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.PlatformId)
                  .IsRequired();

            // Many-to-one relationship with ComplianceFramework
            entity.HasOne<ComplianceFramework>()
                  .WithMany()
                  .HasForeignKey(x => x.ComplianceFrameworkId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Index on ComplianceFrameworkId for query performance
            entity.HasIndex(x => x.ComplianceFrameworkId);

            // HospitalId configuration
            entity.Property(x => x.HospitalId)
                  .IsRequired();

            // Index on HospitalId for query performance
            entity.HasIndex(x => x.HospitalId);

            // Unique constraint on (HospitalId, ComplianceFrameworkId) to prevent duplicate regulations
            entity.HasIndex(x => new { x.HospitalId, x.ComplianceFrameworkId })
                  .IsUnique();
        });

        // ComplianceStandard configuration
        builder.Entity<ComplianceStandard>(entity =>
        {
            entity.ToTable("ComplianceStandards");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.PlatformId)
                  .IsRequired();

            entity.Property(x => x.Code)
                  .IsRequired();

            entity.Property(x => x.Name)
                  .IsRequired();

            entity.Property(x => x.Weight)
                  .IsRequired()
                  .HasColumnType("decimal(5,2)")
                  .HasDefaultValue(1.0m);

            entity.HasOne(x => x.Chapter)
                  .WithMany(c => c.ComplianceStandards)
                  .HasForeignKey(x => x.ChapterId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new { x.ChapterId, x.Code })
                  .IsUnique();

            entity.HasIndex(x => new { x.ChapterId, x.Sequence });
        });

        // ObjectiveElement configuration
        builder.Entity<ObjectiveElement>(entity =>
        {
            entity.ToTable("ObjectiveElements");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.PlatformId)
                  .IsRequired();

            entity.Property(x => x.Code)
                  .IsRequired();

            entity.Property(x => x.Title)
                  .IsRequired();

            entity.Property(x => x.Description)
                  .IsRequired();

            entity.Property(x => x.Classification)
                  .IsRequired()
                  .HasConversion<int>();

            entity.Property(x => x.IsMandatory)
                  .IsRequired();

            entity.Property(x => x.IsScorable)
                  .IsRequired()
                  .HasDefaultValue(true);

            entity.Property(x => x.IsApplicable)
                  .IsRequired()
                  .HasDefaultValue(true);

            entity.Property(x => x.IsDocumentationRequired)
                  .IsRequired()
                  .HasDefaultValue(false);

            entity.Property(x => x.Tags);
            entity.Property(x => x.Department).HasMaxLength(150);
            entity.Property(x => x.ResponsibleRole).HasMaxLength(150);
            entity.Property(x => x.Frequency).HasMaxLength(100);
            entity.Property(x => x.EvidenceCategory).HasMaxLength(100);

            entity.Property(x => x.Weight)
                  .IsRequired()
                  .HasColumnType("decimal(5,2)")
                  .HasDefaultValue(1.0m);

            entity.HasOne(x => x.ComplianceStandard)
                  .WithMany(s => s.ObjectiveElements)
                  .HasForeignKey(x => x.ComplianceStandardId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Unique per ComplianceStandard code
            entity.HasIndex(x => new { x.ComplianceStandardId, x.Code })
                  .IsUnique();

            // Classification index for fast filtering
            entity.HasIndex(x => x.Classification);
        });

        // ObjectiveElementEvidence configuration
        builder.Entity<ObjectiveElementEvidence>(entity =>
        {
            entity.Property(x => x.VerificationStatus)
                  .IsRequired()
                  .HasConversion<int>();

            entity.Property(x => x.ReviewComment);

            entity.Property(x => x.ReviewRecommendation).HasMaxLength(2000);

            entity.Property(x => x.RowVersion)
                  .IsRowVersion();

            entity.HasIndex(x => new { x.ObjectiveElementId, x.EvidenceId });

            entity.HasOne(x => x.ObjectiveElement)
                  .WithMany()
                  .HasForeignKey(x => x.ObjectiveElementId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Evidence)
                  .WithMany()
                  .HasForeignKey(x => x.EvidenceId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<EvidenceReviewAuditEntry>(entity =>
        {
            entity.ToTable("EvidenceReviewAuditEntries");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Decision).HasMaxLength(50).IsRequired();
            entity.Property(x => x.ReviewerRole).HasMaxLength(100);
            entity.Property(x => x.Comment).HasMaxLength(2000);
            entity.Property(x => x.Recommendation).HasMaxLength(2000);
            entity.HasIndex(x => new { x.EvidenceId, x.ObjectiveElementId, x.ReviewedAt });
            entity.HasIndex(x => new { x.HospitalId, x.FrameworkId });
        });

        builder.Entity<ObjectiveElementScore>(entity =>
        {
            entity.HasIndex(x => new { x.JourneyId, x.ObjectiveElementId });
        });

        builder.Entity<ObjectiveElementObservation>(entity =>
        {
            entity.ToTable("ObjectiveElementObservations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Severity).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Text).IsRequired();
            entity.Property(x => x.CreatedByUserId).IsRequired();
            entity.HasIndex(x => new { x.ComplianceJourneyId, x.ObjectiveElementId });
            entity.HasOne(x => x.ComplianceJourney)
                  .WithMany()
                  .HasForeignKey(x => x.ComplianceJourneyId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ObjectiveElement)
                  .WithMany()
                  .HasForeignKey(x => x.ObjectiveElementId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ComplianceSnapshot configuration (Level 2 - Framework Snapshot)
        builder.Entity<ComplianceSnapshot>(entity =>
        {
            entity.ToTable("ComplianceSnapshots");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.RowVersion)
                  .IsRowVersion();

            // Tenant, Hospital and Framework configuration
            entity.Property(x => x.TenantId);
            entity.Property(x => x.HospitalId)
                  .IsRequired();

            entity.Property(x => x.FrameworkId)
                  .IsRequired();

            entity.Property(x => x.ComplianceJourneyId);

            // Time-series model: allow multiple rows over time; no uniqueness on (HospitalId, FrameworkId)
            // Indexes for latest and trend queries
            entity.HasIndex(x => x.HospitalId);
            entity.HasIndex(x => new { x.HospitalId, x.FrameworkId });
            entity.HasIndex(x => x.ComplianceJourneyId);
            entity.HasIndex(x => x.CalculatedAt);
            entity.HasIndex(x => new { x.HospitalId, x.FrameworkId, x.CalculatedAt });

            // If TenantId present, also add composite leading with TenantId for multi-tenant isolation
            entity.HasIndex(x => new { x.TenantId, x.HospitalId, x.FrameworkId });
            entity.HasIndex(x => new { x.TenantId, x.HospitalId, x.FrameworkId, x.CalculatedAt });

            // Configure decimal precision for scores
            entity.Property(x => x.OverallScore)
                  .IsRequired()
                  .HasColumnType("decimal(5,2)");

            entity.Property(x => x.AuditConfidenceScore)
                  .IsRequired()
                  .HasColumnType("decimal(5,2)");

            // Configure integer counts
            entity.Property(x => x.CriticalCount)
                  .IsRequired();

            entity.Property(x => x.MajorCount)
                  .IsRequired();

            entity.Property(x => x.MinorCount)
                  .IsRequired();

            // Configure JSON column
            entity.Property(x => x.ChapterBreakdownJson)
                  .IsRequired()
                  .HasColumnType("nvarchar(max)");

            // Configure audit simulation integer counts
            entity.Property(x => x.MajorNCCount)
                  .IsRequired()
                  .HasDefaultValue(0);

            entity.Property(x => x.MinorNCCount)
                  .IsRequired()
                  .HasDefaultValue(0);

            entity.Property(x => x.ObservationCount)
                  .IsRequired()
                  .HasDefaultValue(0);

            entity.Property(x => x.OFICount)
                  .IsRequired()
                  .HasDefaultValue(0);

            // Configure HighRiskChaptersJson column
            entity.Property(x => x.HighRiskChaptersJson)
                  .IsRequired()
                  .HasColumnType("nvarchar(max)")
                  .HasDefaultValue("[]");

            // Configure CriticalRiskCount
            entity.Property(x => x.CriticalRiskCount)
                  .IsRequired()
                  .HasDefaultValue(0);

            // Configure JourneyState enum
            entity.Property(x => x.JourneyState)
                  .IsRequired()
                  .HasConversion<int>()
                  .HasDefaultValue(Domain.Enums.JourneyState.Enrolled);

            // Configure timestamp
            entity.Property(x => x.CalculatedAt)
                  .IsRequired();
        });

        builder.Entity<AnalyticsDailySnapshot>(entity =>
        {
            entity.ToTable("AnalyticsDailySnapshots");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.ReadinessScore).HasColumnType("decimal(5,2)");
            entity.Property(x => x.ComplianceMaturityIndex).HasColumnType("decimal(5,2)");
            entity.Property(x => x.CreatedAt).IsRequired();

            entity.HasIndex(x => new { x.OrganizationId, x.HospitalId, x.FrameworkId, x.SnapshotDate })
                  .IsUnique();
            entity.HasIndex(x => x.SnapshotDate);
        });

        builder.Entity<DashboardProjection>(entity =>
        {
            entity.ToTable("DashboardProjections");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.OverallComplianceScore).HasColumnType("decimal(5,2)");
            entity.Property(x => x.AuditConfidenceScore).HasColumnType("decimal(5,2)");
            entity.Property(x => x.PayloadJson).IsRequired();
            entity.Property(x => x.LastUpdatedAt).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();

            entity.HasIndex(x => new { x.OrganizationId, x.HospitalId, x.FrameworkId }).IsUnique();
            entity.HasIndex(x => x.LastUpdatedAt);
            entity.HasIndex(x => new { x.IsComplete, x.IsCorrupted });
        });

        // FrameworkChapterSnapshot configuration (Level 2 - Chapter breakdown)
        builder.Entity<FrameworkChapterSnapshot>(entity =>
        {
            entity.ToTable("FrameworkChapterSnapshots");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.HospitalId)
                  .IsRequired();

            entity.Property(x => x.FrameworkId)
                  .IsRequired();

            entity.Property(x => x.ChapterId)
                  .IsRequired();

            entity.Property(x => x.ChapterScore)
                  .IsRequired()
                  .HasColumnType("decimal(5,2)");

            entity.Property(x => x.CompletedTasks)
                  .IsRequired();

            entity.Property(x => x.TotalTasks)
                  .IsRequired();

            entity.Property(x => x.RiskPenalty)
                  .IsRequired()
                  .HasColumnType("decimal(5,2)");

            entity.Property(x => x.CalculatedAt)
                  .IsRequired();

            // Index for efficient queries
            entity.HasIndex(x => new { x.HospitalId, x.FrameworkId });
            entity.HasIndex(x => new { x.HospitalId, x.FrameworkId, x.ChapterId })
                  .IsUnique();
        });

        // OrganizationComplianceOverviewSnapshot configuration (Level 1 - Organization overview)
        builder.Entity<OrganizationComplianceOverviewSnapshot>(entity =>
        {
            entity.ToTable("OrganizationComplianceOverviewSnapshots");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.HospitalId)
                  .IsRequired();

            entity.Property(x => x.OverallScore)
                  .IsRequired()
                  .HasColumnType("decimal(5,2)");

            entity.Property(x => x.WorstFrameworkId);

            entity.Property(x => x.TotalCriticalRisks)
                  .IsRequired()
                  .HasDefaultValue(0);

            entity.Property(x => x.UpcomingRenewals)
                  .IsRequired()
                  .HasColumnType("nvarchar(max)")
                  .HasDefaultValue("[]");

            entity.Property(x => x.LastCalculatedAt)
                  .IsRequired();

            // Unique constraint - one snapshot per hospital
            entity.HasIndex(x => x.HospitalId)
                  .IsUnique();
        });

        builder.Entity<GovernanceTraceLink>(entity =>
        {
            entity.ToTable("GovernanceTraceLinks");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.SourceEntityType).HasMaxLength(64).IsRequired();
            entity.Property(x => x.TargetEntityType).HasMaxLength(64).IsRequired();
            entity.Property(x => x.SourceLabel).HasMaxLength(500).IsRequired();
            entity.Property(x => x.TargetLabel).HasMaxLength(500).IsRequired();
            entity.Property(x => x.RelationshipType).HasMaxLength(64).IsRequired();
            entity.HasIndex(x => new { x.OrganizationId, x.SourceEntityId });
            entity.HasIndex(x => new { x.OrganizationId, x.TargetEntityId });
        });

        // Risk configuration — enterprise risk register (Phase 4 Sprint 4)
        builder.Entity<Risk>(entity =>
        {
            entity.ToTable("Risks");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.RowVersion)
                  .IsRowVersion();

            entity.Property(x => x.TenantId).IsRequired();
            entity.HasIndex(x => x.TenantId);

            entity.Property(x => x.HospitalId).IsRequired();
            entity.HasIndex(x => x.HospitalId);

            entity.Property(x => x.Title)
                  .IsRequired()
                  .HasMaxLength(250);

            entity.Property(x => x.Description);

            entity.Property(x => x.Category).HasMaxLength(100);
            entity.Property(x => x.OwnerName).HasMaxLength(200);
            entity.Property(x => x.MitigationPlan);
            entity.Property(x => x.ReviewDueDate);
            entity.HasIndex(x => x.ReviewDueDate);
            entity.HasIndex(x => x.OwnerUserId);
            entity.HasIndex(x => x.DepartmentId);

            entity.Property(x => x.Severity)
                  .IsRequired()
                  .HasConversion<int>();

            entity.Property(x => x.Likelihood).IsRequired();
            entity.Property(x => x.Impact).IsRequired();
            entity.Ignore(x => x.RiskScore);
            entity.Ignore(x => x.ResidualRiskScore);

            entity.Property(x => x.Status)
                  .IsRequired()
                  .HasConversion<int>()
                  .HasDefaultValue(Domain.Enums.RiskStatus.Identified);

            entity.HasIndex(x => x.Status);

            entity.Property(x => x.IdentifiedAt).IsRequired();
            entity.Property(x => x.ClosedAt);
            entity.Property(x => x.LastUpdatedAt).IsRequired();

            entity.HasOne(x => x.ObjectiveElement)
                  .WithMany()
                  .HasForeignKey(x => x.ObjectiveElementId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Task)
                  .WithMany()
                  .HasForeignKey(x => x.TaskId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Finding)
                  .WithMany()
                  .HasForeignKey(x => x.FindingId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(x => x.Incident)
                  .WithMany()
                  .HasForeignKey(x => x.IncidentId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(x => x.Deficiency)
                  .WithMany()
                  .HasForeignKey(x => x.DeficiencyId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(x => x.ObjectiveElementId);
            entity.HasIndex(x => x.TaskId);
            entity.HasIndex(x => x.FindingId);
            entity.HasIndex(x => x.IncidentId);
            entity.HasIndex(x => x.DeficiencyId);

            entity.HasCheckConstraint(
                "CK_Risk_LikelihoodRange",
                "[Likelihood] >= 1 AND [Likelihood] <= 5");

            entity.HasCheckConstraint(
                "CK_Risk_ImpactRange",
                "[Impact] >= 1 AND [Impact] <= 5");

            entity.HasCheckConstraint(
                "CK_Risk_ResidualLikelihoodRange",
                "[ResidualLikelihood] IS NULL OR ([ResidualLikelihood] >= 1 AND [ResidualLikelihood] <= 5)");

            entity.HasCheckConstraint(
                "CK_Risk_ResidualImpactRange",
                "[ResidualImpact] IS NULL OR ([ResidualImpact] >= 1 AND [ResidualImpact] <= 5)");
        });

        builder.Entity<Finding>(entity =>
        {
            entity.ToTable("Findings");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Scope).HasConversion<int>().IsRequired().HasDefaultValue(Domain.Enums.FindingScope.Organization);
            entity.Property(x => x.ComplianceStatus).HasConversion<int>().IsRequired().HasDefaultValue(Domain.Enums.ComplianceEvaluationStatus.NonCompliant);
            entity.Property(x => x.RiskLevel).HasConversion<int>().IsRequired().HasDefaultValue(Domain.Enums.FindingRiskLevel.Medium);
            entity.Property(x => x.CreatedByType).HasConversion<int>().IsRequired().HasDefaultValue(Domain.Enums.FindingActorType.User);
            entity.Property(x => x.Type).HasConversion<int>();
            entity.Property(x => x.Classification).HasConversion<int?>();
            entity.Property(x => x.FindingNumber).HasMaxLength(32);
            entity.Property(x => x.PromotedDeficiencyId);
            entity.Property(x => x.AuditDate);
            entity.Property(x => x.AuditorName).HasMaxLength(256);
            entity.Property(x => x.AuditCycle).HasMaxLength(128);
            entity.Property(x => x.Observation).HasMaxLength(4000);
            entity.Property(x => x.Requirement).HasMaxLength(4000);
            entity.Property(x => x.Impact).HasMaxLength(2000);
            entity.Property(x => x.Recommendation).HasMaxLength(4000);
            entity.Property(x => x.Severity).HasConversion<int>().IsRequired().HasDefaultValue(Shared.Infrastructure.Domain.Enums.FindingSeverity.Minor);
            entity.Property(x => x.Status).HasConversion<int>().IsRequired().HasDefaultValue(Shared.Infrastructure.Domain.Enums.FindingStatus.Open);
            entity.Property(x => x.Department).HasConversion<int>().IsRequired().HasDefaultValue(Shared.Infrastructure.Domain.Enums.FindingDepartment.Admin);
            entity.Property(x => x.RaisedBy).HasConversion<int>().IsRequired().HasDefaultValue(Shared.Infrastructure.Domain.Enums.FindingRaisedBy.InternalAudit);
            entity.Property(x => x.AssignedDepartment).HasConversion<int>();

            entity.Property(x => x.Title).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.ClauseReference).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Remarks).HasMaxLength(2000);
            entity.Property(x => x.ConfidenceScore).HasColumnType("decimal(5,4)");

            entity.HasIndex(x => new { x.Scope, x.TenantId });
            entity.HasIndex(x => x.ComplianceJourneyId);
            entity.HasIndex(x => x.ObjectiveElementId);
            entity.HasIndex(x => x.TaskId);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.AuditSessionId);

            entity.HasOne(x => x.ComplianceJourney)
                .WithMany()
                .HasForeignKey(x => x.ComplianceJourneyId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.ObjectiveElement)
                .WithMany()
                .HasForeignKey(x => x.ObjectiveElementId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(x => x.Task)
                .WithMany()
                .HasForeignKey(x => x.TaskId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasCheckConstraint(
                "CK_Finding_AuditScopeLinkage",
                "([Scope] <> 1) OR ([ComplianceJourneyId] IS NOT NULL AND [ObjectiveElementId] IS NOT NULL)");

            entity.HasCheckConstraint(
                "CK_Finding_ConfidenceRange",
                "([ConfidenceScore] IS NULL) OR ([ConfidenceScore] >= 0 AND [ConfidenceScore] <= 1)");

            entity.HasDiscriminator<string>("FindingKind")
                .HasValue<Finding>("Finding")
                .HasValue<ComplianceAuditFinding>("ComplianceAuditFinding");
        });

        builder.Entity<FindingEvidence>(entity =>
        {
            entity.ToTable("FindingEvidences");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.FindingId, x.EvidenceId }).IsUnique();

            entity.HasOne(x => x.Finding)
                .WithMany()
                .HasForeignKey(x => x.FindingId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Evidence)
                .WithMany()
                .HasForeignKey(x => x.EvidenceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<FindingAuditTrail>(entity =>
        {
            entity.ToTable("FindingAuditTrails");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Action).HasMaxLength(128).IsRequired();
            entity.Property(x => x.ActorType).HasMaxLength(32).IsRequired();
            entity.Property(x => x.PreviousStateJson).HasColumnType("nvarchar(max)");
            entity.Property(x => x.NewStateJson).HasColumnType("nvarchar(max)");
            entity.HasIndex(x => new { x.FindingId, x.OccurredAt });

            entity.HasOne(x => x.Finding)
                .WithMany()
                .HasForeignKey(x => x.FindingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<DeficiencyAuditTrail>(entity =>
        {
            entity.ToTable("DeficiencyAuditTrails");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Action).HasMaxLength(128).IsRequired();
            entity.Property(x => x.ActorType).HasMaxLength(32).IsRequired();
            entity.Property(x => x.DetailJson).HasColumnType("nvarchar(max)");
            entity.HasIndex(x => new { x.DeficiencyId, x.OccurredAt });
        });

        builder.Entity<DepartmentLabelMapping>(entity =>
        {
            entity.ToTable("DepartmentLabelMappings");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OeDepartmentLabel).HasMaxLength(200).IsRequired();
            entity.Property(x => x.NormalizedCode).HasMaxLength(64).IsRequired();
            entity.Property(x => x.DefaultResponsibleRole).HasMaxLength(128).IsRequired();
            entity.HasIndex(x => x.OeDepartmentLabel).IsUnique();
            entity.HasIndex(x => x.NormalizedCode);
        });

        builder.Entity<DepartmentOwnership>(entity =>
        {
            entity.ToTable("DepartmentOwnerships");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PrimaryOwnerUserId).HasMaxLength(64).IsRequired();
            entity.Property(x => x.BackupOwnerUserId).HasMaxLength(64);
            entity.HasIndex(x => new { x.OrganizationId, x.HospitalId, x.DepartmentId, x.Status });
            entity.HasOne(x => x.Department)
                .WithMany()
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ObjectiveElementOwnership>(entity =>
        {
            entity.ToTable("ObjectiveElementOwnerships");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PrimaryOwnerUserId).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Status).HasConversion<int>().IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();
            entity.HasIndex(x => new { x.HospitalId, x.ObjectiveElementId }).IsUnique();
            entity.HasIndex(x => new { x.HospitalId, x.FrameworkId });
            entity.HasOne(x => x.ObjectiveElement)
                .WithMany()
                .HasForeignKey(x => x.ObjectiveElementId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Department)
                .WithMany()
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AssignmentRoutingLog>(entity =>
        {
            entity.ToTable("AssignmentRoutingLogs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EntityType).HasMaxLength(64).IsRequired();
            entity.Property(x => x.RecommendedUserId).HasMaxLength(64);
            entity.Property(x => x.AppliedUserId).HasMaxLength(64);
            entity.Property(x => x.RoutingReason).HasMaxLength(1000).IsRequired();
            entity.HasIndex(x => new { x.OrganizationId, x.CreatedAt });
            entity.HasIndex(x => new { x.EntityType, x.EntityId });
        });

        builder.Entity<SlaPolicy>(entity =>
        {
            entity.ToTable("SlaPolicies");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => new { x.OrganizationId, x.Severity, x.Status });
            entity.HasOne(x => x.EscalationPolicy)
                .WithMany()
                .HasForeignKey(x => x.EscalationPolicyId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<EscalationPolicy>(entity =>
        {
            entity.ToTable("EscalationPolicies");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => new { x.OrganizationId, x.HospitalId, x.Severity });
        });

        builder.Entity<EscalationLevel>(entity =>
        {
            entity.ToTable("EscalationLevels");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TargetLabel).HasMaxLength(200).IsRequired();
            entity.Property(x => x.TargetDesignation).HasMaxLength(100);
            entity.Property(x => x.TargetUserId).HasMaxLength(64);
            entity.HasOne(x => x.Policy)
                .WithMany(p => p.Levels)
                .HasForeignKey(x => x.EscalationPolicyId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.EscalationPolicyId, x.Level }).IsUnique();
        });

        builder.Entity<DeficiencyEscalationHistory>(entity =>
        {
            entity.ToTable("DeficiencyEscalationHistories");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TargetLabel).HasMaxLength(200).IsRequired();
            entity.Property(x => x.TargetUserId).HasMaxLength(64);
            entity.Property(x => x.Reason).HasMaxLength(500).IsRequired();
            entity.Property(x => x.AcknowledgedBy).HasMaxLength(64);
            entity.HasIndex(x => new { x.DeficiencyId, x.TriggeredAt });
            entity.HasIndex(x => new { x.OrganizationId, x.State });
        });

        builder.Entity<FindingComment>(entity =>
        {
            entity.ToTable("FindingComments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Body).HasMaxLength(4000).IsRequired();
            entity.HasIndex(x => x.FindingId);

            entity.HasOne(x => x.Finding)
                .WithMany()
                .HasForeignKey(x => x.FindingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<FindingAttachment>(entity =>
        {
            entity.ToTable("FindingAttachments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FileName).HasMaxLength(500).IsRequired();
            entity.Property(x => x.FileUrl).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(128);
            entity.Property(x => x.AttachmentType).HasConversion<int>().IsRequired();
            entity.HasIndex(x => x.FindingId);

            entity.HasOne(x => x.Finding)
                .WithMany()
                .HasForeignKey(x => x.FindingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------------------------------------------------------------------
        // Control Center entities
        // ---------------------------------------------------------------------
        builder.Entity<ComplianceDepartment>(entity =>
        {
            entity.ToTable("Departments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.Property(x => x.OrganizationId).IsRequired();
            entity.Property(x => x.Code).IsRequired().HasMaxLength(50);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
            entity.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
            entity.HasIndex(x => new { x.OrganizationId, x.Code }).IsUnique();
        });

        builder.Entity<Capa>(entity =>
        {
            entity.ToTable("Capas");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.Property(x => x.TenantId).IsRequired();
            entity.Property(x => x.OrganizationId).IsRequired();
            entity.Property(x => x.HospitalId).IsRequired();
            entity.Property(x => x.Title).IsRequired().HasMaxLength(500);
            entity.Property(x => x.Description).HasMaxLength(4000);
            entity.Property(x => x.RootCause).HasMaxLength(2000);
            entity.Property(x => x.CorrectiveAction).HasMaxLength(2000);
            entity.Property(x => x.PreventiveAction).HasMaxLength(2000);
            entity.Property(x => x.OwnerName).HasMaxLength(200);
            entity.Property(x => x.Status).IsRequired().HasConversion<int>().HasDefaultValue(CapaStatus.Open);
            entity.HasIndex(x => new { x.TenantId, x.HospitalId });
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.DueDate);
            entity.HasIndex(x => x.SourceFindingId);
        });

        builder.Entity<ScheduledAudit>(entity =>
        {
            entity.ToTable("ScheduledAudits");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.Property(x => x.TenantId).IsRequired();
            entity.Property(x => x.OrganizationId).IsRequired();
            entity.Property(x => x.HospitalId).IsRequired();
            entity.Property(x => x.FrameworkId).IsRequired();
            entity.Property(x => x.Title).IsRequired().HasMaxLength(300);
            entity.Property(x => x.ScheduledDate).IsRequired();
            entity.Property(x => x.Status).IsRequired().HasConversion<int>().HasDefaultValue(ScheduledAuditStatus.Planned);
            entity.Property(x => x.AuditorName).HasMaxLength(300);
            entity.Property(x => x.Result).HasMaxLength(2000);
            entity.Property(x => x.ProgramYear);
            entity.Property(x => x.ProgramMonth);
            entity.Property(x => x.AuditType).HasConversion<int?>();
            entity.Property(x => x.ProgramName).HasMaxLength(300);
            entity.HasIndex(x => new { x.TenantId, x.HospitalId, x.FrameworkId });
            entity.HasIndex(x => x.ScheduledDate);
            entity.HasIndex(x => x.ComplianceJourneyId);
            entity.HasIndex(x => new { x.OrganizationId, x.ProgramYear, x.ProgramMonth });

        });

        builder.Entity<Alert>(entity =>
        {
            entity.ToTable("Alerts");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.Property(x => x.TenantId).IsRequired();
            entity.Property(x => x.OrganizationId).IsRequired();
            entity.Property(x => x.Severity).IsRequired().HasConversion<int>().HasDefaultValue(AlertSeverity.Medium);
            entity.Property(x => x.Type).IsRequired().HasConversion<int>();
            entity.Property(x => x.Status).IsRequired().HasConversion<int>().HasDefaultValue(AlertStatus.Active);
            entity.Property(x => x.Title).IsRequired().HasMaxLength(300);
            entity.Property(x => x.Description).IsRequired().HasMaxLength(2000);
            entity.Property(x => x.Metric).HasMaxLength(100);
            entity.Property(x => x.EntityType).HasMaxLength(100);
            entity.Property(x => x.DedupeKey).HasMaxLength(200);
            entity.HasIndex(x => new { x.TenantId, x.Status });
            entity.HasIndex(x => new { x.OrganizationId, x.HospitalId });
            entity.HasIndex(x => x.DedupeKey);
        });

        builder.Entity<ActivityEvent>(entity =>
        {
            entity.ToTable("ActivityEvents");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.Property(x => x.TenantId).IsRequired();
            entity.Property(x => x.OrganizationId).IsRequired();
            entity.Property(x => x.Type).IsRequired().HasConversion<int>();
            entity.Property(x => x.Title).IsRequired().HasMaxLength(300);
            entity.Property(x => x.Description).IsRequired().HasMaxLength(2000);
            entity.Property(x => x.ActorName).HasMaxLength(200);
            entity.Property(x => x.DepartmentName).HasMaxLength(200);
            entity.Property(x => x.OccurredAt).IsRequired();
            entity.HasIndex(x => new { x.TenantId, x.OccurredAt });
            entity.HasIndex(x => new { x.OrganizationId, x.HospitalId, x.OccurredAt });
            entity.HasIndex(x => x.Type);
        });

        // Apply entity configurations
        builder.ApplyConfiguration(new EntityConfigurations.ComplianceJourneyConfiguration());
        builder.ApplyConfiguration(new EntityConfigurations.ComplianceJourneyHistoryConfiguration());
        builder.ApplyConfiguration(new EntityConfigurations.FrameworkConfigurationConfiguration());
        builder.ApplyConfiguration(new EntityConfigurations.ObjectiveJourneyConfiguration());
        builder.ApplyConfiguration(new EntityConfigurations.OrganizationComplianceObligationConfiguration());
        builder.ApplyConfiguration(new EntityConfigurations.ComplianceDeficiencyConfiguration());

        // Sprint 13A – Copilot Core
        builder.ApplyConfiguration(new CopilotRecommendationConfiguration());
        builder.ApplyConfiguration(new AiDraftConfiguration());

        // Sprint 13D – Approval Card Framework
        builder.ApplyConfiguration(new Copilot.EntityConfigurations.CopilotActionProposalConfiguration());
        builder.ApplyConfiguration(new Copilot.EntityConfigurations.ProposalAuditEventConfiguration());

        // Sprint 13E – Persona Experience Layer
        builder.ApplyConfiguration(new Copilot.EntityConfigurations.CopilotActivityEventConfiguration());

        // Sprint 13F – Copilot Observability
        builder.ApplyConfiguration(new Copilot.EntityConfigurations.CopilotEffectivenessSnapshotConfiguration());

        builder.Entity<OrganizationMaturityStageState>(entity =>
        {
            entity.ToTable("OrganizationMaturityStageStates");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Stage).HasConversion<int>();
            entity.Property(x => x.State).HasConversion<int>();
            entity.Property(x => x.ProgressPercent).HasPrecision(5, 2);
            entity.Property(x => x.ReadinessPercent).HasPrecision(5, 2);
            entity.Property(x => x.UnlockReadinessPercent).HasPrecision(5, 2);
            entity.HasIndex(x => new { x.JourneyId, x.Stage }).IsUnique();
            entity.HasIndex(x => new { x.HospitalId, x.FrameworkId });
        });

        builder.Entity<MaturityStageUnlockAuditEntry>(entity =>
        {
            entity.ToTable("MaturityStageUnlockAuditEntries");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Stage).HasConversion<int>();
            entity.Property(x => x.PreviousState).HasConversion<int>();
            entity.Property(x => x.NewState).HasConversion<int>();
            entity.Property(x => x.TriggerSource).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => new { x.JourneyId, x.Stage, x.EvaluatedAt });
        });

        // OutboxMessage configuration
        builder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("OutboxMessages");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.AggregateId)
                  .IsRequired();

            entity.Property(x => x.OccurredOn)
                  .IsRequired();

            entity.Property(x => x.Type)
                  .IsRequired()
                  .HasMaxLength(500);

            entity.Property(x => x.Payload)
                  .IsRequired()
                  .HasColumnType("nvarchar(max)");

            entity.Property(x => x.ProjectionProcessedOn);
            entity.Property(x => x.RelayProcessedOn);

            entity.Property(x => x.RetryCount)
                  .IsRequired()
                  .HasDefaultValue(0);

            entity.Property(x => x.Error)
                  .HasMaxLength(4000);

            // Indexes for projection and relay processing pipelines.
            entity.HasIndex(x => x.ProjectionProcessedOn)
                  .HasDatabaseName("IX_Outbox_ProjectionProcessedOn");
            entity.HasIndex(x => x.RelayProcessedOn)
                  .HasDatabaseName("IX_Outbox_RelayProcessedOn");

            // Index on AggregateId for aggregate-level queries
            entity.HasIndex(x => x.AggregateId)
                  .HasDatabaseName("IX_Outbox_AggregateId");
        });

        builder.Entity<WizardOnboardingAssessment>(entity =>
        {
            entity.ToTable("WizardOnboardingAssessments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AnswersJson)
                .IsRequired()
                .HasColumnType("nvarchar(max)");
            entity.Property(x => x.WizardVersion).IsRequired();
            entity.HasIndex(x => x.HospitalId);
            entity.HasIndex(x => x.OrganizationId);
            entity.HasIndex(x => new { x.HospitalId, x.CreatedAt });
        });

        // FrameworkChapterDistribution configuration
        builder.Entity<FrameworkChapterDistribution>(entity =>
        {
            entity.ToTable("FrameworkChapterDistributions");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.FrameworkId).IsRequired();
            entity.Property(x => x.ChapterId).IsRequired();

            entity.Property(x => x.TotalStandards).IsRequired();
            entity.Property(x => x.TotalObjectiveElements).IsRequired();

            entity.Property(x => x.CoreCount).IsRequired();
            entity.Property(x => x.CommitmentCount).IsRequired();
            entity.Property(x => x.AchievementCount).IsRequired();
            entity.Property(x => x.ExcellenceCount).IsRequired();

            entity.Property(x => x.Source)
                  .IsRequired()
                  .HasConversion<int>();

            entity.Property(x => x.Version)
                  .IsRequired();

            entity.Property(x => x.CalculatedAt)
                  .IsRequired();

            entity.HasIndex(x => new { x.FrameworkId, x.ChapterId, x.Version })
                  .IsUnique();

            entity.HasCheckConstraint(
                "CK_Distribution_SumMatchesTotalOE",
                "[CoreCount] + [CommitmentCount] + [AchievementCount] + [ExcellenceCount] = [TotalObjectiveElements]");
        });

        builder.Entity<RuleConfig>(entity =>
        {
            entity.ToTable("RuleConfigs");
            entity.Property(x => x.OperationalMode)
                  .IsRequired()
                  .HasConversion<int>();
        });

        // Sprint 14 – External Auditor Workspace
        builder.Entity<AuditSession>(entity =>
        {
            entity.ToTable("AuditSessions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.SessionNumber).HasMaxLength(32).IsRequired();
            entity.Property(x => x.AuditType).HasConversion<int>().IsRequired();
            entity.Property(x => x.Scope).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.LeadAuditorName).HasMaxLength(256);
            entity.Property(x => x.AuditorTeamJson).HasColumnType("nvarchar(max)");
            entity.Property(x => x.Status).HasConversion<int>().IsRequired();
            entity.Property(x => x.OverallResult).HasConversion<int>().IsRequired();
            entity.Property(x => x.Summary).HasMaxLength(4000);
            entity.Property(x => x.ScheduledAuditId);
            entity.Property(x => x.ComplianceProgramId);
            entity.HasIndex(x => new { x.TenantId, x.SessionNumber }).IsUnique();
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.ScheduledAuditId);
            entity.HasIndex(x => x.ComplianceProgramId);
        });

        builder.Entity<AuditObservation>(entity =>
        {
            entity.ToTable("AuditObservations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ObservationNumber).HasMaxLength(48).IsRequired();
            entity.Property(x => x.Type).HasConversion<int>().IsRequired();
            entity.Property(x => x.Title).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.ChapterCode).HasMaxLength(64);
            entity.Property(x => x.OeCode).HasMaxLength(64);
            entity.Property(x => x.Department).HasConversion<int?>();
            entity.Property(x => x.CreatedByName).HasMaxLength(256);
            entity.HasIndex(x => x.AuditSessionId);
            entity.HasIndex(x => new { x.ChapterCode, x.OeCode });
            entity.HasOne(x => x.AuditSession).WithMany(s => s.Observations)
                .HasForeignKey(x => x.AuditSessionId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ObjectiveElement).WithMany()
                .HasForeignKey(x => x.ObjectiveElementId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<AuditComment>(entity =>
        {
            entity.ToTable("AuditComments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Type).HasConversion<int>().IsRequired();
            entity.Property(x => x.Content).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.AuthorName).HasMaxLength(256);
            entity.HasIndex(x => x.AuditSessionId);
            entity.HasOne(x => x.AuditSession).WithMany(s => s.Comments)
                .HasForeignKey(x => x.AuditSessionId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ParentComment).WithMany(c => c.Replies)
                .HasForeignKey(x => x.ParentCommentId).OnDelete(DeleteBehavior.Restrict);
        });

        // Sprint 15 – QDMS
        builder.Entity<ControlledDocument>(entity =>
        {
            entity.ToTable("ControlledDocuments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.DocumentNumber).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(500).IsRequired();
            entity.Property(x => x.DocumentType).HasConversion<int>().IsRequired();
            entity.Property(x => x.Status).HasConversion<int>().IsRequired();
            entity.Property(x => x.Department).HasMaxLength(256);
            entity.Property(x => x.OwnerName).HasMaxLength(256);
            entity.Property(x => x.ReviewerName).HasMaxLength(256);
            entity.Property(x => x.ApproverName).HasMaxLength(256);
            entity.Property(x => x.CurrentVersionLabel).HasMaxLength(16);
            entity.Property(x => x.ReviewOwnerName).HasMaxLength(256);
            entity.Property(x => x.MappedFramework).HasMaxLength(256);
            entity.Property(x => x.MappedChapter).HasMaxLength(256);
            entity.Property(x => x.MappedStandard).HasMaxLength(256);
            entity.Property(x => x.MappedObjectiveElement).HasMaxLength(512);
            entity.Property(x => x.CreatedByName).HasMaxLength(256);
            entity.HasIndex(x => new { x.TenantId, x.DocumentNumber }).IsUnique();
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.DocumentType);
            entity.HasIndex(x => x.NextReviewDate);
            entity.HasIndex(x => x.ExpiryDate);
        });

        builder.Entity<DocumentVersion>(entity =>
        {
            entity.ToTable("DocumentVersions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.VersionLabel).HasMaxLength(16).IsRequired();
            entity.Property(x => x.Content).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(x => x.ChangeSummary).HasMaxLength(2000);
            entity.Property(x => x.AuthoringSchemaKey).HasMaxLength(64);
            entity.Property(x => x.CreatedByName).HasMaxLength(256);
            entity.HasIndex(x => x.ControlledDocumentId);
            entity.HasOne(x => x.ControlledDocument).WithMany(d => d.Versions)
                .HasForeignKey(x => x.ControlledDocumentId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<DocumentReview>(entity =>
        {
            entity.ToTable("DocumentReviews");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ReviewerName).HasMaxLength(256);
            entity.Property(x => x.Outcome).HasMaxLength(256);
            entity.Property(x => x.Notes).HasMaxLength(4000);
            entity.HasOne(x => x.ControlledDocument).WithMany(d => d.Reviews)
                .HasForeignKey(x => x.ControlledDocumentId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<DocumentApproval>(entity =>
        {
            entity.ToTable("DocumentApprovals");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ApproverRole).HasMaxLength(128).IsRequired();
            entity.Property(x => x.ApproverName).HasMaxLength(256);
            entity.Property(x => x.Comments).HasMaxLength(2000);
            entity.Property(x => x.Status).HasConversion<int>().IsRequired();
            entity.HasIndex(x => x.DocumentVersionId);
            entity.HasOne(x => x.DocumentVersion).WithMany(v => v.Approvals)
                .HasForeignKey(x => x.DocumentVersionId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<DocumentAcknowledgement>(entity =>
        {
            entity.ToTable("DocumentAcknowledgements");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AssigneeKey).HasMaxLength(128).IsRequired();
            entity.Property(x => x.AssigneeName).HasMaxLength(256).IsRequired();
            entity.Property(x => x.AssigneeType).HasConversion<int>().IsRequired();
            entity.HasIndex(x => x.ControlledDocumentId);
            entity.HasOne(x => x.ControlledDocument).WithMany(d => d.Acknowledgements)
                .HasForeignKey(x => x.ControlledDocumentId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<DocumentTemplate>(entity =>
        {
            entity.ToTable("DocumentTemplates");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(128).IsRequired();
            entity.Property(x => x.DocumentType).HasConversion<int>().IsRequired();
            entity.Property(x => x.ContentTemplate).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(x => x.SectionsSchemaJson).HasColumnType("nvarchar(max)");
            entity.Property(x => x.AuthoringSchemaKey).HasMaxLength(64);
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.HasIndex(x => x.IsBuiltIn);
        });

        builder.Entity<DocumentLifecycleAuditEntry>(entity =>
        {
            entity.ToTable("DocumentLifecycleAuditEntries");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EventType).HasMaxLength(64).IsRequired();
            entity.Property(x => x.PerformedByName).HasMaxLength(256);
            entity.Property(x => x.Details).HasMaxLength(4000);
            entity.HasIndex(x => x.ControlledDocumentId);
            entity.HasOne(x => x.ControlledDocument).WithMany(d => d.LifecycleAudit)
                .HasForeignKey(x => x.ControlledDocumentId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<DocumentSection>(entity =>
        {
            entity.ToTable("DocumentSections");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.SectionKey).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(256).IsRequired();
            entity.Property(x => x.BlocksJson).HasColumnType("nvarchar(max)").IsRequired();
            entity.HasIndex(x => x.DocumentVersionId);
            entity.HasOne(x => x.DocumentVersion).WithMany(v => v.Sections)
                .HasForeignKey(x => x.DocumentVersionId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<DocumentAuthoringComment>(entity =>
        {
            entity.ToTable("DocumentAuthoringComments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.BlockId).HasMaxLength(64);
            entity.Property(x => x.Content).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.AuthorName).HasMaxLength(256);
            entity.Property(x => x.MentionedUserName).HasMaxLength(256);
            entity.Property(x => x.CommentType).HasConversion<int>().IsRequired();
            entity.HasIndex(x => x.ControlledDocumentId);
            entity.HasOne(x => x.ControlledDocument).WithMany(d => d.AuthoringComments)
                .HasForeignKey(x => x.ControlledDocumentId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<DocumentAuthoringCheckpoint>(entity =>
        {
            entity.ToTable("DocumentAuthoringCheckpoints");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.SnapshotJson).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(x => x.Label).HasMaxLength(128);
            entity.Property(x => x.CreatedByName).HasMaxLength(256);
            entity.HasIndex(x => x.ControlledDocumentId);
            entity.HasOne(x => x.ControlledDocument).WithMany(d => d.AuthoringCheckpoints)
                .HasForeignKey(x => x.ControlledDocumentId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<DocumentCrossReference>(entity =>
        {
            entity.ToTable("DocumentCrossReferences");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.BlockId).HasMaxLength(64);
            entity.Property(x => x.TargetLabel).HasMaxLength(512).IsRequired();
            entity.Property(x => x.TargetRoute).HasMaxLength(512);
            entity.Property(x => x.ReferenceType).HasConversion<int>().IsRequired();
            entity.HasIndex(x => x.ControlledDocumentId);
            entity.HasOne(x => x.ControlledDocument).WithMany(d => d.CrossReferences)
                .HasForeignKey(x => x.ControlledDocumentId).OnDelete(DeleteBehavior.Cascade);
        });

        // Sprint 16 – Incident Management & Patient Safety
        builder.Entity<Incident>(entity =>
        {
            entity.ToTable("Incidents");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.IncidentNumber).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.IncidentType).HasConversion<int>().IsRequired();
            entity.Property(x => x.Severity).HasConversion<int>().IsRequired();
            entity.Property(x => x.Priority).HasConversion<int>().IsRequired();
            entity.Property(x => x.Category).HasConversion<int>().IsRequired();
            entity.Property(x => x.Department).HasConversion<int>().IsRequired();
            entity.Property(x => x.Status).HasConversion<int>().IsRequired();
            entity.Property(x => x.InvestigationStatus).HasConversion<int>().IsRequired();
            entity.Property(x => x.Location).HasMaxLength(256);
            entity.Property(x => x.ReporterName).HasMaxLength(256);
            entity.Property(x => x.OwnerName).HasMaxLength(256);
            entity.Property(x => x.StaffInvolvedJson).HasColumnType("nvarchar(max)");
            entity.Property(x => x.RelatedDocumentationJson).HasColumnType("nvarchar(max)");
            entity.HasIndex(x => new { x.TenantId, x.IncidentNumber }).IsUnique();
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.Severity);
            entity.HasIndex(x => x.Category);
            entity.HasOne(x => x.Investigation).WithOne(i => i.Incident).HasForeignKey<IncidentInvestigation>(i => i.IncidentId);
            entity.HasOne(x => x.RootCauseAnalysis).WithOne(r => r.Incident).HasForeignKey<IncidentRca>(r => r.IncidentId);
        });

        builder.Entity<IncidentInvestigation>(entity =>
        {
            entity.ToTable("IncidentInvestigations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasConversion<int>().IsRequired();
            entity.Property(x => x.IncidentSummary).HasMaxLength(4000);
            entity.Property(x => x.Recommendations).HasMaxLength(4000);
            entity.Property(x => x.ContributingFactorsJson).HasColumnType("nvarchar(max)");
            entity.Property(x => x.PeopleFactorsJson).HasColumnType("nvarchar(max)");
            entity.Property(x => x.EnvironmentFactorsJson).HasColumnType("nvarchar(max)");
            entity.Property(x => x.EquipmentFactorsJson).HasColumnType("nvarchar(max)");
            entity.Property(x => x.ProcessFactorsJson).HasColumnType("nvarchar(max)");
            entity.Property(x => x.CommunicationFactorsJson).HasColumnType("nvarchar(max)");
            entity.Property(x => x.TimelineJson).HasColumnType("nvarchar(max)");
            entity.Property(x => x.ReviewersJson).HasColumnType("nvarchar(max)");
            entity.HasIndex(x => x.IncidentId).IsUnique();
        });

        builder.Entity<IncidentRca>(entity =>
        {
            entity.ToTable("IncidentRcas");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Method).HasConversion<int>().IsRequired();
            entity.Property(x => x.RootCauseSummary).HasMaxLength(4000);
            entity.Property(x => x.CorrectiveActionSummary).HasMaxLength(4000);
            entity.Property(x => x.PreventiveActionSummary).HasMaxLength(4000);
            entity.Property(x => x.RootCauseCategory).HasMaxLength(64);
            entity.Property(x => x.FiveWhysJson).HasColumnType("nvarchar(max)");
            entity.Property(x => x.FishboneJson).HasColumnType("nvarchar(max)");
            entity.Property(x => x.HumanFactorsJson).HasColumnType("nvarchar(max)");
            entity.Property(x => x.ProcessFactorsJson).HasColumnType("nvarchar(max)");
            entity.Property(x => x.EnvironmentalFactorsJson).HasColumnType("nvarchar(max)");
            entity.Property(x => x.EquipmentFactorsJson).HasColumnType("nvarchar(max)");
            entity.Property(x => x.OrganizationFactorsJson).HasColumnType("nvarchar(max)");
            entity.HasIndex(x => x.IncidentId).IsUnique();
        });

        builder.Entity<PatientFeedback>(entity =>
        {
            entity.ToTable("PatientFeedbacks");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FeedbackNumber).HasMaxLength(32).IsRequired();
            entity.Property(x => x.FeedbackType).HasConversion<int>().IsRequired();
            entity.Property(x => x.Title).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(32).IsRequired();
            entity.HasIndex(x => new { x.TenantId, x.FeedbackNumber }).IsUnique();
            entity.HasOne(x => x.LinkedIncident).WithMany().HasForeignKey(x => x.LinkedIncidentId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<LessonLearned>(entity =>
        {
            entity.ToTable("LessonsLearned");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.LessonNumber).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Summary).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.Recommendation).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.DepartmentsJson).HasColumnType("nvarchar(max)");
            entity.Property(x => x.RelatedSopsJson).HasColumnType("nvarchar(max)");
            entity.Property(x => x.TrainingNeeded).HasMaxLength(2000);
            entity.Property(x => x.PreventiveActions).HasMaxLength(2000);
            entity.HasIndex(x => new { x.TenantId, x.LessonNumber }).IsUnique();
            entity.HasOne(x => x.SourceIncident).WithMany().HasForeignKey(x => x.SourceIncidentId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<IncidentAttachment>(entity =>
        {
            entity.ToTable("IncidentAttachments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FileName).HasMaxLength(500).IsRequired();
            entity.Property(x => x.FileUrl).HasMaxLength(2000).IsRequired();
            entity.HasIndex(x => x.IncidentId);
            entity.HasOne(x => x.Incident).WithMany(i => i.Attachments).HasForeignKey(x => x.IncidentId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<IncidentTimelineEntry>(entity =>
        {
            entity.ToTable("IncidentTimelineEntries");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EventType).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.ActorName).HasMaxLength(256);
            entity.HasIndex(x => x.IncidentId);
            entity.HasOne(x => x.Incident).WithMany(i => i.Timeline).HasForeignKey(x => x.IncidentId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<IncidentAuditTrail>(entity =>
        {
            entity.ToTable("IncidentAuditTrails");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Action).HasMaxLength(128).IsRequired();
            entity.Property(x => x.ActorType).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Detail).HasMaxLength(2000);
            entity.HasIndex(x => new { x.IncidentId, x.OccurredAt });
            entity.HasOne(x => x.Incident).WithMany().HasForeignKey(x => x.IncidentId).OnDelete(DeleteBehavior.Cascade);
        });

        // Sprint 17 – Clinical Indicators & Quality Intelligence
        builder.Entity<ClinicalIndicator>(entity =>
        {
            entity.ToTable("ClinicalIndicators");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.IndicatorCode).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.Category).HasConversion<int>().IsRequired();
            entity.Property(x => x.Department).HasConversion<int>().IsRequired();
            entity.Property(x => x.MeasurementUnit).HasConversion<int>().IsRequired();
            entity.Property(x => x.CalculationMethod).HasConversion<int>().IsRequired();
            entity.Property(x => x.Frequency).HasConversion<int>().IsRequired();
            entity.Property(x => x.FrameworkMapping).HasConversion<int>().IsRequired();
            entity.Property(x => x.Status).HasConversion<int>().IsRequired();
            entity.Property(x => x.RiskLevel).HasConversion<int>().IsRequired();
            entity.Property(x => x.ThresholdStatus).HasConversion<int>().IsRequired();
            entity.Property(x => x.Trend).HasConversion<int>().IsRequired();
            entity.Property(x => x.NumeratorDefinition).HasMaxLength(2000);
            entity.Property(x => x.DenominatorDefinition).HasMaxLength(2000);
            entity.Property(x => x.FormulaExpression).HasMaxLength(2000);
            entity.Property(x => x.FrameworkReference).HasMaxLength(128);
            entity.Property(x => x.DataSource).HasMaxLength(256);
            entity.Property(x => x.Comments).HasMaxLength(2000);
            entity.Property(x => x.OwnerName).HasMaxLength(256);
            entity.Property(x => x.RelatedDocumentationJson).HasColumnType("nvarchar(max)");
            entity.Property(x => x.RelatedIncidentCategoriesJson).HasColumnType("nvarchar(max)");
            entity.Property(x => x.CurrentValue).HasColumnType("decimal(18,4)");
            entity.Property(x => x.TargetValue).HasColumnType("decimal(18,4)");
            entity.Property(x => x.GreenThreshold).HasColumnType("decimal(18,4)");
            entity.Property(x => x.YellowThreshold).HasColumnType("decimal(18,4)");
            entity.Property(x => x.RedThreshold).HasColumnType("decimal(18,4)");
            entity.Property(x => x.CriticalThreshold).HasColumnType("decimal(18,4)");
            entity.Property(x => x.CurrentNumerator).HasColumnType("decimal(18,4)");
            entity.Property(x => x.CurrentDenominator).HasColumnType("decimal(18,4)");
            entity.HasIndex(x => new { x.TenantId, x.IndicatorCode }).IsUnique();
            entity.HasIndex(x => x.Category);
            entity.HasIndex(x => x.Department);
            entity.HasIndex(x => x.ThresholdStatus);
        });

        builder.Entity<IndicatorMeasurement>(entity =>
        {
            entity.ToTable("IndicatorMeasurements");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Frequency).HasConversion<int>().IsRequired();
            entity.Property(x => x.Value).HasColumnType("decimal(18,4)").IsRequired();
            entity.Property(x => x.Numerator).HasColumnType("decimal(18,4)");
            entity.Property(x => x.Denominator).HasColumnType("decimal(18,4)");
            entity.Property(x => x.Source).HasMaxLength(256);
            entity.Property(x => x.Notes).HasMaxLength(2000);
            entity.HasIndex(x => new { x.IndicatorId, x.PeriodEnd });
            entity.HasOne(x => x.Indicator).WithMany(i => i.Measurements).HasForeignKey(x => x.IndicatorId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<IndicatorThresholdHistory>(entity =>
        {
            entity.ToTable("IndicatorThresholdHistories");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Reason).HasMaxLength(2000);
            entity.Property(x => x.ChangedByName).HasMaxLength(256);
            entity.Property(x => x.TargetValue).HasColumnType("decimal(18,4)");
            entity.Property(x => x.GreenThreshold).HasColumnType("decimal(18,4)");
            entity.Property(x => x.YellowThreshold).HasColumnType("decimal(18,4)");
            entity.Property(x => x.RedThreshold).HasColumnType("decimal(18,4)");
            entity.Property(x => x.CriticalThreshold).HasColumnType("decimal(18,4)");
            entity.HasIndex(x => new { x.IndicatorId, x.EffectiveFrom });
            entity.HasOne(x => x.Indicator).WithMany(i => i.ThresholdHistory).HasForeignKey(x => x.IndicatorId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<IndicatorAuditTrail>(entity =>
        {
            entity.ToTable("IndicatorAuditTrails");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Action).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Detail).HasMaxLength(2000);
            entity.Property(x => x.ActorName).HasMaxLength(256);
            entity.HasIndex(x => new { x.IndicatorId, x.OccurredAt });
            entity.HasOne(x => x.Indicator).WithMany(i => i.AuditTrail).HasForeignKey(x => x.IndicatorId).OnDelete(DeleteBehavior.Cascade);
        });

        // ---------------------------------------------------------------------
        // Global query filters
        // ---------------------------------------------------------------------
        // Enforce tenant/organization isolation.
        //
        // With `Tenancy:MandatoryQueryFilters` enabled the filters are registered ALWAYS, and an
        // unresolved tenant compares against Guid.Empty — matching nothing. Failing closed is the
        // point: a query returning zero rows is a visible bug found in minutes, whereas one
        // returning every tenant's rows is a breach found by a customer.
        //
        // With the flag off, the original conditional behaviour is preserved so the change can be
        // rolled back without a redeploy.
        if (_mandatoryQueryFilters || TenantScope != Guid.Empty)
        {
            builder.Entity<ComplianceJourney>()
                .HasQueryFilter(j => j.TenantId == TenantScope);

            builder.Entity<ObjectiveJourney>()
                .HasQueryFilter(o => o.TenantId == TenantScope);

            builder.Entity<OrganizationComplianceObligation>()
                .HasQueryFilter(o => o.TenantId == TenantScope);

            builder.Entity<ComplianceDeficiency>()
                .HasQueryFilter(d => d.TenantId == TenantScope);

            builder.Entity<AuditSession>()
                .HasQueryFilter(s => s.TenantId == TenantScope);

            builder.Entity<AuditObservation>()
                .HasQueryFilter(o => o.TenantId == TenantScope);

            builder.Entity<AuditComment>()
                .HasQueryFilter(c => c.TenantId == TenantScope);

            builder.Entity<ControlledDocument>()
                .HasQueryFilter(d => d.TenantId == TenantScope);

            builder.Entity<DocumentReview>()
                .HasQueryFilter(r => r.ControlledDocument.TenantId == TenantScope);

            builder.Entity<DocumentAcknowledgement>()
                .HasQueryFilter(a => a.ControlledDocument.TenantId == TenantScope);

            builder.Entity<DocumentLifecycleAuditEntry>()
                .HasQueryFilter(a => a.ControlledDocument.TenantId == TenantScope);

            builder.Entity<DocumentAuthoringComment>()
                .HasQueryFilter(c => c.ControlledDocument.TenantId == TenantScope);

            builder.Entity<DocumentAuthoringCheckpoint>()
                .HasQueryFilter(c => c.ControlledDocument.TenantId == TenantScope);

            builder.Entity<DocumentCrossReference>()
                .HasQueryFilter(x => x.ControlledDocument.TenantId == TenantScope);

            builder.Entity<Incident>()
                .HasQueryFilter(i => i.TenantId == TenantScope);

            builder.Entity<PatientFeedback>()
                .HasQueryFilter(f => f.TenantId == TenantScope);

            builder.Entity<LessonLearned>()
                .HasQueryFilter(l => l.TenantId == TenantScope);

            builder.Entity<ClinicalIndicator>()
                .HasQueryFilter(i => i.TenantId == TenantScope);
        }
    }
}
