/*
  Platform-default configuration values for the Audit workspace.

  Mirrors AUDIT_CONFIGURATION_CATALOGS in the web app's audit-configuration.catalogs.ts. The client
  keeps that seed so an unreachable configuration API degrades to identical behaviour rather than
  empty pickers; these rows are what makes the same values *editable* by an administrator.

  Data only — no schema changes. Idempotent: each category and its values are inserted only when
  absent, so re-running never disturbs a tenant's overrides or an edited platform default.

  Generated from the catalogs file; regenerate rather than hand-editing so the two cannot drift.
*/

-- QUOTED_IDENTIFIER and ANSI_NULLS must be ON for DML against these tables: they carry filtered
-- unique indexes (platform-default vs tenant-override on the same code), and SQL Server refuses
-- INSERT/UPDATE on such a table when either option is OFF. sqlcmd defaults both OFF, so without
-- this the scripts fail on the documented deployment path while succeeding in SSMS (DEF-RC2-002).
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

SET NOCOUNT ON;
GO

------------------------------------------------------------------------------
-- AuditType — Kinds of audit the platform records, schedules and executes.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'AuditType');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'AuditType', 'Audit Type', 'Kinds of audit the platform records, schedules and executes.', 1, 0);
    PRINT 'Created category AuditType.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES
      (NEWID(), @cat, NULL, 'InternalAudit', 'Internal', 10, 1, N'{"selectableWhenScheduling":true,"selectableWhenStartingSession":true}'),
      (NEWID(), @cat, NULL, 'DepartmentAudit', 'Department', 20, 1, N'{"selectableWhenScheduling":true}'),
      (NEWID(), @cat, NULL, 'FollowUpAudit', 'Follow-up', 30, 1, N'{"selectableWhenScheduling":true}'),
      (NEWID(), @cat, NULL, 'MockAudit', 'Mock NABH', 40, 1, N'{"selectableWhenScheduling":true,"selectableWhenStartingSession":true}'),
      (NEWID(), @cat, NULL, 'FocusedAudit', 'Focused', 50, 1, N'{"selectableWhenScheduling":true,"selectableWhenStartingSession":true}'),
      (NEWID(), @cat, NULL, 'Surveillance', 'Surveillance', 60, 1, N'{"selectableWhenScheduling":true,"selectableWhenStartingSession":true,"surveillance":true}'),
      (NEWID(), @cat, NULL, 'ConsultantReview', 'Consultant Review', 70, 1, N'{"selectableWhenScheduling":false,"selectableWhenStartingSession":true,"external":true}'),
      (NEWID(), @cat, NULL, 'NabhPreAssessment', 'NABH Pre-Assessment', 80, 1, N'{"selectableWhenScheduling":false,"selectableWhenStartingSession":true,"external":true}'),
      (NEWID(), @cat, NULL, 'NabhFinalAssessment', 'NABH Final Assessment', 90, 1, N'{"selectableWhenScheduling":false,"selectableWhenStartingSession":true,"external":true}');
    PRINT 'Seeded 9 platform-default AuditType values.';
END
ELSE PRINT 'AuditType values already present — left unchanged.';

------------------------------------------------------------------------------
-- AuditType metadata refresh
--
-- AuditType is the one category seeded by an earlier script (2026-08-02), and those rows predate
-- `selectableWhenStartingSession`. Without this, a database that ran that script would serve
-- AuditType rows lacking the attribute, and the Auditor "start a session" picker — which selects on
-- it — would render empty. An absent attribute means "not set", so the branch above cannot repair
-- it: the values exist, they are just describing less than they now need to.
--
-- Scoped to platform defaults (OrganizationId IS NULL). A tenant's overrides are that tenant's
-- decisions and are never rewritten here.
------------------------------------------------------------------------------
UPDATE v
SET Metadata = m.Metadata
FROM OrganizationLookupValues v
JOIN (VALUES
      ('InternalAudit',       N'{"selectableWhenScheduling":true,"selectableWhenStartingSession":true}'),
      ('DepartmentAudit',     N'{"selectableWhenScheduling":true}'),
      ('FollowUpAudit',       N'{"selectableWhenScheduling":true}'),
      ('MockAudit',           N'{"selectableWhenScheduling":true,"selectableWhenStartingSession":true}'),
      ('FocusedAudit',        N'{"selectableWhenScheduling":true,"selectableWhenStartingSession":true}'),
      ('Surveillance',        N'{"selectableWhenScheduling":true,"selectableWhenStartingSession":true,"surveillance":true}'),
      ('ConsultantReview',    N'{"selectableWhenScheduling":false,"selectableWhenStartingSession":true,"external":true}'),
      ('NabhPreAssessment',   N'{"selectableWhenScheduling":false,"selectableWhenStartingSession":true,"external":true}'),
      ('NabhFinalAssessment', N'{"selectableWhenScheduling":false,"selectableWhenStartingSession":true,"external":true}')
     ) AS m (Code, Metadata) ON m.Code = v.Code
