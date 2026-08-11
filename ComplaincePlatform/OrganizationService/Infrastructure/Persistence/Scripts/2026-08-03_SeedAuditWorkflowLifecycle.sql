/*
  Platform-default audit lifecycle, as configuration.

  Generated from SEED_AUDIT_WORKFLOW in the web app's workflow-definition.model.ts, using the same
  serialiser the round-trip test exercises: 'AuditWorkflowEngine > round trip through configuration'
  asserts that reading these rows back reproduces the seeded lifecycle exactly, over a matrix of
  fact combinations. That test is what makes shipping these rows safe — with them present every
  organization evaluates its lifecycle through the configured path rather than the in-code seed.

  Data only — no schema changes. Idempotent: categories and values are inserted only when absent,
  so re-running never disturbs a tenant's overrides or an edited platform default.

  Rules live in Metadata as JSON. A stage's completeWhen / outstanding / recommendations /
  entryBlockedWhen are conditions over the facts the workspace reports; see workflow-condition.model.ts
  for the grammar. An unparseable rule set is rejected by the client in favour of its seed, so a bad
  edit degrades to platform behaviour rather than to a broken workspace.
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
-- AuditWorkflowStage — Stages of the audit lifecycle and the rules that govern each.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'AuditWorkflowStage');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'AuditWorkflowStage', 'Audit Workflow Stage', 'Stages of the audit lifecycle and the rules that govern each.', 1, 0);
    PRINT 'Created category AuditWorkflowStage.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES (NEWID(), @cat, NULL, 'planning', 'Planning', 10, 1,
      N'{"tab":"planning","route":"/app/audit/planning","nextActionLabel":"Schedule an audit","consumes":[],"produces":["scheduledAudit"],"completeWhen":{"fact":"hasScheduledAudit","is":"true"},"outstanding":[{"when":{"not":{"fact":"hasScheduledAudit","is":"true"}},"message":"Schedule an audit"}],"recommendations":[{"when":{"all":[{"fact":"hasScheduledAudit","is":"true"},{"not":{"fact":"hasSession","is":"true"}}]},"message":"Assign the audit team before execution begins"}]}');
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES (NEWID(), @cat, NULL, 'execution', 'Execution', 20, 1,
      N'{"tab":"execution","route":"/app/audit/execution/sessions","nextActionLabel":"Start the audit","consumes":["scheduledAudit"],"produces":["auditSession","observation"],"completeWhen":{"all":[{"fact":"hasSession","is":"true"},{"fact":"observationCount","is":"positive"}]},"outstanding":[{"when":{"not":{"fact":"hasSession","is":"true"}},"message":"Start the audit session"},{"when":{"all":[{"fact":"hasSession","is":"true"},{"fact":"observationCount","is":"zero"}]},"message":"Record observations"}],"recommendations":[{"when":{"all":[{"fact":"hasSession","is":"true"},{"fact":"observationCount","is":"positive"},{"not":{"fact":"evidenceCount","is":"positive"}}]},"message":"Attach evidence to the observations recorded so far"}],"entryBlockedWhen":{"when":{"fact":"hasScheduledAudit","is":"false"},"message":"Schedule an audit before executing it"}}');
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES (NEWID(), @cat, NULL, 'evidence', 'Evidence', 30, 1,
      N'{"tab":"evidence","route":"/app/audit/evidence","nextActionLabel":"Collect and review evidence","consumes":["auditSession"],"produces":["evidence"],"completeWhen":{"fact":"evidenceCount","is":"positive"},"outstanding":[{"when":{"not":{"fact":"evidenceCount","is":"positive"}},"message":"Collect and verify evidence"}],"recommendations":[{"when":{"all":[{"fact":"evidenceCount","is":"positive"},{"fact":"findingCount","is":"zero"}]},"message":"Review collected evidence for issues worth raising as findings"}],"entryBlockedWhen":{"when":{"fact":"hasSession","is":"false"},"message":"Start the audit before collecting evidence"}}');
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES (NEWID(), @cat, NULL, 'findings', 'Findings', 40, 1,
      N'{"tab":"findings","route":"/app/audit/findings","nextActionLabel":"Raise findings from observations","consumes":["observation","evidence"],"produces":["finding"],"completeWhen":{"all":[{"fact":"findingCount","is":"positive"},{"fact":"openFindingCount","is":"zero"}]},"outstanding":[{"when":{"not":{"fact":"findingCount","is":"positive"}},"message":"Raise findings from observations"},{"when":{"all":[{"fact":"findingCount","is":"positive"},{"fact":"openFindingCount","is":"positive"}]},"message":"{openFindingCount} finding(s) still open"}],"recommendations":[{"when":{"fact":"openFindingCount","is":"positive"},"message":"Triage open findings by severity before promoting them"},{"when":{"all":[{"fact":"findingCount","is":"positive"},{"fact":"capaCount","is":"zero"}]},"message":"Promote accepted findings to CAPA"}],"entryBlockedWhen":{"when":{"fact":"hasSession","is":"false"},"message":"Findings are raised during an audit session"}}');
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES (NEWID(), @cat, NULL, 'capa', 'CAPA', 50, 1,
      N'{"tab":"capa","route":"/app/audit/capa","nextActionLabel":"Promote findings to CAPA","consumes":["finding"],"produces":["correctiveAction"],"completeWhen":{"all":[{"fact":"capaCount","is":"positive"},{"fact":"openCapaCount","is":"zero"}]},"outstanding":[{"when":{"not":{"fact":"capaCount","is":"positive"}},"message":"Promote findings to CAPA"},{"when":{"all":[{"fact":"capaCount","is":"positive"},{"fact":"openCapaCount","is":"positive"}]},"message":"{openCapaCount} CAPA(s) awaiting closure"}],"recommendations":[{"when":{"fact":"openCapaCount","is":"positive"},"message":"Chase owners of CAPAs still awaiting closure"},{"when":{"all":[{"fact":"capaCount","is":"positive"},{"fact":"verifiedCapaCount","is":"zero"}]},"message":"Schedule effectiveness verification for completed actions"}],"entryBlockedWhen":{"when":{"fact":"findingCount","is":"zero"},"message":"A CAPA is raised from a finding"}}');
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES (NEWID(), @cat, NULL, 'reports', 'Reports', 60, 1,
      N'{"tab":"reports","route":"/app/audit/reports","nextActionLabel":"Generate the audit report","consumes":["correctiveAction"],"produces":["auditReport"],"completeWhen":{"fact":"reportAvailable","is":"true"},"outstanding":[{"when":{"not":{"fact":"sessionClosed","is":"true"}},"message":"Close the audit"},{"when":{"not":{"fact":"reportAvailable","is":"true"}},"message":"Generate the audit report"}],"recommendations":[{"when":{"all":[{"fact":"sessionClosed","is":"true"},{"not":{"fact":"reportAvailable","is":"true"}}]},"message":"Generate the audit report and executive summary"}],"entryBlockedWhen":{"when":{"fact":"sessionClosed","is":"false"},"message":"Close the audit to finalise its report"}}');
    PRINT 'Seeded 6 platform-default AuditWorkflowStage values.';
END
ELSE PRINT 'AuditWorkflowStage values already present — left unchanged.';
GO

------------------------------------------------------------------------------
-- AuditWorkflowSurface — Sub-navigation surfaces within audit capability tabs.
------------------------------------------------------------------------------
DECLARE @cat uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'AuditWorkflowSurface');

IF @cat IS NULL
BEGIN
    SET @cat = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@cat, 'AuditWorkflowSurface', 'Audit Workflow Surface', 'Sub-navigation surfaces within audit capability tabs.', 1, 0);
    PRINT 'Created category AuditWorkflowSurface.';
END

IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @cat)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES (NEWID(), @cat, NULL, 'overview.programme', 'Programme', 10, 1,
      N'{"tab":"overview","surfaceId":"programme","route":"/app/audit/overview/programme"}');
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES (NEWID(), @cat, NULL, 'overview.auditor-home', 'Auditor Home', 20, 1,
      N'{"tab":"overview","surfaceId":"auditor-home","route":"/app/audit/overview/auditor-home","requiresAnyRole":["EXTERNAL_AUDITOR","REGULATOR","REGULATORY_ADMIN","SUPER_ADMIN","ORG_ADMIN"]}');
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES (NEWID(), @cat, NULL, 'execution.sessions', 'Active Audits', 10, 1,
      N'{"tab":"execution","surfaceId":"sessions","route":"/app/audit/execution/sessions","requiresAnyRole":["EXTERNAL_AUDITOR","REGULATOR","REGULATORY_ADMIN","SUPER_ADMIN","ORG_ADMIN"]}');
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES (NEWID(), @cat, NULL, 'execution.observations', 'Observations', 20, 1,
      N'{"tab":"execution","surfaceId":"observations","route":"/app/audit/execution/observations","requiresAnyRole":["EXTERNAL_AUDITOR","REGULATOR","REGULATORY_ADMIN","SUPER_ADMIN","ORG_ADMIN"]}');
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES (NEWID(), @cat, NULL, 'execution.surveillance', 'Surveillance', 30, 1,
      N'{"tab":"execution","surfaceId":"surveillance","route":"/app/audit/execution/surveillance"}');
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES (NEWID(), @cat, NULL, 'execution.simulation', 'Mock / Simulation', 40, 1,
      N'{"tab":"execution","surfaceId":"simulation","route":"/app/audit/execution/simulation"}');
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES (NEWID(), @cat, NULL, 'reports.audit', 'Audit Reports', 10, 1,
      N'{"tab":"reports","surfaceId":"audit","route":"/app/audit/reports/audit"}');
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES (NEWID(), @cat, NULL, 'reports.sessions', 'Session Reports', 20, 1,
      N'{"tab":"reports","surfaceId":"sessions","route":"/app/audit/reports/sessions","requiresAnyRole":["EXTERNAL_AUDITOR","REGULATOR","REGULATORY_ADMIN","SUPER_ADMIN","ORG_ADMIN"]}');
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES (NEWID(), @cat, NULL, 'reports.history', 'History', 30, 1,
      N'{"tab":"reports","surfaceId":"history","route":"/app/audit/reports/history","requiresAnyRole":["EXTERNAL_AUDITOR","REGULATOR","REGULATORY_ADMIN","SUPER_ADMIN","ORG_ADMIN"]}');
    PRINT 'Seeded 9 platform-default AuditWorkflowSurface values.';
END
ELSE PRINT 'AuditWorkflowSurface values already present — left unchanged.';
GO

------------------------------------------------------------------------------
-- Verification
------------------------------------------------------------------------------
SELECT c.Code AS Category, v.Code AS Value, v.SortOrder, LEN(v.Metadata) AS MetadataChars
FROM OrganizationLookupCategories c
JOIN OrganizationLookupValues v ON v.CategoryId = c.Id AND v.OrganizationId IS NULL
WHERE c.Code IN ('AuditWorkflowStage', 'AuditWorkflowSurface')
ORDER BY c.Code, v.SortOrder;
GO