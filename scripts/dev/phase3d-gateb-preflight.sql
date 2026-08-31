-- Phase 3D Gate B preflight (SELECT only). Fail the caller if counts differ from Gate A.
SET NOCOUNT ON;

DECLARE @Total int = (SELECT COUNT(*) FROM dbo.OrganizationComplianceObligations);
DECLARE @DupKeys int = (
  SELECT COUNT(*) FROM (
    SELECT HospitalId, FrameworkId, ObjectiveElementId
    FROM dbo.OrganizationComplianceObligations
    GROUP BY HospitalId, FrameworkId, ObjectiveElementId
    HAVING COUNT(*) > 1
  ) x);
DECLARE @Defs int = (SELECT COUNT(*) FROM dbo.ComplianceDeficiencies);
DECLARE @Tasks int = (SELECT COUNT(*) FROM dbo.ComplianceTasks);
DECLARE @Evidences int = (SELECT COUNT(*) FROM dbo.Evidences);
DECLARE @Findings int = (SELECT COUNT(*) FROM dbo.Findings);
DECLARE @Capa int = (SELECT COUNT(*) FROM dbo.Capas);
DECLARE @Sla int = (SELECT COUNT(*) FROM dbo.SlaPolicies);
DECLARE @Maturity int = (SELECT COUNT(*) FROM dbo.OrganizationMaturityStageStates);

SELECT
  @Total AS TotalObligations,
  @DupKeys AS DuplicateBusinessKeys,
  @Defs AS Deficiencies,
  @Tasks AS Tasks,
  @Evidences AS Evidences,
  @Findings AS Findings,
  @Capa AS Capas,
  @Sla AS SlaPolicies,
  @Maturity AS MaturityStates;

SELECT CASE WHEN @Total = 2662 AND @DupKeys = 1006 AND @Defs = 177
                 AND @Tasks = 1534 AND @Evidences = 473 AND @Findings = 35
                 AND @Capa = 17 AND @Sla = 8 AND @Maturity = 4
            THEN 'GATE_A_COUNTS_MATCH'
            ELSE 'GATE_A_COUNTS_DIFFER'
       END AS CountCheck;
