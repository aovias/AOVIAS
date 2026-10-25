-- ============================================================================
-- Extended Sample Data Insert Script for Compliance Platform
-- ============================================================================
-- This script inserts sample data for:
-- 1. ComplianceFrameworks (NABH, NABL, Fire NOC, Pollution)
-- 2. FrameworkConfigurations (business rules for each framework)
-- 3. ComplianceJourneys (hospital-framework associations)
-- 4. ComplianceSnapshots (compliance scores and metrics)
-- 
-- Organization: Sitaram and sons pvt ltd (02dd651e-489c-442a-a94b-9d4ca999284c)
-- Hospital ID: e198747b-096d-4c19-a341-16be566c8da4 (Kanchan Bai Hospital)
-- ============================================================================

-- ============================================================================
-- IMPORTANT: Update the database name below to match your connection string
-- Default database name from appsettings.json: Compliance
-- ============================================================================
USE [Compliance]  -- Update this to match your actual database name
GO

-- ============================================================================
-- 1. INSERT COMPLIANCE FRAMEWORKS
-- ============================================================================
PRINT 'Inserting Compliance Frameworks...'
GO

-- NABH Framework
IF NOT EXISTS (SELECT 1 FROM ComplianceFrameworks WHERE Name = 'NABH' AND Version = 'Entry Level')
BEGIN
    THROW 50031, 'NABH Entry Level framework missing in canonical catalog. Insert it via canonical bootstrap first.', 1;
END
ELSE
BEGIN
    PRINT 'NABH Framework already exists.';
END

-- NABL Framework
IF NOT EXISTS (SELECT 1 FROM ComplianceFrameworks WHERE Name = 'NABL' AND Version = '2024')
BEGIN
    THROW 50032, 'NABL 2024 framework missing in canonical catalog. Insert it via canonical bootstrap first.', 1;
END
ELSE
BEGIN
    PRINT 'NABL Framework already exists.';
END

-- Fire NOC Framework
IF NOT EXISTS (SELECT 1 FROM ComplianceFrameworks WHERE Name = 'Fire NOC' AND Version = '2024')
BEGIN
    THROW 50033, 'Fire NOC 2024 framework missing in canonical catalog. Insert it via canonical bootstrap first.', 1;
END
ELSE
BEGIN
    PRINT 'Fire NOC Framework already exists.';
END

-- Pollution Framework
IF NOT EXISTS (SELECT 1 FROM ComplianceFrameworks WHERE Name = 'Pollution' AND Version = '2024')
BEGIN
    THROW 50034, 'Pollution 2024 framework missing in canonical catalog. Insert it via canonical bootstrap first.', 1;
END
ELSE
BEGIN
    PRINT 'Pollution Framework already exists.';
END

GO

-- ============================================================================
-- 2. INSERT FRAMEWORK CONFIGURATIONS
-- ============================================================================
PRINT 'Inserting Framework Configurations...'
GO

-- NABH Configuration
IF NOT EXISTS (
    SELECT 1 FROM FrameworkConfigurations fc
    INNER JOIN ComplianceFrameworks f ON fc.FrameworkId = f.Id
    WHERE f.Name = 'NABH' AND f.Version = 'Entry Level')
BEGIN
    INSERT INTO FrameworkConfigurations (
        Id, FrameworkId, DefaultValidityMonths, MinimumReadinessScore, 
        RenewalDaysBeforeExpiry, ReapplyWaitingDays, MajorNcResolutionDays, CreatedAt, PlatformId)
    SELECT 
        NEWID(), f.Id, 36, 75.00, 90, 30, 60, GETUTCDATE(), 'NABH-CFG'
    FROM ComplianceFrameworks f
    WHERE f.Name = 'NABH' AND f.Version = 'Entry Level';
    PRINT 'NABH Framework Configuration inserted.';
END
ELSE
BEGIN
    PRINT 'NABH Framework Configuration already exists.';
END

-- NABL Configuration
IF NOT EXISTS (
    SELECT 1 FROM FrameworkConfigurations fc
    INNER JOIN ComplianceFrameworks f ON fc.FrameworkId = f.Id
    WHERE f.Name = 'NABL' AND f.Version = '2024')
