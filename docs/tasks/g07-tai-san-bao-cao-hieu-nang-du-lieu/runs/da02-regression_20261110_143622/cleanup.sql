SET NOCOUNT ON;
DECLARE @ok int = 1, @m nvarchar(64);
IF DB_ID('CoffeeManager_Test_G07_20261110_144033_ca33988f_demo') IS NOT NULL
BEGIN
  SELECT @m = RunId FROM CoffeeManager_Test_G07_20261110_144033_ca33988f_demo.dbo.G07TestMarker;
  IF @m <> N'20261110_144033_ca33988f' BEGIN PRINT 'marker mismatch 144033: ' + ISNULL(@m,N'NULL'); SET @ok = 0; END
END
IF DB_ID('CoffeeManager_Test_G07_20261110_152830_fceb2357_demo') IS NOT NULL
BEGIN
  SELECT @m = RunId FROM CoffeeManager_Test_G07_20261110_152830_fceb2357_demo.dbo.G07TestMarker;
  IF @m <> N'20261110_152830_fceb2357' BEGIN PRINT 'marker mismatch 152830: ' + ISNULL(@m,N'NULL'); SET @ok = 0; END
END
IF @ok = 0 THROW 50001, 'Marker verification failed; no drop performed.', 1;
IF DB_ID('CoffeeManager_Test_G07_20261110_144033_ca33988f_demo') IS NOT NULL
BEGIN
  ALTER DATABASE CoffeeManager_Test_G07_20261110_144033_ca33988f_demo SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
  DROP DATABASE CoffeeManager_Test_G07_20261110_144033_ca33988f_demo;
  PRINT 'dropped 144033';
END
IF DB_ID('CoffeeManager_Test_G07_20261110_152830_fceb2357_demo') IS NOT NULL
BEGIN
  ALTER DATABASE CoffeeManager_Test_G07_20261110_152830_fceb2357_demo SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
  DROP DATABASE CoffeeManager_Test_G07_20261110_152830_fceb2357_demo;
  PRINT 'dropped 152830';
END
SELECT COUNT(*) AS RemainingG07 FROM sys.databases WHERE name LIKE 'CoffeeManager_Test_G07_%';
