/*
  Platform-default values for the Enterprise Administration configuration domains (Stage 6).

  Generated from ADMINISTRATION_CONFIGURATION_CATALOGS in the web app's
  administration-configuration.catalogs.ts. Data only — no schema changes. Idempotent: categories and
  values are inserted only when absent, so re-running never disturbs a tenant's overrides.

  Applying this is what makes these domains editable in the Configuration Center. Without it the
  client still behaves correctly — it runs on the same values as its in-code seed — but an
  administrator has nothing to edit.
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
-- RiskLikelihood — Likelihood scale for risk scoring. Removing values resizes the matrix.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'RiskLikelihood');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'RiskLikelihood', 'Risk Likelihood', 'Likelihood scale for risk scoring. Removing values resizes the matrix.', 1, 0);
    PRINT 'Created category RiskLikelihood.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES
      (NEWID(), @cat, NULL, '1', 'Rare', 10, 1, N'{"level":1}'),
      (NEWID(), @cat, NULL, '2', 'Unlikely', 20, 1, N'{"level":2}'),
      (NEWID(), @cat, NULL, '3', 'Possible', 30, 1, N'{"level":3}'),
      (NEWID(), @cat, NULL, '4', 'Likely', 40, 1, N'{"level":4}'),
      (NEWID(), @cat, NULL, '5', 'Almost Certain', 50, 1, N'{"level":5}');
    PRINT 'Seeded 5 platform-default RiskLikelihood values.';
END
ELSE PRINT 'RiskLikelihood values already present — left unchanged.';
GO

------------------------------------------------------------------------------
-- RiskImpact — Impact scale for risk scoring. Removing values resizes the matrix.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'RiskImpact');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'RiskImpact', 'Risk Impact', 'Impact scale for risk scoring. Removing values resizes the matrix.', 1, 0);
    PRINT 'Created category RiskImpact.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES
      (NEWID(), @cat, NULL, '1', 'Insignificant', 10, 1, N'{"level":1}'),
      (NEWID(), @cat, NULL, '2', 'Minor', 20, 1, N'{"level":2}'),
      (NEWID(), @cat, NULL, '3', 'Moderate', 30, 1, N'{"level":3}'),
      (NEWID(), @cat, NULL, '4', 'Major', 40, 1, N'{"level":4}'),
      (NEWID(), @cat, NULL, '5', 'Catastrophic', 50, 1, N'{"level":5}');
    PRINT 'Seeded 5 platform-default RiskImpact values.';
END
ELSE PRINT 'RiskImpact values already present — left unchanged.';
GO

------------------------------------------------------------------------------
-- RiskCategory — Taxonomy a risk may be classified under.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'RiskCategory');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'RiskCategory', 'Risk Category', 'Taxonomy a risk may be classified under.', 1, 0);
    PRINT 'Created category RiskCategory.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES
      (NEWID(), @cat, NULL, 'PatientSafety', 'Patient Safety', 10, 1, NULL),
      (NEWID(), @cat, NULL, 'Clinical', 'Clinical', 20, 1, NULL),
      (NEWID(), @cat, NULL, 'Operational', 'Operational', 30, 1, NULL),
      (NEWID(), @cat, NULL, 'Regulatory', 'Regulatory', 40, 1, NULL),
      (NEWID(), @cat, NULL, 'Financial', 'Financial', 50, 1, NULL),
      (NEWID(), @cat, NULL, 'InformationSecurity', 'Information Security', 60, 1, NULL),
      (NEWID(), @cat, NULL, 'Reputational', 'Reputational', 70, 1, NULL),
      (NEWID(), @cat, NULL, 'Strategic', 'Strategic', 80, 1, NULL);
    PRINT 'Seeded 8 platform-default RiskCategory values.';
END
ELSE PRINT 'RiskCategory values already present — left unchanged.';
GO

------------------------------------------------------------------------------
-- RiskEscalationThreshold — Score thresholds that trigger escalation, and the level they escalate to.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'RiskEscalationThreshold');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'RiskEscalationThreshold', 'Risk Escalation Threshold', 'Score thresholds that trigger escalation, and the level they escalate to.', 1, 0);
    PRINT 'Created category RiskEscalationThreshold.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES
      (NEWID(), @cat, NULL, 'Level1', 'Department Head', 10, 1, N'{"minScore":12,"escalationLevel":1}'),
      (NEWID(), @cat, NULL, 'Level2', 'Quality Committee', 20, 1, N'{"minScore":16,"escalationLevel":2}'),
      (NEWID(), @cat, NULL, 'Level3', 'Executive', 30, 1, N'{"minScore":20,"escalationLevel":3}');
    PRINT 'Seeded 3 platform-default RiskEscalationThreshold values.';
END
ELSE PRINT 'RiskEscalationThreshold values already present — left unchanged.';
GO

------------------------------------------------------------------------------
-- ApprovalLevel — Approval levels, their roles, timeouts and escalation.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'ApprovalLevel');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'ApprovalLevel', 'Approval Level', 'Approval levels, their roles, timeouts and escalation.', 1, 0);
    PRINT 'Created category ApprovalLevel.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES
      (NEWID(), @cat, NULL, 'DepartmentHead', 'Department Head', 10, 1, N'{"requiresRole":"DEPARTMENT_HEAD","timeoutHours":72,"delegable":true,"escalatesToLevel":2}'),
      (NEWID(), @cat, NULL, 'QualityLead', 'Quality Lead', 20, 1, N'{"requiresRole":"QUALITY_LEAD","timeoutHours":72,"delegable":true,"escalatesToLevel":3}'),
      (NEWID(), @cat, NULL, 'Executive', 'Executive', 30, 1, N'{"requiresRole":"ORG_ADMIN","timeoutHours":120,"delegable":false}');
    PRINT 'Seeded 3 platform-default ApprovalLevel values.';
END
ELSE PRINT 'ApprovalLevel values already present — left unchanged.';
GO

------------------------------------------------------------------------------
-- ApprovalOutcome — Decisions an approver may record.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'ApprovalOutcome');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'ApprovalOutcome', 'Approval Outcome', 'Decisions an approver may record.', 1, 0);
    PRINT 'Created category ApprovalOutcome.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES
      (NEWID(), @cat, NULL, 'Approved', 'Approved', 10, 1, N'{"terminal":true,"advances":true}'),
      (NEWID(), @cat, NULL, 'Rejected', 'Rejected', 20, 1, N'{"terminal":true,"advances":false}'),
      (NEWID(), @cat, NULL, 'ChangesRequested', 'Changes Requested', 30, 1, N'{"terminal":false,"advances":false}'),
      (NEWID(), @cat, NULL, 'Delegated', 'Delegated', 40, 1, N'{"terminal":false,"advances":false}');
    PRINT 'Seeded 4 platform-default ApprovalOutcome values.';
END
ELSE PRINT 'ApprovalOutcome values already present — left unchanged.';
GO

------------------------------------------------------------------------------
-- NotificationCategory — Subject areas a user may subscribe to notifications for.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'NotificationCategory');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'NotificationCategory', 'Notification Category', 'Subject areas a user may subscribe to notifications for.', 1, 0);
    PRINT 'Created category NotificationCategory.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES
      (NEWID(), @cat, NULL, 'AuditScheduled', 'Audit Scheduled', 10, 1, N'{"defaultChannel":"InApp"}'),
      (NEWID(), @cat, NULL, 'FindingRaised', 'Finding Raised', 20, 1, N'{"defaultChannel":"InApp"}'),
      (NEWID(), @cat, NULL, 'CapaAssigned', 'CAPA Assigned', 30, 1, N'{"defaultChannel":"Email"}'),
      (NEWID(), @cat, NULL, 'CapaOverdue', 'CAPA Overdue', 40, 1, N'{"defaultChannel":"Email"}'),
      (NEWID(), @cat, NULL, 'EvidenceRejected', 'Evidence Rejected', 50, 1, N'{"defaultChannel":"InApp"}'),
      (NEWID(), @cat, NULL, 'SlaBreached', 'SLA Breached', 60, 1, N'{"defaultChannel":"Email"}'),
      (NEWID(), @cat, NULL, 'ApprovalRequested', 'Approval Requested', 70, 1, N'{"defaultChannel":"Email"}'),
      (NEWID(), @cat, NULL, 'ReportAvailable', 'Report Available', 80, 1, N'{"defaultChannel":"InApp"}');
    PRINT 'Seeded 8 platform-default NotificationCategory values.';
END
ELSE PRINT 'NotificationCategory values already present — left unchanged.';
GO

------------------------------------------------------------------------------
-- NotificationChannel — Delivery channels, their retry behaviour and quiet-hours treatment.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'NotificationChannel');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'NotificationChannel', 'Notification Channel', 'Delivery channels, their retry behaviour and quiet-hours treatment.', 1, 0);
    PRINT 'Created category NotificationChannel.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES
      (NEWID(), @cat, NULL, 'InApp', 'In-App', 10, 1, N'{"retryAttempts":0,"quietHoursApply":false}'),
      (NEWID(), @cat, NULL, 'Email', 'Email', 20, 1, N'{"retryAttempts":3,"retryBackoffMinutes":15,"quietHoursApply":true}'),
      (NEWID(), @cat, NULL, 'Sms', 'SMS', 30, 1, N'{"retryAttempts":2,"retryBackoffMinutes":5,"quietHoursApply":true}');
    PRINT 'Seeded 3 platform-default NotificationChannel values.';
END
ELSE PRINT 'NotificationChannel values already present — left unchanged.';
GO

------------------------------------------------------------------------------
-- NotificationPriority — Notification priority and its treatment during quiet hours.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'NotificationPriority');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'NotificationPriority', 'Notification Priority', 'Notification priority and its treatment during quiet hours.', 1, 0);
    PRINT 'Created category NotificationPriority.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES
      (NEWID(), @cat, NULL, 'Low', 'Low', 10, 1, N'{"bypassesQuietHours":false}'),
      (NEWID(), @cat, NULL, 'Normal', 'Normal', 20, 1, N'{"bypassesQuietHours":false}'),
      (NEWID(), @cat, NULL, 'High', 'High', 30, 1, N'{"bypassesQuietHours":false}'),
      (NEWID(), @cat, NULL, 'Critical', 'Critical', 40, 1, N'{"bypassesQuietHours":true}');
    PRINT 'Seeded 4 platform-default NotificationPriority values.';
END
ELSE PRINT 'NotificationPriority values already present — left unchanged.';
GO

------------------------------------------------------------------------------
-- SlaPolicy — Remediation time allowed, by severity.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'SlaPolicy');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'SlaPolicy', 'Sla Policy', 'Remediation time allowed, by severity.', 1, 0);
    PRINT 'Created category SlaPolicy.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES
      (NEWID(), @cat, NULL, 'Critical', 'Critical', 10, 1, N'{"resolutionHours":24,"businessHoursOnly":false}'),
      (NEWID(), @cat, NULL, 'Major', 'Major', 20, 1, N'{"resolutionHours":72,"businessHoursOnly":true}'),
      (NEWID(), @cat, NULL, 'Moderate', 'Moderate', 30, 1, N'{"resolutionHours":168,"businessHoursOnly":true}'),
      (NEWID(), @cat, NULL, 'Minor', 'Minor', 40, 1, N'{"resolutionHours":336,"businessHoursOnly":true}');
    PRINT 'Seeded 4 platform-default SlaPolicy values.';
END
ELSE PRINT 'SlaPolicy values already present — left unchanged.';
GO

------------------------------------------------------------------------------
-- SlaWarningThreshold — Proportion of an SLA consumed before each warning state and reminder.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'SlaWarningThreshold');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'SlaWarningThreshold', 'Sla Warning Threshold', 'Proportion of an SLA consumed before each warning state and reminder.', 1, 0);
    PRINT 'Created category SlaWarningThreshold.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES
      (NEWID(), @cat, NULL, 'Warning', 'Warning', 10, 1, N'{"percentConsumed":50,"slaStatus":"Warning","sendsReminder":true}'),
      (NEWID(), @cat, NULL, 'AtRisk', 'At Risk', 20, 1, N'{"percentConsumed":80,"slaStatus":"AtRisk","sendsReminder":true}'),
      (NEWID(), @cat, NULL, 'Breached', 'Breached', 30, 1, N'{"percentConsumed":100,"slaStatus":"Breached","sendsReminder":true}');
    PRINT 'Seeded 3 platform-default SlaWarningThreshold values.';
END
ELSE PRINT 'SlaWarningThreshold values already present — left unchanged.';
GO

------------------------------------------------------------------------------
-- BusinessCalendar — Working days and hours used when an SLA runs on business hours.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'BusinessCalendar');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'BusinessCalendar', 'Business Calendar', 'Working days and hours used when an SLA runs on business hours.', 1, 0);
    PRINT 'Created category BusinessCalendar.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES
      (NEWID(), @cat, NULL, 'Monday', 'Monday', 10, 1, N'{"working":true,"startHour":9,"endHour":17}'),
      (NEWID(), @cat, NULL, 'Tuesday', 'Tuesday', 20, 1, N'{"working":true,"startHour":9,"endHour":17}'),
      (NEWID(), @cat, NULL, 'Wednesday', 'Wednesday', 30, 1, N'{"working":true,"startHour":9,"endHour":17}'),
      (NEWID(), @cat, NULL, 'Thursday', 'Thursday', 40, 1, N'{"working":true,"startHour":9,"endHour":17}'),
      (NEWID(), @cat, NULL, 'Friday', 'Friday', 50, 1, N'{"working":true,"startHour":9,"endHour":17}'),
      (NEWID(), @cat, NULL, 'Saturday', 'Saturday', 60, 1, N'{"working":false}'),
      (NEWID(), @cat, NULL, 'Sunday', 'Sunday', 70, 1, N'{"working":false}');
    PRINT 'Seeded 7 platform-default BusinessCalendar values.';
END
ELSE PRINT 'BusinessCalendar values already present — left unchanged.';
GO

------------------------------------------------------------------------------
-- EvidenceCategory — Groupings evidence is filed under.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'EvidenceCategory');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'EvidenceCategory', 'Evidence Category', 'Groupings evidence is filed under.', 1, 0);
    PRINT 'Created category EvidenceCategory.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES
      (NEWID(), @cat, NULL, 'Policy', 'Policy & Procedure', 10, 1, NULL),
      (NEWID(), @cat, NULL, 'Record', 'Clinical & Operational Record', 20, 1, NULL),
      (NEWID(), @cat, NULL, 'Training', 'Training & Competency', 30, 1, NULL),
      (NEWID(), @cat, NULL, 'Audit', 'Audit & Inspection', 40, 1, NULL),
      (NEWID(), @cat, NULL, 'Certification', 'Certification & Licence', 50, 1, NULL),
      (NEWID(), @cat, NULL, 'Photographic', 'Photographic', 60, 1, NULL);
    PRINT 'Seeded 6 platform-default EvidenceCategory values.';
END
ELSE PRINT 'EvidenceCategory values already present — left unchanged.';
GO

------------------------------------------------------------------------------
-- EvidenceType — Kinds of evidence and the rules that govern each.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'EvidenceType');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'EvidenceType', 'Evidence Type', 'Kinds of evidence and the rules that govern each.', 1, 0);
    PRINT 'Created category EvidenceType.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES
      (NEWID(), @cat, NULL, 'Document', 'Document', 10, 1, N'{"requiresApproval":true,"retentionPolicy":"Standard"}'),
      (NEWID(), @cat, NULL, 'Photograph', 'Photograph', 20, 1, N'{"requiresApproval":false,"retentionPolicy":"Standard"}'),
      (NEWID(), @cat, NULL, 'Certificate', 'Certificate', 30, 1, N'{"requiresApproval":true,"retentionPolicy":"Extended"}'),
      (NEWID(), @cat, NULL, 'TrainingRecord', 'Training Record', 40, 1, N'{"requiresApproval":true,"retentionPolicy":"Extended"}'),
      (NEWID(), @cat, NULL, 'Screenshot', 'Screenshot', 50, 1, N'{"requiresApproval":false,"retentionPolicy":"Short"}'),
      (NEWID(), @cat, NULL, 'AuditNote', 'Audit Note', 60, 1, N'{"requiresApproval":false,"retentionPolicy":"Standard"}'),
      (NEWID(), @cat, NULL, 'SignedAttestation', 'Signed Attestation', 70, 1, N'{"requiresApproval":true,"mandatory":true,"retentionPolicy":"Permanent"}');
    PRINT 'Seeded 7 platform-default EvidenceType values.';
END
ELSE PRINT 'EvidenceType values already present — left unchanged.';
GO

------------------------------------------------------------------------------
-- EvidenceRetentionPolicy — Retention periods evidence and documents may be held under.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'EvidenceRetentionPolicy');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'EvidenceRetentionPolicy', 'Evidence Retention Policy', 'Retention periods evidence and documents may be held under.', 1, 0);
    PRINT 'Created category EvidenceRetentionPolicy.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES
      (NEWID(), @cat, NULL, 'Short', 'Short — 1 year', 10, 1, N'{"retentionMonths":12}'),
      (NEWID(), @cat, NULL, 'Standard', 'Standard — 3 years', 20, 1, N'{"retentionMonths":36}'),
      (NEWID(), @cat, NULL, 'Extended', 'Extended — 7 years', 30, 1, N'{"retentionMonths":84}'),
      (NEWID(), @cat, NULL, 'Permanent', 'Permanent', 40, 1, N'{"retentionMonths":0,"permanent":true}');
    PRINT 'Seeded 4 platform-default EvidenceRetentionPolicy values.';
END
ELSE PRINT 'EvidenceRetentionPolicy values already present — left unchanged.';
GO

------------------------------------------------------------------------------
-- DocumentType — Document types, their naming, versioning and retention.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'DocumentType');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'DocumentType', 'Document Type', 'Document types, their naming, versioning and retention.', 1, 0);
    PRINT 'Created category DocumentType.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES
      (NEWID(), @cat, NULL, 'Policy', 'Policy', 10, 1, N'{"namingPattern":"POL-{code}-v{version}","versioning":"Major","retentionPolicy":"Permanent","requiresApproval":true}'),
      (NEWID(), @cat, NULL, 'Procedure', 'Procedure', 20, 1, N'{"namingPattern":"SOP-{code}-v{version}","versioning":"Major","retentionPolicy":"Extended","requiresApproval":true}'),
      (NEWID(), @cat, NULL, 'WorkInstruction', 'Work Instruction', 30, 1, N'{"namingPattern":"WI-{code}-v{version}","versioning":"Minor","retentionPolicy":"Standard"}'),
      (NEWID(), @cat, NULL, 'Form', 'Form', 40, 1, N'{"namingPattern":"FRM-{code}-v{version}","versioning":"Minor","retentionPolicy":"Standard"}'),
      (NEWID(), @cat, NULL, 'Report', 'Report', 50, 1, N'{"namingPattern":"RPT-{code}-{year}","versioning":"None","retentionPolicy":"Extended"}');
    PRINT 'Seeded 5 platform-default DocumentType values.';
END
ELSE PRINT 'DocumentType values already present — left unchanged.';
GO

------------------------------------------------------------------------------
-- DocumentClassification — Confidentiality classification and its handling rule.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'DocumentClassification');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'DocumentClassification', 'Document Classification', 'Confidentiality classification and its handling rule.', 1, 0);
    PRINT 'Created category DocumentClassification.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES
      (NEWID(), @cat, NULL, 'Public', 'Public', 10, 1, N'{"restrictsExternalSharing":false}'),
      (NEWID(), @cat, NULL, 'Internal', 'Internal', 20, 1, N'{"restrictsExternalSharing":true}'),
      (NEWID(), @cat, NULL, 'Confidential', 'Confidential', 30, 1, N'{"restrictsExternalSharing":true}'),
      (NEWID(), @cat, NULL, 'Restricted', 'Restricted', 40, 1, N'{"restrictsExternalSharing":true,"requiresNamedAccess":true}');
    PRINT 'Seeded 4 platform-default DocumentClassification values.';
END
ELSE PRINT 'DocumentClassification values already present — left unchanged.';
GO

------------------------------------------------------------------------------
-- DocumentRetentionPolicy — How a document version number advances on revision.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'DocumentRetentionPolicy');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'DocumentRetentionPolicy', 'Document Retention Policy', 'How a document version number advances on revision.', 1, 0);
    PRINT 'Created category DocumentRetentionPolicy.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES
      (NEWID(), @cat, NULL, 'None', 'No versioning', 10, 1, NULL),
      (NEWID(), @cat, NULL, 'Minor', 'Minor increments (1.1, 1.2)', 20, 1, NULL),
      (NEWID(), @cat, NULL, 'Major', 'Major increments (2.0, 3.0)', 30, 1, NULL);
    PRINT 'Seeded 3 platform-default DocumentRetentionPolicy values.';
END
ELSE PRINT 'DocumentRetentionPolicy values already present — left unchanged.';
GO

------------------------------------------------------------------------------
-- ExecutiveKpi — Executive KPI tiles, their metric, ordering and role visibility.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'ExecutiveKpi');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'ExecutiveKpi', 'Executive Kpi', 'Executive KPI tiles, their metric, ordering and role visibility.', 1, 0);
    PRINT 'Created category ExecutiveKpi.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES
      (NEWID(), @cat, NULL, 'ComplianceScore', 'Compliance Score', 10, 1, N'{"metric":"complianceScore","format":"percent"}'),
      (NEWID(), @cat, NULL, 'OpenFindings', 'Open Findings', 20, 1, N'{"metric":"openFindings","format":"count"}'),
      (NEWID(), @cat, NULL, 'OverdueCapas', 'Overdue CAPAs', 30, 1, N'{"metric":"overdueCapas","format":"count","tone":"danger"}'),
      (NEWID(), @cat, NULL, 'AuditsThisQuarter', 'Audits This Quarter', 40, 1, N'{"metric":"auditsThisQuarter","format":"count"}'),
      (NEWID(), @cat, NULL, 'EvidenceCoverage', 'Evidence Coverage', 50, 1, N'{"metric":"evidenceCoverage","format":"percent"}'),
      (NEWID(), @cat, NULL, 'SlaBreaches', 'SLA Breaches', 60, 1, N'{"metric":"slaBreaches","format":"count","tone":"danger","visibleToRole":"ORG_ADMIN"}');
    PRINT 'Seeded 6 platform-default ExecutiveKpi values.';
END
ELSE PRINT 'ExecutiveKpi values already present — left unchanged.';
GO

------------------------------------------------------------------------------
-- DashboardCard — Dashboard cards and widgets, their kind, ordering and role visibility.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'DashboardCard');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'DashboardCard', 'Dashboard Card', 'Dashboard cards and widgets, their kind, ordering and role visibility.', 1, 0);
    PRINT 'Created category DashboardCard.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES
      (NEWID(), @cat, NULL, 'ComplianceTrend', 'Compliance Trend', 10, 1, N'{"widget":"lineChart","span":2}'),
      (NEWID(), @cat, NULL, 'RiskDistribution', 'Risk Distribution', 20, 1, N'{"widget":"donutChart","span":1}'),
      (NEWID(), @cat, NULL, 'ChapterBreakdown', 'Chapter Breakdown', 30, 1, N'{"widget":"barChart","span":2}'),
      (NEWID(), @cat, NULL, 'UpcomingAudits', 'Upcoming Audits', 40, 1, N'{"widget":"list","span":1}'),
      (NEWID(), @cat, NULL, 'ActivityStream', 'Activity Stream', 50, 1, N'{"widget":"timeline","span":1}'),
      (NEWID(), @cat, NULL, 'HighRiskChapters', 'High-Risk Chapters', 60, 1, N'{"widget":"list","span":1,"tone":"danger"}');
    PRINT 'Seeded 6 platform-default DashboardCard values.';
END
ELSE PRINT 'DashboardCard values already present — left unchanged.';
GO

SELECT c.Code AS Category, COUNT(v.Id) AS PlatformDefaults
FROM OrganizationLookupCategories c
LEFT JOIN OrganizationLookupValues v ON v.CategoryId = c.Id AND v.OrganizationId IS NULL
GROUP BY c.Code ORDER BY c.Code;
GO