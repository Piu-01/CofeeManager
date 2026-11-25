-- 048: Cho phép sửa/xóa/tạm dừng khuyến mãi mã cũ (KM24BF, KM25xxx…).
-- 042 kiểm 'KM+8 số' cho mọi thao tác nên chặn cả Update/Delete/Pause/Resume, làm mất
-- quy định của 009 (chỉ siết định dạng khi tạo mới). 048 giữ nguyên định dạng cũ khi
-- các thao tác khác, chỉ bắt mã mới đúng KM+8 số.
-- Chạy sau 047 bằng tools/database/Apply-Migration.ps1 -Version 48.
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF @@TRANCOUNT=0 OR NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=47)
    THROW 54801,N'Migration 047 bắt buộc.',1;
IF OBJECT_ID(N'dbo.sp_PromotionWrite',N'P') IS NULL
 OR OBJECT_ID(N'dbo.sp_PromotionCreate',N'P') IS NULL
 OR OBJECT_ID(N'dbo.sp_PromotionUpdate',N'P') IS NULL
 OR OBJECT_ID(N'dbo.sp_PromotionDelete',N'P') IS NULL
    THROW 54801,N'Thiếu contract khuyến mãi của 042; cần đối chiếu trước nâng cấp.',1;
GO
/* ===== 1. sp_PromotionWrite: chỉ siết mã KM+8 số khi Create ===== */
ALTER PROCEDURE dbo.sp_PromotionWrite @Actor int,@Stamp binary(8),@Action varchar(10),@Code varchar(max),
 @RequestId uniqueidentifier,@ExpectedRevision binary(8)=NULL,@Name nvarchar(max)=NULL,@StartDate date=NULL,
 @EndDate date=NULL,@Discount bigint=NULL,@Audience varchar(10)=NULL,@Status varchar(10)=NULL,
 @TiersJson nvarchar(max)=NULL,@ItemsJson nvarchar(max)=NULL,@Kind varchar(10)=NULL,@GiftJson nvarchar(max)=NULL AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 BEGIN TRY
  BEGIN TRAN; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN';
  IF @Action NOT IN('Create','Update','Delete','Pause','Resume') THROW 52001,N'Thao tác không hợp lệ.',1;
  SET @Code=UPPER(LTRIM(RTRIM(@Code)));
  -- Mã tạo mới phải KM+8 số; mã cũ được giữ khi sửa/xóa/tạm dừng (như 009).
  IF @Code IS NULL OR DATALENGTH(@Code)>10 OR DATALENGTH(@Code)=0
   OR (@Action='Create' AND (DATALENGTH(@Code)<>10 OR @Code COLLATE Latin1_General_100_BIN2 NOT LIKE 'KM[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'))
   THROW 52001,N'Mã KM và 8 chữ số.',1;
  DECLARE @tiers TABLE(MaHang char(5) NOT NULL PRIMARY KEY);
  DECLARE @items TABLE(MaSP char(5) NOT NULL PRIMARY KEY);
  DECLARE @gifts TABLE(MaSP char(5) NOT NULL PRIMARY KEY,SoLuong int NOT NULL);
  IF @Action IN('Create','Update')
  BEGIN
   SET @Name=LTRIM(RTRIM(@Name)); SET @Audience=UPPER(LTRIM(RTRIM(@Audience)));
   SET @Kind=UPPER(LTRIM(RTRIM(@Kind)));
   IF @Status IS NULL AND @Action='Create' SET @Status='DangChay';
   IF @Status IS NULL SET @Status=(SELECT k.TrangThai FROM dbo.KHUYENMAI k WITH(HOLDLOCK) WHERE k.MaKM=@Code);
   IF @Kind IS NULL SET @Kind=(SELECT k.LoaiUuDai FROM dbo.KHUYENMAI k WITH(HOLDLOCK) WHERE k.MaKM=@Code);
   IF @Kind IS NULL SET @Kind='Giam';
   IF @Status IS NULL THROW 51603,N'Chương trình không tồn tại hoặc thiếu trạng thái.',1;
   IF @Name IS NULL OR LEN(@Name)=0 OR DATALENGTH(@Name)>200 OR @Name LIKE N'%'+NCHAR(9)+N'%'
    OR @Name LIKE N'%'+NCHAR(10)+N'%' OR @Name LIKE N'%'+NCHAR(13)+N'%'
    OR @StartDate IS NULL
    OR @Status NOT IN('DangChay','TamDung') OR @Audience NOT IN('ALL','TIER','WALKIN')
    OR @Kind NOT IN('Giam','Tang')
    THROW 52001,N'Thông tin chương trình không hợp lệ.',1;
   -- Ngày chỉ dùng ngày/tháng: BD năm 2000; KT năm 2001 khi vắt năm, NULL = hết năm.
   DECLARE @sm int=MONTH(@StartDate),@sd int=DAY(@StartDate);
   SET @StartDate=DATEFROMPARTS(2000,@sm,@sd);
   IF @EndDate IS NOT NULL
   BEGIN
    DECLARE @em int=MONTH(@EndDate),@ed int=DAY(@EndDate);
    SET @EndDate=DATEFROMPARTS(CASE WHEN @em*100+@ed<@sm*100+@sd THEN 2001 ELSE 2000 END,@em,@ed);
   END;
   IF @Audience='TIER'
   BEGIN
    IF @TiersJson IS NOT NULL AND ISJSON(@TiersJson)<>1 THROW 52001,N'Cần danh sách hạng hợp lệ.',1;
    IF @TiersJson IS NOT NULL
     INSERT @tiers SELECT DISTINCT UPPER(LTRIM(RTRIM(CONVERT(char(5),value)))) FROM OPENJSON(@TiersJson)
      WHERE value IS NOT NULL AND LEN(value) BETWEEN 1 AND 5;
    IF EXISTS(SELECT 1 FROM @tiers t WHERE NOT EXISTS(SELECT 1 FROM dbo.HANGTHANHVIEN h WHERE h.MaHang=t.MaHang))
     THROW 52005,N'Hạng không tồn tại.',1;
   END
   ELSE IF @TiersJson IS NOT NULL AND EXISTS(SELECT 1 FROM OPENJSON(@TiersJson))
    THROW 52001,N'Chỉ ưu đãi theo hạng mới chọn hạng.',1;
   IF @Kind='Giam'
   BEGIN
    IF @Discount IS NULL OR @Discount<0 OR @Discount>100 THROW 52001,N'Tỷ lệ giảm phải 0–100.',1;
    IF @GiftJson IS NOT NULL AND EXISTS(SELECT 1 FROM OPENJSON(@GiftJson)) THROW 52001,N'Ưu đãi giảm không kèm món tặng.',1;
    IF @ItemsJson IS NOT NULL
    BEGIN
     IF ISJSON(@ItemsJson)<>1 THROW 52001,N'Danh sách món không hợp lệ.',1;
     INSERT @items SELECT DISTINCT CONVERT(char(5),value) FROM OPENJSON(@ItemsJson)
      WHERE value IS NOT NULL AND LEN(value)=5;
    END;
    IF (SELECT COUNT(*) FROM @items)>500 THROW 52001,N'Tối đa 500 món mỗi ưu đãi.',1;
    IF EXISTS(SELECT 1 FROM @items i WHERE NOT EXISTS(SELECT 1 FROM dbo.SANPHAM s WHERE s.MaSP=i.MaSP))
     THROW 52005,N'Món không tồn tại.',1;
   END
   ELSE
   BEGIN
    SET @Discount=0;
    IF @ItemsJson IS NOT NULL AND EXISTS(SELECT 1 FROM OPENJSON(@ItemsJson)) THROW 52001,N'Ưu đãi tặng món không có phạm vi món giảm.',1;
    IF @GiftJson IS NULL OR ISJSON(@GiftJson)<>1 THROW 52001,N'Cần danh sách món tặng.',1;
    IF EXISTS(SELECT 1 FROM OPENJSON(@GiftJson) WITH(MaSP nvarchar(20) '$.Product',Quantity int '$.Quantity') j
      WHERE j.MaSP IS NULL OR LEN(LTRIM(RTRIM(j.MaSP)))<>5 OR j.Quantity IS NULL)
     THROW 52001,N'Mã món tặng và số lượng không hợp lệ.',1;
    INSERT @gifts(MaSP,SoLuong)
    SELECT UPPER(LTRIM(RTRIM(CONVERT(char(5),j.MaSP)))),SUM(j.Quantity)
    FROM OPENJSON(@GiftJson) WITH(MaSP nvarchar(20) '$.Product',Quantity int '$.Quantity') j
    WHERE j.MaSP IS NOT NULL GROUP BY UPPER(LTRIM(RTRIM(CONVERT(char(5),j.MaSP))));
    IF NOT EXISTS(SELECT 1 FROM @gifts) THROW 52001,N'Cần ít nhất một món tặng.',1;
    IF (SELECT COUNT(*) FROM @gifts)>50 THROW 52001,N'Tối đa 50 món tặng mỗi ưu đãi.',1;
    IF EXISTS(SELECT 1 FROM @gifts WHERE SoLuong NOT BETWEEN 1 AND 999) THROW 52001,N'Số lượng quà phải 1–999.',1;
    IF EXISTS(SELECT 1 FROM @gifts g WHERE NOT EXISTS(SELECT 1 FROM dbo.SANPHAM s
      WHERE s.MaSP=g.MaSP AND s.TrangThai=N'Dang ban' AND EXISTS(SELECT 1 FROM dbo.CONGTHUC c WHERE c.MaSP=g.MaSP)))
     THROW 52005,N'Món tặng ngừng bán hoặc chưa có công thức.',1;
   END;
  END;
  DECLARE @payload nvarchar(max)=(SELECT @Code c,@Action a,@Name n,@StartDate sd,@EndDate ed,@Discount d,
   @Audience au,@Status st,@TiersJson tj,@ItemsJson ij,@Kind k,@GiftJson gj,@ExpectedRevision r
   FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);
  DECLARE @hash binary(32)=HASHBYTES('SHA2_256',CONVERT(varbinary(max),@payload)),@resultCode varchar(10)=@Code,
   @revision binary(8),@replay bit,@operation varchar(40)='Promotion'+@Action;
  EXEC dbo.sp_G03BeginReceipt @Actor,@operation,@RequestId,@hash,@resultCode OUTPUT,@revision OUTPUT,@replay OUTPUT;
  IF @replay=0
  BEGIN
   IF @Action='Create'
   BEGIN
    IF EXISTS(SELECT 1 FROM dbo.KHUYENMAI WITH(UPDLOCK,HOLDLOCK) WHERE MaKM=@Code) THROW 52003,N'Mã KM đã tồn tại.',1;
    INSERT dbo.KHUYENMAI(MaKM,TenKM,NgayBD,NgayKT,PhanTramGiam,TrangThai,DoiTuongKhach,PhamVi,LoaiUuDai)
    VALUES(@Code,@Name,@StartDate,@EndDate,CONVERT(int,@Discount),@Status,@Audience,
     CASE WHEN @Kind='Giam' AND EXISTS(SELECT 1 FROM @items) THEN 'ITEM' ELSE 'ALL' END,@Kind);
   END
   ELSE
   BEGIN
    IF NOT EXISTS(SELECT 1 FROM dbo.KHUYENMAI WITH(UPDLOCK,HOLDLOCK) WHERE MaKM=@Code AND Revision=@ExpectedRevision)
     THROW 51603,N'Chương trình đã thay đổi.',1;
    IF @Action='Delete'
    BEGIN
     IF EXISTS(SELECT 1 FROM dbo.HoaDonUuDaiApDung WHERE MaKM=@Code)
      OR EXISTS(SELECT 1 FROM dbo.CHITIET_HD WHERE MaKMQuaTang=@Code)
      THROW 52006,N'Ưu đãi đã dùng trong hóa đơn; chỉ tạm dừng, không xóa.',1;
     DELETE dbo.KHUYENMAI WHERE MaKM=@Code;
    END
    ELSE IF @Action='Pause'
     UPDATE dbo.KHUYENMAI SET TrangThai='TamDung' WHERE MaKM=@Code;
    ELSE IF @Action='Resume'
     UPDATE dbo.KHUYENMAI SET TrangThai='DangChay' WHERE MaKM=@Code;
    ELSE
    BEGIN
     UPDATE dbo.KHUYENMAI SET TenKM=@Name,NgayBD=@StartDate,NgayKT=@EndDate,
      PhanTramGiam=CONVERT(int,@Discount),TrangThai=@Status,DoiTuongKhach=@Audience,
      PhamVi=CASE WHEN @Kind='Giam' AND EXISTS(SELECT 1 FROM @items) THEN 'ITEM' ELSE 'ALL' END,
      LoaiUuDai=@Kind
     WHERE MaKM=@Code;
    END;
   END;
   IF @Action IN('Create','Update')
   BEGIN
    DELETE dbo.KHUYENMAI_HANG WHERE MaKM=@Code;
    INSERT dbo.KHUYENMAI_HANG(MaKM,MaHang) SELECT @Code,MaHang FROM @tiers;
    DELETE dbo.KHUYENMAI_MON WHERE MaKM=@Code;
    INSERT dbo.KHUYENMAI_MON(MaKM,MaSP) SELECT @Code,MaSP FROM @items;
    DELETE dbo.KHUYENMAI_MON_TANG WHERE MaKM=@Code;
    INSERT dbo.KHUYENMAI_MON_TANG(MaKM,MaSP,SoLuong) SELECT @Code,MaSP,SoLuong FROM @gifts;
    UPDATE dbo.KHUYENMAI SET TenKM=TenKM WHERE MaKM=@Code;
   END;
   SELECT @revision=Revision FROM dbo.KHUYENMAI WHERE MaKM=@Code;
   INSERT dbo.G03OperationReceipt(RequestId,ActorAccountId,Operation,EntityCode,PayloadHash,ResultRevision)
    VALUES(@RequestId,@Actor,@operation,@Code,@hash,@revision);
  END;
  COMMIT; SELECT @resultCode Code,@revision Revision;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GRANT EXECUTE ON dbo.sp_PromotionWrite TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_PromotionCreate TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_PromotionUpdate TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_PromotionDelete TO CafeManagerApp;
GO
