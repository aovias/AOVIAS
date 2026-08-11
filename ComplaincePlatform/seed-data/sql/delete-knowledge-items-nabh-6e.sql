-- Remove KnowledgeItems for NABH Hospital 6th Edition OEs so seed script can re-insert with full text.
-- FrameworkId: NABH Full Accreditation (Hospital) 6th Edition
SET NOCOUNT ON;
DECLARE @FrameworkId UNIQUEIDENTIFIER = '0FE8A9C0-9EA0-4970-9097-D4F73D45DCA7';

DELETE ki
FROM dbo.KnowledgeItems ki
INNER JOIN dbo.ComplianceNodes n ON n.Id = ki.NodeId
WHERE n.FrameworkId = @FrameworkId
  AND n.NodeType = N'ObjectiveElement';

PRINT 'Deleted KnowledgeItems for ObjectiveElement nodes under framework ' + CONVERT(VARCHAR(36), @FrameworkId);
