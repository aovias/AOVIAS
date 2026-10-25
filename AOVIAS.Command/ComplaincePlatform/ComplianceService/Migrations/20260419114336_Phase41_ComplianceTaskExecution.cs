using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceService.Migrations
{
    /// <inheritdoc />
    public partial class Phase41_ComplianceTaskExecution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "DueDate",
                table: "ComplianceTasks",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "ComplianceTasks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DepartmentName",
                table: "ComplianceTasks",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "ComplianceTasks",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProgramId",
                table: "ComplianceTasks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatusNew",
                table: "ComplianceTasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE ComplianceTasks SET StatusNew = CASE
                  WHEN LOWER(LTRIM(RTRIM(Status))) IN (N'completed', N'done', N'closed', N'verified') THEN 4
                  WHEN LOWER(LTRIM(RTRIM(Status))) IN (N'inprogress', N'in progress') THEN 1
                  WHEN LOWER(LTRIM(RTRIM(Status))) IN (N'onhold', N'on hold') THEN 2
                  WHEN LOWER(LTRIM(RTRIM(Status))) = N'blocked' THEN 3
                  ELSE 0 END
                """);

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ComplianceTasks");

            migrationBuilder.RenameColumn(
                name: "StatusNew",
                table: "ComplianceTasks",
                newName: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceTasks_DepartmentId",
                table: "ComplianceTasks",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceTasks_ProgramId",
                table: "ComplianceTasks",
                column: "ProgramId");

            migrationBuilder.AddForeignKey(
                name: "FK_ComplianceTasks_CompliancePrograms_ProgramId",
                table: "ComplianceTasks",
                column: "ProgramId",
                principalTable: "CompliancePrograms",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ComplianceTasks_CompliancePrograms_ProgramId",
                table: "ComplianceTasks");

            migrationBuilder.DropIndex(
                name: "IX_ComplianceTasks_DepartmentId",
                table: "ComplianceTasks");

            migrationBuilder.DropIndex(
                name: "IX_ComplianceTasks_ProgramId",
                table: "ComplianceTasks");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "ComplianceTasks");

            migrationBuilder.DropColumn(
                name: "DepartmentName",
                table: "ComplianceTasks");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "ComplianceTasks");

            migrationBuilder.DropColumn(
                name: "ProgramId",
                table: "ComplianceTasks");

            migrationBuilder.AddColumn<string>(
                name: "StatusLegacy",
                table: "ComplianceTasks",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.Sql("""
                UPDATE ComplianceTasks SET StatusLegacy = CASE Status
                  WHEN 4 THEN N'Completed'
                  WHEN 1 THEN N'InProgress'
                  WHEN 2 THEN N'OnHold'
                  WHEN 3 THEN N'Blocked'
                  ELSE N'Pending' END
                """);

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ComplianceTasks");

            migrationBuilder.RenameColumn(
                name: "StatusLegacy",
                table: "ComplianceTasks",
                newName: "Status");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DueDate",
                table: "ComplianceTasks",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }
    }
}