BEGIN
    INSERT INTO FrameworkConfigurations (
        Id, FrameworkId, DefaultValidityMonths, MinimumReadinessScore, 
        RenewalDaysBeforeExpiry, ReapplyWaitingDays, MajorNcResolutionDays, CreatedAt, PlatformId)
    SELECT 
        NEWID(), f.Id, 24, 80.00, 90, 30, 45, GETUTCDATE(), 'NABL-CFG'
    FROM ComplianceFrameworks f
    WHERE f.Name = 'NABL' AND f.Version = '2024';
    PRINT 'NABL Framework Configuration inserted.';
END
ELSE
BEGIN
    PRINT 'NABL Framework Configuration already exists.';
END

-- Fire NOC Configuration
IF NOT EXISTS (
    SELECT 1 FROM FrameworkConfigurations fc
    INNER JOIN ComplianceFrameworks f ON fc.FrameworkId = f.Id
    WHERE f.Name = 'Fire NOC' AND f.Version = '2024')
BEGIN
    INSERT INTO FrameworkConfigurations (
        Id, FrameworkId, DefaultValidityMonths, MinimumReadinessScore, 
        RenewalDaysBeforeExpiry, ReapplyWaitingDays, MajorNcResolutionDays, CreatedAt, PlatformId)
    SELECT 
        NEWID(), f.Id, 12, 70.00, 60, 15, 30, GETUTCDATE(), 'FIRE-CFG'
    FROM ComplianceFrameworks f
    WHERE f.Name = 'Fire NOC' AND f.Version = '2024';
    PRINT 'Fire NOC Framework Configuration inserted.';
END
ELSE
BEGIN
    PRINT 'Fire NOC Framework Configuration already exists.';
END

-- Pollution Configuration
IF NOT EXISTS (
    SELECT 1 FROM FrameworkConfigurations fc
    INNER JOIN ComplianceFrameworks f ON fc.FrameworkId = f.Id
    WHERE f.Name = 'Pollution' AND f.Version = '2024')
BEGIN
    INSERT INTO FrameworkConfigurations (
        Id, FrameworkId, DefaultValidityMonths, MinimumReadinessScore, 
        RenewalDaysBeforeExpiry, ReapplyWaitingDays, MajorNcResolutionDays, CreatedAt, PlatformId)
    SELECT 
        NEWID(), f.Id, 12, 70.00, 60, 15, 30, GETUTCDATE(), 'POLL-CFG'
    FROM ComplianceFrameworks f
    WHERE f.Name = 'Pollution' AND f.Version = '2024';
    PRINT 'Pollution Framework Configuration inserted.';
END
ELSE
BEGIN
    PRINT 'Pollution Framework Configuration already exists.';
END

GO

-- ============================================================================
-- 3. INSERT COMPLIANCE JOURNEYS (Hospital-Framework Associations)
-- ============================================================================
PRINT 'Inserting Compliance Journeys...'
GO

-- Organization and Hospital IDs from provided JSON
DECLARE @TenantId UNIQUEIDENTIFIER = '02dd651e-489c-442a-a94b-9d4ca999284c';  -- Organization ID
DECLARE @OrganizationId UNIQUEIDENTIFIER = '02dd651e-489c-442a-a94b-9d4ca999284c';  -- Same as TenantId
DECLARE @HospitalId UNIQUEIDENTIFIER = 'e198747b-096d-4c19-a341-16be566c8da4';  -- Kanchan Bai Hospital

-- NABH Journey (In Progress)
IF NOT EXISTS (
    SELECT 1 FROM ComplianceJourneys cj
    INNER JOIN ComplianceFrameworks f ON cj.FrameworkId = f.Id
    WHERE cj.HospitalId = @HospitalId AND f.Name = 'NABH' AND f.Version = 'Entry Level')
BEGIN
    INSERT INTO ComplianceJourneys (
        Id, TenantId, OrganizationId, HospitalId, FrameworkId, Status,
        ValidityPeriodInMonths, MinimumRequiredScore, CurrentReadinessScore, RiskScore,
        EnrollmentDate, IsLocked, IsPublicCertificateVisible,
        CreatedAt, UpdatedAt, RowVersion)
    SELECT 
        NEWID(), @TenantId, @OrganizationId, @HospitalId, f.Id, 3,  -- Status: InProgress (enum value)
        36, 75.00, 78.50, 5.00,
        DATEADD(MONTH, -6, GETUTCDATE()),  -- Enrolled 6 months ago
        0, 0,  -- Not locked, certificate not visible yet
        DATEADD(MONTH, -6, GETUTCDATE()), GETUTCDATE(), NULL
    FROM ComplianceFrameworks f
    WHERE f.Name = 'NABH' AND f.Version = 'Entry Level';
    PRINT 'NABH Compliance Journey inserted (In Progress).';
