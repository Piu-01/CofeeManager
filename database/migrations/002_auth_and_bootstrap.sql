-- Apply with Apply-Migration.ps1 -Version 2, after 001. Never run on a seed-only target.
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF @@TRANCOUNT=0 THROW 51407,'Migration transaction required.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=1) THROW 51407,'Version 1 required.',1;
IF EXISTS(SELECT 1 FROM dbo.TAIKHOAN WHERE LEN(LTRIM(RTRIM(TenDangNhap)))=0
 OR LTRIM(RTRIM(TenDangNhap)) COLLATE Latin1_General_100_BIN2 LIKE '%[^-A-Za-z0-9._]%')
 THROW 51404,'Legacy username requires operator review.',1;
IF EXISTS(SELECT LOWER(LTRIM(RTRIM(TenDangNhap))) COLLATE Latin1_General_100_CI_AS FROM dbo.TAIKHOAN
 GROUP BY LOWER(LTRIM(RTRIM(TenDangNhap))) COLLATE Latin1_General_100_CI_AS HAVING COUNT(*)>1)
 THROW 51404,'Legacy usernames collide.',1;
UPDATE dbo.TAIKHOAN SET TenDangNhap=LTRIM(RTRIM(TenDangNhap));
ALTER TABLE dbo.TAIKHOAN ADD LoginKey AS
 (LOWER(TenDangNhap COLLATE Latin1_General_100_CI_AS) COLLATE Latin1_General_100_BIN2) PERSISTED,
 SecurityStamp ROWVERSION;
CREATE UNIQUE INDEX UX_TK_LoginKey ON dbo.TAIKHOAN(LoginKey);
ALTER TABLE dbo.TAIKHOAN ADD CONSTRAINT CK_TK_Username CHECK
 (DATALENGTH(TenDangNhap) BETWEEN 1 AND 50 AND DATALENGTH(TenDangNhap)=DATALENGTH(LTRIM(RTRIM(TenDangNhap)))
  AND TenDangNhap COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^-A-Za-z0-9._]%');
IF EXISTS(SELECT 1 FROM dbo.VAITRO WHERE (MaVT='ADMIN' AND TenVT<>N'Quan tri')
 OR (MaVT='CASH' AND TenVT<>N'Thu ngan') OR (MaVT='STORE' AND TenVT<>N'Quan ly kho'))
 THROW 51407,'Role definition requires review.',1;
INSERT dbo.VAITRO(MaVT,TenVT)
 SELECT x.MaVT,x.TenVT FROM (VALUES('ADMIN',N'Quan tri'),('CASH',N'Thu ngan'),('STORE',N'Quan ly kho')) x(MaVT,TenVT)
 WHERE NOT EXISTS(SELECT 1 FROM dbo.VAITRO v WHERE v.MaVT=x.MaVT);
CREATE TABLE dbo.AppSetupState (
 Id INT NOT NULL CONSTRAINT PK_AppSetupState PRIMARY KEY CONSTRAINT CK_Setup_Singleton CHECK(Id=1),
 State VARCHAR(20) NOT NULL CONSTRAINT CK_Setup_State CHECK(State IN('Pending','LegacyReview','Completed')),
 CompletedAt DATETIME2(0) NULL,
 AdminAccountId INT NULL REFERENCES dbo.TAIKHOAN(MaTK),
 CONSTRAINT CK_Setup_Completed CHECK((State='Completed' AND CompletedAt IS NOT NULL AND AdminAccountId IS NOT NULL)
 OR (State<>'Completed' AND CompletedAt IS NULL AND AdminAccountId IS NULL))
);
INSERT dbo.AppSetupState(Id,State) VALUES(1,CASE WHEN EXISTS(SELECT 1 FROM dbo.TAIKHOAN
 WHERE MaVT='ADMIN' AND MatKhauHash COLLATE Latin1_General_100_BIN2<>'!DISABLED_REPLACE_WITH_APP_PASSWORD_HASH!')
 THEN 'LegacyReview' ELSE 'Pending' END);
