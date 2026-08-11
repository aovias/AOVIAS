/*
  Stage 5B.1 — extend the lookup aggregate into a tenant-aware configuration store.

  WHY EXTEND RATHER THAN ADD TABLES
  ---------------------------------
  The engineering rule is "new schema only when justified". Fourteen configuration domains each
  getting their own table is fourteen migrations, fourteen repositories and fourteen chances to
  diverge. One extended aggregate serves every Category A domain with no further migrations —
  adding a domain becomes inserting a category row.

  WHAT WAS MISSING
  ----------------
  As shipped the tables were:

      OrganizationLookupCategories (Id, Code, Name)
      OrganizationLookupValues     (Id, CategoryId, Code, Name, SortOrder)

  Neither carries an OrganizationId. They are **platform-global** lookups, so they cannot express
  "this organization configures its own audit types" — the core requirement of Stage 5B. They also
  lack activation, effective dating, hierarchy and free-form metadata.

  DESIGN
  ------
  * OrganizationId NULL  = platform default, visible to every tenant.
  * OrganizationId set   = that tenant's override of the same (CategoryCode, ValueCode).
    Resolution takes the tenant row when present and falls back to the platform default, so an
    unconfigured organization sees sensible values without a seeding step.
  * IsActive             — retire a value without deleting it and orphaning historical records.
  * EffectiveFrom/To     — scheduled changes (a severity scale that changes at financial year end).
  * ParentValueId        — hierarchy (risk category → sub-category).
  * Metadata             — JSON for domain-specific attributes (e.g. an audit type's
                           selectableWhenScheduling) so a new attribute needs no migration.
  * RowVersion           — optimistic concurrency and cache versioning.

  SAFETY
  ------
  Idempotent and purely additive. Existing rows become platform defaults (OrganizationId NULL,
  IsActive 1), so current behaviour is unchanged.
*/

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

------------------------------------------------------------------------------
-- Categories
------------------------------------------------------------------------------
IF COL_LENGTH('OrganizationLookupCategories', 'Description') IS NULL
    ALTER TABLE OrganizationLookupCategories ADD Description nvarchar(500) NULL;

IF COL_LENGTH('OrganizationLookupCategories', 'IsTenantConfigurable') IS NULL
    ALTER TABLE OrganizationLookupCategories ADD IsTenantConfigurable bit NOT NULL DEFAULT(1);

IF COL_LENGTH('OrganizationLookupCategories', 'IsSystemManaged') IS NULL
    ALTER TABLE OrganizationLookupCategories ADD IsSystemManaged bit NOT NULL DEFAULT(0);

------------------------------------------------------------------------------
-- Values
------------------------------------------------------------------------------
IF COL_LENGTH('OrganizationLookupValues', 'OrganizationId') IS NULL
    ALTER TABLE OrganizationLookupValues ADD OrganizationId uniqueidentifier NULL;

IF COL_LENGTH('OrganizationLookupValues', 'IsActive') IS NULL
    ALTER TABLE OrganizationLookupValues ADD IsActive bit NOT NULL DEFAULT(1);

IF COL_LENGTH('OrganizationLookupValues', 'EffectiveFrom') IS NULL
    ALTER TABLE OrganizationLookupValues ADD EffectiveFrom datetime2 NULL;

IF COL_LENGTH('OrganizationLookupValues', 'EffectiveTo') IS NULL
    ALTER TABLE OrganizationLookupValues ADD EffectiveTo datetime2 NULL;

IF COL_LENGTH('OrganizationLookupValues', 'ParentValueId') IS NULL
    ALTER TABLE OrganizationLookupValues ADD ParentValueId uniqueidentifier NULL;

IF COL_LENGTH('OrganizationLookupValues', 'Metadata') IS NULL
    ALTER TABLE OrganizationLookupValues ADD Metadata nvarchar(max) NULL;

IF COL_LENGTH('OrganizationLookupValues', 'RowVersion') IS NULL
    ALTER TABLE OrganizationLookupValues ADD RowVersion rowversion NOT NULL;

COMMIT TRANSACTION;
GO
-- Batch boundary is required: SQL Server compiles a batch up front, so anything below that
-- references the columns added above would fail to compile if it stayed in the same batch.

------------------------------------------------------------------------------
-- Indexes (outside the transaction; each guarded)
------------------------------------------------------------------------------

-- The shipped unique index covers (CategoryId, Code) with no tenant column, which makes a tenant
-- override impossible: the override shares its Code with the platform default it replaces. It is
-- replaced below by two filtered indexes that keep uniqueness within each scope separately.
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OrganizationLookupValues_CategoryId_Code')
BEGIN
    DROP INDEX IX_OrganizationLookupValues_CategoryId_Code ON OrganizationLookupValues;
    PRINT 'Dropped legacy tenant-blind unique index IX_OrganizationLookupValues_CategoryId_Code.';
