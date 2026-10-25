-- Cleanup Script for Duplicate ComplianceFrameworks
-- This script removes duplicate frameworks, keeping the oldest one (by CreatedAt)
-- and reassigning all related entities to the kept framework

USE Compliance;
GO

PRINT '========================================';
PRINT 'CLEANUP DUPLICATE FRAMEWORKS';
PRINT '========================================';
PRINT '';

-- Step 1: Identify duplicates
PRINT 'Step 1: Identifying duplicate frameworks...';
SELECT 
    Name, 
    Version, 
    COUNT(*) as DuplicateCount,
    STRING_AGG(CAST(Id AS VARCHAR(36)) + ' (Created: ' + CONVERT(VARCHAR(19), CreatedAt, 120) + ')', ', ') as FrameworkDetails
FROM ComplianceFrameworks
GROUP BY Name, Version
HAVING COUNT(*) > 1;
PRINT '';

-- Step 2: For each duplicate group, keep the oldest and mark others for deletion
PRINT 'Step 2: Creating temporary table to track which frameworks to keep...';
-- Create temp table to store framework mappings (duplicate -> keep)
CREATE TABLE #FrameworkMapping (
    DuplicateId UNIQUEIDENTIFIER,
    KeepId UNIQUEIDENTIFIER,
    Name NVARCHAR(MAX),
    Version NVARCHAR(MAX)
);

-- Populate mapping: for each duplicate group, keep the one with earliest CreatedAt
INSERT INTO #FrameworkMapping (DuplicateId, KeepId, Name, Version)
SELECT 
    cf1.Id as DuplicateId,
    (SELECT TOP 1 cf2.Id 
     FROM ComplianceFrameworks cf2 
     WHERE cf2.Name = cf1.Name 
       AND cf2.Version = cf1.Version 
     ORDER BY cf2.CreatedAt ASC) as KeepId,
    cf1.Name,
    cf1.Version
FROM ComplianceFrameworks cf1
WHERE EXISTS (
    SELECT 1 
    FROM ComplianceFrameworks cf2 
    WHERE cf2.Name = cf1.Name 
      AND cf2.Version = cf1.Version 
      AND cf2.Id != cf1.Id
)
AND cf1.Id NOT IN (
    -- Exclude the ones we're keeping (oldest in each group)
    SELECT TOP 1 cf3.Id
    FROM ComplianceFrameworks cf3
    WHERE cf3.Name = cf1.Name 
      AND cf3.Version = cf1.Version
    ORDER BY cf3.CreatedAt ASC
);

PRINT '   Found ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' duplicate frameworks to remove';
PRINT '';

-- Step 3: Reassign ComplianceChapters
PRINT 'Step 3: Reassigning ComplianceChapters...';
UPDATE cc
SET cc.FrameworkId = fm.KeepId
FROM ComplianceChapters cc
INNER JOIN #FrameworkMapping fm ON cc.FrameworkId = fm.DuplicateId;

DECLARE @ChaptersUpdated INT = @@ROWCOUNT;
PRINT '   Updated ' + CAST(@ChaptersUpdated AS VARCHAR(10)) + ' ComplianceChapters';

-- Verify all chapters have been reassigned
IF EXISTS (
    SELECT 1 
    FROM ComplianceChapters cc
    INNER JOIN #FrameworkMapping fm ON cc.FrameworkId = fm.DuplicateId
)
BEGIN
    PRINT '   ⚠ WARNING: Some chapters still reference duplicate frameworks!';
END
PRINT '';

-- Step 3b: NABH — ComplianceStandards and ObjectiveElements hang off ComplianceChapters; chapter reassignment in Step 3 is sufficient.
PRINT 'Step 3b: Skipping legacy ComplianceRequirements check (table removed in NABH reset).';
PRINT '   ✓ Framework graph is Chapter → ComplianceStandard → ObjectiveElement only.';
PRINT '';

