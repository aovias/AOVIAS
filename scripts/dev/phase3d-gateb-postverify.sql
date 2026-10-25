SET NOCOUNT ON;
SELECT COUNT(*) AS Obligations FROM dbo.OrganizationComplianceObligations;
SELECT COUNT(*) AS DuplicateKeys FROM (
  SELECT HospitalId, FrameworkId, ObjectiveElementId
  FROM dbo.OrganizationComplianceObligations
  GROUP BY HospitalId, FrameworkId, ObjectiveElementId HAVING COUNT(*) > 1) x;
SELECT COUNT(*) AS MissingHospital FROM dbo.OrganizationComplianceObligations o
WHERE NOT EXISTS (SELECT 1 FROM Organization.dbo.Hospitals h WHERE h.Id = o.HospitalId);
SELECT COUNT(*) AS OrgEqHospital FROM dbo.OrganizationComplianceObligations WHERE OrganizationId = HospitalId;
SELECT COUNT(*) AS OrgMismatch FROM dbo.OrganizationComplianceObligations o
WHERE NOT EXISTS (SELECT 1 FROM Organization.dbo.Hospitals h WHERE h.Id = o.HospitalId AND h.OrganizationId = o.OrganizationId AND h.IsDeleted = 0);
SELECT COUNT(*) AS OrphanDefs FROM dbo.ComplianceDeficiencies d
WHERE NOT EXISTS (SELECT 1 FROM dbo.OrganizationComplianceObligations o WHERE o.Id = d.ComplianceObligationId);
SELECT COUNT(*) AS OrphanTasks FROM dbo.ComplianceTasks t
WHERE t.SourceDeficiencyId IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.ComplianceDeficiencies d WHERE d.Id = t.SourceDeficiencyId);
SELECT COUNT(*) AS DefBadOrg FROM dbo.ComplianceDeficiencies d
WHERE NOT EXISTS (SELECT 1 FROM Organization.dbo.Hospitals h WHERE h.Id = d.HospitalId AND h.OrganizationId = d.OrganizationId AND h.IsDeleted = 0);
SELECT COUNT(*) AS Deficiencies FROM dbo.ComplianceDeficiencies;
SELECT COUNT(*) AS Tasks FROM dbo.ComplianceTasks;
SELECT COUNT(*) AS Evidences FROM dbo.Evidences;
SELECT COUNT(*) AS Findings FROM dbo.Findings;
SELECT COUNT(*) AS Capas FROM dbo.Capas;
SELECT COUNT(*) AS SlaPolicies FROM dbo.SlaPolicies;
SELECT COUNT(*) AS Maturity FROM dbo.OrganizationMaturityStageStates;
SELECT i.name AS IndexName FROM sys.indexes i
JOIN sys.tables t ON t.object_id = i.object_id
WHERE t.name = 'OrganizationComplianceObligations' AND i.name LIKE 'UX%';
SELECT HospitalId, COUNT(*) AS ObligationCount
FROM dbo.OrganizationComplianceObligations
GROUP BY HospitalId;
SELECT TOP 5 a.ObjectiveElementId
FROM dbo.OrganizationComplianceObligations a
JOIN dbo.OrganizationComplianceObligations b
  ON a.FrameworkId = b.FrameworkId AND a.ObjectiveElementId = b.ObjectiveElementId AND a.HospitalId <> b.HospitalId;