CREATE ROLE CafeManagerSetup AUTHORIZATION dbo;
DENY INSERT,UPDATE,DELETE ON dbo.AppSetupState TO CafeManagerApp;
DENY INSERT,UPDATE,DELETE ON dbo.AppSetupState TO CafeManagerSetup;
DENY INSERT,UPDATE,DELETE ON dbo.TAIKHOAN TO CafeManagerApp;
GO
CREATE PROCEDURE dbo.sp_AppReadState AS
BEGIN
 SET NOCOUNT ON;
 SELECT State,CompletedAt,AdminAccountId,(SELECT MAX(Version) FROM dbo.SchemaMigrations) AS SchemaVersion
 FROM dbo.AppSetupState WHERE Id=1;
END;
GO
CREATE PROCEDURE dbo.sp_AuthReadAccount @LoginKey NVARCHAR(MAX) AS
BEGIN
  SET NOCOUNT ON;
  IF @LoginKey IS NULL OR DATALENGTH(@LoginKey) NOT BETWEEN 2 AND 100
   OR DATALENGTH(@LoginKey)<>DATALENGTH(LTRIM(RTRIM(@LoginKey)))
   OR @LoginKey COLLATE Latin1_General_100_BIN2 LIKE N'%[^-A-Za-z0-9._]%'
   THROW 51404,'Invalid username.',1;
  DECLARE @canonicalKey VARCHAR(50)=CONVERT(VARCHAR(50),@LoginKey);
 SELECT t.MaTK,t.MaNV,n.TenNV,t.TenDangNhap,t.MaVT,t.HoatDong,n.DangLamViec,t.MatKhauHash,t.SecurityStamp
 FROM dbo.TAIKHOAN t JOIN dbo.NHANVIEN n ON n.MaNV=t.MaNV WHERE t.LoginKey=@canonicalKey COLLATE Latin1_General_100_BIN2;
END;
GO
CREATE PROCEDURE dbo.sp_AuthReadIdentity @MaTK INT AS
BEGIN
 SET NOCOUNT ON;
 SELECT t.MaTK,t.MaNV,n.TenNV,t.TenDangNhap,t.MaVT,t.HoatDong,n.DangLamViec,t.SecurityStamp
 FROM dbo.TAIKHOAN t JOIN dbo.NHANVIEN n ON n.MaNV=t.MaNV WHERE t.MaTK=@MaTK;
END;
GO
CREATE PROCEDURE dbo.sp_AuthRehash @MaTK INT,@ExpectedHash VARCHAR(255),@ExpectedStamp BINARY(8),@NewHash VARCHAR(255) AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF DATALENGTH(@NewHash) NOT BETWEEN 20 AND 255 THROW 51403,'Invalid hash.',1;
 UPDATE t SET MatKhauHash=@NewHash OUTPUT inserted.SecurityStamp
 FROM dbo.TAIKHOAN t JOIN dbo.NHANVIEN n ON n.MaNV=t.MaNV
 WHERE t.MaTK=@MaTK AND t.SecurityStamp=@ExpectedStamp
 AND t.MatKhauHash COLLATE Latin1_General_100_BIN2=@ExpectedHash COLLATE Latin1_General_100_BIN2
 AND t.HoatDong=1 AND n.DangLamViec=1;
END;
GO
CREATE PROCEDURE dbo.sp_SetupListCandidates AS
BEGIN
 SET NOCOUNT ON;
 SELECT n.MaNV,n.TenNV,n.DangLamViec,t.MaTK,t.TenDangNhap,t.MaVT,t.HoatDong,t.SecurityStamp,
 CAST(CASE WHEN n.DangLamViec=1 AND (t.MaTK IS NULL OR(t.MaVT='ADMIN' AND t.HoatDong=0
 AND t.MatKhauHash COLLATE Latin1_General_100_BIN2='!DISABLED_REPLACE_WITH_APP_PASSWORD_HASH!')) THEN 1 ELSE 0 END AS BIT) AS Eligible
 FROM dbo.NHANVIEN n LEFT JOIN dbo.TAIKHOAN t ON t.MaNV=n.MaNV ORDER BY n.MaNV;