END
ELSE
BEGIN
    PRINT 'NABH Compliance Journey already exists.';
END

-- NABL Journey (Enrolled)
IF NOT EXISTS (
    SELECT 1 FROM ComplianceJourneys cj
    INNER JOIN ComplianceFrameworks f ON cj.FrameworkId = f.Id
    WHERE cj.HospitalId = @HospitalId AND f.Name = 'NABL' AND f.Version = '2024')
BEGIN
    INSERT INTO ComplianceJourneys (
        Id, TenantId, OrganizationId, HospitalId, FrameworkId, Status,
        ValidityPeriodInMonths, MinimumRequiredScore, CurrentReadinessScore, RiskScore,
        EnrollmentDate, IsLocked, IsPublicCertificateVisible,
        CreatedAt, UpdatedAt, RowVersion)
    SELECT 
        NEWID(), @TenantId, @OrganizationId, @HospitalId, f.Id, 1,  -- Status: Enrolled (enum value)
        24, 80.00, 0.00, 0.00,
        DATEADD(MONTH, -2, GETUTCDATE()),  -- Enrolled 2 months ago
        0, 0,  -- Not locked, certificate not visible yet
        DATEADD(MONTH, -2, GETUTCDATE()), GETUTCDATE(), NULL
    FROM ComplianceFrameworks f
    WHERE f.Name = 'NABL' AND f.Version = '2024';
    PRINT 'NABL Compliance Journey inserted (Enrolled).';
END
ELSE
BEGIN
    PRINT 'NABL Compliance Journey already exists.';
END

-- Fire NOC Journey (Approved)
IF NOT EXISTS (
    SELECT 1 FROM ComplianceJourneys cj
    INNER JOIN ComplianceFrameworks f ON cj.FrameworkId = f.Id
    WHERE cj.HospitalId = @HospitalId AND f.Name = 'Fire NOC' AND f.Version = '2024')
BEGIN
    DECLARE @FireNocApprovalDate DATETIME = DATEADD(MONTH, -3, GETUTCDATE());
    DECLARE @FireNocExpiryDate DATETIME = DATEADD(MONTH, 9, @FireNocApprovalDate);  -- 12 months validity
    DECLARE @FireNocJourneyId UNIQUEIDENTIFIER = NEWID();
    
    INSERT INTO ComplianceJourneys (
        Id, TenantId, OrganizationId, HospitalId, FrameworkId, Status,
        ValidityPeriodInMonths, MinimumRequiredScore, CurrentReadinessScore, RiskScore,
        EnrollmentDate, SubmissionDate, ApprovalDate, ExpiryDate, RenewalWindowStartDate,
        CertificateNumber, IsLocked, IsPublicCertificateVisible,
        CreatedAt, UpdatedAt, RowVersion)
    SELECT 
        @FireNocJourneyId, @TenantId, @OrganizationId, @HospitalId, f.Id, 7,  -- Status: Approved (enum value)
        12, 70.00, 85.00, 0.00,
        DATEADD(MONTH, -6, GETUTCDATE()),  -- Enrolled 6 months ago
        DATEADD(MONTH, -4, GETUTCDATE()),  -- Submitted 4 months ago
        @FireNocApprovalDate,  -- Approved 3 months ago
        @FireNocExpiryDate,  -- Expires in 9 months
        DATEADD(DAY, -90, @FireNocExpiryDate),  -- Renewal window starts 90 days before expiry
        'FIRE-2024-' + SUBSTRING(CAST(@FireNocJourneyId AS VARCHAR(36)), 1, 8),
        1, 1,  -- Locked, certificate visible
        DATEADD(MONTH, -6, GETUTCDATE()), GETUTCDATE(), NULL
    FROM ComplianceFrameworks f
    WHERE f.Name = 'Fire NOC' AND f.Version = '2024';
    PRINT 'Fire NOC Compliance Journey inserted (Approved).';
END
ELSE
BEGIN
    PRINT 'Fire NOC Compliance Journey already exists.';
END

-- Pollution Journey (Draft)
IF NOT EXISTS (
    SELECT 1 FROM ComplianceJourneys cj
    INNER JOIN ComplianceFrameworks f ON cj.FrameworkId = f.Id
    WHERE cj.HospitalId = @HospitalId AND f.Name = 'Pollution' AND f.Version = '2024')
