using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceService.Migrations
{
    /// <inheritdoc />
    public partial class Sprint14_ExternalAuditorWorkspace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AuditSessionId",
                table: "Findings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuditSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HospitalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FrameworkId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SessionNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    AuditType = table.Column<int>(type: "int", nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    LeadAuditorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LeadAuditorName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    AuditorTeamJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PlannedStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PlannedEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OverallResult = table.Column<int>(type: "int", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuditSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsAuditorAuthor = table.Column<bool>(type: "bit", nullable: false),
                    ParentCommentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditComments_AuditComments_ParentCommentId",
                        column: x => x.ParentCommentId,
                        principalTable: "AuditComments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuditComments_AuditSessions_AuditSessionId",
                        column: x => x.AuditSessionId,
                        principalTable: "AuditSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditObservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuditSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObservationNumber = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ObjectiveElementId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChapterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Department = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditObservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditObservations_AuditSessions_AuditSessionId",
                        column: x => x.AuditSessionId,
                        principalTable: "AuditSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuditObservations_ObjectiveElements_ObjectiveElementId",
                        column: x => x.ObjectiveElementId,
                        principalTable: "ObjectiveElements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Findings_AuditSessionId",
                table: "Findings",
                column: "AuditSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditComments_AuditSessionId",
                table: "AuditComments",
                column: "AuditSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditComments_ParentCommentId",
                table: "AuditComments",
                column: "ParentCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditObservations_AuditSessionId",
                table: "AuditObservations",
                column: "AuditSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditObservations_ObjectiveElementId",
                table: "AuditObservations",
                column: "ObjectiveElementId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditSessions_Status",
                table: "AuditSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AuditSessions_TenantId_SessionNumber",
                table: "AuditSessions",
                columns: new[] { "TenantId", "SessionNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditComments");

            migrationBuilder.DropTable(
                name: "AuditObservations");

            migrationBuilder.DropTable(
                name: "AuditSessions");

            migrationBuilder.DropIndex(
                name: "IX_Findings_AuditSessionId",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "AuditSessionId",
                table: "Findings");
        }
    }
}
