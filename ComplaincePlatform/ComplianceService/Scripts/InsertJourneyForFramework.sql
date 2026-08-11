-- ============================================================================
-- Insert Compliance Journey for Specific Framework ID
-- ============================================================================
-- This script creates a ComplianceJourney for:
-- Hospital ID: e198747b-096d-4c19-a341-16be566c8da4 (Kanchan Bai Hospital)
-- Framework ID: d1e32b54-aa6f-4495-c965-08de63de351e
-- 
-- Organization: Sitaram and sons pvt ltd (02dd651e-489c-442a-a94b-9d4ca999284c)
-- ============================================================================

-- ============================================================================
-- IMPORTANT: Update the database name below to match your connection string
-- Default database name from appsettings.json: Compliance
-- ============================================================================
USE [Compliance]  -- Update this to match your actual database name
GO

-- ============================================================================
-- Variables
-- ============================================================================
DECLARE @TenantId UNIQUEIDENTIFIER = '02dd651e-489c-442a-a94b-9d4ca999284c';  -- Organization ID
DECLARE @OrganizationId UNIQUEIDENTIFIER = '02dd651e-489c-442a-a94b-9d4ca999284c';  -- Same as TenantId
DECLARE @HospitalId UNIQUEIDENTIFIER = 'e198747b-096d-4c19-a341-16be566c8da4';  -- Kanchan Bai Hospital
DECLARE @FrameworkId UNIQUEIDENTIFIER = 'd1e32b54-aa6f-4495-c965-08de63de351e';  -- Specific Framework ID

-- ============================================================================
-- 1. Ensure Framework Exists
-- ============================================================================
PRINT 'Checking if Framework exists...'
GO

IF NOT EXISTS (SELECT 1 FROM ComplianceFrameworks WHERE Id = 'd1e32b54-aa6f-4495-c965-08de63de351e')
BEGIN
    THROW 50030, 'Framework ID does not exist in Compliance canonical catalog. Script blocked to avoid divergent framework identities.', 1;
END
ELSE
BEGIN
    PRINT 'Framework with ID d1e32b54-aa6f-4495-c965-08de63de351e already exists.';
    
    -- Display framework details
    SELECT 
        Id,
        Name,
        Authority,
        Version,
        IsActive
    FROM ComplianceFrameworks
    WHERE Id = 'd1e32b54-aa6f-4495-c965-08de63de351e';
END

GO

-- ============================================================================
-- 2. Ensure Framework Configuration Exists
-- ============================================================================
PRINT 'Checking if Framework Configuration exists...'
GO

IF NOT EXISTS (SELECT 1 FROM FrameworkConfigurations WHERE FrameworkId = 'd1e32b54-aa6f-4495-c965-08de63de351e')
BEGIN
    PRINT 'Creating Framework Configuration...'
    
    INSERT INTO FrameworkConfigurations (
        Id, FrameworkId, DefaultValidityMonths, MinimumReadinessScore, 
        RenewalDaysBeforeExpiry, ReapplyWaitingDays, MajorNcResolutionDays, CreatedAt, PlatformId)
    VALUES (
        NEWID(),
        'd1e32b54-aa6f-4495-c965-08de63de351e',
        36,      -- Default validity: 36 months
        75.00,   -- Minimum readiness score: 75%
        90,      -- Renewal window: 90 days before expiry
        30,      -- Reapply waiting period: 30 days
        60,      -- Major NC resolution: 60 days
        GETUTCDATE(),
        'FW-CFG'
    );
    
    PRINT 'Framework Configuration created.';
END
ELSE
BEGIN
    PRINT 'Framework Configuration already exists.';
END

GO

-- ============================================================================
-- 3. Insert Compliance Journey
-- ============================================================================
PRINT 'Inserting Compliance Journey...'
GO

DECLARE @TenantId UNIQUEIDENTIFIER = '02dd651e-489c-442a-a94b-9d4ca999284c';
DECLARE @OrganizationId UNIQUEIDENTIFIER = '02dd651e-489c-442a-a94b-9d4ca999284c';
DECLARE @HospitalId UNIQUEIDENTIFIER = 'e198747b-096d-4c19-a341-16be566c8da4';
DECLARE @FrameworkId UNIQUEIDENTIFIER = 'd1e32b54-aa6f-4495-c965-08de63de351e';

