using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseDirectoryService.Migrations
{
    /// <inheritdoc />
    public partial class M11_EnterpriseDirectoryFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DirectoryFacets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FacetType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FacetValue = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false),
                    AsOfUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DirectoryFacets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DirectoryIndex",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmploymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HospitalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PreferredName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PlatformId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmployeeNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProfessionalNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmailNormalized = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNormalized = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DepartmentName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HospitalName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Designation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PositionTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmploymentStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmploymentType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProfessionalCategory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ManagerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SearchBlob = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneticKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SkillsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CertificationsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CommitteesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResponsibilitiesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TagsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VisibilityFlags = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastIndexedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DirectoryIndex", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DirectoryRankings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RulesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DirectoryRankings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DirectorySnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    RowCount = table.Column<int>(type: "int", nullable: false),
                    AsOfUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DirectorySnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DirectorySuggestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CacheKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DirectorySuggestions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DirectorySynonyms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Synonym = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DirectorySynonyms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProcessedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RelayProcessedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectionCheckpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LastProcessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastMessageId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectionCheckpoints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SavedSearches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QueryJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Visibility = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsScheduled = table.Column<bool>(type: "bit", nullable: false),
                    IsAiGenerated = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedSearches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SearchHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SearchTerms = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FiltersJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResultCount = table.Column<int>(type: "int", nullable: false),
                    ExecutionMs = table.Column<long>(type: "bigint", nullable: false),
                    SearchedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SearchPreferences",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefaultOrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PinnedResultsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NaturalLanguageEnabled = table.Column<bool>(type: "bit", nullable: false),
                    PreferencesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchPreferences", x => x.UserId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DirectoryFacets_OrganizationId_FacetType_FacetValue",
                table: "DirectoryFacets",
                columns: new[] { "OrganizationId", "FacetType", "FacetValue" });

            migrationBuilder.CreateIndex(
                name: "IX_DirectoryIndex_EmploymentId",
                table: "DirectoryIndex",
                column: "EmploymentId");

            migrationBuilder.CreateIndex(
                name: "IX_DirectoryIndex_OrganizationId_IsActive",
                table: "DirectoryIndex",
                columns: new[] { "OrganizationId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_DirectoryIndex_OrganizationId_PersonId",
                table: "DirectoryIndex",
                columns: new[] { "OrganizationId", "PersonId" });

            migrationBuilder.CreateIndex(
                name: "IX_DirectorySuggestions_CacheKey",
                table: "DirectorySuggestions",
                column: "CacheKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DirectorySynonyms_Token",
                table: "DirectorySynonyms",
                column: "Token");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectionCheckpoints_EventType",
                table: "ProjectionCheckpoints",
                column: "EventType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SavedSearches_OwnerUserId_OrganizationId",
                table: "SavedSearches",
                columns: new[] { "OwnerUserId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_SearchHistories_UserId_SearchedAtUtc",
                table: "SearchHistories",
                columns: new[] { "UserId", "SearchedAtUtc" });

            migrationBuilder.Sql("""
                IF FULLTEXTSERVICEPROPERTY('IsFullTextInstalled') = 1
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = 'PramanDirectoryCatalog')
                        CREATE FULLTEXT CATALOG PramanDirectoryCatalog AS DEFAULT;
                    IF NOT EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('DirectoryIndex'))
                        CREATE FULLTEXT INDEX ON DirectoryIndex(SearchBlob) KEY INDEX PK_DirectoryIndex ON PramanDirectoryCatalog;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('DirectoryIndex'))
                    DROP FULLTEXT INDEX ON DirectoryIndex;
                IF EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = 'PramanDirectoryCatalog')
                    DROP FULLTEXT CATALOG PramanDirectoryCatalog;
                """);

            migrationBuilder.DropTable(
                name: "DirectoryFacets");

            migrationBuilder.DropTable(
                name: "DirectoryIndex");

            migrationBuilder.DropTable(
                name: "DirectoryRankings");

            migrationBuilder.DropTable(
                name: "DirectorySnapshots");

            migrationBuilder.DropTable(
                name: "DirectorySuggestions");

            migrationBuilder.DropTable(
                name: "DirectorySynonyms");

            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "ProjectionCheckpoints");

            migrationBuilder.DropTable(
                name: "SavedSearches");

            migrationBuilder.DropTable(
                name: "SearchHistories");

            migrationBuilder.DropTable(
                name: "SearchPreferences");
        }
    }
}
