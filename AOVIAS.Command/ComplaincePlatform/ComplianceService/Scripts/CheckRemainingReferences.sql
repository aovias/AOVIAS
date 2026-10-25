-- Check what's still referencing duplicate frameworks
USE Compliance;
GO

-- Find duplicate framework IDs
DECLARE @DuplicateFrameworkIds TABLE (Id UNIQUEIDENTIFIER);
DECLARE @KeepFrameworkId UNIQUEIDENTIFIER;

-- Get the keep ID (oldest NABH Entry Level)
SELECT TOP 1 @KeepFrameworkId = Id
FROM ComplianceFrameworks
WHERE Name = 'NABH' AND Version = 'Entry Level'
ORDER BY CreatedAt ASC;

-- Get all duplicate IDs (excluding the keep one)
INSERT INTO @DuplicateFrameworkIds (Id)
SELECT Id
FROM ComplianceFrameworks
WHERE Name = 'NABH' AND Version = 'Entry Level'
  AND Id != @KeepFrameworkId;

PRINT 'Keep Framework ID: ' + CAST(@KeepFrameworkId AS VARCHAR(36));
PRINT 'Duplicate Framework IDs:';
SELECT Id FROM @DuplicateFrameworkIds;
PRINT '';

-- Check chapters
PRINT 'Chapters still referencing duplicate frameworks:';
SELECT 
    cc.Id as ChapterId,
    cc.Code as ChapterCode,
    cc.FrameworkId as CurrentFrameworkId
FROM ComplianceChapters cc
INNER JOIN @DuplicateFrameworkIds df ON cc.FrameworkId = df.Id;

PRINT 'Total: ' + CAST(@@ROWCOUNT AS VARCHAR(10));
PRINT '';

-- Check regulations
PRINT 'Regulations still referencing duplicate frameworks:';
SELECT 
    r.Id as RegulationId,
    r.ComplianceFrameworkId as CurrentFrameworkId,
    r.Status,
    r.StartDate
FROM Regulations r
INNER JOIN @DuplicateFrameworkIds df ON r.ComplianceFrameworkId = df.Id;

PRINT 'Total: ' + CAST(@@ROWCOUNT AS VARCHAR(10));
PRINT '';

-- Objective elements (via ComplianceStandards -> Chapters)
PRINT 'ObjectiveElements (via ComplianceStandards / Chapters) still referencing duplicate frameworks:';
SELECT 
    oe.Id as ObjectiveElementId,
    oe.Code,
    cch.FrameworkId as ChapterFrameworkId
FROM ObjectiveElements oe
INNER JOIN ComplianceStandards s ON oe.ComplianceStandardId = s.Id
INNER JOIN ComplianceChapters cch ON s.ChapterId = cch.Id
INNER JOIN @DuplicateFrameworkIds df ON cch.FrameworkId = df.Id;

PRINT 'Total: ' + CAST(@@ROWCOUNT AS VARCHAR(10));
GO
