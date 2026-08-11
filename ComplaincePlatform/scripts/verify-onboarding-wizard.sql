/*
  Verify second-step onboarding (wizard) across Organization, Compliance, and Identity DBs.

  Usage:
    1. Set @OrganizationId and @HospitalId (and optional @InviteEmail) below.
    2. If your database names differ, replace [Organization], [Compliance], [Identity].
    3. Execute in SSMS (or sqlcmd) connected to any database; three-part names target each catalog.

  Expected after a successful POST /api/v1/onboarding/onboard:
    - Organization: hospital wizard fields populated, departments linked, members for invites (if any).
    - Compliance: WizardOnboardingAssessments row; ComplianceJourneys per selected framework; tasks may exist.
    - Identity: new users for invites (if invite flow ran).
*/

SET NOCOUNT ON;

/* ============ PARAMETERS — EDIT THESE ============ */
DECLARE @OrganizationId UNIQUEIDENTIFIER = 'ddf84ca3-bde0-4d05-933e-8cc4c975cbbd';
DECLARE @HospitalId     UNIQUEIDENTIFIER = '4fd7ff48-12e0-4e7c-81aa-63e86551b62d';
DECLARE @WizardVersion  INT              = 1;

-- Optional: invite email from teamInvites (set NULL to skip Identity checks)
DECLARE @InviteEmail    NVARCHAR(256)    = N'ritu.soni@gmail.com';

/* Optional: expected framework codes from the wizard (comma-separated, case-insensitive) — leave NULL to skip count check */
DECLARE @ExpectedFrameworkCodes NVARCHAR(500) = N'NABH-E-26,NABH-F-20,NCAH-26,POLL-24';
/* ================================================= */

PRINT N'--- Organization database ---';

SELECT  o.[Id]              AS OrganizationId,
        o.[Name]            AS OrganizationName
FROM    [Organization].[dbo].[Organizations] AS o
WHERE   o.[Id] = @OrganizationId;

SELECT  h.[Id]              AS HospitalId,
        h.[OrganizationId],
        h.[Name]            AS HospitalName,
        h.[IsOnboarded],
        h.[WizardVersion],
        h.[BedCapacityRange],
        h.[CountryCode],
        h.[StateCode],
        h.[Type]            AS HospitalType
FROM    [Organization].[dbo].[Hospitals] AS h
WHERE   h.[Id] = @HospitalId
  AND   h.[OrganizationId] = @OrganizationId;

SELECT  COUNT(*)            AS HospitalMasterDepartmentRows
FROM    [Organization].[dbo].[HospitalMasterDepartments] AS hmd
WHERE   hmd.[HospitalId] = @HospitalId;

SELECT  md.[Id]             AS MasterDepartmentId,
        md.[Code],
        md.[Name]
FROM    [Organization].[dbo].[HospitalMasterDepartments] AS hmd
INNER JOIN [Organization].[dbo].[MasterDepartments] AS md ON md.[Id] = hmd.[MasterDepartmentId]
WHERE   hmd.[HospitalId] = @HospitalId
ORDER BY md.[Name];

SELECT  m.[Id]              AS MemberId,
        m.[UserId],
        m.[Email],
        m.[Role],
        m.[DepartmentId],
        m.[FirstName],
        m.[LastName]
FROM    [Organization].[dbo].[Members] AS m
WHERE   m.[OrganizationId] = @OrganizationId
ORDER BY m.[CreatedAt];

PRINT N'--- Compliance database ---';

SELECT  w.[Id]              AS AssessmentId,
        w.[OrganizationId],
        w.[HospitalId],
        w.[WizardVersion],
        w.[CreatedAt],
        LEN(w.[AnswersJson]) AS AnswersJsonLength
FROM    [Compliance].[dbo].[WizardOnboardingAssessments] AS w
WHERE   w.[HospitalId] = @HospitalId
  AND   w.[OrganizationId] = @OrganizationId
  AND   w.[WizardVersion] = @WizardVersion
ORDER BY w.[CreatedAt] DESC;

-- Gateway maps "framework code" to ComplianceFrameworks.PlatformId (see FrameworkRepository).
SELECT  cj.[Id]             AS JourneyId,
        cj.[TenantId],
        cj.[OrganizationId],
        cj.[HospitalId],
        cj.[FrameworkId],
        cf.[PlatformId]      AS FrameworkCode,
        cf.[Name]            AS FrameworkName,
        cf.[Version],
        cj.[Status],
        cj.[CreatedAt]
