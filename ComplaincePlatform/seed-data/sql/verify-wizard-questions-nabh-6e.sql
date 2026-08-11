/*
  PRAMAN — Wizard Questions Quality Verification
  Framework: NABH Full Accreditation (Hospital), 6th Edition
  FrameworkId: 0fe8a9c0-9ea0-4970-9097-d4f73d45dca7

  Run this script AFTER seed-wizard-questions-nabh-6e.sql to confirm:
    1. Coverage per chapter (expected vs actual count).
    2. Category distribution.
    3. Weight band distribution.
    4. Questions with empty QuestionText, HelpText, AssessmentGuidance, ExpectedEvidence.
    5. FK integrity: FrameworkId, ChapterId, StandardId, ObjectiveElementId.
    6. Duplicate (ObjectiveElementId, Version) pairs.
    7. Top OEs needing manual review (empty multiple fields).

  Expected targets:
    Total chapters         : 10
    Total standards        : 100
    Total questions (v1)   : 503
    Coverage               : 100%
    Missing OEs            : 0
    Duplicates             : 0
    Broken FKs             : 0

  Apply:
    sqlcmd -S "localhost\SQLEXPRESS" -d KnowledgeBase -E
           -i ./seed-data/sql/verify-wizard-questions-nabh-6e.sql
*/
SET NOCOUNT ON;

DECLARE @FrameworkId UNIQUEIDENTIFIER = '0fe8a9c0-9ea0-4970-9097-d4f73d45dca7';
DECLARE @Version     INT              = 1;

-- ============================================================================
-- A. Top-level summary
-- ============================================================================
PRINT N'=== A. TOP-LEVEL SUMMARY ===';

SELECT
    [Total WizardQuestions (v1, active)]
        = (SELECT COUNT(*) FROM dbo.WizardQuestions
           WHERE FrameworkId = @FrameworkId AND Version = @Version AND IsActive = 1),
    [Total OEs in framework]
        = (SELECT COUNT(*) FROM dbo.ComplianceNodes
           WHERE FrameworkId = @FrameworkId AND NodeType = N'ObjectiveElement'),
    [Coverage %]
        = CASE WHEN (SELECT COUNT(*) FROM dbo.ComplianceNodes
                     WHERE FrameworkId = @FrameworkId AND NodeType = N'ObjectiveElement') > 0
          THEN CAST(CAST(
                   CAST((SELECT COUNT(*) FROM dbo.WizardQuestions
                         WHERE FrameworkId = @FrameworkId AND Version = @Version AND IsActive = 1)
                        AS DECIMAL(10,2))
                   /
                   (SELECT COUNT(*) FROM dbo.ComplianceNodes
                    WHERE FrameworkId = @FrameworkId AND NodeType = N'ObjectiveElement')
                   * 100 AS DECIMAL(5,1)) AS NVARCHAR(8)) + N'%'
          ELSE N'N/A' END,
    [Duplicates]
        = (SELECT COUNT(*) FROM (
               SELECT ObjectiveElementId FROM dbo.WizardQuestions
               WHERE FrameworkId = @FrameworkId AND Version = @Version
               GROUP BY ObjectiveElementId HAVING COUNT(*) > 1) d),
    [Broken FK (OE)]
        = (SELECT COUNT(*) FROM dbo.WizardQuestions wq
           WHERE FrameworkId = @FrameworkId AND Version = @Version
             AND NOT EXISTS (SELECT 1 FROM dbo.ComplianceNodes n WHERE n.Id = wq.ObjectiveElementId)),
    [Broken FK (Chapter)]
        = (SELECT COUNT(*) FROM dbo.WizardQuestions wq
           WHERE FrameworkId = @FrameworkId AND Version = @Version
             AND NOT EXISTS (SELECT 1 FROM dbo.ComplianceNodes n WHERE n.Id = wq.ChapterId)),
    [Broken FK (Standard)]
        = (SELECT COUNT(*) FROM dbo.WizardQuestions wq
           WHERE FrameworkId = @FrameworkId AND Version = @Version
             AND NOT EXISTS (SELECT 1 FROM dbo.ComplianceNodes n WHERE n.Id = wq.StandardId)),
    [Broken FK (Framework)]
        = (SELECT COUNT(*) FROM dbo.WizardQuestions wq
           WHERE FrameworkId = @FrameworkId AND Version = @Version
             AND NOT EXISTS (SELECT 1 FROM dbo.Frameworks f WHERE f.Id = wq.FrameworkId));

