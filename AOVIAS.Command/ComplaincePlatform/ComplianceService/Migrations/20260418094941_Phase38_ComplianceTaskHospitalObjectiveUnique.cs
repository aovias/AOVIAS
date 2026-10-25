using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceService.Migrations
{
    /// <inheritdoc />
    public partial class Phase38_ComplianceTaskHospitalObjectiveUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Deduplicate tasks per (HospitalId, ObjectiveElementId): keep earliest CreatedAt (then smallest Id).
            migrationBuilder.Sql(
                """
                ;WITH ranked AS (
                  SELECT Id, HospitalId, ObjectiveElementId,
                    ROW_NUMBER() OVER (PARTITION BY HospitalId, ObjectiveElementId ORDER BY CreatedAt ASC, Id ASC) AS rn
                  FROM ComplianceTasks
                )
                UPDATE e
                SET TaskId = k.Id
                FROM Evidences e
                INNER JOIN ranked r ON r.Id = e.TaskId AND r.rn > 1
                INNER JOIN ranked k ON k.HospitalId = r.HospitalId AND k.ObjectiveElementId = r.ObjectiveElementId AND k.rn = 1;

                ;WITH ranked AS (
                  SELECT Id, HospitalId, ObjectiveElementId,
                    ROW_NUMBER() OVER (PARTITION BY HospitalId, ObjectiveElementId ORDER BY CreatedAt ASC, Id ASC) AS rn
                  FROM ComplianceTasks
                )
                UPDATE rsk
                SET TaskId = k.Id
                FROM Risks rsk
                INNER JOIN ranked r ON r.Id = rsk.TaskId AND r.rn > 1
                INNER JOIN ranked k ON k.HospitalId = r.HospitalId AND k.ObjectiveElementId = r.ObjectiveElementId AND k.rn = 1
                WHERE rsk.TaskId IS NOT NULL;

                ;WITH ranked AS (
                  SELECT Id, HospitalId, ObjectiveElementId,
                    ROW_NUMBER() OVER (PARTITION BY HospitalId, ObjectiveElementId ORDER BY CreatedAt ASC, Id ASC) AS rn
                  FROM ComplianceTasks
                )
                UPDATE ap
                SET LinkedTaskId = k.Id
                FROM ActionPlans ap
                INNER JOIN ranked r ON r.Id = ap.LinkedTaskId AND r.rn > 1
                INNER JOIN ranked k ON k.HospitalId = r.HospitalId AND k.ObjectiveElementId = r.ObjectiveElementId AND k.rn = 1
                WHERE ap.LinkedTaskId IS NOT NULL;

                ;WITH ranked AS (
                  SELECT Id,
                    ROW_NUMBER() OVER (PARTITION BY HospitalId, ObjectiveElementId ORDER BY CreatedAt ASC, Id ASC) AS rn
                  FROM ComplianceTasks
                )
                DELETE FROM ComplianceTasks WHERE Id IN (SELECT Id FROM ranked WHERE rn > 1);
                """);

            // Backfill empty assignees from latest evidence uploader, else system placeholder (non-whitespace).
            migrationBuilder.Sql(
                """
                UPDATE ct
                SET AssignedToUserId = COALESCE(
                  (SELECT TOP (1) e.UploadedBy FROM Evidences e
                   WHERE e.TaskId = ct.Id AND NULLIF(LTRIM(RTRIM(e.UploadedBy)), N'') IS NOT NULL
                   ORDER BY e.UploadedOn DESC),
                  N'00000000-0000-0000-0000-000000000000')
                FROM ComplianceTasks ct
                WHERE NULLIF(LTRIM(RTRIM(COALESCE(ct.AssignedToUserId, N''))), N'') IS NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceTasks_HospitalId_ObjectiveElementId",
                table: "ComplianceTasks",
                columns: new[] { "HospitalId", "ObjectiveElementId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ComplianceTasks_HospitalId_ObjectiveElementId",
                table: "ComplianceTasks");
        }
    }
}