WHERE v.CategoryId = @cat
  AND v.OrganizationId IS NULL
  AND (v.Metadata IS NULL OR v.Metadata NOT LIKE '%selectableWhenStartingSession%');

IF @@ROWCOUNT > 0 PRINT 'Refreshed AuditType platform-default metadata.';
GO

------------------------------------------------------------------------------
-- AuditRecurrence — Repeat cadence offered when scheduling an audit programme.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'AuditRecurrence');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'AuditRecurrence', 'Audit Recurrence', 'Repeat cadence offered when scheduling an audit programme.', 1, 0);
    PRINT 'Created category AuditRecurrence.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES
      (NEWID(), @cat, NULL, 'none', 'None', 10, 1, N'{"occurrences":1,"intervalMonths":0}'),
      (NEWID(), @cat, NULL, 'monthly', 'Monthly ×3', 20, 1, N'{"occurrences":3,"intervalMonths":1}'),
      (NEWID(), @cat, NULL, 'quarterly', 'Quarterly ×4', 30, 1, N'{"occurrences":4,"intervalMonths":3}');
    PRINT 'Seeded 3 platform-default AuditRecurrence values.';
END
ELSE PRINT 'AuditRecurrence values already present — left unchanged.';
GO

------------------------------------------------------------------------------
-- AuditSessionStatus — Lifecycle states of an audit execution session.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'AuditSessionStatus');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'AuditSessionStatus', 'Audit Session Status', 'Lifecycle states of an audit execution session.', 1, 0);
    PRINT 'Created category AuditSessionStatus.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES
      (NEWID(), @cat, NULL, 'Planned', 'Planned', 10, 1, N'{"lifecycleStep":true,"selectableWhenFiltering":true}'),
      (NEWID(), @cat, NULL, 'InProgress', 'In Progress', 20, 1, N'{"lifecycleStep":true,"selectableWhenFiltering":true}'),
      (NEWID(), @cat, NULL, 'Review', 'Review', 30, 1, N'{"lifecycleStep":true,"selectableWhenFiltering":true}'),
      (NEWID(), @cat, NULL, 'DraftReport', 'Draft Report', 40, 1, N'{"lifecycleStep":true,"selectableWhenFiltering":true}'),
      (NEWID(), @cat, NULL, 'Submitted', 'Submitted', 50, 1, N'{"lifecycleStep":true,"selectableWhenFiltering":true,"terminal":true}'),
      (NEWID(), @cat, NULL, 'Closed', 'Closed', 60, 1, N'{"lifecycleStep":true,"selectableWhenFiltering":true,"terminal":true}');
    PRINT 'Seeded 6 platform-default AuditSessionStatus values.';
END
ELSE PRINT 'AuditSessionStatus values already present — left unchanged.';
GO

------------------------------------------------------------------------------
-- AuditSessionResult — Compliance verdict recorded when an audit session closes.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'AuditSessionResult');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'AuditSessionResult', 'Audit Session Result', 'Compliance verdict recorded when an audit session closes.', 1, 0);
    PRINT 'Created category AuditSessionResult.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES
      (NEWID(), @cat, NULL, 'NotDetermined', 'Not Determined', 10, 1, NULL),
      (NEWID(), @cat, NULL, 'Compliant', 'Compliant', 20, 1, NULL),
      (NEWID(), @cat, NULL, 'ConditionallyCompliant', 'Conditionally Compliant', 30, 1, NULL),
      (NEWID(), @cat, NULL, 'PartiallyCompliant', 'Partially Compliant', 40, 1, NULL),
      (NEWID(), @cat, NULL, 'NonCompliant', 'Non-Compliant', 50, 1, NULL);
    PRINT 'Seeded 5 platform-default AuditSessionResult values.';
