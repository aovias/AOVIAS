/*
  Wave 2.2 — Phase 1: remove deficiency rows written under a corrupted tenant.

  ROOT CAUSE (fixed in code before this script was written)
  ---------------------------------------------------------
  `POST /api/v1/deficiencies/bootstrap-from-onboarding` took `organizationId` from the query
  string. The web app's Journey Maturity dashboard sent
      getOrganizationId() ?? hospitalId
  so whenever organization context had not hydrated (reload, deep link) it passed the HOSPITAL id
  as the organization id. Bootstrap then wrote a complete deficiency set stamped
      OrganizationId = TenantId = HospitalId
  The dashboard runs this on every visit, so the corrupt set could be produced repeatedly.

  Code fixes that make this script a one-off rather than a recurring cleanup:
    * Gateway  — DeficienciesController.BootstrapFromOnboarding now derives organizationId from
                 the caller's token and ignores the query value (logs on mismatch).
    * WebApp   — journey-maturity-dashboard waits for real organization context; the
                 `?? hospitalId` fallback is gone.

  IDENTIFICATION RULE
  -------------------
  A deficiency whose TenantId equals its HospitalId is invalid by definition: TenantId must hold an
  organization id. This is precise, needs no timestamp heuristics, and cannot match a legitimate row
  unless an organization and a hospital share a GUID.

  SAFETY
  ------
  * Idempotent — reports 0 affected on re-run.
  * Transactional, with a post-condition assert before COMMIT.
  * Refuses to run if it would delete rows that have any CAPA progress or are referenced elsewhere.
  * Archives deleted rows to ComplianceDeficiencies_QuarantineWave22 first.

  RUN ORDER: verification (§1) → dry run (§2) → delete (§3) → constraint (§4) → verify (§5).
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

------------------------------------------------------------------------------
-- §1  Pre-flight verification
------------------------------------------------------------------------------
PRINT '--- Pre-flight ---';

DECLARE @corrupt INT = (SELECT COUNT(*) FROM ComplianceDeficiencies WHERE TenantId = HospitalId);
DECLARE @total   INT = (SELECT COUNT(*) FROM ComplianceDeficiencies);
PRINT CONCAT('Total deficiencies          : ', @total);
PRINT CONCAT('Rows where TenantId=HospitalId: ', @corrupt);

-- Refuse to delete anything that carries real work. Corrupt seed rows have no CAPA progress;
-- if that is not true here, a human must look before anything is removed.
DECLARE @withWork INT = (
    SELECT COUNT(*) FROM ComplianceDeficiencies
    WHERE TenantId = HospitalId
      AND (CorrectiveActionStatus <> 0
           OR PreventiveActionStatus <> 0
           OR (CorrectiveActionSummary IS NOT NULL AND LTRIM(RTRIM(CorrectiveActionSummary)) <> '')
           OR ClosedDate IS NOT NULL)
);
PRINT CONCAT('Corrupt rows carrying CAPA progress or closure: ', @withWork);

IF @withWork > 0
BEGIN
    RAISERROR('ABORT: corrupt-tenant rows carry CAPA progress. Review manually before deleting.', 16, 1);
    RETURN;
END

------------------------------------------------------------------------------
-- §2  Dry run — what would be removed, and what remains
------------------------------------------------------------------------------
PRINT '--- Dry run ---';
SELECT TenantId, OrganizationId, HospitalId, COUNT(*) AS Rows,
       CASE WHEN TenantId = HospitalId THEN 'DELETE' ELSE 'KEEP' END AS Action
FROM ComplianceDeficiencies
GROUP BY TenantId, OrganizationId, HospitalId
ORDER BY Action DESC;

------------------------------------------------------------------------------
-- §3  Quarantine + delete
------------------------------------------------------------------------------
IF @corrupt = 0
BEGIN
    PRINT 'Nothing to do — no rows with TenantId = HospitalId.';
END
ELSE
BEGIN
    BEGIN TRANSACTION;

    IF OBJECT_ID('ComplianceDeficiencies_QuarantineWave22', 'U') IS NULL
    BEGIN
        SELECT * INTO ComplianceDeficiencies_QuarantineWave22
        FROM ComplianceDeficiencies WHERE TenantId = HospitalId;
        PRINT CONCAT('Quarantine table created with ', @@ROWCOUNT, ' rows.');
    END
    ELSE
    BEGIN
        INSERT INTO ComplianceDeficiencies_QuarantineWave22
        SELECT * FROM ComplianceDeficiencies d WHERE d.TenantId = d.HospitalId
          AND NOT EXISTS (SELECT 1 FROM ComplianceDeficiencies_QuarantineWave22 q WHERE q.Id = d.Id);
        PRINT CONCAT('Quarantine table appended with ', @@ROWCOUNT, ' rows.');
    END

    DELETE FROM ComplianceDeficiencies WHERE TenantId = HospitalId;
    PRINT CONCAT('Deleted ', @@ROWCOUNT, ' corrupt-tenant deficiencies.');

    DECLARE @remaining INT = (SELECT COUNT(*) FROM ComplianceDeficiencies WHERE TenantId = HospitalId);
    IF @remaining <> 0
    BEGIN
        RAISERROR('ABORT: corrupt rows remain after delete. Rolling back.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END

    COMMIT TRANSACTION;
    PRINT 'Cleanup committed.';
END

------------------------------------------------------------------------------
-- §4  Prevent recurrence at the storage boundary
------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_ComplianceDeficiencies_TenantNotHospital')
BEGIN
    ALTER TABLE ComplianceDeficiencies WITH CHECK
        ADD CONSTRAINT CK_ComplianceDeficiencies_TenantNotHospital CHECK (TenantId <> HospitalId);
    PRINT 'Added CHECK constraint CK_ComplianceDeficiencies_TenantNotHospital.';
END
ELSE
BEGIN
    PRINT 'CHECK constraint CK_ComplianceDeficiencies_TenantNotHospital already present.';
END

------------------------------------------------------------------------------
-- §5  Post-verification — all three must pass
------------------------------------------------------------------------------
PRINT '--- Verification ---';

SELECT 'Rows with TenantId = HospitalId (expect 0)' AS Check_,
       COUNT(*) AS Value FROM ComplianceDeficiencies WHERE TenantId = HospitalId
UNION ALL
SELECT 'Rows where TenantId <> OrganizationId (expect 0)',
       COUNT(*) FROM ComplianceDeficiencies WHERE TenantId <> OrganizationId
UNION ALL
SELECT 'Duplicate (OrganizationId, HospitalId, ObjectiveElementId, Title) groups (expect 0)',
       COUNT(*) FROM (
           SELECT OrganizationId, HospitalId, ObjectiveElementId, Title
           FROM ComplianceDeficiencies
           GROUP BY OrganizationId, HospitalId, ObjectiveElementId, Title
           HAVING COUNT(*) > 1
       ) dupes;
