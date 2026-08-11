/*
  PRAMAN — Production Wizard Questions Master Data
  Framework: NABH Full Accreditation (Hospital), 6th Edition
  FrameworkId: 0fe8a9c0-9ea0-4970-9097-d4f73d45dca7

  Scope: ALL 503 Objective Elements under the framework.
  Coverage target: 100% — no OE may be skipped.

  Data derivation (no manual text, no AI-generated content):
    QuestionTitle      = OE.Code + ': ' + OE.Title
    QuestionText       = OE.Title + ' — ' + Standard.RequirementDetail.Intent
    HelpText           = KnowledgeItem.HowToComply
                         + CHAR(10)+CHAR(10)+'Requirement: ' + RequirementDetail.MeasurableElements
    AssessmentGuidance = KnowledgeItem.AuditChecklist
    ExpectedEvidence   = KnowledgeItem.EvidenceRequired
    Category           = chapter-code mapped to governance taxonomy (CASE expression)
    Weight             = RequirementScore.Weight on parent Standard (fallback 1.00)
    WeightBand         = >= 2.5 → High | >= 1.5 → Medium | else Low
    RiskCategory       = KnowledgeItem.RiskLevel (fallback 'Medium')
    DisplayOrder       = ROW_NUMBER() ordered by chapter.Sequence, oe.Sequence

  Prerequisites:
    1. seed-nabh-hospital-6e.sql applied to KnowledgeBase.
    2. EF migration AddWizardQuestionsTable applied (dotnet ef database update).

  Idempotency:
    Uses WHERE NOT EXISTS on (ObjectiveElementId, Version = 1, IsActive = 1).
    Safe to re-run; will insert only rows that do not yet exist.

  Apply:
    sqlcmd -S "localhost\SQLEXPRESS" -d KnowledgeBase -E
           -i ./seed-data/sql/seed-wizard-questions-nabh-6e.sql

  Verify:
    sqlcmd -S "localhost\SQLEXPRESS" -d KnowledgeBase -E
           -i ./seed-data/sql/verify-wizard-questions-nabh-6e.sql
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @FrameworkId  UNIQUEIDENTIFIER = '0fe8a9c0-9ea0-4970-9097-d4f73d45dca7';
DECLARE @TargetVersion INT             = 1;
DECLARE @Now           DATETIME2       = SYSUTCDATETIME();

-- ============================================================================
-- PHASE 1 — Pre-flight checks
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.Frameworks WHERE Id = @FrameworkId)
    THROW 50040, 'NABH framework not found. Run seed-nabh-hospital-6e.sql first.', 1;

IF OBJECT_ID(N'dbo.WizardQuestions', N'U') IS NULL
    THROW 50041, 'WizardQuestions table not found. Apply EF migration AddWizardQuestionsTable first.', 1;

DECLARE @OeCount INT = (
    SELECT COUNT(*)
    FROM dbo.ComplianceNodes
    WHERE FrameworkId = @FrameworkId AND NodeType = N'ObjectiveElement');

IF @OeCount = 0
    THROW 50042, 'No ObjectiveElement nodes found for this framework. Seed the hierarchy first.', 1;

PRINT CONCAT(N'Pre-flight passed. ObjectiveElement count: ', @OeCount);

-- ============================================================================
-- PHASE 2 — Inventory report
-- ============================================================================
PRINT N'';
PRINT N'=== INVENTORY REPORT ===';

;WITH ChapterStats AS (
    SELECT
        ch.Id AS ChapterId,
        ch.Code AS ChapterCode,
        ch.Title AS ChapterTitle,
        ch.Sequence,
        StdCount = COUNT(DISTINCT std.Id),
        OeCount  = COUNT(DISTINCT oe.Id)
    FROM dbo.ComplianceNodes ch
    JOIN dbo.ComplianceNodes std ON std.ParentId = ch.Id  AND std.NodeType  = N'Standard'
    JOIN dbo.ComplianceNodes oe  ON oe.ParentId  = std.Id AND oe.NodeType   = N'ObjectiveElement'
    WHERE ch.FrameworkId = @FrameworkId
      AND ch.NodeType    = N'Chapter'
    GROUP BY ch.Id, ch.Code, ch.Title, ch.Sequence
)
SELECT
    ChapterCode,
    ChapterTitle,
    StdCount   AS [Standards],
    OeCount    AS [ObjectiveElements_WizardQuestions]
FROM ChapterStats
ORDER BY Sequence;

SELECT
    TotalChapters         = COUNT(DISTINCT ch.Id),
    TotalStandards        = COUNT(DISTINCT std.Id),
    TotalObjectiveElements = COUNT(DISTINCT oe.Id)
FROM dbo.ComplianceNodes ch
JOIN dbo.ComplianceNodes std ON std.ParentId = ch.Id  AND std.NodeType = N'Standard'
JOIN dbo.ComplianceNodes oe  ON oe.ParentId  = std.Id AND oe.NodeType  = N'ObjectiveElement'
WHERE ch.FrameworkId = @FrameworkId AND ch.NodeType = N'Chapter';

-- ============================================================================
-- PHASE 3 — Coverage matrix
-- ============================================================================
PRINT N'';
PRINT N'=== COVERAGE MATRIX CHECK ===';

DECLARE @OesMissingRd  INT;
DECLARE @OesMissingKi  INT;

SELECT @OesMissingRd = COUNT(*)
FROM dbo.ComplianceNodes oe
JOIN dbo.ComplianceNodes std ON std.Id = oe.ParentId AND std.NodeType = N'Standard'
WHERE oe.FrameworkId = @FrameworkId
  AND oe.NodeType    = N'ObjectiveElement'
  AND NOT EXISTS (SELECT 1 FROM dbo.RequirementDetails rd WHERE rd.NodeId = std.Id);

SELECT @OesMissingKi = COUNT(*)
FROM dbo.ComplianceNodes oe
WHERE oe.FrameworkId = @FrameworkId
  AND oe.NodeType    = N'ObjectiveElement'
  AND NOT EXISTS (
      SELECT 1 FROM dbo.KnowledgeItems ki
      WHERE ki.NodeId = oe.Id AND ki.IsActive = 1);

PRINT CONCAT(N'OEs whose parent Standard lacks RequirementDetails: ', @OesMissingRd);
PRINT CONCAT(N'OEs without an active KnowledgeItem: ', @OesMissingKi);

IF @OesMissingRd > 0
    PRINT N'WARNING: Some OEs will have truncated QuestionText (fallback to OE.Title only).';

IF @OesMissingKi > 0
    PRINT N'WARNING: Some OEs will have empty HelpText / AssessmentGuidance / ExpectedEvidence.';

-- ============================================================================
-- PHASE 4 — INSERT...SELECT (idempotent)
-- ============================================================================
PRINT N'';
PRINT N'=== GENERATING WIZARD QUESTIONS ===';

BEGIN TRY
BEGIN TRAN;

INSERT INTO dbo.WizardQuestions
(
    Id,
    FrameworkId,
    FrameworkVersion,
    ChapterId,
    StandardId,
    ObjectiveElementId,
    QuestionTitle,
    QuestionText,
    HelpText,
    AssessmentGuidance,
    ExpectedEvidence,
    Category,
    Weight,
    WeightBand,
    RiskCategory,
    DisplayOrder,
    Version,
    IsActive,
    CreatedBy,
    CreatedAt,
    UpdatedBy,
    UpdatedAt
)
SELECT
    NEWID()                        AS Id,
    oe.FrameworkId,
    f.Version                      AS FrameworkVersion,
    ch.Id                          AS ChapterId,
    std.Id                         AS StandardId,
    oe.Id                          AS ObjectiveElementId,

    -- QuestionTitle: "AAC.1.a: Objective Element A"
    LTRIM(RTRIM(oe.Code)) + N': ' + LTRIM(RTRIM(oe.Title))
                                   AS QuestionTitle,

    -- QuestionText: "Objective Element A — [standard intent]"
    CASE
        WHEN rd.Intent IS NOT NULL AND LTRIM(RTRIM(rd.Intent)) <> N''
        THEN LTRIM(RTRIM(oe.Title)) + N' — ' + LTRIM(RTRIM(rd.Intent))
        ELSE LTRIM(RTRIM(oe.Title))
    END                            AS QuestionText,

    -- HelpText: how-to-comply from knowledge item + measurable elements from standard
    ISNULL(NULLIF(LTRIM(RTRIM(ki.HowToComply)), N''), N'') +
    CASE
        WHEN LTRIM(RTRIM(ISNULL(rd.MeasurableElements, N''))) <> N''
        THEN NCHAR(13) + NCHAR(10) + NCHAR(13) + NCHAR(10) +
             N'Measurable elements: ' + LTRIM(RTRIM(rd.MeasurableElements))
        ELSE N''
    END                            AS HelpText,

    -- AssessmentGuidance: audit checklist from knowledge item
    ISNULL(NULLIF(LTRIM(RTRIM(ki.AuditChecklist)), N''), N'')
                                   AS AssessmentGuidance,

    -- ExpectedEvidence: evidence guidance from knowledge item
    ISNULL(NULLIF(LTRIM(RTRIM(ki.EvidenceRequired)), N''), N'')
                                   AS ExpectedEvidence,

    -- Category: chapter code mapped to governance taxonomy
    CASE ch.Code
        WHEN N'AAC' THEN N'Patient Care'
        WHEN N'COP' THEN N'Clinical Services'
        WHEN N'MOM' THEN N'Medication Management'
        WHEN N'PRE' THEN N'Patient Care'
        WHEN N'IPC' THEN N'Infection Control'
        WHEN N'PSQ' THEN N'Patient Safety'
        WHEN N'ROM' THEN N'Governance'
        WHEN N'FMS' THEN N'Facility Management'
        WHEN N'HRM' THEN N'Human Resources'
        WHEN N'IMS' THEN N'Information Management'
        ELSE ch.Code
    END                            AS Category,

    -- Weight: from parent standard's RequirementScore
    ISNULL(rs.Weight, 1.00)        AS Weight,

    -- WeightBand: derived from weight thresholds
    CASE
        WHEN ISNULL(rs.Weight, 1.00) >= 2.50 THEN N'High'
        WHEN ISNULL(rs.Weight, 1.00) >= 1.50 THEN N'Medium'
        ELSE N'Low'
    END                            AS WeightBand,

    -- RiskCategory: from knowledge item risk level
    ISNULL(NULLIF(LTRIM(RTRIM(ki.RiskLevel)), N''), N'Medium')
                                   AS RiskCategory,

    -- DisplayOrder: global sequential order within framework
    ROW_NUMBER() OVER (
        ORDER BY ch.Sequence, std.Sequence, oe.Sequence
    )                              AS DisplayOrder,

    @TargetVersion                 AS Version,
    1                              AS IsActive,
    N'system'                      AS CreatedBy,
    @Now                           AS CreatedAt,
    NULL                           AS UpdatedBy,
    NULL                           AS UpdatedAt

FROM dbo.ComplianceNodes oe
INNER JOIN dbo.ComplianceNodes std
    ON std.Id       = oe.ParentId
    AND std.NodeType = N'Standard'
INNER JOIN dbo.ComplianceNodes ch
    ON ch.Id        = std.ParentId
    AND ch.NodeType  = N'Chapter'
INNER JOIN dbo.Frameworks f
    ON f.Id         = oe.FrameworkId
LEFT JOIN dbo.RequirementDetails rd
    ON rd.NodeId    = std.Id
LEFT JOIN dbo.RequirementScores rs
    ON rs.NodeId    = std.Id
OUTER APPLY (
    SELECT TOP (1)
        ki.HowToComply,
        ki.AuditChecklist,
        ki.EvidenceRequired,
        ki.RiskLevel
    FROM dbo.KnowledgeItems ki
    WHERE ki.NodeId  = oe.Id
      AND ki.IsActive = 1
    ORDER BY ki.Version DESC
) ki

WHERE oe.FrameworkId = @FrameworkId
  AND oe.NodeType    = N'ObjectiveElement'
  -- Idempotency: skip OEs that already have an active v1 row
  AND NOT EXISTS (
      SELECT 1
      FROM dbo.WizardQuestions wq
      WHERE wq.ObjectiveElementId = oe.Id
        AND wq.Version            = @TargetVersion
        AND wq.IsActive           = 1
  );

DECLARE @Inserted INT = @@ROWCOUNT;

COMMIT TRAN;

PRINT CONCAT(N'Inserted: ', @Inserted, N' WizardQuestion rows.');
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    THROW;
END CATCH;

-- ============================================================================
-- PHASE 5 — Post-generation validation
-- ============================================================================
PRINT N'';
PRINT N'=== POST-GENERATION VALIDATION ===';

-- 5a. Total counts
DECLARE @TotalOes       INT = (SELECT COUNT(*) FROM dbo.ComplianceNodes WHERE FrameworkId = @FrameworkId AND NodeType = N'ObjectiveElement');
DECLARE @TotalQuestions INT = (SELECT COUNT(*) FROM dbo.WizardQuestions  WHERE FrameworkId = @FrameworkId AND Version = @TargetVersion AND IsActive = 1);

PRINT CONCAT(N'Total OEs in framework  : ', @TotalOes);
PRINT CONCAT(N'Total active v1 questions: ', @TotalQuestions);
PRINT CONCAT(N'Coverage                : ', CASE WHEN @TotalOes > 0 THEN CAST(CAST(CAST(@TotalQuestions AS DECIMAL(10,2)) / @TotalOes * 100 AS DECIMAL(5,1)) AS NVARCHAR(8)) ELSE N'0' END, N'%');

-- 5b. Missing OEs (OEs that have no active v1 WizardQuestion row)
SELECT
    oe.Code AS MissingOECode,
    oe.Id   AS MissingOEId
FROM dbo.ComplianceNodes oe
WHERE oe.FrameworkId = @FrameworkId
  AND oe.NodeType    = N'ObjectiveElement'
  AND NOT EXISTS (
      SELECT 1 FROM dbo.WizardQuestions wq
      WHERE wq.ObjectiveElementId = oe.Id
        AND wq.Version = @TargetVersion
        AND wq.IsActive = 1)
ORDER BY oe.Code;

IF NOT EXISTS (
    SELECT 1 FROM dbo.ComplianceNodes oe
    WHERE oe.FrameworkId = @FrameworkId AND oe.NodeType = N'ObjectiveElement'
    AND NOT EXISTS (
        SELECT 1 FROM dbo.WizardQuestions wq
        WHERE wq.ObjectiveElementId = oe.Id AND wq.Version = @TargetVersion AND wq.IsActive = 1))
    PRINT N'Missing OEs: 0 — COVERAGE 100%'
ELSE
    PRINT N'WARNING: One or more OEs are missing WizardQuestion rows (see result set above).';

-- 5c. Duplicate check on (ObjectiveElementId, Version)
DECLARE @Dupes INT = (
    SELECT COUNT(*) FROM (
        SELECT ObjectiveElementId
        FROM dbo.WizardQuestions
        WHERE FrameworkId = @FrameworkId AND Version = @TargetVersion
        GROUP BY ObjectiveElementId
        HAVING COUNT(*) > 1
    ) d);

PRINT CONCAT(N'Duplicate (OE, Version) pairs: ', @Dupes);

-- 5d. Broken FK check — ObjectiveElementId must exist in ComplianceNodes
DECLARE @BrokenFks INT = (
    SELECT COUNT(*) FROM dbo.WizardQuestions wq
    WHERE wq.FrameworkId = @FrameworkId AND wq.Version = @TargetVersion
      AND NOT EXISTS (SELECT 1 FROM dbo.ComplianceNodes n WHERE n.Id = wq.ObjectiveElementId));

PRINT CONCAT(N'Broken ObjectiveElementId FKs: ', @BrokenFks);

-- 5e. Questions with empty QuestionText (fallback used)
DECLARE @EmptyText INT = (
    SELECT COUNT(*) FROM dbo.WizardQuestions
    WHERE FrameworkId = @FrameworkId AND Version = @TargetVersion
      AND LTRIM(RTRIM(QuestionText)) = N'');

PRINT CONCAT(N'Questions with empty QuestionText: ', @EmptyText);

-- 5f. Final summary table
SELECT
    ch.Code AS Chapter,
    ch.Title AS ChapterTitle,
    Expected = COUNT(DISTINCT oe.Id),
    Generated = SUM(CASE WHEN wq.Id IS NOT NULL THEN 1 ELSE 0 END),
    Coverage = CASE WHEN COUNT(DISTINCT oe.Id) > 0
                    THEN CAST(CAST(SUM(CASE WHEN wq.Id IS NOT NULL THEN 1 ELSE 0 END) AS DECIMAL(10,2)) /
                              COUNT(DISTINCT oe.Id) * 100 AS NVARCHAR(10)) + N'%'
                    ELSE N'0%' END
FROM dbo.ComplianceNodes ch
JOIN dbo.ComplianceNodes std ON std.ParentId = ch.Id  AND std.NodeType = N'Standard'
JOIN dbo.ComplianceNodes oe  ON oe.ParentId  = std.Id AND oe.NodeType  = N'ObjectiveElement'
LEFT JOIN dbo.WizardQuestions wq
    ON wq.ObjectiveElementId = oe.Id
    AND wq.Version = @TargetVersion
    AND wq.IsActive = 1
WHERE ch.FrameworkId = @FrameworkId AND ch.NodeType = N'Chapter'
GROUP BY ch.Id, ch.Code, ch.Title, ch.Sequence
ORDER BY ch.Sequence;

PRINT N'';
PRINT N'Seed complete.';
PRINT N'Run verify-wizard-questions-nabh-6e.sql for a full quality report.';