END;
GO
CREATE PROCEDURE dbo.sp_SetupReadLegacyAccount @LoginKey NVARCHAR(MAX) AS
BEGIN
 SET NOCOUNT ON; EXEC dbo.sp_AuthReadAccount @LoginKey;
END;
GO
CREATE PROCEDURE dbo.sp_SetupConfirmLegacy @MaTK INT,@ExpectedHash VARCHAR(255),@ExpectedStamp BINARY(8) AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 BEGIN TRY
  BEGIN TRANSACTION;
  DECLARE @lock INT;
  EXEC @lock=sys.sp_getapplock @Resource=N'CoffeeManager.AdminBootstrap',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
  IF @lock<0 THROW 51407,'Setup lock timeout.',1;
  IF NOT EXISTS(SELECT 1 FROM dbo.AppSetupState WITH(UPDLOCK,HOLDLOCK) WHERE Id=1 AND State='LegacyReview')
   THROW 51401,'Legacy confirmation unavailable.',1;
  IF NOT EXISTS(SELECT 1 FROM dbo.TAIKHOAN t WITH(UPDLOCK,HOLDLOCK) JOIN dbo.NHANVIEN n WITH(HOLDLOCK) ON n.MaNV=t.MaNV
   WHERE t.MaTK=@MaTK AND t.MaVT='ADMIN' AND t.HoatDong=1 AND n.DangLamViec=1 AND t.SecurityStamp=@ExpectedStamp
   AND t.MatKhauHash COLLATE Latin1_General_100_BIN2=@ExpectedHash COLLATE Latin1_General_100_BIN2)
   THROW 51405,'Legacy identity changed.',1;
  UPDATE dbo.AppSetupState SET State='Completed',CompletedAt=SYSUTCDATETIME(),AdminAccountId=@MaTK WHERE Id=1;
  COMMIT;
  EXEC dbo.sp_AppReadState;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH
