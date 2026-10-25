using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceService.Migrations
{
    /// <inheritdoc />
    public partial class Sprint17_ClinicalIndicators : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClinicalIndicators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HospitalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IndicatorCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Department = table.Column<int>(type: "int", nullable: false),
                    MeasurementUnit = table.Column<int>(type: "int", nullable: false),
                    CalculationMethod = table.Column<int>(type: "int", nullable: false),
                    NumeratorDefinition = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DenominatorDefinition = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FormulaExpression = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CurrentNumerator = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    CurrentDenominator = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    CurrentValue = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    TargetValue = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    GreenThreshold = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    YellowThreshold = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    RedThreshold = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    CriticalThreshold = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    LowerIsBetter = table.Column<bool>(type: "bit", nullable: false),
                    Trend = table.Column<int>(type: "int", nullable: false),
                    Frequency = table.Column<int>(type: "int", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OwnerName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    FrameworkMapping = table.Column<int>(type: "int", nullable: false),
                    FrameworkReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ObjectiveElementId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RiskLevel = table.Column<int>(type: "int", nullable: false),
                    ThresholdStatus = table.Column<int>(type: "int", nullable: false),
                    DataSource = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastCalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RelatedDocumentationJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RelatedIncidentCategoriesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicalIndicators", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IndicatorAuditTrails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IndicatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Detail = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActorName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndicatorAuditTrails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IndicatorAuditTrails_ClinicalIndicators_IndicatorId",
                        column: x => x.IndicatorId,
                        principalTable: "ClinicalIndicators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IndicatorMeasurements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IndicatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Frequency = table.Column<int>(type: "int", nullable: false),
                    Numerator = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Denominator = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Value = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndicatorMeasurements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IndicatorMeasurements_ClinicalIndicators_IndicatorId",
                        column: x => x.IndicatorId,
                        principalTable: "ClinicalIndicators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IndicatorThresholdHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IndicatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetValue = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    GreenThreshold = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    YellowThreshold = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    RedThreshold = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    CriticalThreshold = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    LowerIsBetter = table.Column<bool>(type: "bit", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChangedByName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndicatorThresholdHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IndicatorThresholdHistories_ClinicalIndicators_IndicatorId",
                        column: x => x.IndicatorId,
                        principalTable: "ClinicalIndicators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalIndicators_Category",
                table: "ClinicalIndicators",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalIndicators_Department",
                table: "ClinicalIndicators",
                column: "Department");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalIndicators_TenantId_IndicatorCode",
                table: "ClinicalIndicators",
                columns: new[] { "TenantId", "IndicatorCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalIndicators_ThresholdStatus",
                table: "ClinicalIndicators",
                column: "ThresholdStatus");

            migrationBuilder.CreateIndex(
                name: "IX_IndicatorAuditTrails_IndicatorId_OccurredAt",
                table: "IndicatorAuditTrails",
                columns: new[] { "IndicatorId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_IndicatorMeasurements_IndicatorId_PeriodEnd",
                table: "IndicatorMeasurements",
                columns: new[] { "IndicatorId", "PeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_IndicatorThresholdHistories_IndicatorId_EffectiveFrom",
                table: "IndicatorThresholdHistories",
                columns: new[] { "IndicatorId", "EffectiveFrom" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IndicatorAuditTrails");

            migrationBuilder.DropTable(
                name: "IndicatorMeasurements");

            migrationBuilder.DropTable(
                name: "IndicatorThresholdHistories");

            migrationBuilder.DropTable(
                name: "ClinicalIndicators");
        }
    }
}
