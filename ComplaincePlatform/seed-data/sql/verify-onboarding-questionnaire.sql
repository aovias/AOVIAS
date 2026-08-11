/*
  Verify KnowledgeBase data required for onboarding wizard step 4 (AI Compliance Assessment).
  Run against KnowledgeBase after seed-nabh-hospital-6e.sql and/or seed-onboarding-questionnaire-master.sql.
*/
SET NOCOUNT ON;

PRINT N'=== Framework code alignment (KB Code should match Compliance PlatformId) ===';
IF DB_ID(N'Compliance') IS NULL
BEGIN
    PRINT N'Compliance database not found — skipping cross-database alignment check.';
END
ELSE
BEGIN
    SELECT
        kb.Id AS KnowledgeBaseFrameworkId,
        kb.Code AS KnowledgeBaseCode,
        kb.Name AS KnowledgeBaseName,
        kb.Version AS KnowledgeBaseVersion,
        cf.Id AS ComplianceFrameworkId,
        cf.PlatformId AS CompliancePlatformId,
        AlignmentStatus = CASE
            WHEN cf.Id IS NULL THEN N'UNMAPPED — no Compliance framework with same Name+Version'
            WHEN kb.Code = cf.PlatformId THEN N'OK'
            ELSE N'MISMATCH — run seed-onboarding-questionnaire-master.sql (@SyncFrameworkCodes = 1)'
        END
    FROM dbo.Frameworks kb
    LEFT JOIN [Compliance].[dbo].[ComplianceFrameworks] cf
        ON LOWER(LTRIM(RTRIM(kb.Name))) = LOWER(LTRIM(RTRIM(cf.Name)))
       AND LOWER(LTRIM(RTRIM(kb.Version))) = LOWER(LTRIM(RTRIM(cf.Version)))
    WHERE kb.IsActive = 1
    ORDER BY kb.Name, kb.Version;
END;

PRINT N'=== Questionnaire master data counts (one wizard question per ObjectiveElement) ===';
SELECT
    f.Code,
    f.Name,
    f.Version,
    Chapters = SUM(CASE WHEN n.NodeType = N'Chapter' THEN 1 ELSE 0 END),
    Standards = SUM(CASE WHEN n.NodeType = N'Standard' THEN 1 ELSE 0 END),
    ObjectiveElements = SUM(CASE WHEN n.NodeType = N'ObjectiveElement' THEN 1 ELSE 0 END),
    StandardsWithRequirementDetail = (
        SELECT COUNT(DISTINCT rd.NodeId)
        FROM dbo.RequirementDetails rd
        INNER JOIN dbo.ComplianceNodes s ON s.Id = rd.NodeId AND s.FrameworkId = f.Id AND s.NodeType = N'Standard'
    ),
    OEsWithKnowledgeItem = (
        SELECT COUNT(DISTINCT ki.NodeId)
        FROM dbo.KnowledgeItems ki
        INNER JOIN dbo.ComplianceNodes o ON o.Id = ki.NodeId AND o.FrameworkId = f.Id AND o.NodeType = N'ObjectiveElement'
        WHERE ki.IsActive = 1
    )
FROM dbo.Frameworks f
LEFT JOIN dbo.ComplianceNodes n ON n.FrameworkId = f.Id
WHERE f.IsActive = 1
GROUP BY f.Id, f.Code, f.Name, f.Version
ORDER BY f.Name, f.Version;

PRINT N'=== Orphan objective elements (missing Chapter or Standard ancestor — skipped by generator) ===';
;WITH Oes AS (
    SELECT oe.Id, oe.FrameworkId, oe.Code, oe.ParentId
    FROM dbo.ComplianceNodes oe
    WHERE oe.NodeType = N'ObjectiveElement'
)
SELECT oe.Code, oe.Id, oe.FrameworkId
FROM Oes oe
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.ComplianceNodes std
    INNER JOIN dbo.ComplianceNodes ch ON ch.Id = std.ParentId AND ch.NodeType = N'Chapter'
    WHERE std.Id = oe.ParentId AND std.NodeType = N'Standard'
);