END
ELSE PRINT 'AuditSessionResult values already present — left unchanged.';
GO

------------------------------------------------------------------------------
-- ScheduledAuditStatus — States of an audit on the schedule, before and after execution.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'ScheduledAuditStatus');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'ScheduledAuditStatus', 'Scheduled Audit Status', 'States of an audit on the schedule, before and after execution.', 1, 0);
    PRINT 'Created category ScheduledAuditStatus.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES
      (NEWID(), @cat, NULL, 'Scheduled', 'Scheduled', 10, 1, NULL),
      (NEWID(), @cat, NULL, 'InProgress', 'In Progress', 20, 1, NULL),
      (NEWID(), @cat, NULL, 'Completed', 'Completed', 30, 1, N'{"terminal":true}'),
      (NEWID(), @cat, NULL, 'Closed', 'Closed', 40, 1, N'{"terminal":true}'),
      (NEWID(), @cat, NULL, 'Cancelled', 'Cancelled', 50, 1, NULL);
    PRINT 'Seeded 5 platform-default ScheduledAuditStatus values.';
END
ELSE PRINT 'ScheduledAuditStatus values already present — left unchanged.';
GO

------------------------------------------------------------------------------
-- ObservationType — Kinds of observation recorded during an audit session.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'ObservationType');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'ObservationType', 'Observation Type', 'Kinds of observation recorded during an audit session.', 1, 0);
    PRINT 'Created category ObservationType.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES
      (NEWID(), @cat, NULL, 'PositivePractice', 'Positive Practice', 10, 1, NULL),
      (NEWID(), @cat, NULL, 'Suggestion', 'Suggestion', 20, 1, NULL),
      (NEWID(), @cat, NULL, 'MinorObservation', 'Minor Observation', 30, 1, NULL),
      (NEWID(), @cat, NULL, 'Strength', 'Strength', 40, 1, NULL),
      (NEWID(), @cat, NULL, 'Opportunity', 'Opportunity', 50, 1, NULL),
      (NEWID(), @cat, NULL, 'GeneralComment', 'General Comment', 60, 1, NULL);
    PRINT 'Seeded 6 platform-default ObservationType values.';
END
ELSE PRINT 'ObservationType values already present — left unchanged.';
GO

------------------------------------------------------------------------------
-- ReportPeriod — Time windows offered when generating audit reports.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'ReportPeriod');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'ReportPeriod', 'Report Period', 'Time windows offered when generating audit reports.', 1, 0);
    PRINT 'Created category ReportPeriod.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES
      (NEWID(), @cat, NULL, 'programYear', 'Current programme year', 10, 1, NULL),
      (NEWID(), @cat, NULL, 'quarter', 'Last 3 months', 20, 1, NULL),
      (NEWID(), @cat, NULL, 'month', 'Last 30 days', 30, 1, NULL);
    PRINT 'Seeded 3 platform-default ReportPeriod values.';
END
ELSE PRINT 'ReportPeriod values already present — left unchanged.';
GO