END

-- Resolution path: category + tenant (or platform default) + active.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OrgLookupValues_Category_Org_Active')
    CREATE INDEX IX_OrgLookupValues_Category_Org_Active
        ON OrganizationLookupValues (CategoryId, OrganizationId, IsActive)
        INCLUDE (Code, Name, SortOrder);

-- One row per (category, code) per tenant. A filtered unique index rather than a constraint so
-- platform defaults (OrganizationId NULL) and tenant overrides can coexist for the same code.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_OrgLookupValues_Tenant_Code')
    CREATE UNIQUE INDEX UX_OrgLookupValues_Tenant_Code
        ON OrganizationLookupValues (CategoryId, OrganizationId, Code)
        WHERE OrganizationId IS NOT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_OrgLookupValues_Platform_Code')
    CREATE UNIQUE INDEX UX_OrgLookupValues_Platform_Code
        ON OrganizationLookupValues (CategoryId, Code)
        WHERE OrganizationId IS NULL;

GO

------------------------------------------------------------------------------
-- Seed the AuditType category as a platform default
------------------------------------------------------------------------------
DECLARE @auditTypeCategoryId uniqueidentifier =
    (SELECT Id FROM OrganizationLookupCategories WHERE Code = 'AuditType');

IF @auditTypeCategoryId IS NULL
BEGIN
    SET @auditTypeCategoryId = NEWID();
    INSERT INTO OrganizationLookupCategories (Id, Code, Name, Description, IsTenantConfigurable, IsSystemManaged)
    VALUES (@auditTypeCategoryId, 'AuditType', 'Audit Type',
            'Audit types available for scheduling and classification.', 1, 0);
    PRINT 'Created category AuditType.';
END

-- Mirrors DEFAULT_AUDIT_TYPES in the web app's audit-type.catalog.ts. The client keeps that seed so
-- an unreachable configuration API degrades to identical behaviour rather than an empty picker.
IF NOT EXISTS (SELECT 1 FROM OrganizationLookupValues WHERE CategoryId = @auditTypeCategoryId)
BEGIN
    INSERT INTO OrganizationLookupValues (Id, CategoryId, OrganizationId, Code, Name, SortOrder, IsActive, Metadata)
    VALUES
      (NEWID(), @auditTypeCategoryId, NULL, 'InternalAudit',       'Internal',              10, 1, N'{"selectableWhenScheduling":true,"surveillance":false,"external":false}'),
      (NEWID(), @auditTypeCategoryId, NULL, 'DepartmentAudit',     'Department',            20, 1, N'{"selectableWhenScheduling":true,"surveillance":false,"external":false}'),
      (NEWID(), @auditTypeCategoryId, NULL, 'FollowUpAudit',       'Follow-up',             30, 1, N'{"selectableWhenScheduling":true,"surveillance":false,"external":false}'),
      (NEWID(), @auditTypeCategoryId, NULL, 'MockAudit',           'Mock NABH',             40, 1, N'{"selectableWhenScheduling":true,"surveillance":false,"external":false}'),
      (NEWID(), @auditTypeCategoryId, NULL, 'FocusedAudit',        'Focused',               50, 1, N'{"selectableWhenScheduling":true,"surveillance":false,"external":false}'),
      (NEWID(), @auditTypeCategoryId, NULL, 'Surveillance',        'Surveillance',          60, 1, N'{"selectableWhenScheduling":true,"surveillance":true,"external":false}'),
      (NEWID(), @auditTypeCategoryId, NULL, 'ConsultantReview',    'Consultant Review',     70, 1, N'{"selectableWhenScheduling":false,"surveillance":false,"external":true}'),
      (NEWID(), @auditTypeCategoryId, NULL, 'NabhPreAssessment',   'NABH Pre-Assessment',   80, 1, N'{"selectableWhenScheduling":false,"surveillance":false,"external":true}'),
      (NEWID(), @auditTypeCategoryId, NULL, 'NabhFinalAssessment', 'NABH Final Assessment', 90, 1, N'{"selectableWhenScheduling":false,"surveillance":false,"external":true}');
    PRINT 'Seeded 9 platform-default AuditType values.';
END
ELSE
BEGIN
    PRINT 'AuditType values already present — left unchanged.';
END

------------------------------------------------------------------------------
-- Verification
------------------------------------------------------------------------------
SELECT c.Code AS Category, COUNT(v.Id) AS Values_,
       SUM(CASE WHEN v.OrganizationId IS NULL THEN 1 ELSE 0 END) AS PlatformDefaults,
       SUM(CASE WHEN v.OrganizationId IS NOT NULL THEN 1 ELSE 0 END) AS TenantOverrides
FROM OrganizationLookupCategories c
LEFT JOIN OrganizationLookupValues v ON v.CategoryId = c.Id
GROUP BY c.Code ORDER BY c.Code;
