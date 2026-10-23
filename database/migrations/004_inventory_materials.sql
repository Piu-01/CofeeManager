-- T09. Apply with Apply-Migration.ps1 -Version 4; never run on an unselected database.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF @@TRANCOUNT=0 OR NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=3)
 THROW 51607,N'Migration 003 và transaction migration là bắt buộc.',1;
IF EXISTS(SELECT 1 FROM dbo.NGUYENVATLIEU WITH(TABLOCKX,HOLDLOCK)
 WHERE NULLIF(LTRIM(RTRIM(TenNVL)),N'') IS NULL OR NULLIF(LTRIM(RTRIM(DonViTinh)),N'') IS NULL)
 THROW 51607,N'Nguyên liệu cũ có tên/đơn vị không hợp lệ.',1;
ALTER TABLE dbo.NGUYENVATLIEU ADD Revision rowversion;
CREATE TABLE dbo.MaterialOpening(
 MaNVL char(5) NOT NULL PRIMARY KEY REFERENCES dbo.NGUYENVATLIEU(MaNVL),
 Quantity decimal(18,4) NOT NULL CHECK(Quantity>=0), UnitSnapshot nvarchar(20) NOT NULL,
 RecordedAt datetime2(7) NOT NULL DEFAULT SYSDATETIME(), Source nvarchar(100) NOT NULL,
 ActorAccountId int NULL REFERENCES dbo.TAIKHOAN(MaTK), Kind varchar(20) NOT NULL CHECK(Kind IN('Manual','LegacyCutover')),
 RequestId uniqueidentifier NULL);
CREATE UNIQUE INDEX UX_MaterialOpening_Request ON dbo.MaterialOpening(RequestId) WHERE RequestId IS NOT NULL;
INSERT dbo.MaterialOpening(MaNVL,Quantity,UnitSnapshot,Source,Kind)
 SELECT MaNVL,SoLuongTon,DonViTinh,N'Migration004: số dư tại thời điểm chuyển đổi','LegacyCutover' FROM dbo.NGUYENVATLIEU;
CREATE TABLE dbo.G03OperationReceipt(
 RequestId uniqueidentifier NOT NULL PRIMARY KEY, ActorAccountId int NOT NULL REFERENCES dbo.TAIKHOAN(MaTK),
 Operation varchar(40) NOT NULL, EntityCode varchar(10) NOT NULL, PayloadHash binary(32) NOT NULL,
 CompletedAt datetime2(7) NOT NULL DEFAULT SYSDATETIME(), ResultRevision binary(8) NULL);
DENY INSERT,UPDATE,DELETE ON dbo.MaterialOpening TO CafeManagerApp;
DENY INSERT,UPDATE,DELETE ON dbo.G03OperationReceipt TO CafeManagerApp;
DENY INSERT,UPDATE,DELETE ON dbo.NGUYENVATLIEU TO CafeManagerApp;
GO
CREATE PROCEDURE dbo.sp_G03Demand @Actor int,@Stamp binary(8),@Roles varchar(30) AS
BEGIN
 SET NOCOUNT ON;
 EXEC dbo.sp_CatalogDemandActor @Actor,@Stamp,'ANY';
 IF NOT EXISTS(SELECT 1 FROM dbo.TAIKHOAN WHERE MaTK=@Actor AND CHARINDEX(','+RTRIM(MaVT)+',',','+@Roles+',')>0)
  THROW 51605,N'Bạn không có quyền thực hiện thao tác này.',1;
END;
GO
CREATE PROCEDURE dbo.sp_G03BeginReceipt @Actor int,@Operation varchar(40),@RequestId uniqueidentifier,
 @PayloadHash binary(32),@Code varchar(10) OUTPUT,@Revision binary(8) OUTPUT,@Replay bit OUTPUT AS
BEGIN
 SET NOCOUNT ON;
 IF @@TRANCOUNT=0 OR @RequestId IS NULL THROW 51601,N'Thiếu mã yêu cầu hoặc transaction.',1;
 SET @Replay=0;
 IF EXISTS(SELECT 1 FROM dbo.G03OperationReceipt WITH(UPDLOCK,HOLDLOCK) WHERE RequestId=@RequestId)
 BEGIN
  IF NOT EXISTS(SELECT 1 FROM dbo.G03OperationReceipt WHERE RequestId=@RequestId AND ActorAccountId=@Actor AND Operation=@Operation AND PayloadHash=@PayloadHash)
   THROW 51608,N'Mã yêu cầu đã dùng với nội dung hoặc tài khoản khác.',1;
  SELECT @Code=EntityCode,@Revision=ResultRevision FROM dbo.G03OperationReceipt WHERE RequestId=@RequestId;
  SET @Replay=1;
 END;
