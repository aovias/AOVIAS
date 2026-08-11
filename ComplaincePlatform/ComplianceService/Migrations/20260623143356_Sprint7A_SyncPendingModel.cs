using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceService.Migrations
{
    /// <inheritdoc />
    public partial class Sprint7A_SyncPendingModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RootCauseCategory",
                table: "ComplianceDeficiencies",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Classification",
                table: "Findings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FindingNumber",
                table: "Findings",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "PromotedDeficiencyId",
                table: "Findings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FindingAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FindingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    FileUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    AttachmentType = table.Column<int>(type: "int", nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FindingAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FindingAttachments_Findings_FindingId",
                        column: x => x.FindingId,
                        principalTable: "Findings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FindingComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FindingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FindingComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FindingComments_Findings_FindingId",
                        column: x => x.FindingId,
                        principalTable: "Findings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FindingAttachments_FindingId",
                table: "FindingAttachments",
                column: "FindingId");

            migrationBuilder.CreateIndex(
                name: "IX_FindingComments_FindingId",
                table: "FindingComments",
                column: "FindingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FindingAttachments");

            migrationBuilder.DropTable(
                name: "FindingComments");

            migrationBuilder.DropColumn(
                name: "RootCauseCategory",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "Classification",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "FindingNumber",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "PromotedDeficiencyId",
                table: "Findings");
        }
    }
}