-- Step 4: Reassign Regulations
PRINT 'Step 4: Reassigning Regulations...';
UPDATE r
SET r.ComplianceFrameworkId = fm.KeepId
FROM Regulations r
INNER JOIN #FrameworkMapping fm ON r.ComplianceFrameworkId = fm.DuplicateId;

PRINT '   Updated ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' Regulations';
PRINT '';

-- Step 5: Reassign CompliancePrograms (if they exist and are linked)
PRINT 'Step 5: Checking CompliancePrograms...';
-- Note: This column might not exist yet if migration hasn't run
-- We'll check if the column exists first
IF EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'CompliancePrograms' 
      AND COLUMN_NAME = 'ComplianceFrameworkId'
)
BEGIN
    UPDATE cp
    SET cp.ComplianceFrameworkId = fm.KeepId
    FROM CompliancePrograms cp
    INNER JOIN #FrameworkMapping fm ON cp.ComplianceFrameworkId = fm.DuplicateId;
    
    PRINT '   Updated ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' CompliancePrograms';
END
ELSE
BEGIN
    PRINT '   CompliancePrograms.ComplianceFrameworkId column does not exist yet (will be added by migration)';
END
PRINT '';

-- Step 6: Verify no remaining references before deletion
PRINT 'Step 6: Verifying no remaining references to duplicate frameworks...';

-- Check for any remaining chapters
IF EXISTS (
    SELECT 1 
    FROM ComplianceChapters cc
    INNER JOIN #FrameworkMapping fm ON cc.FrameworkId = fm.DuplicateId
)
BEGIN
    PRINT '   ✗ ERROR: Some ComplianceChapters still reference duplicate frameworks!';
    SELECT 
        cc.Id as ChapterId,
        cc.Code as ChapterCode,
        fm.DuplicateId as DuplicateFrameworkId,
        fm.KeepId as ShouldBeFrameworkId
    FROM ComplianceChapters cc
    INNER JOIN #FrameworkMapping fm ON cc.FrameworkId = fm.DuplicateId;
    PRINT '   Cannot proceed with deletion. Please investigate.';
    DROP TABLE #FrameworkMapping;
    RETURN;
END

-- Check for any remaining regulations
IF EXISTS (
    SELECT 1 
    FROM Regulations r
    INNER JOIN #FrameworkMapping fm ON r.ComplianceFrameworkId = fm.DuplicateId
)
BEGIN
    PRINT '   ✗ ERROR: Some Regulations still reference duplicate frameworks!';
    SELECT 
        r.Id as RegulationId,
        fm.DuplicateId as DuplicateFrameworkId,
        fm.KeepId as ShouldBeFrameworkId
    FROM Regulations r
    INNER JOIN #FrameworkMapping fm ON r.ComplianceFrameworkId = fm.DuplicateId;
    PRINT '   Cannot proceed with deletion. Please investigate.';
    DROP TABLE #FrameworkMapping;
    RETURN;
END

PRINT '   ✓ No remaining references found';
PRINT '';

-- Step 7: Delete duplicate frameworks
PRINT 'Step 7: Deleting duplicate frameworks...';
DELETE FROM ComplianceFrameworks
WHERE Id IN (SELECT DuplicateId FROM #FrameworkMapping);

PRINT '   Deleted ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' duplicate frameworks';
PRINT '';

-- Step 8: Verify cleanup
PRINT 'Step 8: Verifying cleanup...';
SELECT 
    Name, 
    Version, 
    COUNT(*) as RemainingCount
FROM ComplianceFrameworks
GROUP BY Name, Version
HAVING COUNT(*) > 1;

IF @@ROWCOUNT = 0
    PRINT '   ✓ No duplicates remain';
ELSE
    PRINT '   ✗ WARNING: Duplicates still exist!';
PRINT '';

-- Cleanup temp table
DROP TABLE #FrameworkMapping;

PRINT '========================================';
PRINT 'CLEANUP COMPLETE';
PRINT '========================================';
PRINT '';
PRINT 'You can now safely run the migration.';
GO
