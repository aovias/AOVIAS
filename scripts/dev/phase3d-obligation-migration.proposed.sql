-- Phase 3D Gate B CANDIDATE. DO NOT EXECUTE until Gate A is approved.
-- Target: localhost\SQLEXPRESS database Compliance (development).
-- This script is intentionally incomplete for the 41 diverged pairs and 650 missing-hospital rows.
-- Running it now would be unauthorized.

/*
PRECONDITIONS (fail if not met):
  Twin IDENTICAL pairs = 965
  Twin DIVERGED pairs = 41 (excluded from this batch until BDR)
  MATCH OrganizationId already equals Organization.Hospitals.OrganizationId

SAFE BATCH (after explicit approval of 965 only):
  1. DELETE obligations whose Id is TwinObligationId AND StateClass=IDENTICAL
     AND NOT EXISTS deficiency? wait identical have 0 defs
  2. Do NOT drop UX_OrgOE until 41 also resolved (identical delete leaves 41 duplicate keys)

DO NOT:
  DELETE WHERE OrganizationId = HospitalId
  DROP INDEX until COUNT(*) = COUNT(DISTINCT HospitalId, FrameworkId, ObjectiveElementId)

ROLLBACK: wrap approved DML in BEGIN TRAN; verify; COMMIT or ROLLBACK.
*/

THROW 50000, 'Phase 3D Gate B script is not authorized to run. Dry-run only.', 1;
