/*
  sync-compliance-catalog-nabh-full-6e.sql

  Replicates Chapters, Standards, and Objective Elements from
  CP00000002 (NABH Entry Level) → CP00000001 (NABH Full 6E).

  Check-first-then-insert:
    1. Both frameworks must exist
    2. Source must have chapters (and reports source counts)
    3. Target must be completely empty (0 chapters, 0 standards, 0 OEs)
    4. Only then inserts cloned rows with new IDs and fresh CP######## PlatformIds

  Idempotent: safe to re-run; skips when target already has catalog data.

  Usage:
    sqlcmd -S localhost\SQLEXPRESS -d Compliance -E -i sync-compliance-catalog-nabh-full-6e.sql
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @SourcePlatformId NVARCHAR(10) = N'CP00000002';
DECLARE @TargetPlatformId NVARCHAR(10) = N'CP00000001';
DECLARE @SourceFrameworkId UNIQUEIDENTIFIER;
DECLARE @TargetFrameworkId UNIQUEIDENTIFIER;
DECLARE @SourceChapters INT;
DECLARE @SourceStandards INT;
DECLARE @SourceOes INT;
DECLARE @TargetChapters INT;
DECLARE @TargetStandards INT;
DECLARE @TargetOes INT;
DECLARE @MaxCpNumber INT;

SELECT @SourceFrameworkId = Id
FROM dbo.ComplianceFrameworks
WHERE PlatformId = @SourcePlatformId;

SELECT @TargetFrameworkId = Id
FROM dbo.ComplianceFrameworks
WHERE PlatformId = @TargetPlatformId;

IF @SourceFrameworkId IS NULL
BEGIN
    RAISERROR(N'CHECK FAILED: Source framework %s not found.', 16, 1, @SourcePlatformId);
    RETURN;
END;

IF @TargetFrameworkId IS NULL
BEGIN
    RAISERROR(N'CHECK FAILED: Target framework %s not found.', 16, 1, @TargetPlatformId);
    RETURN;
END;

SELECT @SourceChapters = COUNT(*)
FROM dbo.ComplianceChapters
WHERE FrameworkId = @SourceFrameworkId;

SELECT @SourceStandards = COUNT(*)
FROM dbo.ComplianceStandards cs
INNER JOIN dbo.ComplianceChapters cc ON cc.Id = cs.ChapterId
WHERE cc.FrameworkId = @SourceFrameworkId;

SELECT @SourceOes = COUNT(*)
FROM dbo.ObjectiveElements oe
INNER JOIN dbo.ComplianceStandards cs ON cs.Id = oe.ComplianceStandardId
INNER JOIN dbo.ComplianceChapters cc ON cc.Id = cs.ChapterId
WHERE cc.FrameworkId = @SourceFrameworkId;

IF @SourceChapters = 0
BEGIN
    RAISERROR(N'CHECK FAILED: Source %s has no chapters to replicate.', 16, 1, @SourcePlatformId);
    RETURN;
END;

SELECT @TargetChapters = COUNT(*)
FROM dbo.ComplianceChapters
WHERE FrameworkId = @TargetFrameworkId;

SELECT @TargetStandards = COUNT(*)
FROM dbo.ComplianceStandards cs
INNER JOIN dbo.ComplianceChapters cc ON cc.Id = cs.ChapterId
WHERE cc.FrameworkId = @TargetFrameworkId;

SELECT @TargetOes = COUNT(*)
FROM dbo.ObjectiveElements oe
INNER JOIN dbo.ComplianceStandards cs ON cs.Id = oe.ComplianceStandardId
INNER JOIN dbo.ComplianceChapters cc ON cc.Id = cs.ChapterId
WHERE cc.FrameworkId = @TargetFrameworkId;

PRINT CONCAT(
    N'Source ', @SourcePlatformId, N': ',
    @SourceChapters, N' chapters, ',
    @SourceStandards, N' standards, ',
    @SourceOes, N' OEs.');

PRINT CONCAT(
    N'Target ', @TargetPlatformId, N': ',
    @TargetChapters, N' chapters, ',
    @TargetStandards, N' standards, ',
    @TargetOes, N' OEs.');

IF @TargetChapters > 0 OR @TargetStandards > 0 OR @TargetOes > 0
BEGIN
    PRINT N'SKIP: Target already has catalog data — no insert performed.';
    RETURN;
END;

SELECT @MaxCpNumber = MAX(n.Num)
FROM (
    SELECT TRY_CAST(SUBSTRING(PlatformId, 3, 8) AS INT) AS Num
    FROM dbo.ComplianceChapters
    WHERE PlatformId LIKE N'CP%'
    UNION ALL
    SELECT TRY_CAST(SUBSTRING(PlatformId, 3, 8) AS INT)
    FROM dbo.ComplianceStandards
    WHERE PlatformId LIKE N'CP%'
    UNION ALL
    SELECT TRY_CAST(SUBSTRING(PlatformId, 3, 8) AS INT)
    FROM dbo.ObjectiveElements
    WHERE PlatformId LIKE N'CP%'
) n
WHERE n.Num IS NOT NULL;

IF @MaxCpNumber IS NULL SET @MaxCpNumber = 0;

BEGIN TRY
BEGIN TRAN;

CREATE TABLE #ChapterMap (
    OldId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    NewId UNIQUEIDENTIFIER NOT NULL,
    PlatformId NVARCHAR(10) NOT NULL
);

CREATE TABLE #StandardMap (
    OldId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    NewId UNIQUEIDENTIFIER NOT NULL,
    PlatformId NVARCHAR(10) NOT NULL
);

INSERT INTO #ChapterMap (OldId, NewId, PlatformId)
SELECT
    cc.Id,
    NEWID(),
    CONCAT(N'CP', RIGHT(CONCAT(N'00000000', CAST(@MaxCpNumber + ROW_NUMBER() OVER (ORDER BY cc.Sequence, cc.Code) AS NVARCHAR(8))), 8))
FROM dbo.ComplianceChapters cc
WHERE cc.FrameworkId = @SourceFrameworkId;

INSERT INTO dbo.ComplianceChapters (Id, PlatformId, FrameworkId, Code, Name, Sequence, Weight, CreatedAt)
SELECT
    m.NewId,
    m.PlatformId,
    @TargetFrameworkId,
    cc.Code,
    cc.Name,
    cc.Sequence,
    cc.Weight,
    SYSUTCDATETIME()
FROM dbo.ComplianceChapters cc
INNER JOIN #ChapterMap m ON m.OldId = cc.Id;

INSERT INTO #StandardMap (OldId, NewId, PlatformId)
SELECT
    cs.Id,
    NEWID(),
    CONCAT(
        N'CP',
        RIGHT(
            CONCAT(
                N'00000000',
                CAST(@MaxCpNumber + @SourceChapters + ROW_NUMBER() OVER (ORDER BY cc.Sequence, cs.Sequence, cs.Code) AS NVARCHAR(8))),
            8))
FROM dbo.ComplianceStandards cs
INNER JOIN dbo.ComplianceChapters cc ON cc.Id = cs.ChapterId
WHERE cc.FrameworkId = @SourceFrameworkId;

INSERT INTO dbo.ComplianceStandards (Id, PlatformId, ChapterId, Code, Name, Sequence, Weight, CreatedAt)
SELECT
    sm.NewId,
    sm.PlatformId,
    cm.NewId,
    cs.Code,
    cs.Name,
    cs.Sequence,
    cs.Weight,
    SYSUTCDATETIME()
FROM dbo.ComplianceStandards cs
INNER JOIN #StandardMap sm ON sm.OldId = cs.Id
INNER JOIN dbo.ComplianceChapters srcCc ON srcCc.Id = cs.ChapterId
INNER JOIN #ChapterMap cm ON cm.OldId = srcCc.Id;

INSERT INTO dbo.ObjectiveElements (
    Id,
    PlatformId,
    ComplianceStandardId,
    Code,
    Title,
    Description,
    Classification,
    IsMandatory,
    IsScorable,
    IsApplicable,
    IsDocumentationRequired,
    Tags,
    Department,
    ResponsibleRole,
    Frequency,
    EvidenceCategory,
    Weight,
    CreatedAt)
SELECT
    NEWID(),
    CONCAT(
        N'CP',
        RIGHT(
            CONCAT(
                N'00000000',
                CAST(@MaxCpNumber + @SourceChapters + @SourceStandards + ROW_NUMBER() OVER (ORDER BY cc.Sequence, cs.Sequence, oe.Code) AS NVARCHAR(8))),
            8)),
    sm.NewId,
    oe.Code,
    oe.Title,
    oe.Description,
    oe.Classification,
    oe.IsMandatory,
    oe.IsScorable,
    oe.IsApplicable,
    oe.IsDocumentationRequired,
    oe.Tags,
    oe.Department,
    oe.ResponsibleRole,
    oe.Frequency,
    oe.EvidenceCategory,
    oe.Weight,
    SYSUTCDATETIME()
FROM dbo.ObjectiveElements oe
INNER JOIN dbo.ComplianceStandards cs ON cs.Id = oe.ComplianceStandardId
INNER JOIN #StandardMap sm ON sm.OldId = cs.Id
INNER JOIN dbo.ComplianceChapters cc ON cc.Id = cs.ChapterId
WHERE cc.FrameworkId = @SourceFrameworkId;

COMMIT TRAN;

SELECT @TargetChapters = COUNT(*) FROM dbo.ComplianceChapters WHERE FrameworkId = @TargetFrameworkId;
SELECT @TargetStandards = COUNT(*)
FROM dbo.ComplianceStandards cs
INNER JOIN dbo.ComplianceChapters cc ON cc.Id = cs.ChapterId
WHERE cc.FrameworkId = @TargetFrameworkId;
SELECT @TargetOes = COUNT(*)
FROM dbo.ObjectiveElements oe
INNER JOIN dbo.ComplianceStandards cs ON cs.Id = oe.ComplianceStandardId
INNER JOIN dbo.ComplianceChapters cc ON cc.Id = cs.ChapterId
WHERE cc.FrameworkId = @TargetFrameworkId;

PRINT CONCAT(
    N'INSERT OK: Target ', @TargetPlatformId, N' now has ',
    @TargetChapters, N' chapters, ',
    @TargetStandards, N' standards, ',
    @TargetOes, N' OEs.');
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    THROW;
END CATCH;
