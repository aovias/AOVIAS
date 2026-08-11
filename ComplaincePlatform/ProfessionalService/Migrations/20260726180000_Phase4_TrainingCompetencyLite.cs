using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProfessionalService.Migrations
{
    /// <inheritdoc />
    public partial class Phase4_TrainingCompetencyLite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RelayProcessedOn",
                table: "OutboxMessages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TrainingPrograms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HospitalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ControlledDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RoleCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    RenewalMonths = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingPrograms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrainingSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrainingProgramId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    StartsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Facilitator = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Capacity = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingSessions_TrainingPrograms_TrainingProgramId",
                        column: x => x.TrainingProgramId,
                        principalTable: "TrainingPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrainingAttendances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrainingSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfessionalProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttendanceStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CheckedInAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CertificateUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CertificateIssuedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingAttendances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingAttendances_TrainingSessions_TrainingSessionId",
                        column: x => x.TrainingSessionId,
                        principalTable: "TrainingSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrainingAssessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrainingAttendanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PassMark = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Passed = table.Column<bool>(type: "bit", nullable: true),
                    AssessorName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingAssessments_TrainingAttendances_TrainingAttendanceId",
                        column: x => x.TrainingAttendanceId,
                        principalTable: "TrainingAttendances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingAssessments_TrainingAttendanceId",
                table: "TrainingAssessments",
                column: "TrainingAttendanceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingAttendances_ExpiryDate",
                table: "TrainingAttendances",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingAttendances_TrainingSessionId_ProfessionalProfileId",
                table: "TrainingAttendances",
                columns: new[] { "TrainingSessionId", "ProfessionalProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingPrograms_ControlledDocumentId",
                table: "TrainingPrograms",
                column: "ControlledDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingPrograms_OrganizationId_Code",
                table: "TrainingPrograms",
                columns: new[] { "OrganizationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingPrograms_RoleCode_DepartmentId",
                table: "TrainingPrograms",
                columns: new[] { "RoleCode", "DepartmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingSessions_TrainingProgramId_StartsAt",
                table: "TrainingSessions",
                columns: new[] { "TrainingProgramId", "StartsAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrainingAssessments");

            migrationBuilder.DropTable(
                name: "TrainingAttendances");

            migrationBuilder.DropTable(
                name: "TrainingSessions");

            migrationBuilder.DropTable(
                name: "TrainingPrograms");

            migrationBuilder.DropColumn(
                name: "RelayProcessedOn",
                table: "OutboxMessages");
        }
    }
}
