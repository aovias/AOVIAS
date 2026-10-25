/*
  Onboarding wizard — AI Compliance Assessment step — questionnaire MASTER DATA

  There is NO dedicated AssessmentQuestionnaire table. The wizard loads questions dynamically
  from KnowledgeBase via GET /api/v1/onboarding/questionnaire/{frameworkId}.

  QuestionnaireGenerator builds one question per ObjectiveElement (OE) from:

    dbo.Frameworks              — framework registry (matched by Code in the UI)
    dbo.ComplianceNodes         — Chapter → Standard → ObjectiveElement hierarchy
    dbo.RequirementDetails      — standard Intent / Classification / MeasurableElements
    dbo.RequirementScores       — question Weight (on Standard nodes)
    dbo.KnowledgeItems          — help text (HowToComply on OE nodes)

  Wizard framework resolution (Angular OnboardingAssessmentApiService):
    1. User selects frameworks on step 3 (codes from Compliance GET /frameworks/all-frameworks)
    2. Those codes are ComplianceFramework.PlatformId values
    3. KB GET /frameworks is filtered by matching Framework.Code (case-insensitive)

  Therefore KB.Frameworks.Code MUST equal Compliance.ComplianceFrameworks.PlatformId
  for each paired framework (same Name + Version).

  ---------------------------------------------------------------------------
  PRODUCTION — NABH Hospital 6th Edition (full questionnaire, ~500 OEs)
  ---------------------------------------------------------------------------
    1. Ensure Compliance has canonical "NABH Full Accreditation (Hospital)" / "6th Edition"
    2. sqlcmd -d KnowledgeBase -i ./seed-data/sql/seed-nabh-hospital-6e.sql
    3. sqlcmd -d KnowledgeBase -i ./seed-data/sql/seed-onboarding-questionnaire-master.sql

  ---------------------------------------------------------------------------
  LOCAL DEMO — minimal framework (@SeedMinimalDemo = 1, 6 sample questions)
  ---------------------------------------------------------------------------
    sqlcmd -d KnowledgeBase -i ./seed-data/sql/seed-onboarding-questionnaire-master.sql
    (Set @SeedMinimalDemo = 1 inside the script first.)

  Verify:
    sqlcmd -d KnowledgeBase -i ./seed-data/sql/verify-onboarding-questionnaire.sql
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @SeedMinimalDemo BIT = 0;   -- 1 = insert ONBDEMO framework with 6 wizard questions
DECLARE @SyncFrameworkCodes BIT = 1; -- 1 = align KB.Frameworks.Code to Compliance PlatformId
DECLARE @Now DATETIME2 = SYSUTCDATETIME();

BEGIN TRY
BEGIN TRAN;

-- ---------------------------------------------------------------------------
-- 1. Align KB framework codes to Compliance PlatformId (required for wizard step 4)
-- ---------------------------------------------------------------------------
IF @SyncFrameworkCodes = 1 AND DB_ID(N'Compliance') IS NOT NULL
BEGIN
    UPDATE kb
    SET kb.Code = LTRIM(RTRIM(cf.PlatformId))
    FROM dbo.Frameworks kb
    INNER JOIN [Compliance].[dbo].[ComplianceFrameworks] cf
        ON LOWER(LTRIM(RTRIM(kb.Name))) = LOWER(LTRIM(RTRIM(cf.Name)))
       AND LOWER(LTRIM(RTRIM(kb.Version))) = LOWER(LTRIM(RTRIM(cf.Version)))
    WHERE cf.PlatformId IS NOT NULL
      AND LTRIM(RTRIM(cf.PlatformId)) <> N''
      AND kb.Code <> LTRIM(RTRIM(cf.PlatformId));

    PRINT CONCAT(N'Synced KB framework codes from Compliance PlatformId (rows updated: ', @@ROWCOUNT, N').');
END
ELSE IF @SyncFrameworkCodes = 1
BEGIN
    PRINT N'Skipped framework code sync — Compliance database not found on this server.';
END;

-- ---------------------------------------------------------------------------
-- 2. Optional minimal demo framework (self-contained questionnaire master data)
--    Code ONBDEMO appears in framework selector when Compliance row is inserted.
-- ---------------------------------------------------------------------------
IF @SeedMinimalDemo = 1
BEGIN
    DECLARE @DemoFrameworkId UNIQUEIDENTIFIER = 'd1000001-0000-4000-8000-000000000001';
    DECLARE @DemoCode NVARCHAR(10) = N'ONBDEMO';
    DECLARE @DemoChapterAac UNIQUEIDENTIFIER = 'd1000011-0000-4000-8000-000000000001';
    DECLARE @DemoChapterPre UNIQUEIDENTIFIER = 'd1000011-0000-4000-8000-000000000002';
    DECLARE @DemoStdAac1 UNIQUEIDENTIFIER = 'd1000021-0000-4000-8000-000000000001';
    DECLARE @DemoStdAac2 UNIQUEIDENTIFIER = 'd1000021-0000-4000-8000-000000000002';
    DECLARE @DemoStdPre1 UNIQUEIDENTIFIER = 'd1000021-0000-4000-8000-000000000003';
    DECLARE @DemoClsAac UNIQUEIDENTIFIER = 'd1000031-0000-4000-8000-000000000001';
    DECLARE @DemoClsPre UNIQUEIDENTIFIER = 'd1000031-0000-4000-8000-000000000002';

    -- Remove prior demo rows (idempotent)
    IF EXISTS (SELECT 1 FROM dbo.Frameworks WHERE Id = @DemoFrameworkId)
    BEGIN
        DELETE ki FROM dbo.KnowledgeItems ki
        INNER JOIN dbo.ComplianceNodes n ON n.Id = ki.NodeId
        WHERE n.FrameworkId = @DemoFrameworkId;

        DELETE rs FROM dbo.RequirementScores rs
        INNER JOIN dbo.ComplianceNodes n ON n.Id = rs.NodeId
        WHERE n.FrameworkId = @DemoFrameworkId;

        DELETE rd FROM dbo.RequirementDetails rd
        INNER JOIN dbo.ComplianceNodes n ON n.Id = rd.NodeId
        WHERE n.FrameworkId = @DemoFrameworkId;

        DELETE FROM dbo.ComplianceNodes WHERE FrameworkId = @DemoFrameworkId;
        DELETE FROM dbo.FrameworkClassifications WHERE FrameworkId = @DemoFrameworkId;
        DELETE FROM dbo.ScoringConfigs WHERE FrameworkId = @DemoFrameworkId;
        DELETE FROM dbo.Frameworks WHERE Id = @DemoFrameworkId;
    END;

    IF DB_ID(N'Compliance') IS NOT NULL
    BEGIN
        IF EXISTS (SELECT 1 FROM [Compliance].[dbo].[ComplianceFrameworks] WHERE Id = @DemoFrameworkId)
            DELETE FROM [Compliance].[dbo].[ComplianceFrameworks] WHERE Id = @DemoFrameworkId;

        INSERT INTO [Compliance].[dbo].[ComplianceFrameworks]
            (Id, PlatformId, Name, Authority, Version, IsActive, CreatedAt)
        VALUES
            (@DemoFrameworkId, @DemoCode, N'Onboarding Demo Framework', N'Demo Authority', N'1.0', 1, @Now);
    END;

    INSERT INTO dbo.Frameworks (Id, Code, Name, Version, Description, IsActive) VALUES
        (@DemoFrameworkId, @DemoCode, N'Onboarding Demo Framework', N'1.0', N'Demo questionnaire for onboarding wizard step 4', 1);

    INSERT INTO dbo.ScoringConfigs (Id, FrameworkId, FullyCompliantScore, PartialScore, NonCompliantScore, AggregationMethod, ScaleType) VALUES
        ('d1000002-0000-4000-8000-000000000001', @DemoFrameworkId, 5, 3, 1, N'WeightedAverage', N'NABH5Point');

    INSERT INTO dbo.FrameworkClassifications (Id, FrameworkId, Name, Weight, IsMandatory) VALUES
        (@DemoClsAac, @DemoFrameworkId, N'Access Assessment and Continuity of Care (AAC)', 1, 1),
        (@DemoClsPre, @DemoFrameworkId, N'Patient Rights and Education (PRE)', 1, 1);

    -- Chapters
    INSERT INTO dbo.ComplianceNodes (Id, FrameworkId, ParentId, Level, Code, Title, Description, NodeType, Sequence) VALUES
        (@DemoChapterAac, @DemoFrameworkId, NULL, 1, N'AAC', N'Access Assessment and Continuity of Care', N'Demo AAC chapter', N'Chapter', 1),
        (@DemoChapterPre, @DemoFrameworkId, NULL, 1, N'PRE', N'Patient Rights and Education', N'Demo PRE chapter', N'Chapter', 2);

    -- Standards
    INSERT INTO dbo.ComplianceNodes (Id, FrameworkId, ParentId, Level, Code, Title, Description, NodeType, Sequence) VALUES
        (@DemoStdAac1, @DemoFrameworkId, @DemoChapterAac, 2, N'AAC.1', N'Service definition and display', N'Demo standard AAC.1', N'Standard', 1),
        (@DemoStdAac2, @DemoFrameworkId, @DemoChapterAac, 2, N'AAC.2', N'Patient assessment on admission', N'Demo standard AAC.2', N'Standard', 2),
        (@DemoStdPre1, @DemoFrameworkId, @DemoChapterPre, 2, N'PRE.1', N'Patient rights promotion', N'Demo standard PRE.1', N'Standard', 1);

    INSERT INTO dbo.RequirementDetails (Id, NodeId, Intent, MeasurableElements, Classification, IsMandatory, ApplicableDepartments, RequiresDocumentation) VALUES
        ('d1000041-0000-4000-8000-000000000001', @DemoStdAac1, N'The organisation defines and displays healthcare services.', N'Policy and signage reviewed annually.', N'AAC', 1, N'Front Office', 1),
        ('d1000041-0000-4000-8000-000000000002', @DemoStdAac2, N'Patients are assessed on admission.', N'Assessment form completed within defined time.', N'AAC', 1, N'Clinical', 1),
        ('d1000041-0000-4000-8000-000000000003', @DemoStdPre1, N'Patient and family rights are protected and communicated.', N'Rights charter displayed and explained.', N'PRE', 1, N'Quality', 1);

    INSERT INTO dbo.RequirementScores (Id, NodeId, MaxScore, Weight, ClassificationId) VALUES
        ('d1000042-0000-4000-8000-000000000001', @DemoStdAac1, 5, 2.00, @DemoClsAac),
        ('d1000042-0000-4000-8000-000000000002', @DemoStdAac2, 5, 1.50, @DemoClsAac),
        ('d1000042-0000-4000-8000-000000000003', @DemoStdPre1, 5, 2.00, @DemoClsPre);

    -- Objective elements (one wizard question each)
    INSERT INTO dbo.ComplianceNodes (Id, FrameworkId, ParentId, Level, Code, Title, Description, NodeType, Sequence) VALUES
        ('d1000051-0000-4000-8000-000000000001', @DemoFrameworkId, @DemoStdAac1, 3, N'AAC.1.a', N'Services list is displayed at reception', N'Demo OE AAC.1.a', N'ObjectiveElement', 1),
        ('d1000051-0000-4000-8000-000000000002', @DemoFrameworkId, @DemoStdAac1, 3, N'AAC.1.b', N'Service list is reviewed annually', N'Demo OE AAC.1.b', N'ObjectiveElement', 2),
        ('d1000051-0000-4000-8000-000000000003', @DemoFrameworkId, @DemoStdAac2, 3, N'AAC.2.a', N'Initial assessment is documented on admission', N'Demo OE AAC.2.a', N'ObjectiveElement', 1),
        ('d1000051-0000-4000-8000-000000000004', @DemoFrameworkId, @DemoStdAac2, 3, N'AAC.2.b', N'Re-assessment triggers are defined', N'Demo OE AAC.2.b', N'ObjectiveElement', 2),
        ('d1000051-0000-4000-8000-000000000005', @DemoFrameworkId, @DemoStdPre1, 3, N'PRE.1.a', N'Rights charter is displayed', N'Demo OE PRE.1.a', N'ObjectiveElement', 1),
        ('d1000051-0000-4000-8000-000000000006', @DemoFrameworkId, @DemoStdPre1, 3, N'PRE.1.b', N'Staff explain rights to patients', N'Demo OE PRE.1.b', N'ObjectiveElement', 2);

    INSERT INTO dbo.KnowledgeItems
        (Id, NodeId, Explanation, WhyItMatters, HowToComply, EvidenceRequired, AuditChecklist, RiskLevel, ImpactLevel, Tags, Version, IsActive)
    VALUES
        ('d1000061-0000-4000-8000-000000000001', 'd1000051-0000-4000-8000-000000000001',
            N'Display services at reception.', N'Patients must know available services.', N'Maintain a current services board at main reception.', N'Signage photos', N'Check display monthly', N'Medium', N'High', N'DEMO;AAC.1.a', 1, 1),
        ('d1000061-0000-4000-8000-000000000002', 'd1000051-0000-4000-8000-000000000002',
            N'Annual service list review.', N'Prevents outdated service information.', N'Schedule annual review with medical superintendent sign-off.', N'Review minutes', N'Verify last review date', N'Medium', N'High', N'DEMO;AAC.1.b', 1, 1),
        ('d1000061-0000-4000-8000-000000000003', 'd1000051-0000-4000-8000-000000000003',
            N'Admission assessment documentation.', N'Supports safe care planning.', N'Use standard admission assessment form within 24 hours.', N'Sample forms', N'Audit 10 records', N'High', N'Critical', N'DEMO;AAC.2.a', 1, 1),
        ('d1000061-0000-4000-8000-000000000004', 'd1000051-0000-4000-8000-000000000004',
            N'Re-assessment criteria.', N'Identifies deteriorating patients.', N'Document triggers in nursing SOP.', N'SOP copy', N'Interview nurses', N'Medium', N'High', N'DEMO;AAC.2.b', 1, 1),
        ('d1000061-0000-4000-8000-000000000005', 'd1000051-0000-4000-8000-000000000005',
            N'Rights charter visibility.', N'Legal and ethical requirement.', N'Post charter in local language at waiting areas.', N'Photos', N'Site walk', N'Medium', N'High', N'DEMO;PRE.1.a', 1, 1),
        ('d1000061-0000-4000-8000-000000000006', 'd1000051-0000-4000-8000-000000000006',
            N'Rights communication.', N'Informed patients participate in care.', N'Train front-line staff on rights briefing script.', N'Training records', N'Observe 3 admissions', N'Medium', N'High', N'DEMO;PRE.1.b', 1, 1);

    PRINT N'Demo onboarding questionnaire master data inserted (framework code ONBDEMO, 6 questions).';
    PRINT CONCAT(N'Demo FrameworkId: ', CONVERT(NVARCHAR(36), @DemoFrameworkId));
    PRINT N'Select ONBDEMO on wizard step 3 to load these questions on step 4.';
END
ELSE
BEGIN
    PRINT N'@SeedMinimalDemo = 0 — skipped demo framework insert.';
    PRINT N'For NABH production questionnaire, run seed-nabh-hospital-6e.sql first (if not already applied).';
END;

COMMIT TRAN;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    THROW;
END CATCH;

-- ---------------------------------------------------------------------------
-- 3. Verification — questionnaire-ready counts per framework
-- ---------------------------------------------------------------------------
SELECT
    f.Id AS FrameworkId,
    f.Code AS FrameworkCode,
    f.Name AS FrameworkName,
    f.Version,
    f.IsActive,
    ChapterCount = SUM(CASE WHEN n.NodeType = N'Chapter' THEN 1 ELSE 0 END),
    StandardCount = SUM(CASE WHEN n.NodeType = N'Standard' THEN 1 ELSE 0 END),
    ObjectiveElementCount = SUM(CASE WHEN n.NodeType = N'ObjectiveElement' THEN 1 ELSE 0 END),
    WizardQuestionCount = SUM(CASE WHEN n.NodeType = N'ObjectiveElement' THEN 1 ELSE 0 END)
FROM dbo.Frameworks f
LEFT JOIN dbo.ComplianceNodes n ON n.FrameworkId = f.Id
GROUP BY f.Id, f.Code, f.Name, f.Version, f.IsActive
ORDER BY f.Name, f.Version;

SELECT TOP (5)
    f.Code AS FrameworkCode,
    ch.Title AS ChapterName,
    oe.Code AS ObjectiveElementCode,
    oe.Title AS ObjectiveElementTitle,
    rd.Intent AS StandardIntent,
    LEFT(ki.HowToComply, 80) AS HelpTextPreview
FROM dbo.Frameworks f
INNER JOIN dbo.ComplianceNodes oe ON oe.FrameworkId = f.Id AND oe.NodeType = N'ObjectiveElement'
INNER JOIN dbo.ComplianceNodes std ON std.Id = oe.ParentId AND std.NodeType = N'Standard'
INNER JOIN dbo.ComplianceNodes ch ON ch.Id = std.ParentId AND ch.NodeType = N'Chapter'
LEFT JOIN dbo.RequirementDetails rd ON rd.NodeId = std.Id
OUTER APPLY (
    SELECT TOP (1) ki.HowToComply
    FROM dbo.KnowledgeItems ki
    WHERE ki.NodeId = oe.Id AND ki.IsActive = 1
    ORDER BY ki.Version DESC
) ki
WHERE f.IsActive = 1
ORDER BY f.Code, ch.Sequence, oe.Sequence;
