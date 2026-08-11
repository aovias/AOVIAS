/*
  Wave 2.2D — NEW-07: widen PlatformSequences.Prefix from nvarchar(10) to nvarchar(64).

  ROOT CAUSE (verified, not assumed)
  ----------------------------------
  CapaNumberAllocator scopes its sequence per hospital:

      var prefix = $"CAPA-{hospitalId:N}";     // "CAPA-" (5) + 32 hex = 37 characters

  PlatformSequences.Prefix was nvarchar(10). Reproduced directly against the database:

      INSERT INTO PlatformSequences (Prefix, CurrentValue)
      VALUES (N'CAPA-d9f5554abf834262b386a31587b0ba06', 0);
      -- String or binary data would be truncated in table
      -- 'Compliance.dbo.PlatformSequences', column 'Prefix'.
      -- Truncated value: 'CAPA-d9f55'.

  EF surfaced this as "An error occurred while saving the entity changes" on every
  PATCH .../corrective-action once the deficiency had CAPA activity.

  CONSEQUENCE
  -----------
  No CAPA number could ever be allocated, so no CAPA could be persisted. This is the mechanical
  reason every CAPA counter read zero (DEF-010) — the metric was arithmetically correct, and the
  cause was that the write path could not complete.

  Unrelated to tenant isolation: it fails identically with or without query filters.

  SAFETY
  ------
  Widening is non-destructive and backward compatible. Existing prefixes ('CHP', 'STD', 'MOE', …)
  are unaffected. The unique index is dropped and recreated because the column it covers changes.
*/

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'PlatformSequences'
      AND COLUMN_NAME = 'Prefix'
      AND CHARACTER_MAXIMUM_LENGTH < 64)
BEGIN
    BEGIN TRANSACTION;

    DECLARE @index sysname = (
        SELECT TOP 1 i.name FROM sys.indexes i
        JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
        JOIN sys.columns c ON c.object_id = i.object_id AND c.column_id = ic.column_id
        WHERE i.object_id = OBJECT_ID('PlatformSequences') AND c.name = 'Prefix');

    IF @index IS NOT NULL
    BEGIN
        EXEC('DROP INDEX ' + @index + ' ON PlatformSequences');
        PRINT 'Dropped index ' + @index + ' on PlatformSequences.Prefix.';
    END

    ALTER TABLE PlatformSequences ALTER COLUMN Prefix nvarchar(64) NOT NULL;
    PRINT 'Widened PlatformSequences.Prefix to nvarchar(64).';

    IF @index IS NOT NULL
    BEGIN
        EXEC('CREATE UNIQUE INDEX ' + @index + ' ON PlatformSequences (Prefix)');
        PRINT 'Recreated unique index ' + @index + '.';
    END

    COMMIT TRANSACTION;
END
ELSE
BEGIN
    PRINT 'PlatformSequences.Prefix is already at least nvarchar(64) — nothing to do.';
END

-- Verification
SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'PlatformSequences' AND COLUMN_NAME = 'Prefix';
