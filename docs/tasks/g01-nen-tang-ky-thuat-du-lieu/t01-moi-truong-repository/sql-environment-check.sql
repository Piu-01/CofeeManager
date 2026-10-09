-- T01: chi doc metadata, khong tao DB va khong chay baseline/seed.
-- Mo SSMS, ket noi default instance local bang Windows Authentication.
USE master;
GO
SET NOCOUNT ON;

SELECT
    CONVERT(NVARCHAR(128), SERVERPROPERTY('ServerName')) AS ServerName,
    CONVERT(NVARCHAR(128), SERVERPROPERTY('MachineName')) AS MachineName,
    COALESCE(CONVERT(NVARCHAR(128), SERVERPROPERTY('InstanceName')), N'MSSQLSERVER') AS InstanceName,
    CONVERT(NVARCHAR(128), SERVERPROPERTY('ProductVersion')) AS ProductVersion,
    CONVERT(NVARCHAR(128), SERVERPROPERTY('Edition')) AS Edition,
    ORIGINAL_LOGIN() AS OriginalLogin,
    SUSER_SNAME() AS CurrentLogin,
    HAS_PERMS_BY_NAME(NULL, NULL, 'CREATE ANY DATABASE') AS CanCreateAnyDatabase,
    HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'CREATE DATABASE') AS CanCreateDatabaseInMaster,
    IS_SRVROLEMEMBER('sysadmin') AS IsSysadmin,
    IS_SRVROLEMEMBER('dbcreator') AS IsDbcreator;
GO
