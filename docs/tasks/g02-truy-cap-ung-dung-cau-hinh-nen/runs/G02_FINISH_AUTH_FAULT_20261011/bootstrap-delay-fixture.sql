
ALTER PROCEDURE dbo.sp_SetupBootstrap
 @Mode VARCHAR(30),@Username VARCHAR(50),@NewHash VARCHAR(255),@MaNV CHAR(5)=NULL,
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
  IF @Username IS NULL OR DATALENGTH(@Username) NOT BETWEEN 1 AND 50
   OR DATALENGTH(@Username)<>DATALENGTH(LTRIM(RTRIM(@Username)))
   OR @Username COLLATE Latin1_General_100_BIN2 LIKE '%[^A-Za-z0-9._-]%' THROW 51404,'Invalid username.',1;
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
    AND TenDangNhap=@Username AND MatKhauHash COLLATE Latin1_General_100_BIN2='!DISABLED_REPLACE_WITH_APP_PASSWORD_HASH!';
   IF @@ROWCOUNT<>1 THROW 51405,'Demo account changed.',1;
   SELECT @id=MaTK FROM dbo.TAIKHOAN WHERE MaNV=@MaNV;
  END
  ELSE
  BEGIN
   INSERT dbo.TAIKHOAN(MaNV,TenDangNhap,MatKhauHash,MaVT,HoatDong) VALUES(@MaNV,@Username,@NewHash,'ADMIN',1);
   SET @id=CONVERT(INT,SCOPE_IDENTITY());
  END;
  UPDATE dbo.AppSetupState SET State='Completed',CompletedAt=SYSUTCDATETIME(),AdminAccountId=@id WHERE Id=1;
  COMMIT; WAITFOR DELAY '00:00:03';
  EXEC dbo.sp_AppReadState;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH
END;