------------------------------------------------------------------------------
-- FindingStatus — Lifecycle states a finding moves through.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'FindingStatus');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'FindingStatus', 'Finding Status', 'Lifecycle states a finding moves through.', 1, 0);
    PRINT 'Created category FindingStatus.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES
      (NEWID(), @cat, NULL, 'Draft', 'Draft', 10, 1, N'{"lifecycleStep":true,"detailStep":true,"selectableWhenFiltering":true,"selectableWhenTransitioning":true,"selectableWhenCreating":true}'),
      (NEWID(), @cat, NULL, 'Open', 'Open', 20, 1, N'{"lifecycleStep":true,"detailStep":true,"selectableWhenFiltering":true,"selectableWhenTransitioning":true,"selectableWhenCreating":true}'),
      (NEWID(), @cat, NULL, 'Reviewed', 'Reviewed', 30, 1, N'{"lifecycleStep":true,"detailStep":true,"selectableWhenFiltering":true,"selectableWhenTransitioning":true}'),
      (NEWID(), @cat, NULL, 'Accepted', 'Accepted', 40, 1, N'{"lifecycleStep":true,"detailStep":true,"selectableWhenFiltering":true,"selectableWhenTransitioning":true}'),
      (NEWID(), @cat, NULL, 'Promoted', 'Promoted', 50, 1, N'{"detailStep":true,"persisted":false}'),
      (NEWID(), @cat, NULL, 'Closed', 'Closed', 60, 1, N'{"lifecycleStep":true,"detailStep":true,"selectableWhenFiltering":true,"selectableWhenTransitioning":true}'),
      (NEWID(), @cat, NULL, 'InProgress', 'In Progress', 70, 1, NULL),
      (NEWID(), @cat, NULL, 'Verified', 'Verified', 80, 1, NULL);
    PRINT 'Seeded 8 platform-default FindingStatus values.';
END
ELSE PRINT 'FindingStatus values already present — left unchanged.';
GO

------------------------------------------------------------------------------
-- FindingSource — Origin of a finding, as raised.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'FindingSource');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'FindingSource', 'Finding Source', 'Origin of a finding, as raised.', 1, 0);
    PRINT 'Created category FindingSource.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES
      (NEWID(), @cat, NULL, 'InternalAudit', 'Internal Audit', 10, 1, N'{"ordinal":1}'),
      (NEWID(), @cat, NULL, 'ExternalAudit', 'External Audit', 20, 1, N'{"ordinal":2}'),
      (NEWID(), @cat, NULL, 'ConsultantAssessment', 'Consultant Assessment', 30, 1, N'{"ordinal":4}'),
      (NEWID(), @cat, NULL, 'NabhAssessment', 'NABH Assessment', 40, 1, N'{"ordinal":5}'),
      (NEWID(), @cat, NULL, 'ManagementReview', 'Management Review', 50, 1, N'{"ordinal":6}'),
      (NEWID(), @cat, NULL, 'DepartmentAssessment', 'Department Assessment', 60, 1, N'{"ordinal":7}'),
      (NEWID(), @cat, NULL, 'IncidentReview', 'Incident Investigation', 70, 1, N'{"ordinal":8}'),
      (NEWID(), @cat, NULL, 'ComplianceTeam', 'Compliance Team', 80, 1, N'{"ordinal":3}');
    PRINT 'Seeded 8 platform-default FindingSource values.';
END
ELSE PRINT 'FindingSource values already present — left unchanged.';
GO

------------------------------------------------------------------------------
-- FindingClassification — Category of finding raised during audit.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'FindingClassification');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'FindingClassification', 'Finding Classification', 'Category of finding raised during audit.', 1, 0);
    PRINT 'Created category FindingClassification.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES
      (NEWID(), @cat, NULL, 'AuditFinding', 'Audit Finding', 10, 1, NULL),
      (NEWID(), @cat, NULL, 'Observation', 'Observation', 20, 1, NULL),
      (NEWID(), @cat, NULL, 'NonConformity', 'Non-Conformity', 30, 1, NULL),
      (NEWID(), @cat, NULL, 'OpportunityForImprovement', 'Opportunity For Improvement', 40, 1, N'{"shortLabel":"OFI"}'),
      (NEWID(), @cat, NULL, 'GoodPractice', 'Good Practice', 50, 1, NULL);
    PRINT 'Seeded 5 platform-default FindingClassification values.';
END
ELSE PRINT 'FindingClassification values already present — left unchanged.';
GO

