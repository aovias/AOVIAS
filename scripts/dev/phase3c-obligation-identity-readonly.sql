-- Phase 3C diagnostic only. No DML.
SET NOCOUNT ON;

SELECT
  COUNT(*) AS PairKeys,
  SUM(CASE WHEN m.ImplementationStatus = t.ImplementationStatus AND m.AssessmentStatus = t.AssessmentStatus AND m.JourneyStatus = t.JourneyStatus AND m.EvidenceStatus = t.EvidenceStatus AND m.VerificationStatus = t.VerificationStatus THEN 1 ELSE 0 END) AS IdenticalState,
  SUM(CASE WHEN m.ImplementationStatus <> t.ImplementationStatus OR m.AssessmentStatus <> t.AssessmentStatus OR m.JourneyStatus <> t.JourneyStatus OR m.EvidenceStatus <> t.EvidenceStatus OR m.VerificationStatus <> t.VerificationStatus THEN 1 ELSE 0 END) AS DifferingState
FROM Compliance.dbo.OrganizationComplianceObligations t
INNER JOIN Organization.dbo.Hospitals h ON h.Id = t.HospitalId AND t.OrganizationId = t.HospitalId
INNER JOIN Compliance.dbo.OrganizationComplianceObligations m
  ON m.HospitalId = t.HospitalId AND m.FrameworkId = t.FrameworkId AND m.ObjectiveElementId = t.ObjectiveElementId
 AND m.OrganizationId = h.OrganizationId;
