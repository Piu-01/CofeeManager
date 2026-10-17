-- Run as migration operator in the explicit application database.
-- Replace the local Windows identity below before executing. No password is stored here.
DECLARE @identity SYSNAME=N'REPLACE_WITH_WINDOWS_SETUP_IDENTITY';
IF @identity=N'REPLACE_WITH_WINDOWS_SETUP_IDENTITY' THROW 51407,'Choose an explicit setup identity.',1;
DECLARE @sql NVARCHAR(MAX);
IF NOT EXISTS(SELECT 1 FROM sys.server_principals WHERE name=@identity)
BEGIN SET @sql=N'CREATE LOGIN '+QUOTENAME(@identity)+N' FROM WINDOWS;'; EXEC sys.sp_executesql @sql; END;
IF NOT EXISTS(SELECT 1 FROM sys.database_principals WHERE name=@identity)
BEGIN SET @sql=N'CREATE USER '+QUOTENAME(@identity)+N' FOR LOGIN '+QUOTENAME(@identity)+N';'; EXEC sys.sp_executesql @sql; END;
SET @sql=N'ALTER ROLE CafeManagerSetup ADD MEMBER '+QUOTENAME(@identity)+N';'; EXEC sys.sp_executesql @sql;
-- After setup, revoke by running ALTER ROLE CafeManagerSetup DROP MEMBER [chosen identity].