------------------------------------------------------------------------------
-- FindingSeverity — How serious a finding is.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'FindingSeverity');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'FindingSeverity', 'Finding Severity', 'How serious a finding is.', 1, 0);
    PRINT 'Created category FindingSeverity.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES
      (NEWID(), @cat, NULL, 'Critical', 'Critical', 10, 1, N'{"badgeToken":"critical"}'),
      (NEWID(), @cat, NULL, 'Major', 'Major', 20, 1, N'{"badgeToken":"major"}'),
      (NEWID(), @cat, NULL, 'Moderate', 'Moderate', 30, 1, N'{"badgeToken":"moderate"}'),
      (NEWID(), @cat, NULL, 'Minor', 'Minor', 40, 1, N'{"badgeToken":"minor"}'),
      (NEWID(), @cat, NULL, 'Informational', 'Informational', 50, 1, N'{"badgeToken":"informational"}');
    PRINT 'Seeded 5 platform-default FindingSeverity values.';
END
ELSE PRINT 'FindingSeverity values already present — left unchanged.';
GO

------------------------------------------------------------------------------
-- FindingAttachmentType — Kinds of evidence attachable to a finding.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'FindingAttachmentType');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'FindingAttachmentType', 'Finding Attachment Type', 'Kinds of evidence attachable to a finding.', 1, 0);
    PRINT 'Created category FindingAttachmentType.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES
      (NEWID(), @cat, NULL, 'Document', 'Document', 10, 1, NULL),
      (NEWID(), @cat, NULL, 'Image', 'Image', 20, 1, NULL),
      (NEWID(), @cat, NULL, 'AuditNote', 'Audit Note', 30, 1, NULL),
      (NEWID(), @cat, NULL, 'Report', 'Report', 40, 1, NULL),
      (NEWID(), @cat, NULL, 'Screenshot', 'Screenshot', 50, 1, NULL);
    PRINT 'Seeded 5 platform-default FindingAttachmentType values.';
END
ELSE PRINT 'FindingAttachmentType values already present — left unchanged.';
GO

------------------------------------------------------------------------------
-- FindingQuickFilter — Saved filters offered as chips on the findings workbench.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'FindingQuickFilter');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'FindingQuickFilter', 'Finding Quick Filter', 'Saved filters offered as chips on the findings workbench.', 1, 0);
    PRINT 'Created category FindingQuickFilter.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES
      (NEWID(), @cat, NULL, 'needsReview', 'Needs Review', 10, 1, NULL),
      (NEWID(), @cat, NULL, 'readyForPromotion', 'Ready For Promotion', 20, 1, NULL),
      (NEWID(), @cat, NULL, 'recentlyAdded', 'Recently Added', 30, 1, NULL),
      (NEWID(), @cat, NULL, 'critical', 'Critical', 40, 1, NULL);
    PRINT 'Seeded 4 platform-default FindingQuickFilter values.';
END
ELSE PRINT 'FindingQuickFilter values already present — left unchanged.';
GO

------------------------------------------------------------------------------
-- PromotionReadiness — Readiness of a finding for promotion to a deficiency.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'PromotionReadiness');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'PromotionReadiness', 'Promotion Readiness', 'Readiness of a finding for promotion to a deficiency.', 1, 0);
    PRINT 'Created category PromotionReadiness.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES
      (NEWID(), @cat, NULL, 'Not Eligible', 'Not Eligible', 10, 1, NULL),
      (NEWID(), @cat, NULL, 'Awaiting Acceptance', 'Awaiting Acceptance', 20, 1, NULL),
      (NEWID(), @cat, NULL, 'Eligible', 'Eligible', 30, 1, NULL),
      (NEWID(), @cat, NULL, 'Promoted', 'Promoted', 40, 1, NULL);
    PRINT 'Seeded 4 platform-default PromotionReadiness values.';
END
ELSE PRINT 'PromotionReadiness values already present — left unchanged.';
GO

