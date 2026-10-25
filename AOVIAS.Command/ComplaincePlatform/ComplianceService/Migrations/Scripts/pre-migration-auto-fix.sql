-- Auto-fix script (safe-only) for unified finding migration
-- Review output from pre-migration-data-audit.sql before running.

-- Auto-fix invalid compliance status to NonCompliant.
UPDATE Findings
SET ComplianceStatus = 3
WHERE ComplianceStatus NOT IN (1,2,3,4);

-- Auto-fix blank clause reference and title.
UPDATE Findings
SET ClauseReference = COALESCE(NULLIF(LTRIM(RTRIM(ClauseReference)), ''), 'UNMAPPED'),
    Title = COALESCE(NULLIF(LTRIM(RTRIM(Title)), ''), 'Legacy finding (migration)')
WHERE (ClauseReference IS NULL OR LTRIM(RTRIM(ClauseReference)) = '')
   OR (Title IS NULL OR LTRIM(RTRIM(Title)) = '');
