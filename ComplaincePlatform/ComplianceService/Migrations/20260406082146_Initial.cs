using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceService.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PerformedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ComplianceFrameworks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Authority = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PlatformId = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplianceFrameworks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ComplianceSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HospitalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FrameworkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OverallScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    AuditConfidenceScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    CriticalCount = table.Column<int>(type: "int", nullable: false),
                    MajorCount = table.Column<int>(type: "int", nullable: false),
                    MinorCount = table.Column<int>(type: "int", nullable: false),
                    ChapterBreakdownJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MajorNCCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    MinorNCCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ObservationCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    OFICount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    HighRiskChaptersJson = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "[]"),
                    CriticalRiskCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    JourneyState = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplianceSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FrameworkChapterDistributions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FrameworkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChapterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalStandards = table.Column<int>(type: "int", nullable: false),
                    TotalObjectiveElements = table.Column<int>(type: "int", nullable: false),
                    CoreCount = table.Column<int>(type: "int", nullable: false),
                    CommitmentCount = table.Column<int>(type: "int", nullable: false),
                    AchievementCount = table.Column<int>(type: "int", nullable: false),
                    ExcellenceCount = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FrameworkChapterDistributions", x => x.Id);
                    table.CheckConstraint("CK_Distribution_SumMatchesTotalOE", "[CoreCount] + [CommitmentCount] + [AchievementCount] + [ExcellenceCount] = [TotalObjectiveElements]");
                });

            migrationBuilder.CreateTable(
                name: "FrameworkChapterSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HospitalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FrameworkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChapterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChapterScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    CompletedTasks = table.Column<int>(type: "int", nullable: false),
                    TotalTasks = table.Column<int>(type: "int", nullable: false),
                    RiskPenalty = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FrameworkChapterSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationComplianceOverviewSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HospitalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OverallScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    WorstFrameworkId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TotalCriticalRisks = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    UpcomingRenewals = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "[]"),
                    LastCalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationComplianceOverviewSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProcessedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Error = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlatformSequences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Prefix = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CurrentValue = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformSequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RuleConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FrameworkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LifecycleStage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RulesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WizardOnboardingAssessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HospitalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WizardVersion = table.Column<int>(type: "int", nullable: false),
                    AnswersJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WizardOnboardingAssessments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ComplianceChapters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FrameworkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 1.0m),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PlatformId = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplianceChapters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComplianceChapters_ComplianceFrameworks_FrameworkId",
                        column: x => x.FrameworkId,
                        principalTable: "ComplianceFrameworks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComplianceJourneys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HospitalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FrameworkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    EnrollmentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmissionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RenewalWindowStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReapplyAllowedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidityPeriodInMonths = table.Column<int>(type: "int", nullable: false),
                    MinimumRequiredScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    AuditDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AuditTeamDetails = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CorrectiveActionDeadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EnrollmentPaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EnrollmentFeeAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    EnrollmentFeePaidOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExternalReferenceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CertificateNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CurrentReadinessScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 0m),
                    RiskScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 0m),
                    IsPublicCertificateVisible = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SuspendedReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplianceJourneys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComplianceJourneys_ComplianceFrameworks_FrameworkId",
                        column: x => x.FrameworkId,
                        principalTable: "ComplianceFrameworks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompliancePrograms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HospitalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComplianceFrameworkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Explainability = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PlatformId = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompliancePrograms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompliancePrograms_ComplianceFrameworks_ComplianceFrameworkId",
                        column: x => x.ComplianceFrameworkId,
                        principalTable: "ComplianceFrameworks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FrameworkConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FrameworkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefaultValidityMonths = table.Column<int>(type: "int", nullable: false),
                    MinimumReadinessScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    RenewalDaysBeforeExpiry = table.Column<int>(type: "int", nullable: false, defaultValue: 90),
                    ReapplyWaitingDays = table.Column<int>(type: "int", nullable: false, defaultValue: 30),
                    MajorNcResolutionDays = table.Column<int>(type: "int", nullable: false, defaultValue: 60),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PlatformId = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FrameworkConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FrameworkConfigurations_ComplianceFrameworks_FrameworkId",
                        column: x => x.FrameworkId,
                        principalTable: "ComplianceFrameworks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Regulations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HospitalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComplianceFrameworkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TargetDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PlatformId = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Regulations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Regulations_ComplianceFrameworks_ComplianceFrameworkId",
                        column: x => x.ComplianceFrameworkId,
                        principalTable: "ComplianceFrameworks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Standards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChapterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 1.0m),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PlatformId = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Standards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Standards_ComplianceChapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "ComplianceChapters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ComplianceJourneyHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComplianceJourneyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OldStatus = table.Column<int>(type: "int", nullable: false),
                    NewStatus = table.Column<int>(type: "int", nullable: false),
                    ChangedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplianceJourneyHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComplianceJourneyHistories_ComplianceJourneys_ComplianceJourneyId",
                        column: x => x.ComplianceJourneyId,
                        principalTable: "ComplianceJourneys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ObjectiveElements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StandardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Classification = table.Column<int>(type: "int", nullable: false),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    IsScorable = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsApplicable = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDocumentationRequired = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Department = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ResponsibleRole = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Frequency = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EvidenceCategory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Weight = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 1.0m),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PlatformId = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObjectiveElements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ObjectiveElements_Standards_StandardId",
                        column: x => x.StandardId,
                        principalTable: "Standards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ActionPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObjectiveElementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RootCause = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorrectiveAction = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    LinkedTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActionPlans_ObjectiveElements_ObjectiveElementId",
                        column: x => x.ObjectiveElementId,
                        principalTable: "ObjectiveElements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComplianceAuditFindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComplianceJourneyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObjectiveElementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplianceAuditFindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComplianceAuditFindings_ComplianceJourneys_ComplianceJourneyId",
                        column: x => x.ComplianceJourneyId,
                        principalTable: "ComplianceJourneys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComplianceAuditFindings_ObjectiveElements_ObjectiveElementId",
                        column: x => x.ObjectiveElementId,
                        principalTable: "ObjectiveElements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Controls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProgramId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObjectiveElementId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phase = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Readiness"),
                    Explainability = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Controls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Controls_CompliancePrograms_ProgramId",
                        column: x => x.ProgramId,
                        principalTable: "CompliancePrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Controls_ObjectiveElements_ObjectiveElementId",
                        column: x => x.ObjectiveElementId,
                        principalTable: "ObjectiveElements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ObjectiveElementScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObjectiveElementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JourneyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    Rationale = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EvidenceSampleCompliancePercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsDocumentationInadequate = table.Column<bool>(type: "bit", nullable: false),
                    DowngradeApplied = table.Column<bool>(type: "bit", nullable: false),
                    DowngradeReason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsApplicable = table.Column<bool>(type: "bit", nullable: false),
                    NaReason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NaApprovedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssessedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObjectiveElementScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ObjectiveElementScores_ObjectiveElements_ObjectiveElementId",
                        column: x => x.ObjectiveElementId,
                        principalTable: "ObjectiveElements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComplianceTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HospitalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RegulationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObjectiveElementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ControlId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedToUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaturityState = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Explainability = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Criticality = table.Column<int>(type: "int", nullable: false, defaultValue: 3),
                    Weight = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 1.0m),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplianceTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComplianceTasks_Controls_ControlId",
                        column: x => x.ControlId,
                        principalTable: "Controls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ComplianceTasks_ObjectiveElements_ObjectiveElementId",
                        column: x => x.ObjectiveElementId,
                        principalTable: "ObjectiveElements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ComplianceTasks_Regulations_RegulationId",
                        column: x => x.RegulationId,
                        principalTable: "Regulations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Evidences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HospitalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FrameworkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvidenceType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ArtifactType = table.Column<int>(type: "int", nullable: false),
                    LifecycleStatus = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CurrentVersion = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    FileUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UploadedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UploadedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Evidences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Evidences_ComplianceTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "ComplianceTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Risks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HospitalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObjectiveElementId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Likelihood = table.Column<int>(type: "int", nullable: false),
                    Impact = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    IdentifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Risks", x => x.Id);
                    table.CheckConstraint("CK_Risk_ImpactRange", "[Impact] >= 1 AND [Impact] <= 5");
                    table.CheckConstraint("CK_Risk_LikelihoodRange", "[Likelihood] >= 1 AND [Likelihood] <= 5");
                    table.CheckConstraint("CK_Risk_ObjectiveElementOrTaskRequired", "([ObjectiveElementId] IS NOT NULL AND [TaskId] IS NULL) OR ([ObjectiveElementId] IS NULL AND [TaskId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Risks_ComplianceTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "ComplianceTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Risks_ObjectiveElements_ObjectiveElementId",
                        column: x => x.ObjectiveElementId,
                        principalTable: "ObjectiveElements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EvidenceVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvidenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvidenceVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvidenceVersions_Evidences_EvidenceId",
                        column: x => x.EvidenceId,
                        principalTable: "Evidences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ObjectiveElementEvidences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObjectiveElementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvidenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LinkedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LinkedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VerifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerificationStatus = table.Column<int>(type: "int", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObjectiveElementEvidences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ObjectiveElementEvidences_Evidences_EvidenceId",
                        column: x => x.EvidenceId,
                        principalTable: "Evidences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ObjectiveElementEvidences_ObjectiveElements_ObjectiveElementId",
                        column: x => x.ObjectiveElementId,
                        principalTable: "ObjectiveElements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActionPlans_ObjectiveElementId",
                table: "ActionPlans",
                column: "ObjectiveElementId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_EntityId",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceAuditFindings_ComplianceJourneyId",
                table: "ComplianceAuditFindings",
                column: "ComplianceJourneyId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceAuditFindings_ComplianceJourneyId_IsResolved",
                table: "ComplianceAuditFindings",
                columns: new[] { "ComplianceJourneyId", "IsResolved" });

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceAuditFindings_DueDate",
                table: "ComplianceAuditFindings",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceAuditFindings_IsResolved",
                table: "ComplianceAuditFindings",
                column: "IsResolved");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceAuditFindings_ObjectiveElementId",
                table: "ComplianceAuditFindings",
                column: "ObjectiveElementId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceChapters_FrameworkId",
                table: "ComplianceChapters",
                column: "FrameworkId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceFrameworks_Name_Version",
                table: "ComplianceFrameworks",
                columns: new[] { "Name", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceJourneyHistories_ChangedAt",
                table: "ComplianceJourneyHistories",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceJourneyHistories_ChangedBy",
                table: "ComplianceJourneyHistories",
                column: "ChangedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceJourneyHistories_ComplianceJourneyId",
                table: "ComplianceJourneyHistories",
                column: "ComplianceJourneyId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceJourneyHistories_ComplianceJourneyId_ChangedAt",
                table: "ComplianceJourneyHistories",
                columns: new[] { "ComplianceJourneyId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceJourneys_ExpiryDate",
                table: "ComplianceJourneys",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceJourneys_FrameworkId",
                table: "ComplianceJourneys",
                column: "FrameworkId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceJourneys_HospitalId",
                table: "ComplianceJourneys",
                column: "HospitalId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceJourneys_HospitalId_FrameworkId",
                table: "ComplianceJourneys",
                columns: new[] { "HospitalId", "FrameworkId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceJourneys_OrganizationId",
                table: "ComplianceJourneys",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceJourneys_RenewalWindowStartDate",
                table: "ComplianceJourneys",
                column: "RenewalWindowStartDate");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceJourneys_Status",
                table: "ComplianceJourneys",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceJourneys_TenantId",
                table: "ComplianceJourneys",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CompliancePrograms_ComplianceFrameworkId",
                table: "CompliancePrograms",
                column: "ComplianceFrameworkId");

            migrationBuilder.CreateIndex(
                name: "IX_CompliancePrograms_HospitalId",
                table: "CompliancePrograms",
                column: "HospitalId");

            migrationBuilder.CreateIndex(
                name: "IX_CompliancePrograms_PlatformId",
                table: "CompliancePrograms",
                column: "PlatformId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceSnapshots_CalculatedAt",
                table: "ComplianceSnapshots",
                column: "CalculatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceSnapshots_HospitalId",
                table: "ComplianceSnapshots",
                column: "HospitalId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceSnapshots_HospitalId_FrameworkId",
                table: "ComplianceSnapshots",
                columns: new[] { "HospitalId", "FrameworkId" });

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceSnapshots_HospitalId_FrameworkId_CalculatedAt",
                table: "ComplianceSnapshots",
                columns: new[] { "HospitalId", "FrameworkId", "CalculatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceSnapshots_TenantId_HospitalId_FrameworkId",
                table: "ComplianceSnapshots",
                columns: new[] { "TenantId", "HospitalId", "FrameworkId" });

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceSnapshots_TenantId_HospitalId_FrameworkId_CalculatedAt",
                table: "ComplianceSnapshots",
                columns: new[] { "TenantId", "HospitalId", "FrameworkId", "CalculatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceTasks_ControlId",
                table: "ComplianceTasks",
                column: "ControlId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceTasks_HospitalId",
                table: "ComplianceTasks",
                column: "HospitalId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceTasks_ObjectiveElementId",
                table: "ComplianceTasks",
                column: "ObjectiveElementId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceTasks_RegulationId",
                table: "ComplianceTasks",
                column: "RegulationId");

            migrationBuilder.CreateIndex(
                name: "IX_Controls_ObjectiveElementId",
                table: "Controls",
                column: "ObjectiveElementId");

            migrationBuilder.CreateIndex(
                name: "IX_Controls_ProgramId",
                table: "Controls",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_Controls_ProgramId_Code",
                table: "Controls",
                columns: new[] { "ProgramId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Evidences_ArtifactType",
                table: "Evidences",
                column: "ArtifactType");

            migrationBuilder.CreateIndex(
                name: "IX_Evidences_Department",
                table: "Evidences",
                column: "Department");

            migrationBuilder.CreateIndex(
                name: "IX_Evidences_HospitalId",
                table: "Evidences",
                column: "HospitalId");

            migrationBuilder.CreateIndex(
                name: "IX_Evidences_HospitalId_FrameworkId",
                table: "Evidences",
                columns: new[] { "HospitalId", "FrameworkId" });

            migrationBuilder.CreateIndex(
                name: "IX_Evidences_TaskId",
                table: "Evidences",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceVersions_EvidenceId_VersionNumber",
                table: "EvidenceVersions",
                columns: new[] { "EvidenceId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FrameworkChapterDistributions_FrameworkId_ChapterId_Version",
                table: "FrameworkChapterDistributions",
                columns: new[] { "FrameworkId", "ChapterId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FrameworkChapterSnapshots_HospitalId_FrameworkId",
                table: "FrameworkChapterSnapshots",
                columns: new[] { "HospitalId", "FrameworkId" });

            migrationBuilder.CreateIndex(
                name: "IX_FrameworkChapterSnapshots_HospitalId_FrameworkId_ChapterId",
                table: "FrameworkChapterSnapshots",
                columns: new[] { "HospitalId", "FrameworkId", "ChapterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FrameworkConfigurations_FrameworkId",
                table: "FrameworkConfigurations",
                column: "FrameworkId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ObjectiveElementEvidences_EvidenceId",
                table: "ObjectiveElementEvidences",
                column: "EvidenceId");

            migrationBuilder.CreateIndex(
                name: "IX_ObjectiveElementEvidences_ObjectiveElementId_EvidenceId",
                table: "ObjectiveElementEvidences",
                columns: new[] { "ObjectiveElementId", "EvidenceId" });

            migrationBuilder.CreateIndex(
                name: "IX_ObjectiveElements_Classification",
                table: "ObjectiveElements",
                column: "Classification");

            migrationBuilder.CreateIndex(
                name: "IX_ObjectiveElements_StandardId_Code",
                table: "ObjectiveElements",
                columns: new[] { "StandardId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ObjectiveElementScores_ObjectiveElementId",
                table: "ObjectiveElementScores",
                column: "ObjectiveElementId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationComplianceOverviewSnapshots_HospitalId",
                table: "OrganizationComplianceOverviewSnapshots",
                column: "HospitalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Outbox_AggregateId",
                table: "OutboxMessages",
                column: "AggregateId");

            migrationBuilder.CreateIndex(
                name: "IX_Outbox_ProcessedOn",
                table: "OutboxMessages",
                column: "ProcessedOn");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformSequences_Prefix",
                table: "PlatformSequences",
                column: "Prefix",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Regulations_ComplianceFrameworkId",
                table: "Regulations",
                column: "ComplianceFrameworkId");

            migrationBuilder.CreateIndex(
                name: "IX_Regulations_HospitalId",
                table: "Regulations",
                column: "HospitalId");

            migrationBuilder.CreateIndex(
                name: "IX_Regulations_HospitalId_ComplianceFrameworkId",
                table: "Regulations",
                columns: new[] { "HospitalId", "ComplianceFrameworkId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Risks_HospitalId",
                table: "Risks",
                column: "HospitalId");

            migrationBuilder.CreateIndex(
                name: "IX_Risks_ObjectiveElementId",
                table: "Risks",
                column: "ObjectiveElementId");

            migrationBuilder.CreateIndex(
                name: "IX_Risks_Status",
                table: "Risks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Risks_TaskId",
                table: "Risks",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Standards_ChapterId_Code",
                table: "Standards",
                columns: new[] { "ChapterId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Standards_ChapterId_Sequence",
                table: "Standards",
                columns: new[] { "ChapterId", "Sequence" });

            migrationBuilder.CreateIndex(
                name: "IX_WizardOnboardingAssessments_HospitalId",
                table: "WizardOnboardingAssessments",
                column: "HospitalId");

            migrationBuilder.CreateIndex(
                name: "IX_WizardOnboardingAssessments_HospitalId_CreatedAt",
                table: "WizardOnboardingAssessments",
                columns: new[] { "HospitalId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WizardOnboardingAssessments_OrganizationId",
                table: "WizardOnboardingAssessments",
                column: "OrganizationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActionPlans");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "ComplianceAuditFindings");

            migrationBuilder.DropTable(
                name: "ComplianceJourneyHistories");

            migrationBuilder.DropTable(
                name: "ComplianceSnapshots");

            migrationBuilder.DropTable(
                name: "EvidenceVersions");

            migrationBuilder.DropTable(
                name: "FrameworkChapterDistributions");

            migrationBuilder.DropTable(
                name: "FrameworkChapterSnapshots");

            migrationBuilder.DropTable(
                name: "FrameworkConfigurations");

            migrationBuilder.DropTable(
                name: "ObjectiveElementEvidences");

            migrationBuilder.DropTable(
                name: "ObjectiveElementScores");

            migrationBuilder.DropTable(
                name: "OrganizationComplianceOverviewSnapshots");

            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "PlatformSequences");

            migrationBuilder.DropTable(
                name: "Risks");

            migrationBuilder.DropTable(
                name: "RuleConfigs");

            migrationBuilder.DropTable(
                name: "WizardOnboardingAssessments");

            migrationBuilder.DropTable(
                name: "ComplianceJourneys");

            migrationBuilder.DropTable(
                name: "Evidences");

            migrationBuilder.DropTable(
                name: "ComplianceTasks");

            migrationBuilder.DropTable(
                name: "Controls");

            migrationBuilder.DropTable(
                name: "Regulations");

            migrationBuilder.DropTable(
                name: "CompliancePrograms");

            migrationBuilder.DropTable(
                name: "ObjectiveElements");

            migrationBuilder.DropTable(
                name: "Standards");

            migrationBuilder.DropTable(
                name: "ComplianceChapters");

            migrationBuilder.DropTable(
                name: "ComplianceFrameworks");
        }
    }
}