BEGIN
    INSERT INTO ComplianceJourneys (
        Id, TenantId, OrganizationId, HospitalId, FrameworkId, Status,
        ValidityPeriodInMonths, MinimumRequiredScore, CurrentReadinessScore, RiskScore,
        IsLocked, IsPublicCertificateVisible,
        CreatedAt, UpdatedAt, RowVersion)
    SELECT 
        NEWID(), @TenantId, @OrganizationId, @HospitalId, f.Id, 0,  -- Status: Draft (enum value)
        12, 70.00, 0.00, 0.00,
        0, 0,  -- Not locked, certificate not visible
        GETUTCDATE(), GETUTCDATE(), NULL
    FROM ComplianceFrameworks f
    WHERE f.Name = 'Pollution' AND f.Version = '2024';
    PRINT 'Pollution Compliance Journey inserted (Draft).';
END
ELSE
BEGIN
    PRINT 'Pollution Compliance Journey already exists.';
END

GO

-- ============================================================================
-- 4. UPDATE/INSERT COMPLIANCE SNAPSHOTS (with FrameworkId)
-- ============================================================================
PRINT 'Updating/Inserting Compliance Snapshots...'
GO

DECLARE @HospitalId UNIQUEIDENTIFIER = 'e198747b-096d-4c19-a341-16be566c8da4';  -- Kanchan Bai Hospital

-- NABH Snapshot (In Progress)
IF EXISTS (
    SELECT 1 FROM ComplianceSnapshots cs
    INNER JOIN ComplianceFrameworks f ON cs.FrameworkId = f.Id
    WHERE cs.HospitalId = @HospitalId AND f.Name = 'NABH' AND f.Version = 'Entry Level')
BEGIN
    UPDATE cs
    SET
        cs.OverallScore = 78.5,
        cs.AuditConfidenceScore = 82.0,
        cs.CriticalCount = 2,
        cs.MajorCount = 5,
        cs.MinorCount = 12,
        cs.ObservationCount = 3,
        cs.OFICount = 1,
        cs.MajorNCCount = 2,
        cs.MinorNCCount = 3,
        cs.CriticalRiskCount = 1,
        cs.JourneyState = 1,  -- InProgress
        cs.ChapterBreakdownJson = N'[
            {
                "chapterId": "00000000-0000-0000-0000-000000000001",
                "weightedScore": 85.5,
                "completionPercentage": 85.5,
                "riskPenalty": 0.0
            },
            {
                "chapterId": "00000000-0000-0000-0000-000000000002",
                "weightedScore": 72.0,
                "completionPercentage": 72.0,
                "riskPenalty": 0.0
            },
            {
                "chapterId": "00000000-0000-0000-0000-000000000003",
                "weightedScore": 60.0,
                "completionPercentage": 65.0,
                "riskPenalty": 5.0
            },
            {
                "chapterId": "00000000-0000-0000-0000-000000000004",
                "weightedScore": 90.0,
                "completionPercentage": 90.0,
                "riskPenalty": 0.0
            }
        ]',
        cs.HighRiskChaptersJson = N'[
            {
                "chapterId": "00000000-0000-0000-0000-000000000003",
                "chapterCode": "HRM",
                "chapterName": "Human Resource Management",
                "completionPercentage": 45.0,
                "totalClauses": 10,
                "completedClauses": 4
            }
        ]',
        cs.CalculatedAt = GETUTCDATE()
    FROM ComplianceSnapshots cs
    INNER JOIN ComplianceFrameworks f ON cs.FrameworkId = f.Id
    WHERE cs.HospitalId = @HospitalId AND f.Name = 'NABH' AND f.Version = 'Entry Level';
    PRINT 'NABH Snapshot updated successfully.';
