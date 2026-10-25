/*
    2026-08-07_BackfillFindingTraceability.sql
    RC2S1-M6.1.1 — Historical Traceability Backfill

    Repairs findings created before the DEF-M6-001 fix, which left ComplianceJourneyId unset.
    Those findings are invisible to AuditReportService, whose query requires
    `f.ComplianceJourneyId.HasValue`, so the audit report under-reported the hospital's findings.

    ── WHAT THIS SCRIPT DOES ──────────────────────────────────────────────────────────────────
    Sets Findings.ComplianceJourneyId where it is NULL and the journey can be PROVEN, by the same
    two rules the runtime fix uses:

      Rule A (mapped finding)   ObjectiveElement -> ComplianceStandard -> ComplianceChapter
                                -> FrameworkId -> the tenant's journey for that framework.
      Rule B (unmapped finding) the tenant's journey, only when the tenant has exactly one.

    ── WHAT THIS SCRIPT DELIBERATELY DOES NOT DO ──────────────────────────────────────────────
    It does not touch AuditSessionId. Session ownership is NOT reconstructable for any historical
    finding in this database:

      * every AuditSession has ActualStartDate and ActualEndDate NULL, so no session has a real
        occurrence window to test a finding's DateRaised against;
      * the only windows recorded are PlannedStartDate/PlannedEndDate, which for the AOVIAS tenant
        fall in November 2026 — months AFTER the findings were raised in June-August 2026;
      * two sessions (IA-2026-016 and IA-2026-014) are simultaneously In Progress, so "the open
        session" is ambiguous even if timing were usable;
      * FindingAuditTrails records no session reference on any event.

    Assigning a session would therefore be a guess. The brief says never guess, so AuditSessionId
    is left NULL and the affected findings are reported as Ambiguous.

    ── MODES ──────────────────────────────────────────────────────────────────────────────────
      REPORT  inventory only; no transaction, no writes.
      DRYRUN  performs the update inside a transaction and ROLLS BACK; prints what would change.
      APPLY   performs the update and COMMITS.

    Idempotent: re-running after APPLY updates 0 rows, because the WHERE clause only ever matches
    ComplianceJourneyId IS NULL. Existing values are never overwritten.

    Auditable: every change is written to FindingTraceabilityBackfillLog with the old value, the
    new value, the rule applied, and the run mode.

    Usage:
      sqlcmd -S "localhost\SQLEXPRESS" -d Compliance -E -C -v Mode="REPORT" -i <this file>
      sqlcmd -S "localhost\SQLEXPRESS" -d Compliance -E -C -v Mode="DRYRUN" -i <this file>
      sqlcmd -S "localhost\SQLEXPRESS" -d Compliance -E -C -v Mode="APPLY"  -i <this file>
*/

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

/*
    Mode is supplied with sqlcmd -v and is REQUIRED.

    Do NOT add ":setvar Mode ..." here: an in-script :setvar OVERRIDES the -v value, which silently
    forces every run into that mode. That happened during this script's own development — a run
    invoked as APPLY executed as REPORT and reported success while changing nothing.
*/
DECLARE @Mode varchar(10) = UPPER('$(Mode)');
DECLARE @RunId uniqueidentifier = NEWID();
DECLARE @RunAt datetime2(3) = SYSUTCDATETIME();

IF @Mode NOT IN ('REPORT', 'DRYRUN', 'APPLY')
BEGIN
    RAISERROR('Mode must be REPORT, DRYRUN or APPLY. Supplied: %s', 16, 1, @Mode);
    RETURN;
END

PRINT '===========================================================';
PRINT ' Finding Traceability Backfill — mode: ' + @Mode;
PRINT ' Run id: ' + CONVERT(varchar(36), @RunId);
PRINT '===========================================================';

