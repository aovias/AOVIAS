-- Direct fix for duplicate frameworks
USE Compliance;
GO

DECLARE @KeepFrameworkId UNIQUEIDENTIFIER;
DECLARE @DuplicateFrameworkId UNIQUEIDENTIFIER = '376A2DFE-B19A-4F62-16C4-08DE693B0B00';

-- Get the keep ID (oldest NABH Entry Level)
SELECT TOP 1 @KeepFrameworkId = Id
FROM ComplianceFrameworks
WHERE Name = 'NABH' AND Version = 'Entry Level'
ORDER BY CreatedAt ASC;

PRINT 'Keep Framework ID: ' + CAST(@KeepFrameworkId AS VARCHAR(36));
PRINT 'Fixing duplicate: ' + CAST(@DuplicateFrameworkId AS VARCHAR(36));
PRINT '';

-- Update all chapters referencing this duplicate
PRINT 'Updating ComplianceChapters...';
UPDATE ComplianceChapters
SET FrameworkId = @KeepFrameworkId
WHERE FrameworkId = @DuplicateFrameworkId;

PRINT '   Updated ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' chapters';
PRINT '';

-- Update all other duplicate frameworks' chapters
PRINT 'Updating chapters for all other duplicate frameworks...';
UPDATE cc
SET cc.FrameworkId = @KeepFrameworkId
FROM ComplianceChapters cc
INNER JOIN ComplianceFrameworks cf ON cc.FrameworkId = cf.Id
WHERE cf.Name = 'NABH' 
  AND cf.Version = 'Entry Level'
  AND cf.Id != @KeepFrameworkId;

PRINT '   Updated ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' additional chapters';
PRINT '';

-- Verify no chapters still reference duplicates
PRINT 'Verifying cleanup...';
SELECT COUNT(*) as RemainingChapters
FROM ComplianceChapters cc
INNER JOIN ComplianceFrameworks cf ON cc.FrameworkId = cf.Id
WHERE cf.Name = 'NABH' 
  AND cf.Version = 'Entry Level'
  AND cf.Id != @KeepFrameworkId;

IF @@ROWCOUNT = 0 OR (SELECT COUNT(*) FROM ComplianceChapters cc INNER JOIN ComplianceFrameworks cf ON cc.FrameworkId = cf.Id WHERE cf.Name = 'NABH' AND cf.Version = 'Entry Level' AND cf.Id != @KeepFrameworkId) = 0
    PRINT '   ✓ All chapters reassigned';
ELSE
    PRINT '   ✗ Some chapters still reference duplicates';
PRINT '';

-- Now delete duplicate frameworks
PRINT 'Deleting duplicate frameworks...';
DELETE FROM ComplianceFrameworks
WHERE Name = 'NABH' 
  AND Version = 'Entry Level'
  AND Id != @KeepFrameworkId;

PRINT '   Deleted ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' duplicate frameworks';
PRINT '';

-- Final verification
PRINT 'Final verification...';
SELECT 
    Name, 
    Version, 
    COUNT(*) as Count
FROM ComplianceFrameworks
WHERE Name = 'NABH' AND Version = 'Entry Level'
GROUP BY Name, Version;

IF (SELECT COUNT(*) FROM ComplianceFrameworks WHERE Name = 'NABH' AND Version = 'Entry Level') = 1
    PRINT '   ✓ Only one NABH Entry Level framework remains';
ELSE
    PRINT '   ✗ WARNING: Multiple frameworks still exist!';
PRINT '';

PRINT '========================================';
PRINT 'CLEANUP COMPLETE';
PRINT '========================================';
GO