END;
GO
CREATE PROCEDURE dbo.sp_G03Receipt @Actor int,@Stamp binary(8),@RequestId uniqueidentifier AS
BEGIN
 SET NOCOUNT ON;
 EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,CASH,STORE';
 SELECT EntityCode,ResultRevision,Operation,CompletedAt FROM dbo.G03OperationReceipt WHERE RequestId=@RequestId AND ActorAccountId=@Actor;
END;
GO
CREATE PROCEDURE dbo.sp_MaterialList @Actor int,@Stamp binary(8),@Search nvarchar(max)=N'',@Skip int=0 AS
BEGIN
 SET NOCOUNT ON;
 EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,CASH,STORE';
 IF @Skip<0 OR LEN(@Search)>100 THROW 51601,N'Bộ lọc không hợp lệ.',1;
 SET @Search=REPLACE(REPLACE(REPLACE(REPLACE(@Search,N'~',N'~~'),N'%',N'~%'),N'_',N'~_'),N'[',N'~[');
 SELECT n.MaNVL,n.TenNVL,n.DonViTinh,n.SoLuongTon,o.Kind,n.Revision,SYSDATETIME() ReadAt
 FROM dbo.NGUYENVATLIEU n LEFT JOIN dbo.MaterialOpening o ON o.MaNVL=n.MaNVL
 WHERE n.TenNVL LIKE N'%'+@Search+N'%' ESCAPE N'~' OR n.MaNVL LIKE N'%'+@Search+N'%' ESCAPE N'~'
 ORDER BY n.MaNVL OFFSET @Skip ROWS FETCH NEXT 50 ROWS ONLY;
END;
GO
CREATE PROCEDURE dbo.sp_MaterialGet @Actor int,@Stamp binary(8),@Code varchar(max) AS
BEGIN
 SET NOCOUNT ON;
 EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,CASH,STORE';
 SELECT n.MaNVL,n.TenNVL,n.DonViTinh,n.SoLuongTon,o.Kind,n.Revision,SYSDATETIME() ReadAt
 FROM dbo.NGUYENVATLIEU n LEFT JOIN dbo.MaterialOpening o ON o.MaNVL=n.MaNVL WHERE n.MaNVL=@Code;
