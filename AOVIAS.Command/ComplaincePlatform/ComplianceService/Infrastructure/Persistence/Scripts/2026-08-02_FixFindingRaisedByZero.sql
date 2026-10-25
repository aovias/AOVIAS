/*
  Wave 2.1 — Data integrity: Findings.RaisedBy = 0

  PROBLEM
  -------
  `FindingRaisedBy` (Shared.Infrastructure/Domain/Enums/FindingRaisedBy.cs) is explicitly
  numbered from 1 (InternalAudit = 1 … IncidentReview = 8). 24 rows hold 0, which is not a
  member. `FindingsWorkspaceQueryService` serialises with `RaisedBy.ToString()`, so an
  undefined value is emitted as the literal string "0" and reached the UI as a bare 0 in the
  SOURCE column and an unlabelled bucket in the BY SOURCE breakdown.

  EVIDENCE THE VALUE IS 'InternalAudit' (verified before writing this script)
  --------------------------------------------------------------------------
    * All 24 rows were inserted in one batch at 2026-07-13 11:59:12.8166667 (identical timestamp).
    * They use a different FindingNumber convention (FND-2026-001) from application-created rows
      (FND-2026-00001) — a separate seed run.
    * All 24 descriptions begin "Internal audit sampling identified a repeatable control gap."
    * Classification and CreatedByType are also unset (NULL / 0) on the same rows — an incomplete
      seed, not a distinct historical meaning.
    * The C# property default is FindingRaisedBy.InternalAudit.

  Conclusion: 0 here means "the seed never set it", and the rows are internal audit findings.

  SAFETY
  ------
  Idempotent. Touches only rows where RaisedBy = 0. Run inside a transaction; the row count is
  asserted before commit.
*/

SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @affected INT = (SELECT COUNT(*) FROM Findings WHERE RaisedBy = 0);
PRINT CONCAT('Rows with RaisedBy = 0 before migration: ', @affected);

UPDATE Findings
SET RaisedBy = 1 -- FindingRaisedBy.InternalAudit
WHERE RaisedBy = 0;

DECLARE @remaining INT = (SELECT COUNT(*) FROM Findings WHERE RaisedBy = 0);
IF @remaining <> 0
BEGIN
    RAISERROR('Migration failed: rows with RaisedBy = 0 remain.', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END

PRINT CONCAT('Rows migrated: ', @affected);

/*
  Guard against recurrence at the storage boundary. The enum is 1..8; anything else is invalid
  regardless of which service wrote it.
*/
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Findings_RaisedBy_Range')
BEGIN
    ALTER TABLE Findings WITH CHECK
        ADD CONSTRAINT CK_Findings_RaisedBy_Range CHECK (RaisedBy BETWEEN 1 AND 8);
    PRINT 'Added CHECK constraint CK_Findings_RaisedBy_Range (1..8).';
END
ELSE
BEGIN
    PRINT 'CHECK constraint CK_Findings_RaisedBy_Range already present.';
END

COMMIT TRANSACTION;
PRINT 'Migration committed.';
