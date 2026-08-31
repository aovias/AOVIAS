-- Phase 3B diagnostic queries only. No DELETE/UPDATE/TRUNCATE/DROP/ALTER.
SET NOCOUNT ON;
SET ANSI_NULLS ON;

PRINT '=== 0. ORG: hospital Id never equals OrganizationId ===';
SELECT COUNT(*) AS HospCount,
       SUM(CASE WHEN Id = OrganizationId THEN 1 ELSE 0 END) AS IdEqualsOrgId
FROM Organization.dbo.Hospitals;

PRINT '=== 1. DISTINCT HOSPITAL IDS IN COMPLIANCE (union) ===';
IF OBJECT_ID('tempdb..#HospRefs') IS NOT NULL DROP TABLE #HospRefs;
CREATE TABLE #HospRefs (SourceTable sysname NOT NULL, HospitalId uniqueidentifier NOT NULL, RowCnt int NOT NULL);

INSERT INTO #HospRefs
SELECT 'OrganizationComplianceObligations', HospitalId, COUNT(*) FROM Compliance.dbo.OrganizationComplianceObligations GROUP BY HospitalId
UNION ALL SELECT 'ComplianceJourneys', HospitalId, COUNT(*) FROM Compliance.dbo.ComplianceJourneys GROUP BY HospitalId
UNION ALL SELECT 'ObjectiveElementOwnerships', HospitalId, COUNT(*) FROM Compliance.dbo.ObjectiveElementOwnerships GROUP BY HospitalId
UNION ALL SELECT 'ComplianceTasks', HospitalId, COUNT(*) FROM Compliance.dbo.ComplianceTasks GROUP BY HospitalId
UNION ALL SELECT 'Evidences', HospitalId, COUNT(*) FROM Compliance.dbo.Evidences GROUP BY HospitalId
UNION ALL SELECT 'Risks', HospitalId, COUNT(*) FROM Compliance.dbo.Risks GROUP BY HospitalId
UNION ALL SELECT 'Capas', HospitalId, COUNT(*) FROM Compliance.dbo.Capas GROUP BY HospitalId
UNION ALL SELECT 'ComplianceDeficiencies', HospitalId, COUNT(*) FROM Compliance.dbo.ComplianceDeficiencies GROUP BY HospitalId
UNION ALL SELECT 'SlaPolicies', HospitalId, COUNT(*) FROM Compliance.dbo.SlaPolicies GROUP BY HospitalId
UNION ALL SELECT 'OrganizationMaturityStageStates', HospitalId, COUNT(*) FROM Compliance.dbo.OrganizationMaturityStageStates GROUP BY HospitalId
UNION ALL SELECT 'ComplianceSnapshots', HospitalId, COUNT(*) FROM Compliance.dbo.ComplianceSnapshots GROUP BY HospitalId
UNION ALL SELECT 'DashboardProjections', HospitalId, COUNT(*) FROM Compliance.dbo.DashboardProjections GROUP BY HospitalId
UNION ALL SELECT 'AnalyticsDailySnapshots', HospitalId, COUNT(*) FROM Compliance.dbo.AnalyticsDailySnapshots GROUP BY HospitalId
UNION ALL SELECT 'Alerts', HospitalId, COUNT(*) FROM Compliance.dbo.Alerts GROUP BY HospitalId
UNION ALL SELECT 'ActivityEvents', HospitalId, COUNT(*) FROM Compliance.dbo.ActivityEvents GROUP BY HospitalId
UNION ALL SELECT 'AuditSessions', HospitalId, COUNT(*) FROM Compliance.dbo.AuditSessions GROUP BY HospitalId
UNION ALL SELECT 'AssignmentRoutingLogs', HospitalId, COUNT(*) FROM Compliance.dbo.AssignmentRoutingLogs GROUP BY HospitalId
UNION ALL SELECT 'ClinicalIndicators', HospitalId, COUNT(*) FROM Compliance.dbo.ClinicalIndicators GROUP BY HospitalId
UNION ALL SELECT 'CompliancePrograms', HospitalId, COUNT(*) FROM Compliance.dbo.CompliancePrograms GROUP BY HospitalId
UNION ALL SELECT 'ComplianceTaskComments', HospitalId, COUNT(*) FROM Compliance.dbo.ComplianceTaskComments GROUP BY HospitalId
UNION ALL SELECT 'ComplianceTaskOwnershipHistories', HospitalId, COUNT(*) FROM Compliance.dbo.ComplianceTaskOwnershipHistories GROUP BY HospitalId
UNION ALL SELECT 'ControlledDocuments', HospitalId, COUNT(*) FROM Compliance.dbo.ControlledDocuments GROUP BY HospitalId
UNION ALL SELECT 'DeficiencyEscalationHistories', HospitalId, COUNT(*) FROM Compliance.dbo.DeficiencyEscalationHistories GROUP BY HospitalId
UNION ALL SELECT 'DepartmentOwnerships', HospitalId, COUNT(*) FROM Compliance.dbo.DepartmentOwnerships GROUP BY HospitalId
UNION ALL SELECT 'EvidenceReviewAuditEntries', HospitalId, COUNT(*) FROM Compliance.dbo.EvidenceReviewAuditEntries GROUP BY HospitalId
UNION ALL SELECT 'EscalationPolicies', HospitalId, COUNT(*) FROM Compliance.dbo.EscalationPolicies GROUP BY HospitalId
UNION ALL SELECT 'FrameworkChapterSnapshots', HospitalId, COUNT(*) FROM Compliance.dbo.FrameworkChapterSnapshots GROUP BY HospitalId
UNION ALL SELECT 'Incidents', HospitalId, COUNT(*) FROM Compliance.dbo.Incidents GROUP BY HospitalId
UNION ALL SELECT 'LessonsLearned', HospitalId, COUNT(*) FROM Compliance.dbo.LessonsLearned GROUP BY HospitalId
UNION ALL SELECT 'MaturityStageUnlockAuditEntries', HospitalId, COUNT(*) FROM Compliance.dbo.MaturityStageUnlockAuditEntries GROUP BY HospitalId
UNION ALL SELECT 'ObjectiveJourneys', HospitalId, COUNT(*) FROM Compliance.dbo.ObjectiveJourneys GROUP BY HospitalId
UNION ALL SELECT 'OrganizationComplianceOverviewSnapshots', HospitalId, COUNT(*) FROM Compliance.dbo.OrganizationComplianceOverviewSnapshots GROUP BY HospitalId
UNION ALL SELECT 'PatientFeedbacks', HospitalId, COUNT(*) FROM Compliance.dbo.PatientFeedbacks GROUP BY HospitalId
UNION ALL SELECT 'RecurringTaskDefinitions', HospitalId, COUNT(*) FROM Compliance.dbo.RecurringTaskDefinitions GROUP BY HospitalId
UNION ALL SELECT 'Regulations', HospitalId, COUNT(*) FROM Compliance.dbo.Regulations GROUP BY HospitalId
UNION ALL SELECT 'ScheduledAudits', HospitalId, COUNT(*) FROM Compliance.dbo.ScheduledAudits GROUP BY HospitalId
UNION ALL SELECT 'WizardOnboardingAssessments', HospitalId, COUNT(*) FROM Compliance.dbo.WizardOnboardingAssessments GROUP BY HospitalId;