-- ============================================================================
-- B. Coverage per chapter
-- ============================================================================
PRINT N'';
PRINT N'=== B. COVERAGE PER CHAPTER ===';

SELECT
    ch.Code                                     AS Chapter,
    ch.Title                                    AS ChapterTitle,
    ExpectedOEs  = COUNT(DISTINCT oe.Id),
    GeneratedWQs = SUM(CASE WHEN wq.Id IS NOT NULL THEN 1 ELSE 0 END),
    MissingOEs   = COUNT(DISTINCT oe.Id) -
                   SUM(CASE WHEN wq.Id IS NOT NULL THEN 1 ELSE 0 END),
    [Coverage %] = CAST(
        CAST(SUM(CASE WHEN wq.Id IS NOT NULL THEN 1 ELSE 0 END) AS DECIMAL(10,2)) /
        NULLIF(COUNT(DISTINCT oe.Id), 0) * 100 AS DECIMAL(5,1))
FROM dbo.ComplianceNodes ch
JOIN dbo.ComplianceNodes std
    ON std.ParentId = ch.Id AND std.NodeType = N'Standard'
JOIN dbo.ComplianceNodes oe
    ON oe.ParentId  = std.Id AND oe.NodeType  = N'ObjectiveElement'
LEFT JOIN dbo.WizardQuestions wq
    ON wq.ObjectiveElementId = oe.Id
    AND wq.Version  = @Version
    AND wq.IsActive = 1
WHERE ch.FrameworkId = @FrameworkId AND ch.NodeType = N'Chapter'
GROUP BY ch.Id, ch.Code, ch.Title, ch.Sequence
ORDER BY ch.Sequence;

-- ============================================================================
-- C. Category distribution
-- ============================================================================
PRINT N'';
PRINT N'=== C. CATEGORY DISTRIBUTION ===';

SELECT
    Category,
    QuestionCount = COUNT(*),
    [% of Total]  = CAST(
        CAST(COUNT(*) AS DECIMAL(10,2)) /
        NULLIF((SELECT COUNT(*) FROM dbo.WizardQuestions
                WHERE FrameworkId = @FrameworkId AND Version = @Version AND IsActive = 1), 0)
        * 100 AS DECIMAL(5,1))
FROM dbo.WizardQuestions
WHERE FrameworkId = @FrameworkId AND Version = @Version AND IsActive = 1
GROUP BY Category
ORDER BY QuestionCount DESC;

-- ============================================================================
-- D. Weight band distribution
-- ============================================================================
PRINT N'';
PRINT N'=== D. WEIGHT BAND DISTRIBUTION ===';

SELECT
    WeightBand,
    QuestionCount = COUNT(*),
    AvgWeight     = CAST(AVG(Weight) AS DECIMAL(18,4)),
    MinWeight     = CAST(MIN(Weight) AS DECIMAL(18,4)),
    MaxWeight     = CAST(MAX(Weight) AS DECIMAL(18,4))
FROM dbo.WizardQuestions
WHERE FrameworkId = @FrameworkId AND Version = @Version AND IsActive = 1
GROUP BY WeightBand
ORDER BY AVG(Weight) DESC;

-- ============================================================================
-- E. Risk category distribution
-- ============================================================================
PRINT N'';
PRINT N'=== E. RISK CATEGORY DISTRIBUTION ===';

SELECT
    RiskCategory,
    QuestionCount = COUNT(*)
FROM dbo.WizardQuestions
WHERE FrameworkId = @FrameworkId AND Version = @Version AND IsActive = 1
GROUP BY RiskCategory
ORDER BY QuestionCount DESC;

-- ============================================================================
-- F. Empty-field audit
-- ============================================================================
PRINT N'';
PRINT N'=== F. EMPTY-FIELD AUDIT ===';

