-- Phase 0 Database Verification Script
-- Run this after the PowerShell script to verify data directly from the database

USE Compliance;
GO

-- Verify ComplianceProgram exists
SELECT 
    'CompliancePrograms' AS TableName,
    COUNT(*) AS RecordCount,
    MIN(CreatedAt) AS OldestRecord,
    MAX(CreatedAt) AS NewestRecord
FROM CompliancePrograms;

-- Show sample ComplianceProgram
SELECT TOP 5
    Id,
    PlatformId,
    OrganizationId,
    Name,
    CreatedAt
FROM CompliancePrograms
ORDER BY CreatedAt DESC;

-- Verify Controls exist
SELECT 
    'Controls' AS TableName,
    COUNT(*) AS RecordCount,
    COUNT(DISTINCT ProgramId) AS UniquePrograms,
    MIN(CreatedAt) AS OldestRecord,
    MAX(CreatedAt) AS NewestRecord
FROM Controls;

-- Show sample Controls with Program association
SELECT TOP 10
    c.Id,
    c.ProgramId,
    c.Code,
    c.Description,
    c.CreatedAt,
    p.Name AS ProgramName
FROM Controls c
LEFT JOIN CompliancePrograms p ON c.ProgramId = p.Id
ORDER BY c.CreatedAt DESC;

-- Verify ComplianceTasks exist
SELECT 
    'ComplianceTasks' AS TableName,
    COUNT(*) AS RecordCount,
    MIN(CreatedAt) AS OldestRecord,
    MAX(CreatedAt) AS NewestRecord
FROM ComplianceTasks;

-- Show sample ComplianceTasks
SELECT TOP 5
    Id,
    OrganizationId,
    RegulationId,
    RequirementId,
    Status,
    CreatedAt
FROM ComplianceTasks
ORDER BY CreatedAt DESC;

-- Verify Evidence exists
SELECT 
    'Evidence' AS TableName,
    COUNT(*) AS RecordCount,
    COUNT(DISTINCT TaskId) AS UniqueTasks,
    MIN(CreatedAt) AS OldestRecord,
    MAX(CreatedAt) AS NewestRecord
FROM Evidence;

-- Show sample Evidence with Task association
SELECT TOP 5
    e.Id,
    e.TaskId,
    e.EvidenceType,
    e.FileUrl,
    e.UploadedOn,
    e.CreatedAt
FROM Evidence e
ORDER BY e.CreatedAt DESC;

-- Check for any orphaned records (Controls without Programs)
SELECT 
    'Orphaned Controls' AS Issue,
    COUNT(*) AS Count
FROM Controls c
LEFT JOIN CompliancePrograms p ON c.ProgramId = p.Id
WHERE p.Id IS NULL;

-- Check for any orphaned Evidence (without Tasks)
SELECT 
    'Orphaned Evidence' AS Issue,
    COUNT(*) AS Count
FROM Evidence e
LEFT JOIN ComplianceTasks t ON e.TaskId = t.Id
WHERE t.Id IS NULL;

-- Summary
SELECT 
    (SELECT COUNT(*) FROM CompliancePrograms) AS TotalPrograms,
    (SELECT COUNT(*) FROM Controls) AS TotalControls,
    (SELECT COUNT(*) FROM ComplianceTasks) AS TotalTasks,
    (SELECT COUNT(*) FROM Evidence) AS TotalEvidence;
