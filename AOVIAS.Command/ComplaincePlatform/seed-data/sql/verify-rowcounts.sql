-- Verification queries for NABH Hospital 6th Edition seed (adjust @FrameworkId if needed)
DECLARE @FrameworkId UNIQUEIDENTIFIER = '0FE8A9C0-9EA0-4970-9097-D4F73D45DCA7';

SELECT 'Frameworks' AS [Table], COUNT(*) AS Cnt FROM dbo.Frameworks WHERE Id = @FrameworkId
UNION ALL SELECT 'ScoringConfigs', COUNT(*) FROM dbo.ScoringConfigs WHERE FrameworkId = @FrameworkId
UNION ALL SELECT 'FrameworkClassifications', COUNT(*) FROM dbo.FrameworkClassifications WHERE FrameworkId = @FrameworkId
UNION ALL SELECT 'ComplianceNodes (all)', COUNT(*) FROM dbo.ComplianceNodes WHERE FrameworkId = @FrameworkId
UNION ALL SELECT 'ComplianceNodes Chapter', COUNT(*) FROM dbo.ComplianceNodes WHERE FrameworkId = @FrameworkId AND NodeType = N'Chapter'
UNION ALL SELECT 'ComplianceNodes Standard', COUNT(*) FROM dbo.ComplianceNodes WHERE FrameworkId = @FrameworkId AND NodeType = N'Standard'
UNION ALL SELECT 'ComplianceNodes ObjectiveElement', COUNT(*) FROM dbo.ComplianceNodes WHERE FrameworkId = @FrameworkId AND NodeType = N'ObjectiveElement'
UNION ALL SELECT 'RequirementDetails', COUNT(*) FROM dbo.RequirementDetails rd INNER JOIN dbo.ComplianceNodes n ON n.Id = rd.NodeId WHERE n.FrameworkId = @FrameworkId AND n.NodeType = N'Standard'
UNION ALL SELECT 'RequirementScores', COUNT(*) FROM dbo.RequirementScores rs INNER JOIN dbo.ComplianceNodes n ON n.Id = rs.NodeId WHERE n.FrameworkId = @FrameworkId AND n.NodeType = N'Standard'
UNION ALL SELECT 'KnowledgeItems', COUNT(*) FROM dbo.KnowledgeItems ki INNER JOIN dbo.ComplianceNodes n ON n.Id = ki.NodeId WHERE n.FrameworkId = @FrameworkId AND n.NodeType = N'ObjectiveElement';

-- Orphan ParentId check (should return 0 rows)
SELECT cn.Id, cn.Code, cn.ParentId
FROM dbo.ComplianceNodes cn
LEFT JOIN dbo.ComplianceNodes p ON p.Id = cn.ParentId
WHERE cn.FrameworkId = @FrameworkId AND cn.ParentId IS NOT NULL AND p.Id IS NULL;

-- Expected approximate counts from generator: 1 framework, 1 scoring, 10 chapters, 100 standards, 503 OEs (verify file line count)