END
ELSE
BEGIN
    INSERT INTO ComplianceSnapshots (
        Id, HospitalId, FrameworkId, OverallScore, AuditConfidenceScore,
        CriticalCount, MajorCount, MinorCount, ObservationCount, OFICount,
        MajorNCCount, MinorNCCount, CriticalRiskCount, JourneyState,
        ChapterBreakdownJson, HighRiskChaptersJson, CalculatedAt, CreatedAt, RowVersion)
    SELECT 
        NEWID(), @HospitalId, f.Id, 78.5, 82.0,
        2, 5, 12, 3, 1, 2, 3, 1, 1,  -- JourneyState: InProgress
        N'[
            {
                "chapterId": "00000000-0000-0000-0000-000000000001",
                "weightedScore": 85.5,
                "completionPercentage": 85.5,
                "riskPenalty": 0.0
            },
            {
                "chapterId": "00000000-0000-0000-0000-000000000002",
                "weightedScore": 72.0,
                "completionPercentage": 72.0,
                "riskPenalty": 0.0
            },
            {
                "chapterId": "00000000-0000-0000-0000-000000000003",
                "weightedScore": 60.0,
                "completionPercentage": 65.0,
                "riskPenalty": 5.0
            },
            {
                "chapterId": "00000000-0000-0000-0000-000000000004",
                "weightedScore": 90.0,
                "completionPercentage": 90.0,
                "riskPenalty": 0.0
            }
        ]',
        N'[
            {
                "chapterId": "00000000-0000-0000-0000-000000000003",
                "chapterCode": "HRM",
                "chapterName": "Human Resource Management",
                "completionPercentage": 45.0,
                "totalClauses": 10,
                "completedClauses": 4
            }
        ]',
        GETUTCDATE(), GETUTCDATE(), NULL
    FROM ComplianceFrameworks f
    WHERE f.Name = 'NABH' AND f.Version = 'Entry Level';
    PRINT 'NABH Snapshot inserted successfully.';
END

-- NABL Snapshot (Enrolled - early stage)
IF EXISTS (
    SELECT 1 FROM ComplianceSnapshots cs
    INNER JOIN ComplianceFrameworks f ON cs.FrameworkId = f.Id
    WHERE cs.HospitalId = @HospitalId AND f.Name = 'NABL' AND f.Version = '2024')
BEGIN
    UPDATE cs
    SET
        cs.OverallScore = 45.0,
        cs.AuditConfidenceScore = 50.0,
        cs.CriticalCount = 0,
        cs.MajorCount = 2,
        cs.MinorCount = 5,
        cs.ObservationCount = 1,
        cs.OFICount = 0,
        cs.MajorNCCount = 0,
        cs.MinorNCCount = 0,
        cs.CriticalRiskCount = 0,
        cs.JourneyState = 0,  -- Enrolled
        cs.ChapterBreakdownJson = N'[]',
        cs.HighRiskChaptersJson = N'[]',
        cs.CalculatedAt = GETUTCDATE()
    FROM ComplianceSnapshots cs
    INNER JOIN ComplianceFrameworks f ON cs.FrameworkId = f.Id
    WHERE cs.HospitalId = @HospitalId AND f.Name = 'NABL' AND f.Version = '2024';
    PRINT 'NABL Snapshot updated successfully.';
END
ELSE
BEGIN
    INSERT INTO ComplianceSnapshots (
        Id, HospitalId, FrameworkId, OverallScore, AuditConfidenceScore,
        CriticalCount, MajorCount, MinorCount, ObservationCount, OFICount,
        MajorNCCount, MinorNCCount, CriticalRiskCount, JourneyState,
        ChapterBreakdownJson, HighRiskChaptersJson, CalculatedAt, CreatedAt, RowVersion)
    SELECT 
        NEWID(), @HospitalId, f.Id, 45.0, 50.0,
        0, 2, 5, 1, 0, 0, 0, 0, 0,  -- JourneyState: Enrolled
        N'[]', N'[]', GETUTCDATE(), GETUTCDATE(), NULL
    FROM ComplianceFrameworks f
    WHERE f.Name = 'NABL' AND f.Version = '2024';
    PRINT 'NABL Snapshot inserted successfully.';
END

-- Fire NOC Snapshot (Approved)
IF EXISTS (
    SELECT 1 FROM ComplianceSnapshots cs
    INNER JOIN ComplianceFrameworks f ON cs.FrameworkId = f.Id
    WHERE cs.HospitalId = @HospitalId AND f.Name = 'Fire NOC' AND f.Version = '2024')
BEGIN
    UPDATE cs
    SET
        cs.OverallScore = 92.0,
        cs.AuditConfidenceScore = 95.0,
        cs.CriticalCount = 0,
        cs.MajorCount = 0,
        cs.MinorCount = 1,
        cs.ObservationCount = 0,
        cs.OFICount = 0,
        cs.MajorNCCount = 0,
        cs.MinorNCCount = 0,
        cs.CriticalRiskCount = 0,
        cs.JourneyState = 7,  -- Approved (Certified)
        cs.ChapterBreakdownJson = N'[
            {
                "chapterId": "00000000-0000-0000-0000-000000000001",
                "weightedScore": 95.0,
                "completionPercentage": 95.0,
                "riskPenalty": 0.0
            }
        ]',
        cs.HighRiskChaptersJson = N'[]',
        cs.CalculatedAt = GETUTCDATE()
    FROM ComplianceSnapshots cs
    INNER JOIN ComplianceFrameworks f ON cs.FrameworkId = f.Id
    WHERE cs.HospitalId = @HospitalId AND f.Name = 'Fire NOC' AND f.Version = '2024';
    PRINT 'Fire NOC Snapshot updated successfully.';
