-- =============================================================================

-- classify-wizard-questions-nabh-6e.sql

-- Classifies all WizardQuestions rows into maturity levels (Core / Commitment /

-- Achievement / Excellence), sets Weight, IsMandatory, IsCritical,

-- AssessmentPhase, DisplayPriority, and AssessmentJourneyOrder.

--

-- Strategy (v2):

--   Proportional chapter-based split aligned with NABH 6E domain bands and CMI

--   weights (~40/30/20/10). Keyword rules only adjust IsMandatory/IsCritical.

--   Previous v1 keyword-first ordering assigned all 503 rows to Core+Commitment,

--   leaving Achievement and Excellence at zero.

--

-- Safe to re-run (idempotent UPDATE).

-- Target DB : KnowledgeBase

-- =============================================================================



SET NOCOUNT ON;

PRINT '================================================================';

PRINT 'classify-wizard-questions-nabh-6e.sql  —  START (v2 chapter split)';

PRINT CONVERT(VARCHAR, GETUTCDATE(), 120);

PRINT '================================================================';



DECLARE @TotalRows INT = (SELECT COUNT(*) FROM [dbo].[WizardQuestions]);

PRINT CONCAT('Total WizardQuestions rows: ', @TotalRows);



IF @TotalRows = 0

BEGIN

    PRINT 'ERROR: No rows found. Run seed-wizard-questions-nabh-6e.sql first.';

    RETURN;

END



-- =============================================================================

-- STEP 1 — Reset to safe defaults (idempotency)

-- =============================================================================

UPDATE [dbo].[WizardQuestions]

SET

    AssessmentMaturityLevel = '',

    AssessmentPhase         = 'Onboarding',

    IsMandatory             = 0,

    IsCritical              = 0,

    UnlockThreshold         = NULL,

    DisplayPriority         = 0,

    AssessmentJourneyOrder  = 0,

    UpdatedAt               = GETUTCDATE(),

    UpdatedBy               = 'classify-script-v2';



PRINT 'Defaults reset.';



-- =============================================================================

-- STEP 2 — Chapter-based proportional maturity assignment

--   Percentages per NABH 6E chapter (must sum to 100 per chapter).

--   Row order: DisplayOrder within chapter (deterministic).

-- =============================================================================

;WITH ChapterRatios AS (

    SELECT Code, CorePct, CommitPct, AchievePct, ExcelPct

    FROM (VALUES

        (N'AAC', 35, 45, 15,  5),

        (N'COP', 40, 30, 25,  5),

        (N'PRE',100,  0,  0,  0),

        (N'IPC',100,  0,  0,  0),

        (N'MOM',100,  0,  0,  0),

        (N'PSQ',  0,  0, 70, 30),

        (N'ROM',  0,100,  0,  0),

        (N'FMS',  0,100,  0,  0),

        (N'HRM',  0,100,  0,  0),

        (N'IMS',  0, 40, 40, 20)

    ) AS v(Code, CorePct, CommitPct, AchievePct, ExcelPct)

),

Ranked AS (

    SELECT

        wq.Id,

        ch.Code AS ChapterCode,

        ROW_NUMBER() OVER (

            PARTITION BY wq.FrameworkId, ch.Code

            ORDER BY wq.DisplayOrder, wq.QuestionTitle

        ) AS Rn,

        COUNT(*) OVER (

            PARTITION BY wq.FrameworkId, ch.Code

        ) AS Cnt,

        cr.CorePct,

        cr.CommitPct,

        cr.AchievePct,

        cr.ExcelPct

    FROM [dbo].[WizardQuestions] wq

    INNER JOIN [dbo].[ComplianceNodes] ch ON ch.Id = wq.ChapterId

    INNER JOIN ChapterRatios cr ON cr.Code = ch.Code

),

Cutoffs AS (

    SELECT

        Id,

        ChapterCode,

        CASE

            WHEN Rn <= CAST(ROUND(Cnt * CorePct / 100.0, 0) AS INT) THEN N'Core'

            WHEN Rn <= CAST(ROUND(Cnt * (CorePct + CommitPct) / 100.0, 0) AS INT) THEN N'Commitment'

            WHEN Rn <= CAST(ROUND(Cnt * (CorePct + CommitPct + AchievePct) / 100.0, 0) AS INT) THEN N'Achievement'

            ELSE N'Excellence'

        END AS MaturityLevel

    FROM Ranked

)

UPDATE wq

SET

    wq.AssessmentMaturityLevel = c.MaturityLevel,

    wq.Weight = CASE c.MaturityLevel

        WHEN N'Core'        THEN 10

        WHEN N'Commitment'  THEN 7

        WHEN N'Achievement' THEN 5

        WHEN N'Excellence'  THEN 3

        ELSE 5

    END,

    wq.AssessmentPhase = CASE c.MaturityLevel

        WHEN N'Core' THEN N'Onboarding'

        ELSE N'Full'

    END,

    wq.UpdatedAt = GETUTCDATE(),

    wq.UpdatedBy = N'classify-script-v2'

FROM [dbo].[WizardQuestions] wq

INNER JOIN Cutoffs c ON c.Id = wq.Id;



PRINT 'Chapter-based maturity levels assigned.';



-- =============================================================================

-- STEP 3 — Patient-safety keywords → IsMandatory / IsCritical (Core band only)

-- =============================================================================

UPDATE [dbo].[WizardQuestions]

SET

    IsMandatory = 1,

    IsCritical  = 1,

    UpdatedAt   = GETUTCDATE(),

    UpdatedBy   = N'classify-script-v2'

