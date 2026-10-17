-- Include before each owner-only T08 fixture. Never apply fixtures to application data.
IF DB_NAME() NOT LIKE 'CoffeeManager_Test_G02[_]%'
 OR OBJECT_ID(N'dbo.G02TestMarker',N'U') IS NULL
 THROW 52500,'Isolated G02 database and marker required.',1;
