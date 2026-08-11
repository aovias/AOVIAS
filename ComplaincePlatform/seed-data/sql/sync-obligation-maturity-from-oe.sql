/*
  sync-obligation-maturity-from-oe.sql

  Aligns OrganizationComplianceObligation.MaturityLevel with the current
  ObjectiveElements.Classification for each enrolled organization.

  Run after sync-oe-classifications-from-kb.sql when maturity bands are
  reclassified (e.g. Achievement / Excellence were zero).

  Idempotent: safe to re-run.

  Usage:
    sqlcmd -S localhost\SQLEXPRESS -E -i sync-obligation-maturity-from-oe.sql
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

USE Compliance;

BEGIN TRY
    BEGIN TRAN;

    UPDATE oco
    SET oco.MaturityLevel = oe.Classification
    FROM dbo.OrganizationComplianceObligations AS oco
    INNER JOIN dbo.ObjectiveElements AS oe ON oe.Id = oco.ObjectiveElementId
    WHERE oco.MaturityLevel <> oe.Classification;

    DECLARE @Updated INT = @@ROWCOUNT;

    COMMIT TRAN;

    SELECT
        oco.MaturityLevel,
        COUNT(*) AS Cnt
    FROM dbo.OrganizationComplianceObligations AS oco
    GROUP BY oco.MaturityLevel
    ORDER BY oco.MaturityLevel;

    PRINT CONCAT(N'Obligation maturity sync complete. Updated ', @Updated, N' row(s).');
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    THROW;
END CATCH;
