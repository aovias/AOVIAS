-- Destructive: removes all rows tied to NABH Hospital 6th Edition framework seed.
-- Set @Confirm = 1 after reviewing impact.
SET NOCOUNT ON;
DECLARE @Confirm BIT = 0;
DECLARE @FrameworkId UNIQUEIDENTIFIER = '0FE8A9C0-9EA0-4970-9097-D4F73D45DCA7';

IF @Confirm = 0
BEGIN
    RAISERROR('Set @Confirm = 1 to execute rollback.', 16, 1);
    RETURN;
END;

BEGIN TRY
    BEGIN TRAN;

    DELETE ki FROM dbo.KnowledgeItems ki
    INNER JOIN dbo.ComplianceNodes n ON n.Id = ki.NodeId
    WHERE n.FrameworkId = @FrameworkId;

    DELETE rs FROM dbo.RequirementScores rs
    INNER JOIN dbo.ComplianceNodes n ON n.Id = rs.NodeId
    WHERE n.FrameworkId = @FrameworkId;

    DELETE rd FROM dbo.RequirementDetails rd
    INNER JOIN dbo.ComplianceNodes n ON n.Id = rd.NodeId
    WHERE n.FrameworkId = @FrameworkId;

    DELETE FROM dbo.ComplianceNodes WHERE FrameworkId = @FrameworkId;

    DELETE FROM dbo.FrameworkClassifications WHERE FrameworkId = @FrameworkId;

    DELETE FROM dbo.ScoringConfigs WHERE FrameworkId = @FrameworkId;

    DELETE FROM dbo.Frameworks WHERE Id = @FrameworkId;

    COMMIT TRAN;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    THROW;
END CATCH;
