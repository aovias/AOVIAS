using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OrganizationService.Infrastructure.Persistence;

#nullable disable

namespace OrganizationService.Infrastructure.Migrations;

[DbContext(typeof(OrganizationDbContext))]
[Migration("20260731220000_Phase6_LicensingConsolidation")]
public partial class Phase6_LicensingConsolidation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "LifecycleStatus",
            table: "SubscriptionPlans",
            type: "nvarchar(20)",
            maxLength: 20,
            nullable: false,
            defaultValue: "Published");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "LifecycleStatus",
            table: "SubscriptionPlans");
    }
}
