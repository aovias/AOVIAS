-- Phase 3D Gate A dry-run (SELECT only).
SET NOCOUNT ON;
SELECT COUNT(*) AS TotalObligations FROM Compliance.dbo.OrganizationComplianceObligations;
SELECT COUNT(*) AS DuplicateBusinessKeys
FROM (
  SELECT HospitalId, FrameworkId, ObjectiveElementId
  FROM Compliance.dbo.OrganizationComplianceObligations
  GROUP BY HospitalId, FrameworkId, ObjectiveElementId
  HAVING COUNT(*) > 1
) x;
