using System;
using AIOrchestratorService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIOrchestratorService.Migrations;

/// <inheritdoc />
[DbContext(typeof(AIOrchestratorDbContext))]
[Migration("20260726140000_Phase5_KnowledgeEngine")]
public partial class Phase5_KnowledgeEngine : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "KnowledgeEmbeddingChunks",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                HospitalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                FrameworkId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                SourceType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                SourceId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                DocumentId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                Version = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                Title = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                EmbeddingJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                EmbeddingModel = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                Dimension = table.Column<int>(type: "int", nullable: false),
                IndexedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                SourceUpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_KnowledgeEmbeddingChunks", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "AceInsights",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                HospitalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                FrameworkId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                Category = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                Title = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                Summary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Reasoning = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Confidence = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                Priority = table.Column<int>(type: "int", nullable: false),
                Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                CitationsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                SuggestedActionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                AcknowledgedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                AcknowledgedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AceInsights", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_KnowledgeEmbeddingChunks_HospitalId_FrameworkId",
            table: "KnowledgeEmbeddingChunks",
            columns: new[] { "HospitalId", "FrameworkId" });

        migrationBuilder.CreateIndex(
            name: "IX_KnowledgeEmbeddingChunks_HospitalId_SourceType_SourceId",
            table: "KnowledgeEmbeddingChunks",
            columns: new[] { "HospitalId", "SourceType", "SourceId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_AceInsights_CreatedAtUtc",
            table: "AceInsights",
            column: "CreatedAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_AceInsights_HospitalId_Status_Priority",
            table: "AceInsights",
            columns: new[] { "HospitalId", "Status", "Priority" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AceInsights");
        migrationBuilder.DropTable(name: "KnowledgeEmbeddingChunks");
    }
}