IF NOT EXISTS (
    SELECT 1 FROM ComplianceJourneys 
    WHERE HospitalId = @HospitalId AND FrameworkId = @FrameworkId)
BEGIN
    -- Create an Approved journey with expiry date for better journey summary data
    DECLARE @ApprovalDate DATETIME = DATEADD(MONTH, -6, GETUTCDATE());  -- Approved 6 months ago
    DECLARE @ExpiryDate DATETIME = DATEADD(MONTH, 30, @ApprovalDate);  -- Expires in 30 months (36 months total validity)
    DECLARE @JourneyId UNIQUEIDENTIFIER = NEWID();
    
    INSERT INTO ComplianceJourneys (
        Id, TenantId, OrganizationId, HospitalId, FrameworkId, Status,
        ValidityPeriodInMonths, MinimumRequiredScore, CurrentReadinessScore, RiskScore,
        EnrollmentDate, SubmissionDate, ApprovalDate, ExpiryDate, RenewalWindowStartDate,
        CertificateNumber, IsLocked, IsPublicCertificateVisible,
        CreatedAt, UpdatedAt, RowVersion)
    VALUES (
        @JourneyId,
        @TenantId,
        @OrganizationId,
        @HospitalId,
        @FrameworkId,
        7,  -- Status: Approved (enum value 7)
        36,  -- Validity period: 36 months
        75.00,  -- Minimum required score: 75%
        82.50,  -- Current readiness score: 82.5%
        15.00,  -- Risk score: 15% (low risk)
        DATEADD(MONTH, -8, GETUTCDATE()),  -- Enrolled 8 months ago
        DATEADD(MONTH, -7, GETUTCDATE()),  -- Submitted 7 months ago
        @ApprovalDate,  -- Approved 6 months ago
        @ExpiryDate,  -- Expires in 30 months
        DATEADD(DAY, -90, @ExpiryDate),  -- Renewal window starts 90 days before expiry
        'CERT-' + SUBSTRING(CAST(@JourneyId AS VARCHAR(36)), 1, 8),  -- Certificate number
        1,  -- IsLocked: true
        1,  -- IsPublicCertificateVisible: true
        DATEADD(MONTH, -8, GETUTCDATE()),  -- Created 8 months ago
        GETUTCDATE(),  -- Updated now
        NULL  -- RowVersion (auto-generated)
    );
    
    PRINT 'Compliance Journey inserted successfully.';
    PRINT 'Journey ID: ' + CAST(@JourneyId AS VARCHAR(36));
    PRINT 'Status: Approved';
    PRINT 'Expiry Date: ' + CONVERT(VARCHAR, @ExpiryDate, 120);
END
ELSE
BEGIN
    PRINT 'Compliance Journey already exists for this hospital and framework.';
    
    -- Display existing journey details
    SELECT 
        Id,
        Status,
        EnrollmentDate,
        SubmissionDate,
        ApprovalDate,
        ExpiryDate,
        CurrentReadinessScore,
        RiskScore,
        CertificateNumber
    FROM ComplianceJourneys
    WHERE HospitalId = @HospitalId AND FrameworkId = @FrameworkId;
END

GO

-- ============================================================================
-- 4. Verify Data
-- ============================================================================
PRINT 'Verifying inserted data...'
GO

DECLARE @HospitalId UNIQUEIDENTIFIER = 'e198747b-096d-4c19-a341-16be566c8da4';
DECLARE @FrameworkId UNIQUEIDENTIFIER = 'd1e32b54-aa6f-4495-c965-08de63de351e';

SELECT 
    cj.Id AS JourneyId,
    cj.Status,
    cj.EnrollmentDate,
    cj.ApprovalDate,
    cj.ExpiryDate,
    cj.CurrentReadinessScore,
    cj.RiskScore,
    cj.CertificateNumber,
    cf.Name AS FrameworkName,
    cf.Version AS FrameworkVersion
FROM ComplianceJourneys cj
INNER JOIN ComplianceFrameworks cf ON cj.FrameworkId = cf.Id
WHERE cj.HospitalId = @HospitalId AND cj.FrameworkId = @FrameworkId;

PRINT 'Script completed successfully!'
GO