END;
GO
CREATE PROCEDURE dbo.sp_MaterialWrite @Actor int,@Stamp binary(8),@Action varchar(10),@Code varchar(max),
 @RequestId uniqueidentifier,@ExpectedRevision binary(8)=NULL,@Name nvarchar(max)=NULL,@Unit nvarchar(max)=NULL,
 @Quantity decimal(38,18)=NULL,@Source nvarchar(max)=NULL AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 BEGIN TRY
  BEGIN TRAN;
  IF @Action='Open' EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN';
  ELSE EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,STORE';
  IF @Action NOT IN('Create','Update','Delete','Open') THROW 51601,N'Thao tác không hợp lệ.',1;
  SET @Code=UPPER(LTRIM(RTRIM(@Code))); SET @Name=LTRIM(RTRIM(@Name)); SET @Unit=LTRIM(RTRIM(@Unit)); SET @Source=LTRIM(RTRIM(@Source));
  IF @Code IS NULL OR DATALENGTH(@Code)<>5 OR @Code COLLATE Latin1_General_100_BIN2 NOT LIKE 'NVL[0-9][0-9]' OR @Code='NVL00'
   THROW 51601,N'Mã nguyên liệu phải từ NVL01 đến NVL99.',1;
  IF @Action IN('Create','Update') AND (@Name IS NULL OR LEN(@Name)=0 OR DATALENGTH(@Name)>100 OR @Unit IS NULL OR LEN(@Unit)=0 OR DATALENGTH(@Unit)>40
    OR @Name LIKE N'%'+NCHAR(9)+N'%' OR @Name LIKE N'%'+NCHAR(10)+N'%' OR @Name LIKE N'%'+NCHAR(13)+N'%'
    OR @Unit LIKE N'%'+NCHAR(9)+N'%' OR @Unit LIKE N'%'+NCHAR(10)+N'%' OR @Unit LIKE N'%'+NCHAR(13)+N'%') THROW 51601,N'Tên/đơn vị không hợp lệ.',1;
  IF @Action='Open' AND (@Quantity IS NULL OR @Quantity<0 OR @Quantity>99999999999999.9999 OR ROUND(@Quantity,4)<>@Quantity OR @Source IS NULL OR LEN(@Source)=0 OR DATALENGTH(@Source)>200)
   THROW 51601,N'Lượng mở sổ hoặc nguồn không hợp lệ.',1;
  DECLARE @payload nvarchar(max)=(SELECT @Action a,@Code c,@Name n,@Unit u,@Quantity q,@Source s,@ExpectedRevision r FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);
  DECLARE @hash binary(32)=HASHBYTES('SHA2_256',CONVERT(varbinary(max),@payload)),@replay bit,@resultCode varchar(10)=@Code,@revision binary(8),@operation varchar(40)='Material'+@Action;
  EXEC dbo.sp_G03BeginReceipt @Actor,@operation,@RequestId,@hash,@resultCode OUTPUT,@revision OUTPUT,@replay OUTPUT;
  IF @replay=0
  BEGIN
   IF @Action='Create'
    INSERT dbo.NGUYENVATLIEU(MaNVL,TenNVL,DonViTinh,SoLuongTon) VALUES(@Code,@Name,@Unit,0);
   ELSE
   BEGIN
    IF NOT EXISTS(SELECT 1 FROM dbo.NGUYENVATLIEU WITH(UPDLOCK,HOLDLOCK) WHERE MaNVL=@Code AND Revision=@ExpectedRevision)
     THROW 51603,N'Nguyên liệu đã thay đổi hoặc bị xóa; tải lại.',1;
    DECLARE @hasSource bit=0;
    IF EXISTS(SELECT 1 FROM dbo.MaterialOpening WITH(UPDLOCK,HOLDLOCK) WHERE MaNVL=@Code)
     OR EXISTS(SELECT 1 FROM dbo.CHITIET_PN WITH(UPDLOCK,HOLDLOCK) WHERE MaNVL=@Code)
     OR EXISTS(SELECT 1 FROM dbo.CHITIET_PX WITH(UPDLOCK,HOLDLOCK) WHERE MaNVL=@Code)
     OR EXISTS(SELECT 1 FROM dbo.CHITIET_KK WITH(UPDLOCK,HOLDLOCK) WHERE MaNVL=@Code)
     OR EXISTS(SELECT 1 FROM dbo.TIEUHAO_HOADON WITH(UPDLOCK,HOLDLOCK) WHERE MaNVL=@Code) SET @hasSource=1;
    IF @Action='Open'
    BEGIN
     IF @hasSource=1 THROW 51602,N'Nguyên liệu đã mở sổ hoặc phát sinh; không thể mở sổ lại.',1;
     INSERT dbo.MaterialOpening(MaNVL,Quantity,UnitSnapshot,Source,ActorAccountId,Kind,RequestId)
      SELECT MaNVL,@Quantity,DonViTinh,@Source,@Actor,'Manual',@RequestId FROM dbo.NGUYENVATLIEU WHERE MaNVL=@Code;
     UPDATE dbo.NGUYENVATLIEU SET SoLuongTon=CONVERT(decimal(18,4),@Quantity) WHERE MaNVL=@Code;
    END
    ELSE IF @Action='Update'
    BEGIN
     IF EXISTS(SELECT 1 FROM dbo.NGUYENVATLIEU WHERE MaNVL=@Code AND DonViTinh<>@Unit)
      AND (@hasSource=1 OR EXISTS(SELECT 1 FROM dbo.CONGTHUC WITH(UPDLOCK,HOLDLOCK) WHERE MaNVL=@Code))
      THROW 51602,N'Đã có mốc, công thức hoặc giao dịch; không được đổi đơn vị.',1;
     UPDATE dbo.NGUYENVATLIEU SET TenNVL=@Name,DonViTinh=@Unit WHERE MaNVL=@Code;
    END
    ELSE
    BEGIN
     IF @hasSource=1 OR EXISTS(SELECT 1 FROM dbo.CONGTHUC WITH(UPDLOCK,HOLDLOCK) WHERE MaNVL=@Code) THROW 51602,N'Nguyên liệu đã được dùng; không thể xóa.',1;
     DELETE dbo.NGUYENVATLIEU WHERE MaNVL=@Code;
    END;
   END;
   SELECT @revision=Revision FROM dbo.NGUYENVATLIEU WHERE MaNVL=@Code;
   INSERT dbo.G03OperationReceipt(RequestId,ActorAccountId,Operation,EntityCode,PayloadHash,ResultRevision) VALUES(@RequestId,@Actor,@operation,@Code,@hash,@revision);
  END;
  COMMIT;
  SELECT @resultCode Code,@revision Revision;
 END TRY
 BEGIN CATCH
  IF @@TRANCOUNT>0 ROLLBACK;
  THROW;
 END CATCH;
