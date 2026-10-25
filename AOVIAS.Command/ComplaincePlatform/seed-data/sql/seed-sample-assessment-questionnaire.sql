/*
  Sample assessment questionnaire data for KnowledgeBase (dev / demo / gap-analysis UI).

  Prerequisites:
    1. KnowledgeBase schema applied (EF migrations, including GapAnalysis tables).
    2. NABH framework seed applied: seed-nabh-hospital-6e.sql

  Configure before running:
    @FacilityId  — hospital / facility GUID used by your app login or Compliance DB.

  Fixed sample assessment (for manual testing):
    AssessmentId : c1000001-0000-4000-8000-000000000001
    Gap analysis : POST /api/v1.0/assessments/{AssessmentId}/gap-analysis
                   or open /app/gap-analysis/c1000001-0000-4000-8000-000000000001

  Apply (example):
    sqlcmd -S "localhost\SQLEXPRESS" -d KnowledgeBase -E -i ./seed-data/sql/seed-sample-assessment-questionnaire.sql

  Wizard answer mapping (see WizardAssessmentAnswerMapper.cs):
    yes      -> Score 5, Met,     HasActionPlan 0
    no       -> Score 1, NotMet,  HasActionPlan 1
    not_sure -> Score 3, Partial, HasActionPlan 1
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
BEGIN TRAN;

-- ---------------------------------------------------------------------------
-- Configuration
-- ---------------------------------------------------------------------------
DECLARE @FacilityId UNIQUEIDENTIFIER = 'f1000001-0000-4000-8000-000000000001'; -- replace with your hospital Id
DECLARE @FrameworkId UNIQUEIDENTIFIER = '0fe8a9c0-9ea0-4970-9097-d4f73d45dca7';
DECLARE @AssessmentId UNIQUEIDENTIFIER = 'c1000001-0000-4000-8000-000000000001';
DECLARE @EvaluatedAt DATETIME2 = SYSUTCDATETIME();

IF @FacilityId = 'f1000001-0000-4000-8000-000000000001'
BEGIN
    THROW 50030, 'Set @FacilityId to your hospital GUID (from Organization / Compliance) before running.', 1;
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Frameworks WHERE Id = @FrameworkId)
BEGIN
    THROW 50031, 'NABH framework not found. Run seed-nabh-hospital-6e.sql first.', 1;
END;

-- Objective element NodeIds from seed-nabh-hospital-6e.sql (AAC chapter sample set)
DECLARE @Oe_Aac1a UNIQUEIDENTIFIER = '3c0cb137-f429-407b-ba26-d7296569e981'; -- AAC.1.a
DECLARE @Oe_Aac1b UNIQUEIDENTIFIER = '21b3b378-1e4e-4404-b868-a564b2a3abb9'; -- AAC.1.b
DECLARE @Oe_Aac1c UNIQUEIDENTIFIER = '15d6d712-1c52-42ed-a8ff-0364676b31a2'; -- AAC.1.c
DECLARE @Oe_Aac1f UNIQUEIDENTIFIER = 'ead72e81-a61c-4e84-a777-2da1a1056eb6'; -- AAC.1.f
DECLARE @Oe_Aac10a UNIQUEIDENTIFIER = '7ae0962c-40fd-40ba-84bf-46815ba8c1c7'; -- AAC.10.a
DECLARE @Oe_Aac10b UNIQUEIDENTIFIER = '3bdff76b-ba63-421b-96a2-75aa5516710c'; -- AAC.10.b

IF (SELECT COUNT(*)
    FROM dbo.ComplianceNodes
    WHERE Id IN (@Oe_Aac1a, @Oe_Aac1b, @Oe_Aac1c, @Oe_Aac1f, @Oe_Aac10a, @Oe_Aac10b)) <> 6
BEGIN
    THROW 50032, 'One or more sample objective elements are missing. Run seed-nabh-hospital-6e.sql first.', 1;
END;

-- Idempotent re-run: remove prior sample assessment (cascades items and gap analysis)
IF EXISTS (SELECT 1 FROM dbo.Assessments WHERE Id = @AssessmentId)
    DELETE FROM dbo.Assessments WHERE Id = @AssessmentId;

-- ---------------------------------------------------------------------------
-- Assessment header (simulates onboarding wizard submission for one facility/day)
-- ---------------------------------------------------------------------------
INSERT INTO dbo.Assessments (Id, FacilityId, FrameworkId, Status, CreatedAt)
VALUES (@AssessmentId, @FacilityId, @FrameworkId, N'Open', @EvaluatedAt);

-- ---------------------------------------------------------------------------
-- Assessment items (simulated wizard yes / no / not_sure answers)
-- Remarks column stores the wizard question text.
-- ---------------------------------------------------------------------------
INSERT INTO dbo.AssessmentItems
    (Id, AssessmentId, NodeId, ComplianceStatus, Score, Remarks, HasActionPlan, EvaluatedAt)
VALUES
    ('c1000002-0000-4000-8000-000000000001', @AssessmentId, @Oe_Aac1a,
        N'Met', 5,
        N'Does your organisation define and display the healthcare services it provides?',
        0, @EvaluatedAt),

    ('c1000002-0000-4000-8000-000000000002', @AssessmentId, @Oe_Aac1b,
        N'NotMet', 1,
        N'Is the displayed list of services reviewed and updated at least annually?',
        1, @EvaluatedAt),

    ('c1000002-0000-4000-8000-000000000003', @AssessmentId, @Oe_Aac1c,
        N'Partial', 3,
        N'Are patients and visitors able to easily locate information about available services?',
        1, @EvaluatedAt),

    ('c1000002-0000-4000-8000-000000000004', @AssessmentId, @Oe_Aac1f,
        N'Met', 5,
        N'Is accountability assigned for maintaining service-definition documentation?',
        0, @EvaluatedAt),

    ('c1000002-0000-4000-8000-000000000005', @AssessmentId, @Oe_Aac10a,
        N'NotMet', 1,
        N'Is patient care planned and delivered in a coordinated, multi-disciplinary manner?',
        1, @EvaluatedAt),

    ('c1000002-0000-4000-8000-000000000006', @AssessmentId, @Oe_Aac10b,
        N'Partial', 3,
        N'Are handoffs between care teams documented and communicated effectively?',
        1, @EvaluatedAt);

COMMIT TRAN;

-- Verification
SELECT
    a.Id AS AssessmentId,
    a.FacilityId,
    a.FrameworkId,
    a.Status,
    a.CreatedAt,
    ItemCount = (SELECT COUNT(*) FROM dbo.AssessmentItems i WHERE i.AssessmentId = a.Id)
FROM dbo.Assessments a
WHERE a.Id = @AssessmentId;

SELECT
    n.Code,
    n.Title,
    i.Score,
    i.ComplianceStatus,
    i.HasActionPlan,
    LEFT(i.Remarks, 80) AS QuestionPreview
FROM dbo.AssessmentItems i
INNER JOIN dbo.ComplianceNodes n ON n.Id = i.NodeId
WHERE i.AssessmentId = @AssessmentId
ORDER BY n.Code;

PRINT N'Sample assessment seeded.';
PRINT N'AssessmentId: c1000001-0000-4000-8000-000000000001';
PRINT N'Gap analysis UI: /app/gap-analysis/c1000001-0000-4000-8000-000000000001';
PRINT N'Generate report: POST /api/v1.0/assessments/c1000001-0000-4000-8000-000000000001/gap-analysis';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    THROW;
END CATCH;
