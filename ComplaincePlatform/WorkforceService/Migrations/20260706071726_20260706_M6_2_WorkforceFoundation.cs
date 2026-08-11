using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkforceService.Migrations
{
    /// <inheritdoc />
    public partial class _20260706_M6_2_WorkforceFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmploymentHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmploymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PreviousValueJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValueJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmploymentHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Employments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmploymentNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HospitalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrganizationMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EmploymentTypeValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EmploymentStatusValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PersonSearchName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    JoiningDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConfirmationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProbationEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SeparationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NoticePeriodDays = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employments", x => x.Id);
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
                name: "PlatformSequences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Prefix = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CurrentValue = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformSequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Positions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HospitalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PositionCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PositionName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PositionLevelValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GradeValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BandValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsClinical = table.Column<bool>(type: "bit", nullable: false),
                    IsAdministrative = table.Column<bool>(type: "bit", nullable: false),
                    IsLeadership = table.Column<bool>(type: "bit", nullable: false),
                    ApprovalLevel = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Positions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkforceAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmploymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EventCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkforceAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkforceLookupCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkforceLookupCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkforceTimeline",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmploymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OldValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkforceTimeline", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AvailabilityProfiles",
                columns: table => new
                {
                    EmploymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkingDaysJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WorkingHoursJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShiftPlaceholder = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LeaveStatus = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvailabilityProfiles", x => x.EmploymentId);
                    table.ForeignKey(
                        name: "FK_AvailabilityProfiles_Employments_EmploymentId",
                        column: x => x.EmploymentId,
                        principalTable: "Employments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DepartmentAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmploymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MasterDepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    AllocationPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsTemporary = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartmentAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DepartmentAssignments_Employments_EmploymentId",
                        column: x => x.EmploymentId,
                        principalTable: "Employments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmploymentContracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmploymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContractTypeValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContractNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RenewalDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AttachmentUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmploymentContracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmploymentContracts_Employments_EmploymentId",
                        column: x => x.EmploymentId,
                        principalTable: "Employments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Promotions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmploymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousPositionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NewPositionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReasonValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SalaryPlaceholder = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GradeValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BandValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Promotions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Promotions_Employments_EmploymentId",
                        column: x => x.EmploymentId,
                        principalTable: "Employments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReportingRelationships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmploymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ManagerEmploymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RelationshipTypeValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportingRelationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportingRelationships_Employments_EmploymentId",
                        column: x => x.EmploymentId,
                        principalTable: "Employments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Transfers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmploymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransferKind = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetOrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetHospitalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetMasterDepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetPositionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReasonValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transfers_Employments_EmploymentId",
                        column: x => x.EmploymentId,
                        principalTable: "Employments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PositionAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmploymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PositionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PositionAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PositionAssignments_Employments_EmploymentId",
                        column: x => x.EmploymentId,
                        principalTable: "Employments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PositionAssignments_Positions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkforceLookupValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkforceLookupValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkforceLookupValues_WorkforceLookupCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "WorkforceLookupCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentAssignments_EmploymentId",
                table: "DepartmentAssignments",
                column: "EmploymentId");

            migrationBuilder.CreateIndex(
                name: "IX_EmploymentContracts_EmploymentId",
                table: "EmploymentContracts",
                column: "EmploymentId");

            migrationBuilder.CreateIndex(
                name: "IX_EmploymentHistories_EmploymentId_OccurredAt",
                table: "EmploymentHistories",
                columns: new[] { "EmploymentId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Employments_EmploymentNumber",
                table: "Employments",
                column: "EmploymentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employments_PersonId_OrganizationId_HospitalId",
                table: "Employments",
                columns: new[] { "PersonId", "OrganizationId", "HospitalId" });

            migrationBuilder.CreateIndex(
                name: "IX_Employments_PersonSearchName",
                table: "Employments",
                column: "PersonSearchName");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedOn",
                table: "OutboxMessages",
                column: "ProcessedOn");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformSequences_Prefix",
                table: "PlatformSequences",
                column: "Prefix",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PositionAssignments_EmploymentId_EffectiveFrom",
                table: "PositionAssignments",
                columns: new[] { "EmploymentId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_PositionAssignments_PositionId",
                table: "PositionAssignments",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_OrganizationId_PositionCode",
                table: "Positions",
                columns: new[] { "OrganizationId", "PositionCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_EmploymentId",
                table: "Promotions",
                column: "EmploymentId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportingRelationships_EmploymentId",
                table: "ReportingRelationships",
                column: "EmploymentId");

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_EmploymentId",
                table: "Transfers",
                column: "EmploymentId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkforceAuditLogs_EmploymentId",
                table: "WorkforceAuditLogs",
                column: "EmploymentId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkforceLookupCategories_Code",
                table: "WorkforceLookupCategories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkforceLookupValues_CategoryId_Code",
                table: "WorkforceLookupValues",
                columns: new[] { "CategoryId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkforceTimeline_EmploymentId_OccurredAt",
                table: "WorkforceTimeline",
                columns: new[] { "EmploymentId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AvailabilityProfiles");

            migrationBuilder.DropTable(
                name: "DepartmentAssignments");

            migrationBuilder.DropTable(
                name: "EmploymentContracts");

            migrationBuilder.DropTable(
                name: "EmploymentHistories");

            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "PlatformSequences");

            migrationBuilder.DropTable(
                name: "PositionAssignments");

            migrationBuilder.DropTable(
                name: "Promotions");

            migrationBuilder.DropTable(
                name: "ReportingRelationships");

            migrationBuilder.DropTable(
                name: "Transfers");

            migrationBuilder.DropTable(
                name: "WorkforceAuditLogs");

            migrationBuilder.DropTable(
                name: "WorkforceLookupValues");

            migrationBuilder.DropTable(
                name: "WorkforceTimeline");

            migrationBuilder.DropTable(
                name: "Positions");

            migrationBuilder.DropTable(
                name: "Employments");

            migrationBuilder.DropTable(
                name: "WorkforceLookupCategories");
        }
    }
}
