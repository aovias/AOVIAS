-- Phase 3B PROPOSED cleanup identification only.
-- DO NOT EXECUTE DELETE/UPDATE. Not authorized by Phase 3B.
-- Run individual SELECTs after business approval in a later phase.

SET NOCOUNT ON;

-- C1. Phase 2 audit artifact hospital (DO NOT TOUCH until approved)
DECLARE @Phase2Artifact uniqueidentifier = 'F653E630-C9A9-4014-918D-23B33DE93845';
SELECT 'C1_artifact_obligations' AS Candidate, COUNT(*) AS Rows
FROM Compliance.dbo.OrganizationComplianceObligations
WHERE HospitalId = @Phase2Artifact;

-- C2. Accidental live enroll against old :5016 (DO NOT TOUCH until approved)
DECLARE @Accidental uniqueidentifier = 'EDB9321A-F922-4CBC-8C1C-1D2C0D5DC1AB';
SELECT 'C2_accidental_obligations' AS Candidate, COUNT(*) AS Rows
FROM Compliance.dbo.OrganizationComplianceObligations
WHERE HospitalId = @Accidental;

-- C3. Duplicate obligation twin: same Hospital+Framework+OE where OrganizationId = HospitalId
-- Keep the MATCH row (OrganizationId = Organization.Hospitals.OrganizationId).
SELECT 'C3_org_eq_hosp_twins' AS Candidate, COUNT(*) AS Rows
FROM Compliance.dbo.OrganizationComplianceObligations o
INNER JOIN Organization.dbo.Hospitals h ON h.Id = o.HospitalId
WHERE o.OrganizationId = o.HospitalId
  AND h.OrganizationId <> o.HospitalId;

-- C4. Dashboard projections with empty or hospital-copied OrganizationId
SELECT 'C4_dashboard_org_empty_or_eq_hospital' AS Candidate, COUNT(*) AS Rows
FROM Compliance.dbo.DashboardProjections p
INNER JOIN Organization.dbo.Hospitals h ON h.Id = p.HospitalId
WHERE p.OrganizationId = '00000000-0000-0000-0000-000000000000'
   OR p.OrganizationId = p.HospitalId;