/* ── Audit log table ─────────────────────────────────────────────────────────────────────── */
IF OBJECT_ID('dbo.FindingTraceabilityBackfillLog', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.FindingTraceabilityBackfillLog
    (
        Id                  bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
        RunId               uniqueidentifier NOT NULL,
        RunAt               datetime2(3)     NOT NULL,
        Mode                varchar(10)      NOT NULL,
        FindingId           uniqueidentifier NOT NULL,
        FindingNumber       nvarchar(64)     NULL,
        TenantId            uniqueidentifier NOT NULL,
        Outcome             varchar(16)      NOT NULL,   -- Updated | Skipped | Ambiguous
        RuleApplied         varchar(32)      NULL,       -- OeFramework | SingleTenantJourney
        OldJourneyId        uniqueidentifier NULL,
        NewJourneyId        uniqueidentifier NULL,
        Notes               nvarchar(400)    NULL
    );
    PRINT 'Created dbo.FindingTraceabilityBackfillLog.';
END

/* ── Candidate resolution ────────────────────────────────────────────────────────────────── */
IF OBJECT_ID('tempdb..#Candidates') IS NOT NULL DROP TABLE #Candidates;

SELECT
    f.Id                AS FindingId,
    f.FindingNumber,
    f.TenantId,
    f.ObjectiveElementId,
    f.AuditSessionId,
    oeJourney.JourneyId AS OeJourneyId,
    single.JourneyId    AS SingleJourneyId,
    single.JourneyCount
INTO #Candidates
FROM dbo.Findings f

/* Rule A — framework implied by the finding's objective element */
OUTER APPLY (
    SELECT TOP 1 j.Id AS JourneyId
    FROM dbo.ObjectiveElements oe
    JOIN dbo.ComplianceStandards st ON st.Id = oe.ComplianceStandardId
    JOIN dbo.ComplianceChapters  ch ON ch.Id = st.ChapterId
    JOIN dbo.ComplianceJourneys  j  ON j.FrameworkId = ch.FrameworkId
                                   AND j.TenantId    = f.TenantId
    WHERE oe.Id = f.ObjectiveElementId
    ORDER BY j.UpdatedAt DESC
) AS oeJourney

/* Rule B — the tenant's journey, only when unambiguous */
OUTER APPLY (
    SELECT MIN(j2.Id) AS JourneyId, COUNT(*) AS JourneyCount
    FROM dbo.ComplianceJourneys j2
    WHERE j2.TenantId = f.TenantId
) AS single

WHERE f.ComplianceJourneyId IS NULL;

/* ── Classification ──────────────────────────────────────────────────────────────────────── */
IF OBJECT_ID('tempdb..#Plan') IS NOT NULL DROP TABLE #Plan;

SELECT
    c.*,
    CASE
        WHEN c.OeJourneyId IS NOT NULL                       THEN c.OeJourneyId
        WHEN c.JourneyCount = 1 AND c.SingleJourneyId IS NOT NULL THEN c.SingleJourneyId
        ELSE NULL
    END AS ResolvedJourneyId,
    CASE
        WHEN c.OeJourneyId IS NOT NULL                       THEN 'OeFramework'
        WHEN c.JourneyCount = 1 AND c.SingleJourneyId IS NOT NULL THEN 'SingleTenantJourney'
        ELSE NULL
    END AS RuleApplied,
    CASE
        WHEN c.OeJourneyId IS NOT NULL                       THEN 'Updated'
        WHEN c.JourneyCount = 1 AND c.SingleJourneyId IS NOT NULL THEN 'Updated'
        WHEN ISNULL(c.JourneyCount, 0) = 0                   THEN 'Skipped'
        ELSE 'Ambiguous'
    END AS Outcome
INTO #Plan
FROM #Candidates c;

PRINT '';
PRINT '--- Planned outcomes ---';
SELECT Outcome, RuleApplied, COUNT(*) AS Findings
FROM #Plan GROUP BY Outcome, RuleApplied ORDER BY Outcome, RuleApplied;

PRINT '';
PRINT '--- Detail ---';
SELECT FindingNumber, TenantId, Outcome, ISNULL(RuleApplied, '-') AS RuleUsed,
       ISNULL(CONVERT(varchar(36), ResolvedJourneyId), '-') AS NewJourneyId,
       CASE WHEN AuditSessionId IS NULL THEN 'session: left NULL (unprovable)' ELSE 'session: already set' END AS SessionNote
FROM #Plan ORDER BY TenantId, FindingNumber;

IF @Mode = 'REPORT'
BEGIN
    PRINT '';
    PRINT 'REPORT mode — no changes written.';
    RETURN;
END

/* ── Apply ───────────────────────────────────────────────────────────────────────────────── */
BEGIN TRY
    BEGIN TRANSACTION;

    UPDATE f
       SET f.ComplianceJourneyId = p.ResolvedJourneyId,
           f.LastUpdatedAt       = @RunAt
    FROM dbo.Findings f
    JOIN #Plan p ON p.FindingId = f.Id
    WHERE p.Outcome = 'Updated'
      AND p.ResolvedJourneyId IS NOT NULL
      AND f.ComplianceJourneyId IS NULL;   -- idempotency guard: never overwrite

    DECLARE @Updated int = @@ROWCOUNT;

    INSERT INTO dbo.FindingTraceabilityBackfillLog
        (RunId, RunAt, Mode, FindingId, FindingNumber, TenantId, Outcome, RuleApplied,
         OldJourneyId, NewJourneyId, Notes)
    SELECT @RunId, @RunAt, @Mode, p.FindingId, p.FindingNumber, p.TenantId, p.Outcome, p.RuleApplied,
           NULL, p.ResolvedJourneyId,
           CASE p.Outcome
               WHEN 'Updated'   THEN 'ComplianceJourneyId set; AuditSessionId left NULL (unprovable).'
               WHEN 'Ambiguous' THEN 'Tenant has multiple journeys and finding has no objective element.'
               ELSE 'Tenant has no compliance journey.'
           END
    FROM #Plan p;

    PRINT '';
    PRINT '--- Result ---';
    PRINT 'Updated   : ' + CONVERT(varchar(10), @Updated);
    SELECT Outcome, COUNT(*) AS Findings FROM #Plan GROUP BY Outcome ORDER BY Outcome;

    IF @Mode = 'DRYRUN'
    BEGIN
        ROLLBACK TRANSACTION;
        PRINT '';
        PRINT 'DRYRUN mode — transaction rolled back. No changes persisted.';
    END
    ELSE
    BEGIN
        COMMIT TRANSACTION;
        PRINT '';
        PRINT 'APPLY mode — committed.';
    END
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT 'FAILED — rolled back.';
    THROW;
END CATCH
GO