------------------------------------------------------------------------------
-- DeficiencyStatus — Lifecycle states a deficiency moves through.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'DeficiencyStatus');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'DeficiencyStatus', 'Deficiency Status', 'Lifecycle states a deficiency moves through.', 1, 0);
    PRINT 'Created category DeficiencyStatus.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES
      (NEWID(), @cat, NULL, 'Identified', 'Identified', 10, 1, N'{"lifecycleStep":true,"selectableWhenFiltering":true}'),
      (NEWID(), @cat, NULL, 'Assigned', 'Assigned', 20, 1, N'{"lifecycleStep":true,"selectableWhenFiltering":true}'),
      (NEWID(), @cat, NULL, 'InProgress', 'In Progress', 30, 1, N'{"lifecycleStep":true,"selectableWhenFiltering":true}'),
      (NEWID(), @cat, NULL, 'EvidenceSubmitted', 'Awaiting Evidence', 40, 1, N'{"lifecycleStep":true,"selectableWhenFiltering":true}'),
      (NEWID(), @cat, NULL, 'ReviewPending', 'Verification Pending', 50, 1, N'{"lifecycleStep":true,"selectableWhenFiltering":true}'),
      (NEWID(), @cat, NULL, 'Verified', 'Verified', 60, 1, N'{"lifecycleStep":true,"selectableWhenFiltering":true}'),
      (NEWID(), @cat, NULL, 'Closed', 'Closed', 70, 1, N'{"lifecycleStep":true,"selectableWhenFiltering":true}'),
      (NEWID(), @cat, NULL, 'Cancelled', 'Cancelled', 80, 1, NULL);
    PRINT 'Seeded 8 platform-default DeficiencyStatus values.';
END
ELSE PRINT 'DeficiencyStatus values already present — left unchanged.';
GO

------------------------------------------------------------------------------
-- DeficiencySeverity — How serious a deficiency is.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'DeficiencySeverity');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'DeficiencySeverity', 'Deficiency Severity', 'How serious a deficiency is.', 1, 0);
    PRINT 'Created category DeficiencySeverity.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES
      (NEWID(), @cat, NULL, 'Critical', 'Critical', 10, 1, N'{"badgeToken":"critical"}'),
      (NEWID(), @cat, NULL, 'Major', 'Major', 20, 1, N'{"badgeToken":"major"}'),
      (NEWID(), @cat, NULL, 'Moderate', 'Moderate', 30, 1, N'{"badgeToken":"moderate"}'),
      (NEWID(), @cat, NULL, 'Minor', 'Minor', 40, 1, N'{"badgeToken":"minor"}');
    PRINT 'Seeded 4 platform-default DeficiencySeverity values.';
END
ELSE PRINT 'DeficiencySeverity values already present — left unchanged.';
GO

------------------------------------------------------------------------------
-- DeficiencyGrouping — Ways the CAPA workspace may group its rows.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'DeficiencyGrouping');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'DeficiencyGrouping', 'Deficiency Grouping', 'Ways the CAPA workspace may group its rows.', 1, 0);
    PRINT 'Created category DeficiencyGrouping.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES
      (NEWID(), @cat, NULL, 'severity', 'Group by Severity', 10, 1, N'{"field":"severity","labelFromCategory":"DeficiencySeverity"}'),
      (NEWID(), @cat, NULL, 'stage', 'Group by Stage', 20, 1, N'{"field":"maturityLevel","labelFromCategory":"MaturityStage"}'),
      (NEWID(), @cat, NULL, 'status', 'Group by Status', 30, 1, N'{"field":"status","labelFromCategory":"DeficiencyStatus"}');
    PRINT 'Seeded 3 platform-default DeficiencyGrouping values.';
END
ELSE PRINT 'DeficiencyGrouping values already present — left unchanged.';
GO

------------------------------------------------------------------------------
-- RiskLevel — Assessed risk rating of a deficiency.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'RiskLevel');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'RiskLevel', 'Risk Level', 'Assessed risk rating of a deficiency.', 1, 0);
    PRINT 'Created category RiskLevel.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES
      (NEWID(), @cat, NULL, 'Low', 'Low', 10, 1, N'{"badgeClass":"risk--low"}'),
      (NEWID(), @cat, NULL, 'Medium', 'Medium', 20, 1, N'{"badgeClass":"risk--medium"}'),
      (NEWID(), @cat, NULL, 'High', 'High', 30, 1, N'{"badgeClass":"risk--high"}'),
      (NEWID(), @cat, NULL, 'Critical', 'Critical', 40, 1, N'{"badgeClass":"risk--critical"}');
    PRINT 'Seeded 4 platform-default RiskLevel values.';
END
ELSE PRINT 'RiskLevel values already present — left unchanged.';
GO

