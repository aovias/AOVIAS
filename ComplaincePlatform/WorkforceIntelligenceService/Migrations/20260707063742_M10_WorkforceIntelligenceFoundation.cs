using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkforceIntelligenceService.Migrations
{
    /// <inheritdoc />
    public partial class M10_WorkforceIntelligenceFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnalyticsCaches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CacheKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalyticsCaches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AvailabilityAnalyticsSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HospitalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WorkingCount = table.Column<int>(type: "int", nullable: false),
                    OnLeaveCount = table.Column<int>(type: "int", nullable: false),
                    VacationCount = table.Column<int>(type: "int", nullable: false),
                    UnavailableCount = table.Column<int>(type: "int", nullable: false),
                    InTrainingCount = table.Column<int>(type: "int", nullable: false),
                    InMeetingCount = table.Column<int>(type: "int", nullable: false),
                    CoveragePercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AsOfUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvailabilityAnalyticsSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CertificationMatrixEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmploymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CertificationName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DaysUntilExpiry = table.Column<int>(type: "int", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProfessionalCategory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AsOfUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificationMatrixEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CommitteeAnalyticsSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommitteeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommitteeName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MemberCount = table.Column<int>(type: "int", nullable: false),
                    AttendanceRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MeetingFrequencyPerMonth = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DecisionCompletionRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ActionClosureRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MemberParticipationJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AsOfUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommitteeAnalyticsSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompetencyMatrixEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmploymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompetencyName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CurrentLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetLevel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Gap = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProfessionalCategory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssessmentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AsOfUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetencyMatrixEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExecutiveMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HospitalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AsOfUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Headcount = table.Column<int>(type: "int", nullable: false),
                    OpenVacancies = table.Column<int>(type: "int", nullable: false),
                    TrainingCompliancePercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CredentialRiskCount = table.Column<int>(type: "int", nullable: false),
                    CriticalSkillGaps = table.Column<int>(type: "int", nullable: false),
                    CommitteeActionBacklog = table.Column<int>(type: "int", nullable: false),
                    AvailabilityCoveragePercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutiveMetrics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ForecastSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ForecastType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    GeneratedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InputSnapshotIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParametersJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResultsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForecastSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LicenseMatrixEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmploymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LicenseName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DaysUntilExpiry = table.Column<int>(type: "int", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProfessionalCategory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AsOfUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LicenseMatrixEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationAnalyticsSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentSizeJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReportingDepth = table.Column<int>(type: "int", nullable: false),
                    LeadershipDistributionJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SpanOfControlAvg = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CrossFunctionalAssignments = table.Column<int>(type: "int", nullable: false),
                    Vacancies = table.Column<int>(type: "int", nullable: false),
                    TransfersInPeriod = table.Column<int>(type: "int", nullable: false),
                    PromotionsInPeriod = table.Column<int>(type: "int", nullable: false),
                    AsOfUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationAnalyticsSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProcessedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectionCheckpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LastProcessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastMessageId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectionCheckpoints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecommendationCaches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecommendationCaches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResponsibilityMatrixEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResponsibilityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResponsibilityName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApproverId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CommitteeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CoverageStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EscalationChainId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AsOfUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResponsibilityMatrixEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SkillMatrixEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HospitalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmploymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkillName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CurrentLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetLevel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Gap = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsCritical = table.Column<bool>(type: "bit", nullable: false),
                    CoverageScore = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AsOfUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillMatrixEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrainingMatrixEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HospitalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmploymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompliancePercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AsOfUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingMatrixEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkforceRecommendations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecommendationType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Explanation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceReferencesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Confidence = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ModelVersion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Priority = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneratedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkforceRecommendations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkforceSearchIndexes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmploymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SearchText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SkillsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CertificationsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DepartmentName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProfessionalCategory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LanguagesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    YearsOfExperience = table.Column<int>(type: "int", nullable: true),
                    IndexedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkforceSearchIndexes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkforceSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HospitalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AsOfUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalHeadcount = table.Column<int>(type: "int", nullable: false),
                    TotalFte = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ByDepartmentJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ByProfessionJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ByEmploymentTypeJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ByHospitalJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AgeDistributionJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExperienceDistributionJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QualificationDistributionJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GrowthRate = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AttritionRate = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    RetentionRate = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkforceSnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsCaches_CacheKey",
                table: "AnalyticsCaches",
                column: "CacheKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilityAnalyticsSnapshots_OrganizationId_AsOfUtc",
                table: "AvailabilityAnalyticsSnapshots",
                columns: new[] { "OrganizationId", "AsOfUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CertificationMatrixEntries_OrganizationId_Status",
                table: "CertificationMatrixEntries",
                columns: new[] { "OrganizationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CommitteeAnalyticsSnapshots_OrganizationId_CommitteeId",
                table: "CommitteeAnalyticsSnapshots",
                columns: new[] { "OrganizationId", "CommitteeId" });

            migrationBuilder.CreateIndex(
                name: "IX_CompetencyMatrixEntries_OrganizationId_AsOfUtc",
                table: "CompetencyMatrixEntries",
                columns: new[] { "OrganizationId", "AsOfUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutiveMetrics_OrganizationId_AsOfUtc",
                table: "ExecutiveMetrics",
                columns: new[] { "OrganizationId", "AsOfUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ForecastSnapshots_OrganizationId_ForecastType_Version",
                table: "ForecastSnapshots",
                columns: new[] { "OrganizationId", "ForecastType", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_LicenseMatrixEntries_OrganizationId_Status",
                table: "LicenseMatrixEntries",
                columns: new[] { "OrganizationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationAnalyticsSnapshots_OrganizationId_AsOfUtc",
                table: "OrganizationAnalyticsSnapshots",
                columns: new[] { "OrganizationId", "AsOfUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedOn",
                table: "OutboxMessages",
                column: "ProcessedOn");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectionCheckpoints_EventType",
                table: "ProjectionCheckpoints",
                column: "EventType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationCaches_OrganizationId",
                table: "RecommendationCaches",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityMatrixEntries_OrganizationId_ResponsibilityId",
                table: "ResponsibilityMatrixEntries",
                columns: new[] { "OrganizationId", "ResponsibilityId" });

            migrationBuilder.CreateIndex(
                name: "IX_SkillMatrixEntries_OrganizationId_AsOfUtc",
                table: "SkillMatrixEntries",
                columns: new[] { "OrganizationId", "AsOfUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingMatrixEntries_OrganizationId_AsOfUtc",
                table: "TrainingMatrixEntries",
                columns: new[] { "OrganizationId", "AsOfUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkforceRecommendations_OrganizationId_IsActive_GeneratedAtUtc",
                table: "WorkforceRecommendations",
                columns: new[] { "OrganizationId", "IsActive", "GeneratedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkforceSearchIndexes_OrganizationId",
                table: "WorkforceSearchIndexes",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkforceSearchIndexes_PersonId",
                table: "WorkforceSearchIndexes",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkforceSnapshots_OrganizationId_AsOfUtc",
                table: "WorkforceSnapshots",
                columns: new[] { "OrganizationId", "AsOfUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalyticsCaches");

            migrationBuilder.DropTable(
                name: "AvailabilityAnalyticsSnapshots");

            migrationBuilder.DropTable(
                name: "CertificationMatrixEntries");

            migrationBuilder.DropTable(
                name: "CommitteeAnalyticsSnapshots");

            migrationBuilder.DropTable(
                name: "CompetencyMatrixEntries");

            migrationBuilder.DropTable(
                name: "ExecutiveMetrics");

            migrationBuilder.DropTable(
                name: "ForecastSnapshots");

            migrationBuilder.DropTable(
                name: "LicenseMatrixEntries");

            migrationBuilder.DropTable(
                name: "OrganizationAnalyticsSnapshots");

            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "ProjectionCheckpoints");

            migrationBuilder.DropTable(
                name: "RecommendationCaches");

            migrationBuilder.DropTable(
                name: "ResponsibilityMatrixEntries");

            migrationBuilder.DropTable(
                name: "SkillMatrixEntries");

            migrationBuilder.DropTable(
                name: "TrainingMatrixEntries");

            migrationBuilder.DropTable(
                name: "WorkforceRecommendations");

            migrationBuilder.DropTable(
                name: "WorkforceSearchIndexes");

            migrationBuilder.DropTable(
                name: "WorkforceSnapshots");
        }
    }
}
