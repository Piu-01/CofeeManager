-- Owner-only fixture on isolated marked G02 DB. Application connections impersonate
-- these users with Pooling=false; this is not provisioning for normal installation.
IF DB_NAME() NOT LIKE 'CoffeeManager_Test_G02[_]%' OR OBJECT_ID(N'dbo.G02TestMarker',N'U') IS NULL
 THROW 52600,'G02 test marker required.',1;
CREATE USER G02_Runtime WITHOUT LOGIN;
CREATE USER G02_Setup WITHOUT LOGIN;
ALTER ROLE CafeManagerApp ADD MEMBER G02_Runtime;
ALTER ROLE CafeManagerSetup ADD MEMBER G02_Setup;