------------------------------------------------------------------------------
-- RootCauseCategory — Root cause taxonomy for deficiency analysis.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'RootCauseCategory');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'RootCauseCategory', 'Root Cause Category', 'Root cause taxonomy for deficiency analysis.', 1, 0);
    PRINT 'Created category RootCauseCategory.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES
      (NEWID(), @cat, NULL, 'PolicyMissing', 'Policy Missing', 10, 1, NULL),
      (NEWID(), @cat, NULL, 'EvidenceMissing', 'Evidence Missing', 20, 1, NULL),
      (NEWID(), @cat, NULL, 'TrainingMissing', 'Training Missing', 30, 1, NULL),
      (NEWID(), @cat, NULL, 'ProcessFailure', 'Process Failure', 40, 1, NULL),
      (NEWID(), @cat, NULL, 'DocumentationIssue', 'Documentation Issue', 50, 1, NULL),
      (NEWID(), @cat, NULL, 'VerificationFailure', 'Verification Failure', 60, 1, NULL),
      (NEWID(), @cat, NULL, 'Other', 'Other', 70, 1, NULL);
    PRINT 'Seeded 7 platform-default RootCauseCategory values.';
END
ELSE PRINT 'RootCauseCategory values already present — left unchanged.';
GO

------------------------------------------------------------------------------
-- CapaActionStatus — Progress of a corrective or preventive action.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'CapaActionStatus');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'CapaActionStatus', 'Capa Action Status', 'Progress of a corrective or preventive action.', 1, 0);
    PRINT 'Created category CapaActionStatus.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES
      (NEWID(), @cat, NULL, 'NotStarted', 'Not Started', 10, 1, NULL),
      (NEWID(), @cat, NULL, 'InProgress', 'In Progress', 20, 1, NULL),
      (NEWID(), @cat, NULL, 'Completed', 'Completed', 30, 1, NULL);
    PRINT 'Seeded 3 platform-default CapaActionStatus values.';
END
ELSE PRINT 'CapaActionStatus values already present — left unchanged.';
GO

------------------------------------------------------------------------------
-- EffectivenessReviewStatus — Outcome of a CAPA effectiveness review.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'EffectivenessReviewStatus');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'EffectivenessReviewStatus', 'Effectiveness Review Status', 'Outcome of a CAPA effectiveness review.', 1, 0);
    PRINT 'Created category EffectivenessReviewStatus.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES
      (NEWID(), @cat, NULL, 'NotRequired', 'Not Required', 10, 1, NULL),
      (NEWID(), @cat, NULL, 'Pending', 'Pending', 20, 1, NULL),
      (NEWID(), @cat, NULL, 'Scheduled', 'Scheduled', 30, 1, NULL),
      (NEWID(), @cat, NULL, 'Effective', 'Effective', 40, 1, NULL),
      (NEWID(), @cat, NULL, 'NotEffective', 'Not Effective', 50, 1, NULL);
    PRINT 'Seeded 5 platform-default EffectivenessReviewStatus values.';
END
ELSE PRINT 'EffectivenessReviewStatus values already present — left unchanged.';
GO

------------------------------------------------------------------------------
-- EffectivenessReviewSchedule — Offsets at which a CAPA effectiveness review may be scheduled.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'EffectivenessReviewSchedule');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'EffectivenessReviewSchedule', 'Effectiveness Review Schedule', 'Offsets at which a CAPA effectiveness review may be scheduled.', 1, 0);
    PRINT 'Created category EffectivenessReviewSchedule.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES
      (NEWID(), @cat, NULL, '30', '30 Day Review', 10, 1, N'{"days":30}'),
      (NEWID(), @cat, NULL, '60', '60 Day Review', 20, 1, N'{"days":60}'),
      (NEWID(), @cat, NULL, '90', '90 Day Review', 30, 1, N'{"days":90}');
    PRINT 'Seeded 3 platform-default EffectivenessReviewSchedule values.';
END
ELSE PRINT 'EffectivenessReviewSchedule values already present — left unchanged.';
GO