WHERE AssessmentMaturityLevel = N'Core'

  AND (

        QuestionText LIKE '%patient identif%'

     OR QuestionText LIKE '%medication safety%'

     OR QuestionText LIKE '%hand hygiene%'

     OR QuestionText LIKE '%fire safety%'

     OR QuestionText LIKE '%consent%'

     OR QuestionText LIKE '%incident%'

     OR QuestionText LIKE '%emergency%'

     OR HelpText     LIKE '%patient identif%'

     OR HelpText     LIKE '%medication safety%'

     OR HelpText     LIKE '%hand hygiene%'

     OR HelpText     LIKE '%fire safety%'

     OR HelpText     LIKE '%consent%'

     OR HelpText     LIKE '%incident%'

     OR HelpText     LIKE '%emergency%'

  );



-- =============================================================================

-- STEP 4 — DisplayPriority within each maturity level

-- =============================================================================

;WITH Ranked AS (

    SELECT Id,

           ROW_NUMBER() OVER (

               PARTITION BY FrameworkId, AssessmentMaturityLevel

               ORDER BY IsMandatory DESC, Weight DESC, DisplayOrder ASC

           ) AS Rn

    FROM [dbo].[WizardQuestions]

)

UPDATE q

SET q.DisplayPriority = r.Rn

FROM [dbo].[WizardQuestions] q

INNER JOIN Ranked r ON q.Id = r.Id;



-- =============================================================================

-- STEP 5 — AssessmentJourneyOrder (global cross-maturity ordering)

-- =============================================================================

;WITH LevelOrder AS (

    SELECT Id,

           ROW_NUMBER() OVER (

               ORDER BY

                   CASE AssessmentMaturityLevel

                       WHEN N'Core'        THEN 1

                       WHEN N'Commitment'  THEN 2

                       WHEN N'Achievement' THEN 3

                       WHEN N'Excellence'  THEN 4

                       ELSE 5

                   END,

                   DisplayPriority ASC

           ) AS JourneyOrder

    FROM [dbo].[WizardQuestions]

)

UPDATE q

SET q.AssessmentJourneyOrder = lo.JourneyOrder

FROM [dbo].[WizardQuestions] q

INNER JOIN LevelOrder lo ON q.Id = lo.Id;



PRINT 'DisplayPriority and AssessmentJourneyOrder assigned.';



-- =============================================================================

-- STEP 6 — Classification report

-- =============================================================================

PRINT '';

PRINT '================================================================';

PRINT 'CLASSIFICATION REPORT';

PRINT '================================================================';



SELECT

    AssessmentMaturityLevel AS [Level],

    COUNT(*)                AS [Total],

    SUM(CASE WHEN IsMandatory = 1 THEN 1 ELSE 0 END) AS [Mandatory],

    SUM(CASE WHEN IsCritical  = 1 THEN 1 ELSE 0 END) AS [Critical]

FROM [dbo].[WizardQuestions]

GROUP BY AssessmentMaturityLevel

ORDER BY

    CASE AssessmentMaturityLevel

        WHEN N'Core'        THEN 1

        WHEN N'Commitment'  THEN 2

        WHEN N'Achievement' THEN 3

        WHEN N'Excellence'  THEN 4

        ELSE 5

    END;



SELECT

    cn.Code             AS [Chapter],

    wq.AssessmentMaturityLevel AS [Level],

    COUNT(*)            AS [Questions]

FROM [dbo].[WizardQuestions] wq

INNER JOIN [dbo].[ComplianceNodes] cn ON cn.Id = wq.ChapterId

GROUP BY cn.Code, wq.AssessmentMaturityLevel

ORDER BY cn.Code,

    CASE wq.AssessmentMaturityLevel

        WHEN N'Core'        THEN 1

        WHEN N'Commitment'  THEN 2

        WHEN N'Achievement' THEN 3

        WHEN N'Excellence'  THEN 4

        ELSE 5

    END;



-- =============================================================================

-- STEP 7 — Validation

-- =============================================================================

DECLARE @NullRows INT =

    (SELECT COUNT(*) FROM [dbo].[WizardQuestions]

     WHERE AssessmentMaturityLevel IS NULL OR AssessmentMaturityLevel = N'');



DECLARE @AchieveCount INT = (SELECT COUNT(*) FROM [dbo].[WizardQuestions] WHERE AssessmentMaturityLevel = N'Achievement');

DECLARE @ExcelCount  INT = (SELECT COUNT(*) FROM [dbo].[WizardQuestions] WHERE AssessmentMaturityLevel = N'Excellence');



IF @NullRows > 0

BEGIN

    PRINT CONCAT(N'FAIL — ', CAST(@NullRows AS VARCHAR), N' rows have no AssessmentMaturityLevel!');

    THROW 51001, 'Classification incomplete: NULL maturity rows found.', 1;

END



IF @AchieveCount = 0 OR @ExcelCount = 0

BEGIN

    PRINT CONCAT(N'FAIL — Achievement=', @AchieveCount, N', Excellence=', @ExcelCount, N' (both must be > 0).');

    THROW 51002, 'Classification incomplete: Achievement or Excellence band is empty.', 1;

END



PRINT CONCAT(N'PASS — All ', @TotalRows, N' rows classified across four maturity bands.');

PRINT '================================================================';

PRINT 'classify-wizard-questions-nabh-6e.sql  —  COMPLETE';

PRINT CONVERT(VARCHAR, GETUTCDATE(), 120);

PRINT '================================================================';