END;
GO
CREATE PROCEDURE dbo.sp_MaterialCreate @Actor int,@Stamp binary(8),@Code varchar(max),@RequestId uniqueidentifier,
 @ExpectedRevision binary(8)=NULL,@Name nvarchar(max)=NULL,@Unit nvarchar(max)=NULL,@Quantity decimal(38,18)=NULL,@Source nvarchar(max)=NULL AS
BEGIN
 SET NOCOUNT ON;
 EXEC dbo.sp_MaterialWrite @Actor,@Stamp,'Create',@Code,@RequestId,@ExpectedRevision,@Name,@Unit,@Quantity,@Source;
END;
GO
GRANT EXECUTE ON dbo.sp_MaterialCreate TO CafeManagerApp;
GO
CREATE PROCEDURE dbo.sp_MaterialUpdate @Actor int,@Stamp binary(8),@Code varchar(max),@RequestId uniqueidentifier,
 @ExpectedRevision binary(8)=NULL,@Name nvarchar(max)=NULL,@Unit nvarchar(max)=NULL,@Quantity decimal(38,18)=NULL,@Source nvarchar(max)=NULL AS
BEGIN
 SET NOCOUNT ON;
 EXEC dbo.sp_MaterialWrite @Actor,@Stamp,'Update',@Code,@RequestId,@ExpectedRevision,@Name,@Unit,@Quantity,@Source;
END;
GO
GRANT EXECUTE ON dbo.sp_MaterialUpdate TO CafeManagerApp;
GO
CREATE PROCEDURE dbo.sp_MaterialDelete @Actor int,@Stamp binary(8),@Code varchar(max),@RequestId uniqueidentifier,
 @ExpectedRevision binary(8)=NULL,@Name nvarchar(max)=NULL,@Unit nvarchar(max)=NULL,@Quantity decimal(38,18)=NULL,@Source nvarchar(max)=NULL AS
BEGIN
 SET NOCOUNT ON;
 EXEC dbo.sp_MaterialWrite @Actor,@Stamp,'Delete',@Code,@RequestId,@ExpectedRevision,@Name,@Unit,@Quantity,@Source;
END;
GO
GRANT EXECUTE ON dbo.sp_MaterialDelete TO CafeManagerApp;
GO
CREATE PROCEDURE dbo.sp_MaterialOpen @Actor int,@Stamp binary(8),@Code varchar(max),@RequestId uniqueidentifier,
 @ExpectedRevision binary(8)=NULL,@Name nvarchar(max)=NULL,@Unit nvarchar(max)=NULL,@Quantity decimal(38,18)=NULL,@Source nvarchar(max)=NULL AS
BEGIN
 SET NOCOUNT ON;
 EXEC dbo.sp_MaterialWrite @Actor,@Stamp,'Open',@Code,@RequestId,@ExpectedRevision,@Name,@Unit,@Quantity,@Source;
END;
GO
GRANT EXECUTE ON dbo.sp_MaterialOpen TO CafeManagerApp;
GO
CREATE PROCEDURE dbo.sp_MaterialReconcile @Actor int,@Stamp binary(8),@RequestId uniqueidentifier AS
BEGIN SET NOCOUNT ON; EXEC dbo.sp_G03Receipt @Actor,@Stamp,@RequestId; END;
GO
GRANT EXECUTE ON dbo.sp_G03Receipt TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_MaterialList TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_MaterialGet TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_MaterialReconcile TO CafeManagerApp;