------------------------------------------------------------------------------
-- MaturityStage — Accreditation maturity stages a deficiency may block.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'MaturityStage');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'MaturityStage', 'Maturity Stage', 'Accreditation maturity stages a deficiency may block.', 1, 0);
    PRINT 'Created category MaturityStage.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES
      (NEWID(), @cat, NULL, 'Core', 'Core', 10, 1, NULL),
      (NEWID(), @cat, NULL, 'Commitment', 'Commitment', 20, 1, NULL),
      (NEWID(), @cat, NULL, 'Achievement', 'Achievement', 30, 1, NULL),
      (NEWID(), @cat, NULL, 'Excellence', 'Excellence', 40, 1, NULL);
    PRINT 'Seeded 4 platform-default MaturityStage values.';
END
ELSE PRINT 'MaturityStage values already present — left unchanged.';
GO

------------------------------------------------------------------------------
-- Department — Departments findings and deficiencies are attributed to.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'Department');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'Department', 'Department', 'Departments findings and deficiencies are attributed to.', 1, 0);
    PRINT 'Created category Department.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES
      (NEWID(), @cat, NULL, 'hr', 'HR', 10, 1, NULL),
      (NEWID(), @cat, NULL, 'security', 'Security', 20, 1, NULL),
      (NEWID(), @cat, NULL, 'clinical', 'Clinical', 30, 1, NULL),
      (NEWID(), @cat, NULL, 'admin', 'Admin', 40, 1, NULL);
    PRINT 'Seeded 4 platform-default Department values.';
END
ELSE PRINT 'Department values already present — left unchanged.';
GO

------------------------------------------------------------------------------
-- SlaStatus — States of a deficiency remediation SLA clock.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'SlaStatus');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'SlaStatus', 'Sla Status', 'States of a deficiency remediation SLA clock.', 1, 0);
    PRINT 'Created category SlaStatus.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES
      (NEWID(), @cat, NULL, 'OnTrack', 'On Track', 10, 1, N'{"badgeClass":"sla sla--ontrack"}'),
      (NEWID(), @cat, NULL, 'Warning', 'Warning', 20, 1, N'{"badgeClass":"sla sla--warning"}'),
      (NEWID(), @cat, NULL, 'AtRisk', 'At Risk', 30, 1, N'{"badgeClass":"sla sla--atrisk"}'),
      (NEWID(), @cat, NULL, 'Breached', 'Breached', 40, 1, N'{"badgeClass":"sla sla--breached"}'),
      (NEWID(), @cat, NULL, 'Closed', 'Closed', 50, 1, N'{"badgeClass":"sla sla--closed","fallback":true}');
    PRINT 'Seeded 5 platform-default SlaStatus values.';
END
ELSE PRINT 'SlaStatus values already present — left unchanged.';
GO

------------------------------------------------------------------------------
-- AssignmentConfidence — Confidence of an automated assignment suggestion.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'AssignmentConfidence');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'AssignmentConfidence', 'Assignment Confidence', 'Confidence of an automated assignment suggestion.', 1, 0);
    PRINT 'Created category AssignmentConfidence.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES
      (NEWID(), @cat, NULL, 'None', 'None', 10, 1, N'{"badgeClass":"conf conf--none","fallback":true}'),
      (NEWID(), @cat, NULL, 'Low', 'Low', 20, 1, N'{"badgeClass":"conf conf--low"}'),
      (NEWID(), @cat, NULL, 'Medium', 'Medium', 30, 1, N'{"badgeClass":"conf conf--medium"}'),
      (NEWID(), @cat, NULL, 'High', 'High', 40, 1, N'{"badgeClass":"conf conf--high"}');
    PRINT 'Seeded 4 platform-default AssignmentConfidence values.';
END
ELSE PRINT 'AssignmentConfidence values already present — left unchanged.';
GO

------------------------------------------------------------------------------
-- Verification
------------------------------------------------------------------------------
SELECT c.Code AS Category, COUNT(v.Id) AS PlatformDefaults
FROM OrganizationLookupCategories c
LEFT JOIN OrganizationLookupValues v ON v.CategoryId = c.Id AND v.OrganizationId IS NULL
GROUP BY c.Code
ORDER BY c.Code;
GO