PRINT '=== 1a. SOURCE TABLES WITH HOSPITAL REFS (collapsed) ===';
SELECT SourceTable, COUNT(DISTINCT HospitalId) AS DistH, SUM(RowCnt) AS Rows
FROM #HospRefs
GROUP BY SourceTable
ORDER BY SourceTable;

PRINT '=== 1b. HOSPITAL CLASSIFICATION vs Organization.Hospitals ===';
SELECT r.HospitalId,
       CASE
         WHEN h.Id IS NULL THEN 'MISSING_HOSPITAL'
         WHEN h.IsDeleted = 1 THEN 'DELETED_HOSPITAL'
         WHEN h.Status IS NULL OR LTRIM(RTRIM(h.Status)) = '' THEN 'UNKNOWN'
         WHEN h.Status = 'Active' THEN 'VALID_HOSPITAL'
         ELSE 'INACTIVE_HOSPITAL'
       END AS HospitalClass,
       h.OrganizationId AS OrgHospitalOrganizationId,
       h.Status AS OrgStatus,
       h.IsDeleted,
       SUM(r.RowCnt) AS RefRowSum
FROM #HospRefs r
LEFT JOIN Organization.dbo.Hospitals h ON h.Id = r.HospitalId
GROUP BY r.HospitalId, h.Id, h.OrganizationId, h.Status, h.IsDeleted
ORDER BY HospitalClass, r.HospitalId;

