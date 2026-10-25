/*
  sync-oe-classifications-from-kb.sql

  Aligns Compliance.dbo.ObjectiveElements.Classification with KnowledgeBase
  WizardQuestions.AssessmentMaturityLevel (matched by OE code).

  Why: ObjectiveElement.Classification must match KnowledgeBase
  WizardQuestions.AssessmentMaturityLevel (by OE code). Run
  classify-wizard-questions-nabh-6e.sql first, then this script, then
  sync-obligation-maturity-from-oe.sql for enrolled hospitals.

  Idempotent: safe to re-run.

  Usage:
    sqlcmd -S localhost\SQLEXPRESS -E -i sync-oe-classifications-from-kb.sql
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_ID(N'Compliance') IS NULL OR DB_ID(N'KnowledgeBase') IS NULL
BEGIN
    RAISERROR(N'Both Compliance and KnowledgeBase databases must exist.', 16, 1);
    RETURN;
END;

BEGIN TRY
    BEGIN TRAN;

    ;WITH KbLevel AS (
        SELECT
            cn.Code,
            wq.AssessmentMaturityLevel,
            NewClassification = CASE LTRIM(RTRIM(wq.AssessmentMaturityLevel))
                WHEN N'Core' THEN 0
                WHEN N'Commitment' THEN 1
                WHEN N'Achievement' THEN 2
                WHEN N'Excellence' THEN 3
                ELSE NULL
            END
        FROM KnowledgeBase.dbo.ComplianceNodes AS cn
        INNER JOIN KnowledgeBase.dbo.WizardQuestions AS wq
            ON wq.ObjectiveElementId = cn.Id
        WHERE cn.NodeType = N'ObjectiveElement'
          AND wq.AssessmentMaturityLevel IS NOT NULL
          AND LTRIM(RTRIM(wq.AssessmentMaturityLevel)) <> N''
    )
    UPDATE oe
    SET oe.Classification = kl.NewClassification
    FROM Compliance.dbo.ObjectiveElements AS oe
    INNER JOIN Compliance.dbo.ComplianceStandards AS cs ON cs.Id = oe.ComplianceStandardId
    INNER JOIN Compliance.dbo.ComplianceChapters AS cc ON cc.Id = cs.ChapterId
    INNER JOIN Compliance.dbo.ComplianceFrameworks AS cf ON cf.Id = cc.FrameworkId
    INNER JOIN KbLevel AS kl ON kl.Code = oe.Code
    WHERE cf.PlatformId IN (N'CP00000001', N'CP00000002')
      AND kl.NewClassification IS NOT NULL
      AND oe.Classification <> kl.NewClassification;

    DECLARE @Updated INT = @@ROWCOUNT;

    COMMIT TRAN;

    SELECT
        cf.PlatformId,
        oe.Classification,
        COUNT(*) AS Cnt
    FROM Compliance.dbo.ObjectiveElements AS oe
    INNER JOIN Compliance.dbo.ComplianceStandards AS cs ON cs.Id = oe.ComplianceStandardId
    INNER JOIN Compliance.dbo.ComplianceChapters AS cc ON cc.Id = cs.ChapterId
    INNER JOIN Compliance.dbo.ComplianceFrameworks AS cf ON cf.Id = cc.FrameworkId
    WHERE cf.PlatformId IN (N'CP00000001', N'CP00000002')
    GROUP BY cf.PlatformId, oe.Classification
    ORDER BY cf.PlatformId, oe.Classification;

    PRINT CONCAT(N'Sync complete. Updated ', @Updated, N' objective element classification(s).');
    PRINT N'NOTE: Restart ComplianceService to refresh the in-memory framework catalog cache,';
    PRINT N'      or wait up to 45 minutes for cache TTL expiry.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    THROW;
END CATCH;