SELECT
    [Empty QuestionText]
        = (SELECT COUNT(*) FROM dbo.WizardQuestions
           WHERE FrameworkId = @FrameworkId AND Version = @Version AND IsActive = 1
             AND LTRIM(RTRIM(QuestionText)) = N''),
    [Empty HelpText]
        = (SELECT COUNT(*) FROM dbo.WizardQuestions
           WHERE FrameworkId = @FrameworkId AND Version = @Version AND IsActive = 1
             AND LTRIM(RTRIM(HelpText)) = N''),
    [Empty AssessmentGuidance]
        = (SELECT COUNT(*) FROM dbo.WizardQuestions
           WHERE FrameworkId = @FrameworkId AND Version = @Version AND IsActive = 1
             AND LTRIM(RTRIM(AssessmentGuidance)) = N''),
    [Empty ExpectedEvidence]
        = (SELECT COUNT(*) FROM dbo.WizardQuestions
           WHERE FrameworkId = @FrameworkId AND Version = @Version AND IsActive = 1
             AND LTRIM(RTRIM(ExpectedEvidence)) = N''),
    [Empty QuestionTitle]
        = (SELECT COUNT(*) FROM dbo.WizardQuestions
           WHERE FrameworkId = @FrameworkId AND Version = @Version AND IsActive = 1
             AND LTRIM(RTRIM(QuestionTitle)) = N'');

-- ============================================================================
-- G. Sample questions (first 10 by DisplayOrder) — spot-check
-- ============================================================================
PRINT N'';
PRINT N'=== G. SAMPLE QUESTIONS (TOP 10) ===';

SELECT TOP (10)
    wq.DisplayOrder,
    wq.QuestionTitle,
    [QuestionText_Preview] = LEFT(wq.QuestionText, 120) + N'...',
    wq.Category,
    wq.WeightBand,
    wq.RiskCategory
FROM dbo.WizardQuestions wq
WHERE wq.FrameworkId = @FrameworkId
  AND wq.Version     = @Version
  AND wq.IsActive    = 1
ORDER BY wq.DisplayOrder;

-- ============================================================================
-- H. OEs that need attention (all content fields empty)
-- ============================================================================
PRINT N'';
PRINT N'=== H. OEs NEEDING MANUAL REVIEW (all content fields empty) ===';

SELECT
    wq.DisplayOrder,
    wq.QuestionTitle,
    oe.Code AS OECode,
    oe.Id   AS OEId
FROM dbo.WizardQuestions wq
JOIN dbo.ComplianceNodes oe ON oe.Id = wq.ObjectiveElementId
WHERE wq.FrameworkId = @FrameworkId
  AND wq.Version     = @Version
  AND wq.IsActive    = 1
  AND LTRIM(RTRIM(wq.HelpText))           = N''
  AND LTRIM(RTRIM(wq.AssessmentGuidance)) = N''
  AND LTRIM(RTRIM(wq.ExpectedEvidence))   = N''
ORDER BY wq.DisplayOrder;

-- ============================================================================
-- I. Missing OEs
-- ============================================================================
PRINT N'';
PRINT N'=== I. MISSING OEs (expected but not in WizardQuestions) ===';

SELECT
    oe.Code AS OECode,
    oe.Title AS OETitle,
    oe.Id   AS OEId,
    ch.Code AS Chapter
FROM dbo.ComplianceNodes oe
JOIN dbo.ComplianceNodes std ON std.Id = oe.ParentId AND std.NodeType = N'Standard'
JOIN dbo.ComplianceNodes ch  ON ch.Id  = std.ParentId AND ch.NodeType = N'Chapter'
WHERE oe.FrameworkId = @FrameworkId
  AND oe.NodeType    = N'ObjectiveElement'
  AND NOT EXISTS (
      SELECT 1 FROM dbo.WizardQuestions wq
      WHERE wq.ObjectiveElementId = oe.Id
        AND wq.Version = @Version AND wq.IsActive = 1)
ORDER BY ch.Sequence, oe.Code;

-- Final assertion hint
PRINT N'';
DECLARE @FinalTotal INT = (
    SELECT COUNT(*) FROM dbo.WizardQuestions
    WHERE FrameworkId = @FrameworkId AND Version = @Version AND IsActive = 1);
DECLARE @FinalOes INT = (
    SELECT COUNT(*) FROM dbo.ComplianceNodes
    WHERE FrameworkId = @FrameworkId AND NodeType = N'ObjectiveElement');

IF @FinalTotal = @FinalOes
    PRINT CONCAT(N'PASS — 100% coverage: ', @FinalTotal, N' questions generated for ', @FinalOes, N' OEs.');
ELSE
    PRINT CONCAT(N'FAIL — Coverage gap: ', @FinalTotal, N' questions generated, expected ', @FinalOes, N'.');
