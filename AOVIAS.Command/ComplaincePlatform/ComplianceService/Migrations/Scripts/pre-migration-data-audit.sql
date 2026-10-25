-- Pre-migration data audit for unified Finding model rollout
-- Run before enabling MigrationSafety:UseUnifiedFinding=true

SELECT COUNT(*) AS TasksWithoutObjectiveElementId
FROM ComplianceTasks
WHERE ObjectiveElementId IS NULL OR ObjectiveElementId = '00000000-0000-0000-0000-000000000000';

SELECT COUNT(*) AS EvidenceWithoutTaskId
FROM Evidences
WHERE TaskId IS NULL OR TaskId = '00000000-0000-0000-0000-000000000000';

SELECT COUNT(*) AS FindingsWithoutAuditScopeLinkage
FROM Findings
WHERE Scope = 1
  AND (ComplianceJourneyId IS NULL OR ObjectiveElementId IS NULL);

SELECT COUNT(*) AS FindingsWithoutOrganizationScopeTenant
FROM Findings
WHERE Scope = 2
  AND (TenantId IS NULL OR TenantId = '00000000-0000-0000-0000-000000000000');

SELECT COUNT(*) AS InvalidComplianceStatuses
FROM Findings
WHERE ComplianceStatus NOT IN (1,2,3,4);
