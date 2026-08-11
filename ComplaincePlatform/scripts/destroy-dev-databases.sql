-- Destructive: drops dev databases matching docker-compose.dev.yml defaults (SQL Server).
-- Run as a user with permission to DROP DATABASE (e.g. sa), against the correct server instance.
-- Close apps using these DBs first.

USE master;
GO
 
DECLARE @dbs TABLE (name sysname);
INSERT INTO @dbs (name) VALUES
  (N'Compliance'),
  (N'Organization'),
  (N'Identity'),
  (N'Audit'),
  (N'AIOrchestrator');

DECLARE @name sysname;
DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT name FROM @dbs;
OPEN c;
FETCH NEXT FROM c INTO @name;
WHILE @@FETCH_STATUS = 0
BEGIN
  IF DB_ID(@name) IS NOT NULL
  BEGIN
    DECLARE @sql nvarchar(max) = N'ALTER DATABASE [' + REPLACE(@name, N']', N']]') + N'] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;';
    EXEC sp_executesql @sql;
    SET @sql = N'DROP DATABASE [' + REPLACE(@name, N']', N']]') + N'];';
    EXEC sp_executesql @sql;
  END
  FETCH NEXT FROM c INTO @name;
END
CLOSE c;
DEALLOCATE c;
GO