PRINT '=== 1c. CLASS COUNTS (distinct hospitals) ===';
SELECT
  CASE
    WHEN h.Id IS NULL THEN 'MISSING_HOSPITAL'
    WHEN h.IsDeleted = 1 THEN 'DELETED_HOSPITAL'
    WHEN h.Status IS NULL OR LTRIM(RTRIM(h.Status)) = '' THEN 'UNKNOWN'
    WHEN h.Status = 'Active' THEN 'VALID_HOSPITAL'
    ELSE 'INACTIVE_HOSPITAL'
  END AS HospitalClass,
  COUNT(*) AS DistinctHospitals
FROM (SELECT DISTINCT HospitalId FROM #HospRefs) d
LEFT JOIN Organization.dbo.Hospitals h ON h.Id = d.HospitalId
GROUP BY
  CASE
    WHEN h.Id IS NULL THEN 'MISSING_HOSPITAL'
    WHEN h.IsDeleted = 1 THEN 'DELETED_HOSPITAL'
    WHEN h.Status IS NULL OR LTRIM(RTRIM(h.Status)) = '' THEN 'UNKNOWN'
    WHEN h.Status = 'Active' THEN 'VALID_HOSPITAL'
    ELSE 'INACTIVE_HOSPITAL'
  END;

PRINT '=== 2. OBLIGATION ORG/HOSPITAL PAIRS ===';
SELECT o.OrganizationId, o.HospitalId, o.TenantId,
       COUNT(*) AS Cnt,
       MIN(o.CreatedAt) AS MinCreated,
       MAX(o.CreatedAt) AS MaxCreated,
       MIN(o.CreatedBy) AS CreatedBySample,
       CASE WHEN o.OrganizationId = o.HospitalId THEN 1 ELSE 0 END AS OrgEqHosp,
       CASE WHEN o.TenantId = o.OrganizationId THEN 1 ELSE 0 END AS TenantEqOrg,
       CASE WHEN o.TenantId = o.HospitalId THEN 1 ELSE 0 END AS TenantEqHosp,
       h.OrganizationId AS TrueOrgId,
       CASE
         WHEN h.Id IS NULL THEN 'MISSING_HOSPITAL'
         WHEN h.OrganizationId = o.OrganizationId THEN 'MATCH'
         ELSE 'MISMATCH'
       END AS OrgOwnership
FROM Compliance.dbo.OrganizationComplianceObligations o
LEFT JOIN Organization.dbo.Hospitals h ON h.Id = o.HospitalId
GROUP BY o.OrganizationId, o.HospitalId, o.TenantId, h.Id, h.OrganizationId
ORDER BY OrgOwnership, o.HospitalId;

PRINT '=== 2b. OBLIGATION OWNERSHIP ROLLUP ===';
SELECT
  CASE
    WHEN h.Id IS NULL THEN 'MISSING_HOSPITAL'
    WHEN h.OrganizationId = o.OrganizationId THEN 'MATCH'
    ELSE 'MISMATCH'
  END AS OrgOwnership,
  SUM(CASE WHEN o.OrganizationId = o.HospitalId THEN 1 ELSE 0 END) AS OrgEqHospRows,
  COUNT(*) AS Rows
FROM Compliance.dbo.OrganizationComplianceObligations o
LEFT JOIN Organization.dbo.Hospitals h ON h.Id = o.HospitalId
GROUP BY
  CASE
    WHEN h.Id IS NULL THEN 'MISSING_HOSPITAL'
    WHEN h.OrganizationId = o.OrganizationId THEN 'MATCH'
    ELSE 'MISMATCH'
  END;

PRINT '=== 2c. ORG_EQ_HOSP breakdown vs Org authority ===';
SELECT
  CASE
    WHEN o.OrganizationId <> o.HospitalId THEN 'NOT_EQUAL'
    WHEN h.Id IS NULL THEN 'EQUAL_MISSING_HOSPITAL'
    WHEN h.Id = h.OrganizationId THEN 'EQUAL_AND_ORG_HOSPITAL_SAME_GUID'
    WHEN h.OrganizationId = o.OrganizationId THEN 'EQUAL_BUT_MATCHES_TRUE_ORG' -- impossible if equal unless hospital.Id = hospital.OrganizationId
    ELSE 'EQUAL_INVALID_HOSPITAL_COPIED_INTO_ORG'
  END AS EqClass,
  COUNT(*) AS Rows,
  COUNT(DISTINCT o.HospitalId) AS DistinctHospitals
FROM Compliance.dbo.OrganizationComplianceObligations o
LEFT JOIN Organization.dbo.Hospitals h ON h.Id = o.HospitalId
GROUP BY
  CASE
    WHEN o.OrganizationId <> o.HospitalId THEN 'NOT_EQUAL'
    WHEN h.Id IS NULL THEN 'EQUAL_MISSING_HOSPITAL'
    WHEN h.Id = h.OrganizationId THEN 'EQUAL_AND_ORG_HOSPITAL_SAME_GUID'
    WHEN h.OrganizationId = o.OrganizationId THEN 'EQUAL_BUT_MATCHES_TRUE_ORG'
    ELSE 'EQUAL_INVALID_HOSPITAL_COPIED_INTO_ORG'
  END;

PRINT '=== 3. JOURNEY ORG/HOSPITAL ===';
SELECT j.OrganizationId, j.HospitalId, j.TenantId, j.FrameworkId, j.Status, j.CreatedAt,
       CASE WHEN j.OrganizationId = j.HospitalId THEN 1 ELSE 0 END AS OrgEqHosp,
       CASE
         WHEN h.Id IS NULL THEN 'MISSING_HOSPITAL'
         WHEN h.OrganizationId = j.OrganizationId THEN 'MATCH'
         ELSE 'MISMATCH'
       END AS OrgOwnership
FROM Compliance.dbo.ComplianceJourneys j
LEFT JOIN Organization.dbo.Hospitals h ON h.Id = j.HospitalId
ORDER BY OrgOwnership, j.CreatedAt;

PRINT '=== 4. DUPLICATE OBLIGATIONS Hospital+Framework+OE (summary) ===';
SELECT HospitalId, FrameworkId, COUNT(*) AS DuplicateOeKeys, SUM(DupCnt) AS TotalRowsInDupKeys
FROM (
  SELECT HospitalId, FrameworkId, ObjectiveElementId, COUNT(*) AS DupCnt
  FROM Compliance.dbo.OrganizationComplianceObligations
  GROUP BY HospitalId, FrameworkId, ObjectiveElementId
  HAVING COUNT(*) > 1
) x
GROUP BY HospitalId, FrameworkId;

PRINT '=== 4b. DUPLICATE OrganizationId+OE (unique index key) ===';
SELECT OrganizationId, ObjectiveElementId, COUNT(*) AS DupCnt, COUNT(DISTINCT HospitalId) AS DistinctHospitals
FROM Compliance.dbo.OrganizationComplianceObligations
GROUP BY OrganizationId, ObjectiveElementId
HAVING COUNT(*) > 1;

PRINT '=== 4c. same Hospital+Framework obligation count vs OE catalog ===';
SELECT HospitalId, FrameworkId, COUNT(*) AS OblCnt, COUNT(DISTINCT ObjectiveElementId) AS DistinctOE
FROM Compliance.dbo.OrganizationComplianceObligations
GROUP BY HospitalId, FrameworkId
ORDER BY OblCnt DESC;

PRINT '=== 5. PHASE2 ARTIFACT f653e630 ===';
DECLARE @A uniqueidentifier = 'F653E630-C9A9-4014-918D-23B33DE93845';
DECLARE @E uniqueidentifier = 'EDB9321A-F922-4CBC-8C1C-1D2C0D5DC1AB';
SELECT 'OrganizationComplianceObligations' AS T, COUNT(*) AS C FROM Compliance.dbo.OrganizationComplianceObligations WHERE HospitalId=@A
UNION ALL SELECT 'ComplianceJourneys', COUNT(*) FROM Compliance.dbo.ComplianceJourneys WHERE HospitalId=@A
UNION ALL SELECT 'ObjectiveElementOwnerships', COUNT(*) FROM Compliance.dbo.ObjectiveElementOwnerships WHERE HospitalId=@A
UNION ALL SELECT 'ComplianceTasks', COUNT(*) FROM Compliance.dbo.ComplianceTasks WHERE HospitalId=@A
UNION ALL SELECT 'Evidences', COUNT(*) FROM Compliance.dbo.Evidences WHERE HospitalId=@A
UNION ALL SELECT 'Risks', COUNT(*) FROM Compliance.dbo.Risks WHERE HospitalId=@A
UNION ALL SELECT 'Capas', COUNT(*) FROM Compliance.dbo.Capas WHERE HospitalId=@A
UNION ALL SELECT 'ComplianceDeficiencies', COUNT(*) FROM Compliance.dbo.ComplianceDeficiencies WHERE HospitalId=@A
UNION ALL SELECT 'DashboardProjections', COUNT(*) FROM Compliance.dbo.DashboardProjections WHERE HospitalId=@A
UNION ALL SELECT 'ComplianceSnapshots', COUNT(*) FROM Compliance.dbo.ComplianceSnapshots WHERE HospitalId=@A
UNION ALL SELECT 'AnalyticsDailySnapshots', COUNT(*) FROM Compliance.dbo.AnalyticsDailySnapshots WHERE HospitalId=@A
UNION ALL SELECT 'SlaPolicies', COUNT(*) FROM Compliance.dbo.SlaPolicies WHERE HospitalId=@A
UNION ALL SELECT 'OrganizationMaturityStageStates', COUNT(*) FROM Compliance.dbo.OrganizationMaturityStageStates WHERE HospitalId=@A
UNION ALL SELECT 'ObjectiveJourneys', COUNT(*) FROM Compliance.dbo.ObjectiveJourneys WHERE HospitalId=@A
UNION ALL SELECT 'OutboxMessages', COUNT(*) FROM Compliance.dbo.OutboxMessages WHERE Payload LIKE '%f653e630%' OR Payload LIKE '%F653E630%';

PRINT '=== 5b. ACCIDENTAL 5016 ENROLL edb9321a ===';
SELECT 'OrganizationComplianceObligations' AS T, COUNT(*) AS C FROM Compliance.dbo.OrganizationComplianceObligations WHERE HospitalId=@E
UNION ALL SELECT 'ComplianceJourneys', COUNT(*) FROM Compliance.dbo.ComplianceJourneys WHERE HospitalId=@E
UNION ALL SELECT 'OutboxMessages', COUNT(*) FROM Compliance.dbo.OutboxMessages WHERE Payload LIKE '%edb9321a%' OR Payload LIKE '%EDB9321A%';

PRINT '=== 6. FRAMEWORK / OE / NODE ===';
SELECT
  COUNT(*) AS Obl,
  SUM(CASE WHEN f.Id IS NULL THEN 1 ELSE 0 END) AS MissingFramework,
  SUM(CASE WHEN oe.Id IS NULL THEN 1 ELSE 0 END) AS MissingOE,
  SUM(CASE WHEN o.ComplianceNodeId IS NULL THEN 1 ELSE 0 END) AS NullNodeId
FROM Compliance.dbo.OrganizationComplianceObligations o
LEFT JOIN Compliance.dbo.ComplianceFrameworks f ON f.Id = o.FrameworkId
LEFT JOIN Compliance.dbo.ObjectiveElements oe ON oe.Id = o.ObjectiveElementId;

PRINT '=== 7. TASKS / EVIDENCE / RISKS / CAPA / DEF HOSPITAL CLASS ===';
SELECT 'ComplianceTasks' AS T, COUNT(*) AS Rows, COUNT(DISTINCT HospitalId) AS DistH,
  SUM(CASE WHEN h.Id IS NULL THEN 1 ELSE 0 END) AS MissingHospRows
FROM Compliance.dbo.ComplianceTasks t LEFT JOIN Organization.dbo.Hospitals h ON h.Id=t.HospitalId
UNION ALL
SELECT 'Evidences', COUNT(*), COUNT(DISTINCT e.HospitalId), SUM(CASE WHEN h.Id IS NULL THEN 1 ELSE 0 END)
FROM Compliance.dbo.Evidences e LEFT JOIN Organization.dbo.Hospitals h ON h.Id=e.HospitalId
UNION ALL
SELECT 'Risks', COUNT(*), COUNT(DISTINCT r.HospitalId), SUM(CASE WHEN h.Id IS NULL THEN 1 ELSE 0 END)
FROM Compliance.dbo.Risks r LEFT JOIN Organization.dbo.Hospitals h ON h.Id=r.HospitalId
UNION ALL
SELECT 'Capas', COUNT(*), COUNT(DISTINCT c.HospitalId), SUM(CASE WHEN h.Id IS NULL THEN 1 ELSE 0 END)
FROM Compliance.dbo.Capas c LEFT JOIN Organization.dbo.Hospitals h ON h.Id=c.HospitalId
UNION ALL
SELECT 'ComplianceDeficiencies', COUNT(*), COUNT(DISTINCT d.HospitalId), SUM(CASE WHEN h.Id IS NULL THEN 1 ELSE 0 END)
FROM Compliance.dbo.ComplianceDeficiencies d LEFT JOIN Organization.dbo.Hospitals h ON h.Id=d.HospitalId;

PRINT '=== 8. DEFICIENCY -> OBLIGATION PARENT ===';
SELECT
  COUNT(*) AS DefRows,
  SUM(CASE WHEN d.ComplianceObligationId IS NULL THEN 1 ELSE 0 END) AS NullObligationId,
  SUM(CASE WHEN d.ComplianceObligationId IS NOT NULL AND o.Id IS NULL THEN 1 ELSE 0 END) AS OrphanObligation,
  SUM(CASE WHEN o.Id IS NOT NULL AND d.HospitalId <> o.HospitalId THEN 1 ELSE 0 END) AS CrossHospitalParent
FROM Compliance.dbo.ComplianceDeficiencies d
LEFT JOIN Compliance.dbo.OrganizationComplianceObligations o ON o.Id = d.ComplianceObligationId;

PRINT '=== 8b. TASK -> DEFICIENCY HOSPITAL ===';
SELECT
  COUNT(*) AS TaskRows,
  SUM(CASE WHEN t.SourceDeficiencyId IS NOT NULL AND d.Id IS NULL THEN 1 ELSE 0 END) AS OrphanDeficiency,
  SUM(CASE WHEN d.Id IS NOT NULL AND t.HospitalId <> d.HospitalId THEN 1 ELSE 0 END) AS CrossHospitalParent
FROM Compliance.dbo.ComplianceTasks t
LEFT JOIN Compliance.dbo.ComplianceDeficiencies d ON d.Id = t.SourceDeficiencyId;

PRINT '=== 8c. EVIDENCE -> TASK HOSPITAL ===';
SELECT
  COUNT(*) AS EvRows,
  SUM(CASE WHEN e.TaskId IS NOT NULL AND t.Id IS NULL THEN 1 ELSE 0 END) AS OrphanTask,
  SUM(CASE WHEN t.Id IS NOT NULL AND e.HospitalId <> t.HospitalId THEN 1 ELSE 0 END) AS CrossHospitalParent
FROM Compliance.dbo.Evidences e
LEFT JOIN Compliance.dbo.ComplianceTasks t ON t.Id = e.TaskId;

PRINT '=== 8d. CAPA -> FINDING HOSPITAL ===';
SELECT
  COUNT(*) AS CapaRows,
  SUM(CASE WHEN c.SourceFindingId IS NOT NULL AND f.Id IS NULL THEN 1 ELSE 0 END) AS OrphanFinding,
  SUM(CASE WHEN f.Id IS NOT NULL AND j.Id IS NOT NULL AND c.HospitalId <> j.HospitalId THEN 1 ELSE 0 END) AS CrossHospitalViaJourney
FROM Compliance.dbo.Capas c
LEFT JOIN Compliance.dbo.Findings f ON f.Id = c.SourceFindingId
LEFT JOIN Compliance.dbo.ComplianceJourneys j ON j.Id = f.ComplianceJourneyId;

PRINT '=== 8e. FINDING -> JOURNEY HOSPITAL ===';
SELECT
  COUNT(*) AS FindingRows,
  SUM(CASE WHEN f.ComplianceJourneyId IS NOT NULL AND j.Id IS NULL THEN 1 ELSE 0 END) AS OrphanJourney,
  SUM(CASE WHEN j.Id IS NOT NULL AND f.TenantId <> j.TenantId THEN 1 ELSE 0 END) AS TenantMismatch
FROM Compliance.dbo.Findings f
LEFT JOIN Compliance.dbo.ComplianceJourneys j ON j.Id = f.ComplianceJourneyId;

PRINT '=== 8f. OWNERSHIP USER / DEPARTMENT ===';
SELECT
  COUNT(*) AS OwnRows,
  SUM(CASE WHEN u.Id IS NULL THEN 1 ELSE 0 END) AS MissingOwnerUser,
  SUM(CASE WHEN ow.DepartmentId IS NOT NULL AND dept.Id IS NULL THEN 1 ELSE 0 END) AS MissingDepartment,
  SUM(CASE WHEN dept.Id IS NOT NULL AND dept.HospitalId <> ow.HospitalId THEN 1 ELSE 0 END) AS DeptWrongHospital
FROM Compliance.dbo.ObjectiveElementOwnerships ow
LEFT JOIN [Identity].dbo.AspNetUsers u ON u.Id = TRY_CONVERT(uniqueidentifier, ow.PrimaryOwnerUserId)
LEFT JOIN Organization.dbo.Departments dept ON dept.Id = ow.DepartmentId;

PRINT '=== 9. OWNERSHIP org match ===';
SELECT
  CASE
    WHEN h.Id IS NULL THEN 'MISSING_HOSPITAL'
    WHEN h.OrganizationId = ow.OrganizationId THEN 'MATCH'
    ELSE 'MISMATCH'
  END AS OrgOwnership,
  COUNT(*) AS Rows
FROM Compliance.dbo.ObjectiveElementOwnerships ow
LEFT JOIN Organization.dbo.Hospitals h ON h.Id = ow.HospitalId
GROUP BY
  CASE
    WHEN h.Id IS NULL THEN 'MISSING_HOSPITAL'
    WHEN h.OrganizationId = ow.OrganizationId THEN 'MATCH'
    ELSE 'MISMATCH'
  END;

PRINT '=== 10. DASHBOARD / SNAPSHOT HOSPITAL CLASS ===';
SELECT 'DashboardProjections' AS T, COUNT(*) AS Rows, COUNT(DISTINCT HospitalId) AS DistH,
  SUM(CASE WHEN h.Id IS NULL THEN 1 ELSE 0 END) AS MissingH
FROM Compliance.dbo.DashboardProjections p LEFT JOIN Organization.dbo.Hospitals h ON h.Id=p.HospitalId
UNION ALL
SELECT 'ComplianceSnapshots', COUNT(*), COUNT(DISTINCT s.HospitalId), SUM(CASE WHEN h.Id IS NULL THEN 1 ELSE 0 END)
FROM Compliance.dbo.ComplianceSnapshots s LEFT JOIN Organization.dbo.Hospitals h ON h.Id=s.HospitalId
UNION ALL
SELECT 'AnalyticsDailySnapshots', COUNT(*), COUNT(DISTINCT a.HospitalId), SUM(CASE WHEN h.Id IS NULL THEN 1 ELSE 0 END)
FROM Compliance.dbo.AnalyticsDailySnapshots a LEFT JOIN Organization.dbo.Hospitals h ON h.Id=a.HospitalId;

PRINT '=== 11. JOURNEY OWNERSHIP ROLLUP ===';
SELECT
  CASE
    WHEN h.Id IS NULL THEN 'MISSING_HOSPITAL'
    WHEN h.OrganizationId = j.OrganizationId THEN 'MATCH'
    ELSE 'MISMATCH'
  END AS OrgOwnership,
  SUM(CASE WHEN j.OrganizationId = j.HospitalId THEN 1 ELSE 0 END) AS OrgEqHosp,
  COUNT(*) AS Rows
FROM Compliance.dbo.ComplianceJourneys j
LEFT JOIN Organization.dbo.Hospitals h ON h.Id = j.HospitalId
GROUP BY
  CASE
    WHEN h.Id IS NULL THEN 'MISSING_HOSPITAL'
    WHEN h.OrganizationId = j.OrganizationId THEN 'MATCH'
    ELSE 'MISMATCH'
  END;

PRINT '=== 12. DASHBOARD ORG MATCH ===';
SELECT
  CASE
    WHEN h.Id IS NULL THEN 'MISSING_HOSPITAL'
    WHEN h.OrganizationId = p.OrganizationId THEN 'MATCH'
    ELSE 'MISMATCH'
  END AS OrgOwnership,
  COUNT(*) AS Rows
FROM Compliance.dbo.DashboardProjections p
LEFT JOIN Organization.dbo.Hospitals h ON h.Id = p.HospitalId
GROUP BY
  CASE
    WHEN h.Id IS NULL THEN 'MISSING_HOSPITAL'
    WHEN h.OrganizationId = p.OrganizationId THEN 'MATCH'
    ELSE 'MISMATCH'
  END;

PRINT '=== 13. TASK ASSIGNED USER ORPHANS ===';
SELECT
  COUNT(*) AS Tasks,
  SUM(CASE WHEN t.AssignedToUserId IS NOT NULL AND u.Id IS NULL THEN 1 ELSE 0 END) AS MissingAssignee
FROM Compliance.dbo.ComplianceTasks t
LEFT JOIN [Identity].dbo.AspNetUsers u ON u.Id = t.AssignedToUserId;

PRINT '=== 14. OUTBOX ARTIFACT / ENROLL TYPES ===';
SELECT Type, COUNT(*) AS C
FROM Compliance.dbo.OutboxMessages
WHERE Type LIKE '%Enroll%' OR Type LIKE '%Framework%'
GROUP BY Type;

PRINT '=== 15. SLA / MATURITY HOSPITAL CLASS ===';
SELECT 'SlaPolicies' AS T, COUNT(*) AS Rows, SUM(CASE WHEN h.Id IS NULL THEN 1 ELSE 0 END) AS MissingH
FROM Compliance.dbo.SlaPolicies s LEFT JOIN Organization.dbo.Hospitals h ON h.Id=s.HospitalId
UNION ALL
SELECT 'OrganizationMaturityStageStates', COUNT(*), SUM(CASE WHEN h.Id IS NULL THEN 1 ELSE 0 END)
FROM Compliance.dbo.OrganizationMaturityStageStates m LEFT JOIN Organization.dbo.Hospitals h ON h.Id=m.HospitalId;

PRINT '=== 16. OBLIGATION CreatedBy / dates for EQ pairs ===';
SELECT CreatedBy, CAST(MIN(CreatedAt) AS date) AS MinD, CAST(MAX(CreatedAt) AS date) AS MaxD, COUNT(*) AS Cnt
FROM Compliance.dbo.OrganizationComplianceObligations
WHERE OrganizationId = HospitalId
GROUP BY CreatedBy;

PRINT '=== 17. OBLIGATION CreatedBy DISTINCT pairs MATCH ===';
SELECT CreatedBy, CAST(MIN(CreatedAt) AS date) AS MinD, CAST(MAX(CreatedAt) AS date) AS MaxD, COUNT(*) AS Cnt
FROM Compliance.dbo.OrganizationComplianceObligations
WHERE OrganizationId <> HospitalId
GROUP BY CreatedBy;

