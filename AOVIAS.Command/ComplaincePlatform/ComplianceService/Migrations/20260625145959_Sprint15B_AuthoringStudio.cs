using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceService.Migrations
{
    /// <inheritdoc />
    public partial class Sprint15B_AuthoringStudio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthoringSchemaKey",
                table: "DocumentVersions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuthoringSchemaKey",
                table: "DocumentTemplates",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SectionsSchemaJson",
                table: "DocumentTemplates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DocumentAuthoringCheckpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ControlledDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentAuthoringCheckpoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentAuthoringCheckpoints_ControlledDocuments_ControlledDocumentId",
                        column: x => x.ControlledDocumentId,
                        principalTable: "ControlledDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentAuthoringComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ControlledDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BlockId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CommentType = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AuthorName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    MentionedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentAuthoringComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentAuthoringComments_ControlledDocuments_ControlledDocumentId",
                        column: x => x.ControlledDocumentId,
                        principalTable: "ControlledDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentCrossReferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ControlledDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BlockId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ReferenceType = table.Column<int>(type: "int", nullable: false),
                    TargetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetLabel = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    TargetRoute = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentCrossReferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentCrossReferences_ControlledDocuments_ControlledDocumentId",
                        column: x => x.ControlledDocumentId,
                        principalTable: "ControlledDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentSections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SectionKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    IsCollapsed = table.Column<bool>(type: "bit", nullable: false),
                    BlocksJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastEditedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentSections_DocumentVersions_DocumentVersionId",
                        column: x => x.DocumentVersionId,
                        principalTable: "DocumentVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAuthoringCheckpoints_ControlledDocumentId",
                table: "DocumentAuthoringCheckpoints",
                column: "ControlledDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAuthoringComments_ControlledDocumentId",
                table: "DocumentAuthoringComments",
                column: "ControlledDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentCrossReferences_ControlledDocumentId",
                table: "DocumentCrossReferences",
                column: "ControlledDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentSections_DocumentVersionId",
                table: "DocumentSections",
                column: "DocumentVersionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentAuthoringCheckpoints");

            migrationBuilder.DropTable(
                name: "DocumentAuthoringComments");

            migrationBuilder.DropTable(
                name: "DocumentCrossReferences");

            migrationBuilder.DropTable(
                name: "DocumentSections");

            migrationBuilder.DropColumn(
                name: "AuthoringSchemaKey",
                table: "DocumentVersions");

            migrationBuilder.DropColumn(
                name: "AuthoringSchemaKey",
                table: "DocumentTemplates");

            migrationBuilder.DropColumn(
                name: "SectionsSchemaJson",
                table: "DocumentTemplates");
        }
    }
}
