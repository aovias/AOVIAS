SET NOCOUNT ON;

/*
  Canonical framework inventory and deterministic KB->Compliance mapping precheck.
  ComplianceService is the single source of truth for framework identities.
*/

IF DB_ID(N'Compliance') IS NULL
BEGIN
    THROW 50010, 'Compliance database not found. Cannot build canonical framework mapping.', 1;
END;

IF DB_ID(N'KnowledgeBase') IS NULL
BEGIN
    THROW 50011, 'KnowledgeBase database not found. Cannot build canonical framework mapping.', 1;
END;

;WITH ComplianceCanonical AS (
    SELECT
        cf.Id AS ComplianceFrameworkId,
        LTRIM(RTRIM(cf.Name)) AS Name,
        LTRIM(RTRIM(cf.Version)) AS Version,
        LTRIM(RTRIM(ISNULL(cf.PlatformId, N''))) AS CodeOrPlatformId,
        cf.IsActive
    FROM [Compliance].[dbo].[ComplianceFrameworks] cf
),
KnowledgeBaseFrameworks AS (
    SELECT
        f.Id AS KnowledgeBaseFrameworkId,
        LTRIM(RTRIM(f.Name)) AS Name,
        LTRIM(RTRIM(f.Version)) AS Version,
        LTRIM(RTRIM(ISNULL(f.Code, N''))) AS Code,
        f.IsActive
    FROM [KnowledgeBase].[dbo].[Frameworks] f
),
DeterministicMap AS (
    SELECT
        kb.KnowledgeBaseFrameworkId,
        kb.Name AS KnowledgeBaseName,
        kb.Version AS KnowledgeBaseVersion,
        kb.Code AS KnowledgeBaseCode,
        c.ComplianceFrameworkId,
        c.Name AS ComplianceName,
        c.Version AS ComplianceVersion,
        c.CodeOrPlatformId AS ComplianceCodeOrPlatformId
    FROM KnowledgeBaseFrameworks kb
    INNER JOIN ComplianceCanonical c
        ON LOWER(kb.Name) = LOWER(c.Name)
       AND LOWER(kb.Version) = LOWER(c.Version)
)
SELECT
    m.KnowledgeBaseFrameworkId,
    m.ComplianceFrameworkId,
    m.KnowledgeBaseName,
    m.KnowledgeBaseVersion,
    m.KnowledgeBaseCode,
    m.ComplianceCodeOrPlatformId
FROM DeterministicMap m
ORDER BY m.KnowledgeBaseName, m.KnowledgeBaseVersion;

-- Unmapped KB frameworks should block cutover.
SELECT
    kb.KnowledgeBaseFrameworkId,
    kb.Name,
    kb.Version,
    kb.Code
FROM (
    SELECT
        f.Id AS KnowledgeBaseFrameworkId,
        LTRIM(RTRIM(f.Name)) AS Name,
        LTRIM(RTRIM(f.Version)) AS Version,
        LTRIM(RTRIM(ISNULL(f.Code, N''))) AS Code
    FROM [KnowledgeBase].[dbo].[Frameworks] f
) kb
LEFT JOIN [Compliance].[dbo].[ComplianceFrameworks] cf
    ON LOWER(kb.Name) = LOWER(LTRIM(RTRIM(cf.Name)))
   AND LOWER(kb.Version) = LOWER(LTRIM(RTRIM(cf.Version)))
WHERE cf.Id IS NULL
ORDER BY kb.Name, kb.Version;
