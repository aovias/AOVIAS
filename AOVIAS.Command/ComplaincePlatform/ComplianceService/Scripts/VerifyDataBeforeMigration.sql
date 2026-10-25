-- Data Verification Script for Structural Refactor Migration
-- Run BEFORE applying a migration to check for potential issues.
-- ObjectiveElements column names below assume Phase 36 rename (ComplianceStandardId). For a DB still on StandardId, adjust queries accordingly.

USE Compliance;
GO

PRINT '========================================';
PRINT 'DATA VERIFICATION BEFORE MIGRATION';
PRINT '========================================';
PRINT '';

-- 1. Check for duplicate ComplianceFrameworks (Name, Version)
PRINT '1. Checking for duplicate ComplianceFrameworks (Name, Version)...';
SELECT 
    Name, 
    Version, 
    COUNT(*) as DuplicateCount,
    STRING_AGG(CAST(Id AS VARCHAR(36)), ', ') as FrameworkIds
FROM ComplianceFrameworks
GROUP BY Name, Version
HAVING COUNT(*) > 1;

IF @@ROWCOUNT = 0
    PRINT '   ✓ No duplicate Frameworks found';
ELSE
    PRINT '   ✗ DUPLICATES FOUND - Must clean before migration!';
PRINT '';

-- 2. Check for duplicate ObjectiveElements (ComplianceStandardId, Code)
PRINT '2. Checking for duplicate ObjectiveElements (ComplianceStandardId, Code)...';
SELECT 
    ComplianceStandardId, 
    Code, 
    COUNT(*) as DuplicateCount,
    STRING_AGG(CAST(Id AS VARCHAR(36)), ', ') as ObjectiveElementIds
FROM ObjectiveElements
GROUP BY ComplianceStandardId, Code
HAVING COUNT(*) > 1;

IF @@ROWCOUNT = 0
    PRINT '   ✓ No duplicate ObjectiveElements found';
ELSE
    PRINT '   ✗ DUPLICATES FOUND - Must clean before migration!';
PRINT '';

-- 3. Check for duplicate Controls (ProgramId, Code)
PRINT '3. Checking for duplicate Controls (ProgramId, Code)...';
SELECT 
    ProgramId, 
    Code, 
    COUNT(*) as DuplicateCount,
    STRING_AGG(CAST(Id AS VARCHAR(36)), ', ') as ControlIds
FROM Controls
GROUP BY ProgramId, Code
HAVING COUNT(*) > 1;

IF @@ROWCOUNT = 0
    PRINT '   ✓ No duplicate Controls found';
ELSE
    PRINT '   ✗ DUPLICATES FOUND - Must clean before migration!';
PRINT '';

-- 4. Check string lengths (must be <= 450 for unique indexes)
PRINT '4. Checking string lengths for column type changes...';

-- ComplianceFrameworks.Name
SELECT 
    'ComplianceFrameworks.Name' as ColumnName,
    MAX(LEN(Name)) as MaxLength,
    COUNT(*) as RowsExceeding450
FROM ComplianceFrameworks
WHERE LEN(Name) > 450;

-- ComplianceFrameworks.Version
SELECT 
    'ComplianceFrameworks.Version' as ColumnName,
    MAX(LEN(Version)) as MaxLength,
    COUNT(*) as RowsExceeding450
FROM ComplianceFrameworks
WHERE LEN(Version) > 450;

-- Controls.Code
SELECT 
    'Controls.Code' as ColumnName,
    MAX(LEN(Code)) as MaxLength,
    COUNT(*) as RowsExceeding450
FROM Controls
WHERE LEN(Code) > 450;

-- ObjectiveElements.Code
SELECT 
    'ObjectiveElements.Code' as ColumnName,
    MAX(LEN(Code)) as MaxLength,
    COUNT(*) as RowsExceeding450
FROM ObjectiveElements
WHERE LEN(Code) > 450;

PRINT '   ✓ String length check complete';
PRINT '';

-- 5. Check existing CompliancePrograms (need FrameworkId backfill)
PRINT '5. Checking existing CompliancePrograms...';
SELECT 
    COUNT(*) as TotalPrograms,
    COUNT(DISTINCT OrganizationId) as UniqueOrganizations
FROM CompliancePrograms;

-- Check if programs have Regulations that can help with backfill
SELECT 
    COUNT(DISTINCT cp.Id) as ProgramsWithRegulationLink
FROM CompliancePrograms cp
INNER JOIN Regulations r ON r.OrganizationId = cp.OrganizationId
WHERE r.ComplianceFrameworkId IS NOT NULL;

PRINT '   ✓ ComplianceProgram check complete';
PRINT '';

-- 6. Check if at least one Framework exists (required for backfill)
PRINT '6. Checking for existing ComplianceFrameworks...';
SELECT 
    COUNT(*) as FrameworkCount,
    STRING_AGG(Name + ' ' + Version, ', ') as Frameworks
FROM ComplianceFrameworks;

IF @@ROWCOUNT > 0 AND (SELECT COUNT(*) FROM ComplianceFrameworks) > 0
    PRINT '   ✓ At least one Framework exists';
ELSE
    PRINT '   ⚠ WARNING: No Frameworks exist - seeding must run first!';
PRINT '';

PRINT '========================================';
PRINT 'VERIFICATION COMPLETE';
PRINT '========================================';
GO
