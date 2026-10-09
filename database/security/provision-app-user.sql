-- Execute as migration administrator in the intended database.
-- Supply @WindowsLogin and @DatabaseUser as NVARCHAR(128) command parameters.
-- Login must already exist; this script never creates a password/login.
IF DB_NAME() IN ('master','model','msdb','tempdb') THROW 51310,'User database required.',1;
IF NULLIF(LTRIM(RTRIM(@WindowsLogin)),N'') IS NULL OR NULLIF(LTRIM(RTRIM(@DatabaseUser)),N'') IS NULL
    THROW 51310,'Windows login and database user are required.',1;
IF NOT EXISTS(SELECT 1 FROM sys.server_principals WHERE name=@WindowsLogin AND type IN ('U','G'))
    THROW 51310,'Provision a dedicated Windows login first.',1;
IF IS_SRVROLEMEMBER('sysadmin',@WindowsLogin)=1 THROW 51310,'App login cannot be sysadmin.',1;
IF USER_ID(@DatabaseUser) IS NOT NULL THROW 51310,'User exists; review its permissions explicitly.',1;
DECLARE @sql NVARCHAR(MAX)=N'CREATE USER '+QUOTENAME(@DatabaseUser)+N' FOR LOGIN '+QUOTENAME(@WindowsLogin)+N'; ALTER ROLE CafeManagerApp ADD MEMBER '+QUOTENAME(@DatabaseUser)+N';';
EXEC sys.sp_executesql @sql;
