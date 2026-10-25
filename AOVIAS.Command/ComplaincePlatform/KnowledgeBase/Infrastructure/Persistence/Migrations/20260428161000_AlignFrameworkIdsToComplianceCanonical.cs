using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace KnowledgeBase.Infrastructure.Persistence.Migrations;

[DbContext(typeof(KnowledgeBaseContext))]
[Migration("20260428161000_AlignFrameworkIdsToComplianceCanonical")]
public partial class AlignFrameworkIdsToComplianceCanonical : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF DB_ID(N'Compliance') IS NULL
                THROW 50100, 'Compliance database not found. Framework ID alignment aborted.', 1;

            IF OBJECT_ID(N'[Compliance].[dbo].[ComplianceFrameworks]', N'U') IS NULL
                THROW 50101, 'ComplianceFrameworks table not found in Compliance database. Framework ID alignment aborted.', 1;

            IF OBJECT_ID(N'[dbo].[Frameworks]', N'U') IS NULL
                THROW 50102, 'KnowledgeBase Frameworks table not found. Framework ID alignment aborted.', 1;

            DECLARE @Mapping TABLE (
                OldFrameworkId UNIQUEIDENTIFIER NOT NULL,
                NewFrameworkId UNIQUEIDENTIFIER NOT NULL
            );

            INSERT INTO @Mapping (OldFrameworkId, NewFrameworkId)
            SELECT
                kb.Id AS OldFrameworkId,
                cf.Id AS NewFrameworkId
            FROM [dbo].[Frameworks] kb
            INNER JOIN [Compliance].[dbo].[ComplianceFrameworks] cf
                ON LOWER(LTRIM(RTRIM(kb.Name))) = LOWER(LTRIM(RTRIM(cf.Name)))
               AND LOWER(LTRIM(RTRIM(kb.Version))) = LOWER(LTRIM(RTRIM(cf.Version)));

            IF EXISTS (
                SELECT 1
                FROM [dbo].[Frameworks] kb
                LEFT JOIN @Mapping m ON m.OldFrameworkId = kb.Id
                WHERE m.OldFrameworkId IS NULL
            )
                THROW 50103, 'At least one KnowledgeBase framework could not be mapped to Compliance canonical frameworks.', 1;

            IF EXISTS (
                SELECT m.NewFrameworkId
                FROM @Mapping m
                GROUP BY m.NewFrameworkId
                HAVING COUNT(*) > 1
            )
                THROW 50104, 'Multiple KnowledgeBase frameworks map to the same Compliance framework ID.', 1;

            IF EXISTS (
                SELECT 1
                FROM @Mapping m
                INNER JOIN [dbo].[Frameworks] f ON f.Id = m.NewFrameworkId
                WHERE m.OldFrameworkId <> m.NewFrameworkId
            )
                THROW 50105, 'Collision: target canonical framework ID already exists in KnowledgeBase.Frameworks.', 1;

            INSERT INTO [dbo].[Frameworks] (Id, Code, Name, Version, Description, IsActive)
            SELECT
                m.NewFrameworkId,
                f.Code,
                f.Name,
                f.Version,
                f.Description,
                f.IsActive
            FROM @Mapping m
            INNER JOIN [dbo].[Frameworks] f ON f.Id = m.OldFrameworkId
            WHERE m.OldFrameworkId <> m.NewFrameworkId;

            UPDATE n
            SET n.FrameworkId = m.NewFrameworkId
            FROM [dbo].[ComplianceNodes] n
            INNER JOIN @Mapping m ON m.OldFrameworkId = n.FrameworkId
            WHERE m.OldFrameworkId <> m.NewFrameworkId;

            UPDATE fc
            SET fc.FrameworkId = m.NewFrameworkId
            FROM [dbo].[FrameworkClassifications] fc
            INNER JOIN @Mapping m ON m.OldFrameworkId = fc.FrameworkId
            WHERE m.OldFrameworkId <> m.NewFrameworkId;

            UPDATE sc
            SET sc.FrameworkId = m.NewFrameworkId
            FROM [dbo].[ScoringConfigs] sc
            INNER JOIN @Mapping m ON m.OldFrameworkId = sc.FrameworkId
            WHERE m.OldFrameworkId <> m.NewFrameworkId;

            UPDATE a
            SET a.FrameworkId = m.NewFrameworkId
            FROM [dbo].[Assessments] a
            INNER JOIN @Mapping m ON m.OldFrameworkId = a.FrameworkId
            WHERE m.OldFrameworkId <> m.NewFrameworkId;

            UPDATE ai
            SET ai.FrameworkId = m.NewFrameworkId
            FROM [dbo].[AIInteractions] ai
            INNER JOIN @Mapping m ON m.OldFrameworkId = ai.FrameworkId
            WHERE m.OldFrameworkId <> m.NewFrameworkId;

            UPDATE d
            SET d.FrameworkId = m.NewFrameworkId
            FROM [dbo].[DecisionLogs] d
            INNER JOIN @Mapping m ON m.OldFrameworkId = d.FrameworkId
            WHERE m.OldFrameworkId <> m.NewFrameworkId;

            DELETE f
            FROM [dbo].[Frameworks] f
            INNER JOIN @Mapping m ON m.OldFrameworkId = f.Id
            WHERE m.OldFrameworkId <> m.NewFrameworkId;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException("Framework ID canonicalization migration is irreversible.");
    }
}
