using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIOrchestratorService.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DraftArtifacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DraftType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SourceEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Explainability = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GeneratedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DraftArtifacts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DraftArtifacts_DraftType",
                table: "DraftArtifacts",
                column: "DraftType");

            migrationBuilder.CreateIndex(
                name: "IX_DraftArtifacts_SourceEntityId",
                table: "DraftArtifacts",
                column: "SourceEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_DraftArtifacts_SourceEntityId_DraftType",
                table: "DraftArtifacts",
                columns: new[] { "SourceEntityId", "DraftType" });

            migrationBuilder.CreateIndex(
                name: "IX_DraftArtifacts_Status",
                table: "DraftArtifacts",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DraftArtifacts");
        }
    }
}
