-- Verify migration was applied correctly
USE Compliance;
GO

PRINT '========================================';
PRINT 'MIGRATION VERIFICATION';
PRINT '========================================';
PRINT '';

-- 1. Check unique constraint on ComplianceFrameworks
PRINT '1. Checking ComplianceFrameworks unique constraint...';
SELECT 
    i.name as IndexName,
    i.is_unique as IsUnique,
    STRING_AGG(c.name, ', ') WITHIN GROUP (ORDER BY ic.key_ordinal) as Columns
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
INNER JOIN sys.tables t ON i.object_id = t.object_id
WHERE t.name = 'ComplianceFrameworks'
  AND i.name = 'IX_ComplianceFrameworks_Name_Version'
GROUP BY i.name, i.is_unique;

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ComplianceFrameworks_Name_Version' AND object_id = OBJECT_ID('ComplianceFrameworks') AND is_unique = 1)
    PRINT '   ✓ Unique constraint exists';
ELSE
    PRINT '   ✗ Unique constraint missing';
PRINT '';

-- 2. Check CompliancePrograms.ComplianceFrameworkId
PRINT '2. Checking CompliancePrograms.ComplianceFrameworkId...';
IF EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('CompliancePrograms') 
      AND name = 'ComplianceFrameworkId'
      AND is_nullable = 0
)
    PRINT '   ✓ Column exists and is NOT NULL';
ELSE
    PRINT '   ✗ Column missing or nullable';
PRINT '';

-- 3. Check foreign keys
PRINT '3. Checking foreign keys...';
SELECT 
    fk.name as ForeignKeyName,
    OBJECT_NAME(fk.parent_object_id) as TableName,
    OBJECT_NAME(fk.referenced_object_id) as ReferencedTable
FROM sys.foreign_keys fk
WHERE fk.name IN (
    'FK_CompliancePrograms_ComplianceFrameworks_ComplianceFrameworkId',
    'FK_Regulations_ComplianceFrameworks_ComplianceFrameworkId',
    'FK_ComplianceChapters_ComplianceFrameworks_FrameworkId'
)
ORDER BY fk.name;

IF (SELECT COUNT(*) FROM sys.foreign_keys WHERE name IN ('FK_CompliancePrograms_ComplianceFrameworks_ComplianceFrameworkId', 'FK_Regulations_ComplianceFrameworks_ComplianceFrameworkId', 'FK_ComplianceChapters_ComplianceFrameworks_FrameworkId')) = 3
    PRINT '   ✓ All foreign keys exist';
ELSE
    PRINT '   ✗ Some foreign keys missing';
PRINT '';

-- 4. Check for duplicate frameworks (should be 0)
PRINT '4. Checking for duplicate frameworks...';
SELECT 
    Name, 
    Version, 
    COUNT(*) as Count
FROM ComplianceFrameworks
GROUP BY Name, Version
HAVING COUNT(*) > 1;

IF @@ROWCOUNT = 0
    PRINT '   ✓ No duplicate frameworks';
ELSE
    PRINT '   ✗ Duplicates found!';
PRINT '';

-- 5. Check CompliancePrograms have FrameworkId
PRINT '5. Checking CompliancePrograms have FrameworkId...';
SELECT 
    COUNT(*) as TotalPrograms,
    COUNT(ComplianceFrameworkId) as ProgramsWithFrameworkId,
    COUNT(*) - COUNT(ComplianceFrameworkId) as ProgramsWithoutFrameworkId
FROM CompliancePrograms;

DECLARE @TotalPrograms INT = (SELECT COUNT(*) FROM CompliancePrograms);
DECLARE @ProgramsWithFramework INT = (SELECT COUNT(ComplianceFrameworkId) FROM CompliancePrograms);
PRINT '   Total Programs: ' + CAST(@TotalPrograms AS VARCHAR(10));
PRINT '   With FrameworkId: ' + CAST(@ProgramsWithFramework AS VARCHAR(10));
PRINT '';

PRINT '========================================';
PRINT 'VERIFICATION COMPLETE';
PRINT '========================================';
GO
