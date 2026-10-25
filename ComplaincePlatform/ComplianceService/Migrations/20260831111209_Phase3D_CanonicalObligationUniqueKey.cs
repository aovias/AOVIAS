using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceService.Migrations
{
    /// <inheritdoc />
    public partial class Phase3D_CanonicalObligationUniqueKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = N'UX_OrganizationComplianceObligations_OrgOE'
                      AND object_id = OBJECT_ID(N'dbo.OrganizationComplianceObligations'))
                    DROP INDEX UX_OrganizationComplianceObligations_OrgOE ON dbo.OrganizationComplianceObligations;

                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = N'UX_OrganizationComplianceObligations_HospitalFrameworkOE'
                      AND object_id = OBJECT_ID(N'dbo.OrganizationComplianceObligations'))
                    CREATE UNIQUE INDEX UX_OrganizationComplianceObligations_HospitalFrameworkOE
                    ON dbo.OrganizationComplianceObligations (HospitalId, FrameworkId, ObjectiveElementId);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = N'UX_OrganizationComplianceObligations_HospitalFrameworkOE'
                      AND object_id = OBJECT_ID(N'dbo.OrganizationComplianceObligations'))
                    DROP INDEX UX_OrganizationComplianceObligations_HospitalFrameworkOE ON dbo.OrganizationComplianceObligations;

                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = N'UX_OrganizationComplianceObligations_OrgOE'
                      AND object_id = OBJECT_ID(N'dbo.OrganizationComplianceObligations'))
                    CREATE UNIQUE INDEX UX_OrganizationComplianceObligations_OrgOE
                    ON dbo.OrganizationComplianceObligations (OrganizationId, ObjectiveElementId);
                """);
        }
    }
}
