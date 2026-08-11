-- Check existing indexes
USE Compliance;
GO

PRINT 'Checking for existing indexes...';
PRINT '';

-- Check ComplianceChapters indexes
PRINT 'ComplianceChapters indexes:';
SELECT 
    i.name as IndexName,
    i.type_desc as IndexType,
    STRING_AGG(c.name, ', ') WITHIN GROUP (ORDER BY ic.key_ordinal) as Columns
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
INNER JOIN sys.tables t ON i.object_id = t.object_id
WHERE t.name = 'ComplianceChapters'
  AND i.name LIKE '%FrameworkId%'
GROUP BY i.name, i.type_desc;

-- Check if IX_ComplianceChapters_FrameworkId exists
IF EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE name = 'IX_ComplianceChapters_FrameworkId' 
      AND object_id = OBJECT_ID('ComplianceChapters')
)
    PRINT '   ✓ IX_ComplianceChapters_FrameworkId exists';
ELSE
    PRINT '   ✗ IX_ComplianceChapters_FrameworkId does not exist';
PRINT '';

-- Check other indexes from migration
PRINT 'Other migration-related indexes:';
SELECT 
    t.name as TableName,
    i.name as IndexName
FROM sys.indexes i
INNER JOIN sys.tables t ON i.object_id = t.object_id
WHERE i.name IN (
    'IX_ComplianceFrameworks_Name_Version',
    'IX_ObjectiveElements_ComplianceStandardId_Code',
    'IX_Controls_ProgramId_Code',
    'IX_CompliancePrograms_ComplianceFrameworkId',
    'IX_CompliancePrograms_OrganizationId',
    'IX_Regulations_ComplianceFrameworkId',
    'IX_Regulations_OrganizationId',
    'IX_ObjectiveElementScores_JourneyId_ObjectiveElementId',
    'IX_ObjectiveElementObservations_ComplianceJourneyId_ObjectiveElementId',
    'IX_ObjectiveElementObservations_ObjectiveElementId'
)
ORDER BY t.name, i.name;

GO
