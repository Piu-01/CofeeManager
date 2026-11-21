-- 041: bỏ thao tác "Mở sổ" khỏi ứng dụng; gộp tồn đầu tùy chọn vào tạo nguyên liệu.
-- Chạy qua tools/database/Apply-Migration.ps1 -Version 41.
-- Giữ bảng MaterialOpening, dữ liệu mốc lịch sử và sp_MaterialOpen cho báo cáo/audit;
-- app không còn quyền gọi sp_MaterialOpen.
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF @@TRANCOUNT=0 OR NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=40)
    THROW 54101,N'Migration 040 bắt buộc.',1;
IF OBJECT_ID(N'dbo.sp_MaterialWrite',N'P') IS NULL OR OBJECT_ID(N'dbo.MaterialOpening',N'U') IS NULL
    THROW 54101,N'Thiếu sp_MaterialWrite/MaterialOpening; cần đối chiếu trước nâng cấp.',1;

-- Nguyên liệu tạo sau 004 chưa có mốc: chốt mốc bằng tồn hiện tại tại thời điểm nâng cấp.
-- Nguyên liệu đã có mốc (kể cả LegacyCutover) giữ nguyên.
INSERT dbo.MaterialOpening(MaNVL,Quantity,UnitSnapshot,Source,Kind)
SELECT n.MaNVL,n.SoLuongTon,n.DonViTinh,N'Migration041: mốc tồn đầu khi bỏ thao tác mở sổ','Manual'
FROM dbo.NGUYENVATLIEU n
WHERE NOT EXISTS(SELECT 1 FROM dbo.MaterialOpening o WHERE o.MaNVL=n.MaNVL);
GO
ALTER PROCEDURE dbo.sp_MaterialWrite @Actor int,@Stamp binary(8),@Action varchar(10),@Code varchar(max),
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
  -- Tồn đầu chỉ áp dụng lúc tạo; bỏ trống thì ghi mốc 0 để báo cáo tồn không bị "chưa mở sổ".
  IF @Action='Create' AND @Quantity IS NOT NULL AND (@Quantity<0 OR @Quantity>99999999999999.9999 OR ROUND(@Quantity,4)<>@Quantity)
   THROW 51601,N'Lượng tồn đầu không hợp lệ.',1;
  IF @Action='Create' AND @Quantity>0 AND (@Source IS NULL OR LEN(@Source)=0) THROW 51601,N'Cần nguồn cho tồn đầu.',1;
  IF @Action='Create' AND @Source IS NOT NULL AND (DATALENGTH(@Source)>200
    OR @Source LIKE N'%'+NCHAR(9)+N'%' OR @Source LIKE N'%'+NCHAR(10)+N'%' OR @Source LIKE N'%'+NCHAR(13)+N'%')
   THROW 51601,N'Nguồn tồn đầu không hợp lệ.',1;
  DECLARE @payload nvarchar(max)=(SELECT @Action a,@Code c,@Name n,@Unit u,@Quantity q,@Source s,@ExpectedRevision r FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);
  DECLARE @hash binary(32)=HASHBYTES('SHA2_256',CONVERT(varbinary(max),@payload)),@replay bit,@resultCode varchar(10)=@Code,@revision binary(8),@operation varchar(40)='Material'+@Action;
  EXEC dbo.sp_G03BeginReceipt @Actor,@operation,@RequestId,@hash,@resultCode OUTPUT,@revision OUTPUT,@replay OUTPUT;
  IF @replay=0
  BEGIN
   IF @Action='Create'
   BEGIN
    DECLARE @opening decimal(18,4)=CONVERT(decimal(18,4),COALESCE(@Quantity,0));
    INSERT dbo.NGUYENVATLIEU(MaNVL,TenNVL,DonViTinh,SoLuongTon) VALUES(@Code,@Name,@Unit,@opening);
    INSERT dbo.MaterialOpening(MaNVL,Quantity,UnitSnapshot,Source,ActorAccountId,Kind,RequestId)
     VALUES(@Code,@opening,@Unit,COALESCE(NULLIF(@Source,N''),N'Khởi tạo nguyên liệu'),@Actor,'Manual',@RequestId);
   END;
   ELSE
   BEGIN
    IF NOT EXISTS(SELECT 1 FROM dbo.NGUYENVATLIEU WITH(UPDLOCK,HOLDLOCK) WHERE MaNVL=@Code AND Revision=@ExpectedRevision)
     THROW 51603,N'Nguyên liệu đã thay đổi hoặc bị xóa; tải lại.',1;
    DECLARE @hasSource bit=0,@hasTransaction bit=0;
    IF EXISTS(SELECT 1 FROM dbo.MaterialOpening WITH(UPDLOCK,HOLDLOCK) WHERE MaNVL=@Code)
     OR EXISTS(SELECT 1 FROM dbo.CHITIET_PN WITH(UPDLOCK,HOLDLOCK) WHERE MaNVL=@Code)
     OR EXISTS(SELECT 1 FROM dbo.CHITIET_PX WITH(UPDLOCK,HOLDLOCK) WHERE MaNVL=@Code)
     OR EXISTS(SELECT 1 FROM dbo.CHITIET_KK WITH(UPDLOCK,HOLDLOCK) WHERE MaNVL=@Code)
     OR EXISTS(SELECT 1 FROM dbo.TIEUHAO_HOADON WITH(UPDLOCK,HOLDLOCK) WHERE MaNVL=@Code) SET @hasSource=1;
    IF EXISTS(SELECT 1 FROM dbo.CHITIET_PN WITH(UPDLOCK,HOLDLOCK) WHERE MaNVL=@Code)
     OR EXISTS(SELECT 1 FROM dbo.CHITIET_PX WITH(UPDLOCK,HOLDLOCK) WHERE MaNVL=@Code)
     OR EXISTS(SELECT 1 FROM dbo.CHITIET_KK WITH(UPDLOCK,HOLDLOCK) WHERE MaNVL=@Code)
     OR EXISTS(SELECT 1 FROM dbo.TIEUHAO_HOADON WITH(UPDLOCK,HOLDLOCK) WHERE MaNVL=@Code) SET @hasTransaction=1;
    IF @Action='Open'
    BEGIN
     IF @hasSource=1 THROW 51602,N'Nguyên liệu đã mở sổ hoặc phát sinh; không thể mở sổ lại.',1;
     INSERT dbo.MaterialOpening(MaNVL,Quantity,UnitSnapshot,Source,ActorAccountId,Kind,RequestId)
      SELECT MaNVL,@Quantity,DonViTinh,@Source,@Actor,'Manual',@RequestId FROM dbo.NGUYENVATLIEU WHERE MaNVL=@Code;
     UPDATE dbo.NGUYENVATLIEU SET SoLuongTon=CONVERT(decimal(18,4),@Quantity) WHERE MaNVL=@Code;
    END
    ELSE IF @Action='Update'
    BEGIN
     -- Tồn đầu 0 chưa mang nghĩa định lượng nên vẫn cho đổi đơn vị của nguyên liệu chưa dùng.
     IF EXISTS(SELECT 1 FROM dbo.NGUYENVATLIEU WHERE MaNVL=@Code AND DonViTinh<>@Unit)
      AND (@hasTransaction=1 OR EXISTS(SELECT 1 FROM dbo.CONGTHUC WITH(UPDLOCK,HOLDLOCK) WHERE MaNVL=@Code)
       OR EXISTS(SELECT 1 FROM dbo.MaterialOpening WITH(UPDLOCK,HOLDLOCK) WHERE MaNVL=@Code AND Quantity>0))
      THROW 51602,N'Đã có giao dịch, công thức hoặc tồn đầu; không được đổi đơn vị.',1;
     UPDATE dbo.MaterialOpening SET UnitSnapshot=@Unit WHERE MaNVL=@Code;
     UPDATE dbo.NGUYENVATLIEU SET TenNVL=@Name,DonViTinh=@Unit WHERE MaNVL=@Code;
    END
    ELSE
    BEGIN
     -- Mốc tồn đầu được tạo cùng nguyên liệu; xóa nguyên liệu chưa dùng thì xóa kèm mốc.
     IF @hasTransaction=1 OR EXISTS(SELECT 1 FROM dbo.CONGTHUC WITH(UPDLOCK,HOLDLOCK) WHERE MaNVL=@Code) THROW 51602,N'Nguyên liệu đã được dùng; không thể xóa.',1;
     DELETE dbo.MaterialOpening WHERE MaNVL=@Code;
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
-- App không còn thao tác mở sổ; procedure giữ lại chỉ cho lịch sử/khôi phục có kiểm soát.
REVOKE EXECUTE ON dbo.sp_MaterialOpen FROM CafeManagerApp;