END;
GO
CREATE PROCEDURE dbo.sp_SetupBootstrap
 @Mode VARCHAR(30),@Username NVARCHAR(MAX),@NewHash VARCHAR(255),@MaNV CHAR(5)=NULL,
 @TenNV NVARCHAR(50)=NULL,@SDT VARCHAR(15)=NULL,@ExpectedStamp BINARY(8)=NULL AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 BEGIN TRY
  BEGIN TRANSACTION;
  DECLARE @lock INT,@id INT;
  EXEC @lock=sys.sp_getapplock @Resource=N'CoffeeManager.AdminBootstrap',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
  IF @lock<0 THROW 51407,'Setup lock timeout.',1;
  IF NOT EXISTS(SELECT 1 FROM dbo.AppSetupState WITH(UPDLOCK,HOLDLOCK) WHERE Id=1 AND State='Pending') THROW 51401,'Bootstrap unavailable.',1;
  IF EXISTS(SELECT 1 FROM dbo.TAIKHOAN WITH(UPDLOCK,HOLDLOCK) WHERE MaVT='ADMIN'
   AND MatKhauHash COLLATE Latin1_General_100_BIN2<>'!DISABLED_REPLACE_WITH_APP_PASSWORD_HASH!') THROW 51402,'Legacy review required.',1;
  IF @Mode NOT IN('NewEmployee','ExistingEmployee','ActivateDemo') OR @Mode IS NULL
   OR @NewHash IS NULL OR DATALENGTH(@NewHash) NOT BETWEEN 20 AND 255 THROW 51403,'Invalid setup input.',1;
  -- Preserve the original Unicode input until length/ASCII validation, before converting to storage type.
  IF @Username IS NULL OR DATALENGTH(@Username) NOT BETWEEN 2 AND 100
   OR DATALENGTH(@Username)<>DATALENGTH(LTRIM(RTRIM(@Username)))
   OR @Username COLLATE Latin1_General_100_BIN2 LIKE N'%[^-A-Za-z0-9._]%' THROW 51404,'Invalid username.',1;
  DECLARE @canonicalUsername VARCHAR(50)=CONVERT(VARCHAR(50),@Username);
  IF @Mode='NewEmployee'
  BEGIN
   IF NULLIF(LTRIM(RTRIM(@TenNV)),N'') IS NULL THROW 51403,'Employee name required.',1;
   IF EXISTS(SELECT 1 FROM dbo.CHUCVU WITH(UPDLOCK,HOLDLOCK) WHERE MaCV='CVADM'
    AND(TenCV<>N'Quản trị ban đầu' OR PhuCap<>0 OR HeSoTangCa<>1.5)) THROW 51407,'Bootstrap position conflict.',1;
   IF NOT EXISTS(SELECT 1 FROM dbo.CHUCVU WHERE MaCV='CVADM')
    INSERT dbo.CHUCVU(MaCV,TenCV,PhuCap,HeSoTangCa) VALUES('CVADM',N'Quản trị ban đầu',0,1.5);
   DECLARE @code TABLE(Loai VARCHAR(20),Ma CHAR(5));
   INSERT @code EXEC dbo.sp_CapMa 'NHANVIEN'; SELECT @MaNV=Ma FROM @code;
   INSERT dbo.NHANVIEN(MaNV,TenNV,SDT,LoaiHopDong,LuongCung,LuongTheoGio,MaCV,DangLamViec)
    VALUES(@MaNV,LTRIM(RTRIM(@TenNV)),NULLIF(LTRIM(RTRIM(@SDT)),''),'Full-time',0,0,'CVADM',1);
  END;
  IF NOT EXISTS(SELECT 1 FROM dbo.NHANVIEN WITH(UPDLOCK,HOLDLOCK) WHERE MaNV=@MaNV AND DangLamViec=1) THROW 51403,'Employee unavailable.',1;
  IF @Mode='ActivateDemo'
  BEGIN
   UPDATE dbo.TAIKHOAN SET MatKhauHash=@NewHash,HoatDong=1
    WHERE MaNV=@MaNV AND MaVT='ADMIN' AND HoatDong=0 AND SecurityStamp=@ExpectedStamp
    AND TenDangNhap=@canonicalUsername AND MatKhauHash COLLATE Latin1_General_100_BIN2='!DISABLED_REPLACE_WITH_APP_PASSWORD_HASH!';
   IF @@ROWCOUNT<>1 THROW 51405,'Demo account changed.',1;
   SELECT @id=MaTK FROM dbo.TAIKHOAN WHERE MaNV=@MaNV;
  END
  ELSE
  BEGIN
   INSERT dbo.TAIKHOAN(MaNV,TenDangNhap,MatKhauHash,MaVT,HoatDong) VALUES(@MaNV,@canonicalUsername,@NewHash,'ADMIN',1);
   SET @id=CONVERT(INT,SCOPE_IDENTITY());
  END;
  UPDATE dbo.AppSetupState SET State='Completed',CompletedAt=SYSUTCDATETIME(),AdminAccountId=@id WHERE Id=1;
  COMMIT;
  EXEC dbo.sp_AppReadState;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH
END;
GO
GRANT EXECUTE ON dbo.sp_AppReadState TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_AuthReadAccount TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_AuthReadIdentity TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_AuthRehash TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_AppReadState TO CafeManagerSetup;
GRANT EXECUTE ON dbo.sp_SetupListCandidates TO CafeManagerSetup;
GRANT EXECUTE ON dbo.sp_SetupReadLegacyAccount TO CafeManagerSetup;
GRANT EXECUTE ON dbo.sp_SetupBootstrap TO CafeManagerSetup;
GRANT EXECUTE ON dbo.sp_SetupConfirmLegacy TO CafeManagerSetup;
GO
CREATE PROCEDURE dbo.sp_SetupCheckAccess AS
BEGIN
 SET NOCOUNT ON;
 IF ISNULL(IS_ROLEMEMBER('CafeManagerSetup'),0)<>1 OR ISNULL(IS_ROLEMEMBER('CafeManagerApp'),0)=1
  THROW 51406,'Separate provisioned setup identity required.',1;
 SELECT CAST(1 AS BIT) Allowed;
END;
GO
GRANT EXECUTE ON dbo.sp_SetupCheckAccess TO CafeManagerSetup;