FROM    [Compliance].[dbo].[ComplianceJourneys] AS cj
INNER JOIN [Compliance].[dbo].[ComplianceFrameworks] AS cf ON cf.[Id] = cj.[FrameworkId]
WHERE   cj.[HospitalId] = @HospitalId
  AND   cj.[OrganizationId] = @OrganizationId
ORDER BY cf.[PlatformId];

SELECT  COUNT(*)            AS ComplianceTaskRowsForHospital
FROM    [Compliance].[dbo].[ComplianceTasks] AS t
WHERE   t.[HospitalId] = @HospitalId;

IF @ExpectedFrameworkCodes IS NOT NULL AND LEN(LTRIM(RTRIM(@ExpectedFrameworkCodes))) > 0
BEGIN
    ;WITH Expected AS (
        SELECT LTRIM(RTRIM(value)) AS Code
        FROM STRING_SPLIT(REPLACE(@ExpectedFrameworkCodes, N' ', N''), N',')
        WHERE LEN(LTRIM(RTRIM(value))) > 0
    ),
    Actual AS (
        SELECT DISTINCT UPPER(cf.[PlatformId]) AS Code
        FROM [Compliance].[dbo].[ComplianceJourneys] AS cj
        INNER JOIN [Compliance].[dbo].[ComplianceFrameworks] AS cf ON cf.[Id] = cj.[FrameworkId]
        WHERE cj.[HospitalId] = @HospitalId
          AND cj.[OrganizationId] = @OrganizationId
    )
    SELECT
        (SELECT COUNT(*) FROM Expected) AS ExpectedFrameworkCount,
        (SELECT COUNT(*) FROM Actual a INNER JOIN Expected e ON UPPER(e.Code) = a.Code) AS MatchedJourneyFrameworkCount,
        CASE
            WHEN (SELECT COUNT(*) FROM Expected) =
                 (SELECT COUNT(*) FROM Actual a INNER JOIN Expected e ON UPPER(e.Code) = a.Code)
                 AND (SELECT COUNT(*) FROM Expected) = (SELECT COUNT(*) FROM Actual)
            THEN N'OK: journey rows match expected framework codes'
            ELSE N'CHECK: expected codes vs journeys (missing/extra frameworks)'
        END AS FrameworkCheckSummary;
END;

PRINT N'--- Identity database (optional) ---';

IF @InviteEmail IS NOT NULL AND LEN(LTRIM(RTRIM(@InviteEmail))) > 0
BEGIN
    SELECT  u.[Id]          AS UserId,
            u.[UserName],
            u.[Email],
            u.[EmailConfirmed]
    FROM    [Identity].[dbo].[AspNetUsers] AS u
    WHERE   u.[Email] = LTRIM(RTRIM(@InviteEmail));
END
ELSE
BEGIN
    PRINT N'Skip Identity checks (@InviteEmail not set).';
END;

PRINT N'--- Summary row ---';

SELECT
    CASE WHEN EXISTS (
        SELECT 1 FROM [Organization].[dbo].[Hospitals] h
        WHERE h.[Id] = @HospitalId AND h.[OrganizationId] = @OrganizationId
          AND h.[IsOnboarded] = 1 AND h.[WizardVersion] IS NOT NULL
    ) THEN 1 ELSE 0 END AS OrgHospitalWizardProfileOk,

    CASE WHEN EXISTS (
        SELECT 1 FROM [Organization].[dbo].[HospitalMasterDepartments] hmd
        WHERE hmd.[HospitalId] = @HospitalId
    ) THEN 1 ELSE 0 END AS OrgHospitalDepartmentsLinkedOk,

    CASE WHEN EXISTS (
        SELECT 1 FROM [Compliance].[dbo].[WizardOnboardingAssessments] w
        WHERE w.[HospitalId] = @HospitalId AND w.[OrganizationId] = @OrganizationId
          AND w.[WizardVersion] = @WizardVersion
    ) THEN 1 ELSE 0 END AS ComplianceAssessmentOk,

    CASE WHEN EXISTS (
        SELECT 1 FROM [Compliance].[dbo].[ComplianceJourneys] cj
        WHERE cj.[HospitalId] = @HospitalId AND cj.[OrganizationId] = @OrganizationId
    ) THEN 1 ELSE 0 END AS ComplianceJourneysOk;