END
ELSE
BEGIN
    INSERT INTO ComplianceSnapshots (
        Id, HospitalId, FrameworkId, OverallScore, AuditConfidenceScore,
        CriticalCount, MajorCount, MinorCount, ObservationCount, OFICount,
        MajorNCCount, MinorNCCount, CriticalRiskCount, JourneyState,
        ChapterBreakdownJson, HighRiskChaptersJson, CalculatedAt, CreatedAt, RowVersion)
    SELECT 
        NEWID(), @HospitalId, f.Id, 92.0, 95.0,
        0, 0, 1, 0, 0, 0, 0, 0, 7,  -- JourneyState: Approved
        N'[
            {
                "chapterId": "00000000-0000-0000-0000-000000000001",
                "weightedScore": 95.0,
                "completionPercentage": 95.0,
                "riskPenalty": 0.0
            }
        ]',
        N'[]', GETUTCDATE(), GETUTCDATE(), NULL
    FROM ComplianceFrameworks f
    WHERE f.Name = 'Fire NOC' AND f.Version = '2024';
    PRINT 'Fire NOC Snapshot inserted successfully.';
END

GO

-- ============================================================================
-- 5. VERIFICATION QUERIES
-- ============================================================================
PRINT 'Verifying inserted data...'
GO

DECLARE @HospitalId UNIQUEIDENTIFIER = 'e198747b-096d-4c19-a341-16be566c8da4';  -- Kanchan Bai Hospital

-- Verify Frameworks
SELECT 'Frameworks' AS TableName, COUNT(*) AS RecordCount FROM ComplianceFrameworks;
SELECT Id, Name, Version, Authority, IsActive FROM ComplianceFrameworks;

-- Verify Framework Configurations
SELECT 'Framework Configurations' AS TableName, COUNT(*) AS RecordCount FROM FrameworkConfigurations;
SELECT fc.Id, f.Name AS FrameworkName, fc.DefaultValidityMonths, fc.MinimumReadinessScore, 
       fc.RenewalDaysBeforeExpiry, fc.ReapplyWaitingDays, fc.MajorNcResolutionDays
FROM FrameworkConfigurations fc
INNER JOIN ComplianceFrameworks f ON fc.FrameworkId = f.Id;

-- Verify Compliance Journeys
SELECT 'Compliance Journeys' AS TableName, COUNT(*) AS RecordCount 
FROM ComplianceJourneys WHERE HospitalId = @HospitalId;
SELECT cj.Id, f.Name AS FrameworkName, cj.Status, cj.CurrentReadinessScore, 
       cj.EnrollmentDate, cj.ApprovalDate, cj.ExpiryDate, cj.CertificateNumber
FROM ComplianceJourneys cj
INNER JOIN ComplianceFrameworks f ON cj.FrameworkId = f.Id
WHERE cj.HospitalId = @HospitalId;

-- Verify Compliance Snapshots
SELECT 'Compliance Snapshots' AS TableName, COUNT(*) AS RecordCount 
FROM ComplianceSnapshots WHERE HospitalId = @HospitalId;
SELECT cs.Id, f.Name AS FrameworkName, cs.OverallScore, cs.AuditConfidenceScore, 
       cs.JourneyState, cs.CriticalCount, cs.MajorCount, cs.MinorCount, cs.CalculatedAt
FROM ComplianceSnapshots cs
INNER JOIN ComplianceFrameworks f ON cs.FrameworkId = f.Id
WHERE cs.HospitalId = @HospitalId;

GO

PRINT '===================================================================='
PRINT 'Extended sample data script completed successfully!'
PRINT '===================================================================='
PRINT 'Summary:'
PRINT '  - Compliance Frameworks: 4 (NABH, NABL, Fire NOC, Pollution)'
PRINT '  - Framework Configurations: 4'
PRINT '  - Compliance Journeys: 4 (one per framework)'
PRINT '  - Compliance Snapshots: 3 (NABH, NABL, Fire NOC)'
PRINT '===================================================================='
GO
