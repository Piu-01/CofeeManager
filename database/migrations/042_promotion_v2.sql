-- 042: Khuyến mãi v2 — lặp hằng năm theo ngày/tháng, trạng thái Áp dụng/Tạm ngưng,
-- đối tượng ALL/TIER/WALKIN, loại Giảm % hoặc Tặng món.
-- Chạy sau 041 bằng tools/database/Apply-Migration.ps1 -Version 42.
-- Không đổi dữ liệu lịch sử hóa đơn; không backfill snapshot/ưu đãi đã áp.
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF @@TRANCOUNT=0 OR NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=41)
    THROW 54201,N'Migration 041 bắt buộc.',1;
IF OBJECT_ID(N'dbo.KHUYENMAI',N'U') IS NULL OR OBJECT_ID(N'dbo.KHUYENMAI_MON',N'U') IS NULL
 OR OBJECT_ID(N'dbo.HoaDonUuDaiApDung',N'U') IS NULL OR OBJECT_ID(N'dbo.sp_PromotionWrite',N'P') IS NULL
 OR OBJECT_ID(N'dbo.sp_ThanhToanHoaDon',N'P') IS NULL OR OBJECT_ID(N'dbo.sp_InvoiceDetail',N'P') IS NULL
 OR OBJECT_ID(N'dbo.fn_CheckoutPreview',N'IF') IS NULL
    THROW 54201,N'Thiếu object khuyến mãi/bán hàng của 039; cần đối chiếu trước nâng cấp.',1;
GO
/* ===== 1. DDL ===== */
ALTER TABLE dbo.KHUYENMAI ADD LoaiUuDai varchar(10) NOT NULL
 CONSTRAINT DF_KHUYENMAI_LoaiUuDai DEFAULT 'Giam';
GO
ALTER TABLE dbo.KHUYENMAI ADD CONSTRAINT CK_KHUYENMAI_LoaiUuDai CHECK(LoaiUuDai IN('Giam','Tang'));
GO
ALTER TABLE dbo.KHUYENMAI DROP CONSTRAINT CK_KHUYENMAI_TrangThai;
ALTER TABLE dbo.KHUYENMAI DROP CONSTRAINT CK_KHUYENMAI_DoiTuong;
ALTER TABLE dbo.KHUYENMAI DROP CONSTRAINT CK_KHUYENMAI_NgayHopLe;
GO
ALTER TABLE dbo.KHUYENMAI ADD CONSTRAINT CK_KHUYENMAI_TrangThai CHECK(TrangThai IN('DangChay','TamDung'));
ALTER TABLE dbo.KHUYENMAI ADD CONSTRAINT CK_KHUYENMAI_DoiTuong CHECK(DoiTuongKhach IN('ALL','TIER','WALKIN'));
GO
CREATE TABLE dbo.KHUYENMAI_MON_TANG(
 MaKM char(10) NOT NULL CONSTRAINT FK_KM_MON_TANG_KM REFERENCES dbo.KHUYENMAI(MaKM) ON DELETE CASCADE,
 MaSP char(5) NOT NULL CONSTRAINT FK_KM_MON_TANG_SP REFERENCES dbo.SANPHAM(MaSP),
 SoLuong int NOT NULL CONSTRAINT CK_KM_MON_TANG_SoLuong CHECK(SoLuong BETWEEN 1 AND 999),
 CONSTRAINT PK_KHUYENMAI_MON_TANG PRIMARY KEY(MaKM,MaSP));
GO
ALTER TABLE dbo.CHITIET_HD ADD MaKMQuaTang char(10) NULL
 CONSTRAINT FK_CTHD_KM_QUATANG REFERENCES dbo.KHUYENMAI(MaKM);
GO
ALTER TABLE dbo.CHITIET_HD DROP CONSTRAINT CHK_CTHD_GiaBan;
GO
ALTER TABLE dbo.CHITIET_HD ADD CONSTRAINT CHK_CTHD_GiaBan CHECK(GiaBan >= 0);
GO
CREATE INDEX IX_CHITIET_HD_MaKMQuaTang ON dbo.CHITIET_HD(MaKMQuaTang)
 WHERE MaKMQuaTang IS NOT NULL;
GO
ALTER TABLE dbo.HoaDonUuDaiApDung DROP CONSTRAINT CK_HDUD_Audience;
GO
ALTER TABLE dbo.HoaDonUuDaiApDung ADD CONSTRAINT CK_HDUD_Audience CHECK(Audience IN('ALL','TIER','WALKIN'));
GO
DENY INSERT,UPDATE,DELETE ON dbo.KHUYENMAI_MON_TANG TO CafeManagerApp;
GO
/* ===== 2. Chuẩn hóa dữ liệu cũ ===== */
UPDATE dbo.KHUYENMAI SET TrangThai='DangChay' WHERE TrangThai='Nhap';
UPDATE dbo.KHUYENMAI SET DoiTuongKhach='ALL' WHERE DoiTuongKhach IN('FIRST','MEMBER');
-- Ngày: BD luôn năm 2000; KT năm 2001 khi ngày/tháng kết thúc nhỏ hơn bắt đầu (vắt năm).
UPDATE dbo.KHUYENMAI SET NgayBD=DATEFROMPARTS(2000,MONTH(NgayBD),DAY(NgayBD));
GO
UPDATE dbo.KHUYENMAI SET NgayKT=DATEFROMPARTS(
 CASE WHEN MONTH(NgayKT)*100+DAY(NgayKT)<MONTH(NgayBD)*100+DAY(NgayBD) THEN 2001 ELSE 2000 END,
 MONTH(NgayKT),DAY(NgayKT))
WHERE NgayKT IS NOT NULL;
GO
/* ===== 3. Hàm ngày/tháng và đối tượng ===== */
CREATE FUNCTION dbo.fn_NgayTrongKy(@Day date,@Start date,@End date) RETURNS bit
AS
BEGIN
 IF @Day IS NULL OR @Start IS NULL RETURN 0;
 DECLARE @d int=MONTH(@Day)*100+DAY(@Day),@s int=MONTH(@Start)*100+DAY(@Start);
 IF @End IS NULL RETURN CONVERT(bit,CASE WHEN @d>=@s THEN 1 ELSE 0 END);
 DECLARE @e int=MONTH(@End)*100+DAY(@End);
 RETURN CONVERT(bit,CASE WHEN @s<=@e THEN CASE WHEN @d BETWEEN @s AND @e THEN 1 ELSE 0 END
  ELSE CASE WHEN @d>=@s OR @d<=@e THEN 1 ELSE 0 END END);
END;
GO
CREATE FUNCTION dbo.fn_NgaySauKy(@Day date,@Start date,@End date) RETURNS bit
AS
BEGIN
 IF @Day IS NULL OR @Start IS NULL OR @End IS NULL RETURN 0;
 DECLARE @d int=MONTH(@Day)*100+DAY(@Day),@s int=MONTH(@Start)*100+DAY(@Start),
  @e int=MONTH(@End)*100+DAY(@End);
 RETURN CONVERT(bit,CASE WHEN @s<=@e THEN CASE WHEN @d>@e THEN 1 ELSE 0 END
  ELSE CASE WHEN @d>@e AND @d<@s THEN 1 ELSE 0 END END);
END;
GO
CREATE FUNCTION dbo.fn_KhachThoaDoiTuong(@Audience varchar(10),@Customer char(5),@MaKM char(10)) RETURNS bit
AS
BEGIN
 IF @Audience='ALL' RETURN 1;
 IF @Audience='WALKIN' RETURN CONVERT(bit,CASE WHEN @Customer IS NULL THEN 1 ELSE 0 END);
 IF @Audience<>'TIER' OR @Customer IS NULL RETURN 0;
 IF NOT EXISTS(SELECT 1 FROM dbo.KHUYENMAI_HANG WHERE MaKM=@MaKM) RETURN 1;
 RETURN CONVERT(bit,CASE WHEN EXISTS(SELECT 1 FROM dbo.KHUYENMAI_HANG x
  JOIN dbo.KHACHHANG c ON c.MaKH=@Customer WHERE x.MaKM=@MaKM AND x.MaHang=c.MaHang) THEN 1 ELSE 0 END);
END;
GO
CREATE FUNCTION dbo.fn_QuaTangApDung(@BusinessDate date,@Customer char(5)) RETURNS TABLE AS RETURN
(
 SELECT k.MaKM,g.MaSP,g.SoLuong,k.TenKM
 FROM dbo.KHUYENMAI k
 JOIN dbo.KHUYENMAI_MON_TANG g ON g.MaKM=k.MaKM
 JOIN dbo.SANPHAM s ON s.MaSP=g.MaSP
 WHERE k.LoaiUuDai='Tang' AND k.TrangThai='DangChay'
  AND dbo.fn_NgayTrongKy(@BusinessDate,k.NgayBD,k.NgayKT)=1
  AND dbo.fn_KhachThoaDoiTuong(k.DoiTuongKhach,@Customer,k.MaKM)=1
  AND s.TrangThai=N'Dang ban'
  AND EXISTS(SELECT 1 FROM dbo.CONGTHUC c WHERE c.MaSP=g.MaSP)
);
GO
CREATE FUNCTION dbo.fn_HoaDonQuaTang(@Code char(10))
RETURNS @result TABLE(MaKM char(10) NOT NULL,MaSP char(5) NOT NULL,SoLuong int NOT NULL,TenKM nvarchar(100) NOT NULL)
AS
BEGIN
 DECLARE @status nvarchar(30),@customer char(5),@date date;
 SELECT @status=h.TrangThai,@customer=h.MaKH,@date=CONVERT(date,h.NgayLap) FROM dbo.HOADON h WHERE h.MaHD=@Code;
 IF @@ROWCOUNT=0 RETURN;
 IF @status IN(N'Chua thanh toan',N'Cho thanh toan')
  INSERT @result SELECT MaKM,MaSP,SoLuong,TenKM FROM dbo.fn_QuaTangApDung(@date,@customer);
 ELSE
  INSERT @result SELECT d.MaKMQuaTang,d.MaSP,d.SoLuong,k.TenKM
   FROM dbo.CHITIET_HD d JOIN dbo.KHUYENMAI k ON k.MaKM=d.MaKMQuaTang
   WHERE d.MaHD=@Code AND d.MaKMQuaTang IS NOT NULL;
 RETURN;
END;
GO
CREATE FUNCTION dbo.fn_HoaDonQuaTangDigest(@Code char(10)) RETURNS TABLE AS RETURN
(
 SELECT CONVERT(binary(32),HASHBYTES('SHA2_256',CONVERT(varbinary(max),COALESCE((
  SELECT STRING_AGG(CONVERT(nvarchar(60),MaKM)+':'+MaSP+':'+CONVERT(nvarchar(10),SoLuong),N',')
   WITHIN GROUP(ORDER BY MaKM,MaSP) FROM dbo.fn_HoaDonQuaTang(@Code)),N'')))) Digest
);
GO
/* ===== 4. Hàm ưu đãi 039 theo ngày/tháng và đối tượng mới ===== */
ALTER FUNCTION dbo.fn_UuDaiCap(@BusinessDate date,@Customer char(5),@LegacyPromotion char(10),@LinesJson nvarchar(max))
RETURNS @result TABLE(MaKM char(10) NOT NULL,[Percent] int NOT NULL,No int NOT NULL,Amount decimal(18,0) NOT NULL)
AS
BEGIN
 IF @LinesJson IS NULL OR LTRIM(RTRIM(@LinesJson))='' SET @LinesJson=N'[]';
 DECLARE @line TABLE(No int IDENTITY(1,1) NOT NULL,MaSP char(5) NULL,Amount decimal(18,0) NOT NULL);
 INSERT @line(MaSP,Amount)
 SELECT TRY_CONVERT(char(5),JSON_VALUE(v.value,'$.Product')),CONVERT(decimal(18,0),JSON_VALUE(v.value,'$.Amount'))
 FROM OPENJSON(@LinesJson) v WHERE JSON_VALUE(v.value,'$.Amount') IS NOT NULL;
 DECLARE @promo TABLE(MaKM char(10) NOT NULL PRIMARY KEY,[Percent] int NOT NULL,IsItem bit NOT NULL);
 INSERT @promo
 SELECT k.MaKM,k.PhanTramGiam,CONVERT(bit,CASE WHEN k.PhamVi='ITEM' THEN 1 ELSE 0 END)
 FROM dbo.KHUYENMAI k
 WHERE k.LoaiUuDai='Giam' AND k.MaKM<>COALESCE(@LegacyPromotion,'') AND k.TrangThai='DangChay'
  AND dbo.fn_NgayTrongKy(@BusinessDate,k.NgayBD,k.NgayKT)=1
  AND dbo.fn_KhachThoaDoiTuong(k.DoiTuongKhach,@Customer,k.MaKM)=1;
 IF @LegacyPromotion IS NOT NULL AND NOT EXISTS(SELECT 1 FROM @promo WHERE MaKM=@LegacyPromotion)
  INSERT @promo SELECT k.MaKM,k.PhanTramGiam,0 FROM dbo.KHUYENMAI k
  WHERE k.MaKM=@LegacyPromotion AND dbo.fn_NgayTrongKy(@BusinessDate,k.NgayBD,k.NgayKT)=1;
 INSERT @result(MaKM,[Percent],No,Amount)
 SELECT p.MaKM,p.[Percent],l.No,l.Amount
 FROM @promo p JOIN @line l ON p.IsItem=0
   OR (l.MaSP IS NOT NULL AND EXISTS(SELECT 1 FROM dbo.KHUYENMAI_MON m WHERE m.MaKM=p.MaKM AND m.MaSP=l.MaSP))
 WHERE p.[Percent]>0;
 RETURN;
END;
GO
/* ===== 5. Catalog digest gồm nội dung quà ===== */
ALTER FUNCTION dbo.fn_PromotionCatalogDigest() RETURNS TABLE AS RETURN
(
 SELECT CONVERT(binary(32),HASHBYTES('SHA2_256',CONVERT(varbinary(max),
  COALESCE((SELECT STRING_AGG(CONVERT(nvarchar(60),k.MaKM)+':'+CONVERT(nvarchar(20),CONVERT(bigint,k.Revision)),N',')
   WITHIN GROUP(ORDER BY k.MaKM) FROM dbo.KHUYENMAI k),N'')+N'|'+
  COALESCE((SELECT STRING_AGG(CONVERT(nvarchar(60),g.MaKM)+':'+g.MaSP+':'+CONVERT(nvarchar(10),g.SoLuong),N',')
   WITHIN GROUP(ORDER BY g.MaKM,g.MaSP) FROM dbo.KHUYENMAI_MON_TANG g),N'')))) Digest
);
GO
/* ===== 6. Preview tạm tính: thêm quà ===== */
ALTER PROCEDURE dbo.sp_DiscountPreview @Actor int,@Stamp binary(8),@Money decimal(38,18),@Ship decimal(38,18),
 @BusinessDate date,@Customer varchar(max)=NULL,@Lines dbo.PreviewLines READONLY AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 BEGIN TRY
  BEGIN TRAN; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,CASH';
  DECLARE @lock int; EXEC @lock=sys.sp_getapplock @Resource=N'CafeManager:LoyaltyPolicy',@LockMode='Shared',@LockOwner='Transaction',@LockTimeout=10000;
  IF @lock<0 THROW 52004,N'Chính sách đang thay đổi.',1;
  IF @Customer IS NOT NULL AND DATALENGTH(@Customer)>5 THROW 52001,N'Mã khách không hợp lệ.',1;
  IF @Ship IS NULL OR @Ship<0 OR @Ship>999999999999999999 OR ROUND(@Ship,0)<>@Ship OR @BusinessDate IS NULL THROW 52001,N'Phí ship/ngày tạm tính không hợp lệ.',1;
  IF @Customer IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.KHACHHANG WITH(HOLDLOCK) WHERE MaKH=@Customer) THROW 52001,N'Khách không tồn tại.',1;
  DECLARE @json nvarchar(max),@total decimal(18,0);
  IF NOT EXISTS(SELECT 1 FROM @Lines)
  BEGIN
   IF @Money IS NULL OR @Money<0 OR @Money>999999999999999999 OR ROUND(@Money,0)<>@Money THROW 52001,N'Tiền tạm tính không hợp lệ.',1;
   SET @total=CONVERT(decimal(18,0),@Money);
   SET @json=(SELECT CONVERT(char(5),NULL) Product,@total Amount FOR JSON PATH,INCLUDE_NULL_VALUES);
  END
  ELSE
  BEGIN
   IF EXISTS(SELECT 1 FROM @Lines WHERE SoLuong NOT BETWEEN 1 AND 2147483647) THROW 52001,N'Số lượng dòng không hợp lệ.',1;
   IF EXISTS(SELECT 1 FROM @Lines l WHERE NOT EXISTS(SELECT 1 FROM dbo.SANPHAM s WITH(HOLDLOCK) WHERE s.MaSP=l.MaSP)) THROW 52005,N'Món không tồn tại.',1;
   SET @json=(SELECT l.MaSP Product,CONVERT(decimal(18,0),s.GiaBan)*l.SoLuong Amount
    FROM @Lines l JOIN dbo.SANPHAM s ON s.MaSP=l.MaSP ORDER BY l.MaSP,l.SoLuong FOR JSON PATH,INCLUDE_NULL_VALUES);
   SET @total=(SELECT SUM(CONVERT(decimal(18,0),s.GiaBan)*l.SoLuong) FROM @Lines l JOIN dbo.SANPHAM s ON s.MaSP=l.MaSP);
   IF @total>999999999999999999 THROW 52001,N'Tổng tiền vượt giới hạn.',1;
  END;
  DECLARE @tier int,@pp int,@td decimal(18,0),@pd decimal(18,0),@discount decimal(18,0);
  SELECT @tier=TierPercent,@pp=PromotionPercent,@td=TierDiscount,@pd=PromotionDiscount,@discount=Discount
  FROM dbo.fn_UuDaiTong(@BusinessDate,CONVERT(char(5),@Customer),NULL,@json);
  DECLARE @count int=(SELECT COUNT(*) FROM dbo.fn_UuDaiChia(@BusinessDate,CONVERT(char(5),@Customer),NULL,@json));
  IF @total+@Ship-@discount>999999999999999999 THROW 52001,N'Phải trả vượt giới hạn tiền.',1;
  SELECT CASE WHEN @tier>0 AND @count>0 THEN 'TierAndPromotion' WHEN @count>0 THEN 'Promotion'
   WHEN @tier>0 THEN 'Tier' ELSE 'None' END Source,
   k.MaHang Tier,CONVERT(char(10),NULL) Promotion,
   CASE WHEN @tier+@pp>100 THEN 100 ELSE @tier+@pp END PercentDiscount,
   @tier TierPercent,@pp PromotionPercent,@td TierDiscount,@pd PromotionDiscount,@count PromotionCount,
   @discount Discount,CONVERT(decimal(18,0),@total+@Ship-@discount) Payable,
   SYSDATETIME() ReadAt,s.Revision PolicyRevision,
   (SELECT COUNT(*) FROM dbo.fn_QuaTangApDung(@BusinessDate,CONVERT(char(5),@Customer))) GiftCount
  FROM dbo.LoyaltyPolicyState s LEFT JOIN dbo.KHACHHANG k ON k.MaKH=@Customer;
  SELECT c.MaKM Code,k.TenKM Name,c.[Percent],c.BaseAmount,c.Discount
  FROM dbo.fn_UuDaiChia(@BusinessDate,CONVERT(char(5),@Customer),NULL,@json) c JOIN dbo.KHUYENMAI k ON k.MaKM=c.MaKM
  ORDER BY c.MaKM;
  SELECT q.MaKM Code,q.TenKM Name,q.MaSP Product,s.TenSP ProductName,q.SoLuong Quantity
  FROM dbo.fn_QuaTangApDung(@BusinessDate,CONVERT(char(5),@Customer)) q JOIN dbo.SANPHAM s ON s.MaSP=q.MaSP
  ORDER BY q.MaKM,q.MaSP;
  COMMIT;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
/* ===== 7. Danh mục ưu đãi ===== */
ALTER PROCEDURE dbo.sp_PromotionList @Actor int,@Stamp binary(8),@Search nvarchar(max)=N'',@Skip int=0,@OnDate date=NULL AS
BEGIN
 SET NOCOUNT ON; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,CASH';
 IF LEN(@Search)>100 OR @Skip<0 THROW 52001,N'Bộ lọc không hợp lệ.',1;
 IF EXISTS(SELECT 1 FROM dbo.TAIKHOAN WHERE MaTK=@Actor AND MaVT='CASH')
 BEGIN
  SET @OnDate=COALESCE(@OnDate,CONVERT(date,SYSDATETIME()));
  SET @Search=REPLACE(REPLACE(REPLACE(REPLACE(@Search,N'~',N'~~'),N'%',N'~%'),N'_',N'~_'),N'[',N'~[');
  SELECT k.MaKM Code,k.TenKM Name,k.NgayBD StartDate,k.NgayKT EndDate,k.PhanTramGiam Discount,
   CASE WHEN dbo.fn_NgaySauKy(@OnDate,k.NgayBD,k.NgayKT)=1 THEN 'HetHan' ELSE k.TrangThai END TrangThai,
   k.DoiTuongKhach Audience,k.PhamVi Scope,
   (SELECT COUNT(*) FROM dbo.KHUYENMAI_MON m WHERE m.MaKM=k.MaKM) ItemCount,k.Revision,
   k.LoaiUuDai Kind,(SELECT COUNT(*) FROM dbo.KHUYENMAI_MON_TANG g WHERE g.MaKM=k.MaKM) GiftItemCount,
   (SELECT COALESCE(SUM(g.SoLuong),0) FROM dbo.KHUYENMAI_MON_TANG g WHERE g.MaKM=k.MaKM) GiftQuantity
  FROM dbo.KHUYENMAI k
  WHERE (k.TenKM LIKE N'%'+@Search+N'%' ESCAPE N'~' OR k.MaKM LIKE N'%'+@Search+N'%' ESCAPE N'~')
   AND k.TrangThai='DangChay'
   AND dbo.fn_NgayTrongKy(@OnDate,k.NgayBD,k.NgayKT)=1
  ORDER BY k.MaKM OFFSET @Skip ROWS FETCH NEXT 50 ROWS ONLY;
  RETURN;
 END;
 SET @Search=REPLACE(REPLACE(REPLACE(REPLACE(@Search,N'~',N'~~'),N'%',N'~%'),N'_',N'~_'),N'[',N'~[');
 SELECT k.MaKM Code,k.TenKM Name,k.NgayBD StartDate,k.NgayKT EndDate,k.PhanTramGiam Discount,
  CASE WHEN k.TrangThai='DangChay' AND dbo.fn_NgaySauKy(CONVERT(date,SYSDATETIME()),k.NgayBD,k.NgayKT)=1
   THEN 'HetHan' ELSE k.TrangThai END TrangThai,
  k.DoiTuongKhach Audience,k.PhamVi Scope,
  (SELECT COUNT(*) FROM dbo.KHUYENMAI_MON m WHERE m.MaKM=k.MaKM) ItemCount,k.Revision,
  k.LoaiUuDai Kind,(SELECT COUNT(*) FROM dbo.KHUYENMAI_MON_TANG g WHERE g.MaKM=k.MaKM) GiftItemCount,
  (SELECT COALESCE(SUM(g.SoLuong),0) FROM dbo.KHUYENMAI_MON_TANG g WHERE g.MaKM=k.MaKM) GiftQuantity
 FROM dbo.KHUYENMAI k
 WHERE (k.TenKM LIKE N'%'+@Search+N'%' ESCAPE N'~' OR k.MaKM LIKE N'%'+@Search+N'%' ESCAPE N'~')
  AND (@OnDate IS NULL OR dbo.fn_NgayTrongKy(@OnDate,k.NgayBD,k.NgayKT)=1)
 ORDER BY k.MaKM OFFSET @Skip ROWS FETCH NEXT 50 ROWS ONLY;
END;
GO
ALTER PROCEDURE dbo.sp_PromotionGet @Actor int,@Stamp binary(8),@Code varchar(max) AS
BEGIN
 SET NOCOUNT ON; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,CASH';
 SELECT k.MaKM Code,k.TenKM Name,k.NgayBD StartDate,k.NgayKT EndDate,k.PhanTramGiam Discount,
  k.TrangThai,k.DoiTuongKhach Audience,k.PhamVi Scope,
  (SELECT COUNT(*) FROM dbo.KHUYENMAI_MON m WHERE m.MaKM=k.MaKM) ItemCount,k.Revision,
  k.LoaiUuDai Kind,(SELECT COUNT(*) FROM dbo.KHUYENMAI_MON_TANG g WHERE g.MaKM=k.MaKM) GiftItemCount,
  (SELECT COALESCE(SUM(g.SoLuong),0) FROM dbo.KHUYENMAI_MON_TANG g WHERE g.MaKM=k.MaKM) GiftQuantity
 FROM dbo.KHUYENMAI k WHERE k.MaKM=@Code;
 SELECT h.MaHang,h.DiemToiThieu,h.PhanTramGiam FROM dbo.KHUYENMAI_HANG x
  JOIN dbo.HANGTHANHVIEN h ON h.MaHang=x.MaHang WHERE x.MaKM=@Code ORDER BY h.DiemToiThieu;
 SELECT m.MaSP,s.TenSP,s.GiaBan,s.TrangThai FROM dbo.KHUYENMAI_MON m
  JOIN dbo.SANPHAM s ON s.MaSP=m.MaSP WHERE m.MaKM=@Code ORDER BY m.MaSP;
 SELECT g.MaSP,s.TenSP,s.GiaBan,s.TrangThai,g.SoLuong FROM dbo.KHUYENMAI_MON_TANG g
  JOIN dbo.SANPHAM s ON s.MaSP=g.MaSP WHERE g.MaKM=@Code ORDER BY g.MaSP;
END;
GO
/* ===== 8. Ghi ưu đãi ===== */
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
  IF @Code IS NULL OR DATALENGTH(@Code)<>10 OR @Code COLLATE Latin1_General_100_BIN2 NOT LIKE 'KM[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'
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
GO
ALTER PROCEDURE dbo.sp_PromotionCreate @Actor int,@Stamp binary(8),@Code varchar(max),@RequestId uniqueidentifier,
 @ExpectedRevision binary(8)=NULL,@Name nvarchar(max)=NULL,@StartDate date=NULL,@EndDate date=NULL,@Discount bigint=NULL,
 @Audience varchar(10)=NULL,@Status varchar(10)=NULL,@TiersJson nvarchar(max)=NULL,@ItemsJson nvarchar(max)=NULL,
 @Kind varchar(10)=NULL,@GiftJson nvarchar(max)=NULL AS
BEGIN SET NOCOUNT ON; EXEC dbo.sp_PromotionWrite @Actor,@Stamp,'Create',@Code,@RequestId,@ExpectedRevision,@Name,@StartDate,@EndDate,@Discount,@Audience,@Status,@TiersJson,@ItemsJson,@Kind,@GiftJson; END;
GO
ALTER PROCEDURE dbo.sp_PromotionUpdate @Actor int,@Stamp binary(8),@Code varchar(max),@RequestId uniqueidentifier,
 @ExpectedRevision binary(8)=NULL,@Name nvarchar(max)=NULL,@StartDate date=NULL,@EndDate date=NULL,@Discount bigint=NULL,
 @Audience varchar(10)=NULL,@Status varchar(10)=NULL,@TiersJson nvarchar(max)=NULL,@ItemsJson nvarchar(max)=NULL,
 @Kind varchar(10)=NULL,@GiftJson nvarchar(max)=NULL AS
BEGIN SET NOCOUNT ON; EXEC dbo.sp_PromotionWrite @Actor,@Stamp,'Update',@Code,@RequestId,@ExpectedRevision,@Name,@StartDate,@EndDate,@Discount,@Audience,@Status,@TiersJson,@ItemsJson,@Kind,@GiftJson; END;
GO
ALTER PROCEDURE dbo.sp_PromotionDelete @Actor int,@Stamp binary(8),@Code varchar(max),@RequestId uniqueidentifier,
 @ExpectedRevision binary(8)=NULL,@Name nvarchar(max)=NULL,@StartDate date=NULL,@EndDate date=NULL,@Discount bigint=NULL,
 @Audience varchar(10)=NULL,@Status varchar(10)=NULL,@TiersJson nvarchar(max)=NULL,@ItemsJson nvarchar(max)=NULL,
 @Kind varchar(10)=NULL,@GiftJson nvarchar(max)=NULL AS
BEGIN SET NOCOUNT ON; EXEC dbo.sp_PromotionWrite @Actor,@Stamp,'Delete',@Code,@RequestId,@ExpectedRevision,@Name,@StartDate,@EndDate,@Discount,@Audience,@Status,@TiersJson,@ItemsJson,@Kind,@GiftJson; END;
GO
/* ===== 9. Checkout preview gồm digest quà ===== */
ALTER FUNCTION dbo.fn_CheckoutPreview(@Code char(10)) RETURNS TABLE AS RETURN
(
 SELECT CASE WHEN k.MaHang IS NOT NULL AND c.PromotionCount>0 THEN 'TierAndPromotion'
   WHEN c.PromotionCount>0 THEN 'Promotion' WHEN k.MaHang IS NOT NULL THEN 'Tier' ELSE 'None' END Source,
  k.MaHang Tier,m.MaKM Promotion,
  CASE WHEN COALESCE(d.TierPercent,0)+COALESCE(d.PromotionPercent,0)>100 THEN 100
   ELSE COALESCE(d.TierPercent,0)+COALESCE(d.PromotionPercent,0) END PercentDiscount,
  d.TierPercent,d.PromotionPercent,d.TierDiscount,d.PromotionDiscount,d.Discount,
  CONVERT(decimal(38,0),h.TongTien+h.PhiShip-d.Discount) Payable,c.PromotionCount,
  COALESCE(pt.ChietKhau,CONVERT(decimal(5,2),0)) PartnerPercent,
  CONVERT(decimal(18,0),ROUND(CONVERT(decimal(28,4),h.TongTien-d.Discount)*COALESCE(pt.ChietKhau,0)/100,0)) PartnerFee,
  CONVERT(bigint,FLOOR(CONVERT(decimal(28,4),h.TongTien-d.Discount)/1000)) Points,
  s.Revision PolicyRevision,k.Revision CustomerRevision,m.Revision PromotionRevision,
  CONVERT(binary(32),HASHBYTES('SHA2_256',
   CONVERT(varbinary(max),h.Revision)+s.Revision+COALESCE(k.Revision,0x0000000000000000)
   +COALESCE(m.Revision,0x0000000000000000)+COALESCE(pt.CatalogRevision,0x0000000000000000)
   +CONVERT(binary(8),CONVERT(bigint,d.Discount))+CONVERT(binary(8),CONVERT(bigint,h.TongTien+h.PhiShip-d.Discount))
   +CONVERT(binary(4),COALESCE(d.TierPercent,0))+CONVERT(binary(4),COALESCE(d.PromotionPercent,0))
   +CONVERT(binary(4),c.PromotionCount)+cd.Digest+ad.Digest+qd.Digest)) PreviewToken,
  g.GiftCount
 FROM dbo.HOADON h CROSS JOIN dbo.LoyaltyPolicyState s
 LEFT JOIN dbo.KHACHHANG k ON k.MaKH=h.MaKH LEFT JOIN dbo.KHUYENMAI m ON m.MaKM=h.MaKM
 LEFT JOIN dbo.DOITACGIAOHANG pt ON pt.MaDT=h.MaDT
 CROSS APPLY dbo.fn_HoaDonGiamGia(h.MaHD) d
 CROSS APPLY(SELECT (SELECT COUNT(*) FROM dbo.fn_HoaDonUuDaiApDung(h.MaHD)) PromotionCount) c
 CROSS APPLY(SELECT (SELECT COUNT(*) FROM dbo.fn_HoaDonQuaTang(h.MaHD)) GiftCount) g
 CROSS APPLY dbo.fn_PromotionCatalogDigest() cd CROSS APPLY dbo.fn_HoaDonUuDaiDigest(h.MaHD) ad
 CROSS APPLY dbo.fn_HoaDonQuaTangDigest(h.MaHD) qd
 WHERE h.MaHD=@Code
);
GO
ALTER PROCEDURE dbo.sp_SalesCheckoutPreview @Actor int,@Stamp binary(8),@Code varchar(max),@ExpectedRevision binary(8) AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 BEGIN TRY
  BEGIN TRAN; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,CASH'; EXEC dbo.sp_SalesLock N'CafeManager:LoyaltyPolicy','Shared';
  DECLARE @resource nvarchar(255)=N'CafeManager:SalesOrder:'+CONVERT(nvarchar(10),@Code); EXEC dbo.sp_SalesLock @resource,'Shared';
  EXEC dbo.sp_SalesCheckPayable @Code,@ExpectedRevision;
  IF EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD=@Code AND TongTien<=0) THROW 51102,N'Hóa đơn phải có món.',1;
  SELECT h.MaHD Code,h.Revision,h.TongTien Total,h.PhiShip Ship,p.Discount,p.Payable,p.Source,p.PercentDiscount,
   p.TierPercent,p.PromotionPercent,p.TierDiscount,p.PromotionDiscount,p.PromotionCount,p.PartnerPercent,p.PartnerFee,
   p.Points,p.PreviewToken,p.GiftCount
  FROM dbo.HOADON h CROSS APPLY dbo.fn_CheckoutPreview(h.MaHD) p WHERE h.MaHD=@Code;
  SELECT MaKM Code,Name,[Percent],BaseAmount,Discount FROM dbo.fn_HoaDonUuDaiApDung(@Code) ORDER BY MaKM;
  SELECT q.MaKM Code,q.TenKM Name,q.MaSP Product,s.TenSP ProductName,q.SoLuong Quantity
  FROM dbo.fn_HoaDonQuaTang(@Code) q JOIN dbo.SANPHAM s ON s.MaSP=q.MaSP ORDER BY q.MaKM,q.MaSP;
  COMMIT;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
/* ===== 10. Thanh toán: ghi dòng quà trước khi tính tiêu hao ===== */
ALTER PROCEDURE dbo.sp_ThanhToanHoaDon @Actor int,@Stamp binary(8),@Code varchar(max),@ExpectedRevision binary(8),
 @RequestId uniqueidentifier,@ExpectedPreviewToken binary(32),@ThanhToan dbo.DanhSachThanhToan READONLY AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 52101,N'Không gọi ghi trong transaction bên ngoài.',1;
 BEGIN TRY
  BEGIN TRAN;
  DECLARE @payments nvarchar(max)=(SELECT MaPT,SoTien,MaThamChieu FROM @ThanhToan ORDER BY MaPT,MaThamChieu,SoTien
   FOR JSON PATH,INCLUDE_NULL_VALUES);
  DECLARE @payload nvarchar(max)=(SELECT @Code c,@ExpectedRevision r,@ExpectedPreviewToken p,@payments a
   FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES),@replay bit;
  EXEC dbo.sp_SalesBegin @Actor,@Stamp,@RequestId,'SalesCheckout',@Code,@payload,@replay OUTPUT;
  IF @replay=0
  BEGIN
   EXEC dbo.sp_SalesLock N'CafeManager:LoyaltyPolicy','Shared'; EXEC dbo.sp_SalesLock N'CafeManager:Recipes','Shared';
   DECLARE @resource nvarchar(255)=N'CafeManager:SalesOrder:'+CONVERT(nvarchar(10),@Code); EXEC dbo.sp_SalesLock @resource;
   EXEC dbo.sp_SalesCheckPayable @Code,@ExpectedRevision;
   DECLARE @customer char(5),@total decimal(18,0),@date datetime2(0),@created datetime2(0),@channel nvarchar(20),
    @recipient nvarchar(100),@phone varchar(15),@address nvarchar(250);
   SELECT @customer=MaKH,@total=TongTien,@created=NgayLap,@channel=LoaiDon,@recipient=TenNguoiNhan,
    @phone=SDTNguoiNhan,@address=DiaChiGiao FROM dbo.HOADON WHERE MaHD=@Code;
   SET @date=SYSDATETIME();
   IF @date<@created THROW 52205,N'Ngày thanh toán trước lập đơn.',1;
   IF @total<=0 OR NOT EXISTS(SELECT 1 FROM dbo.CHITIET_HD WHERE MaHD=@Code) THROW 51102,N'Hóa đơn phải có món.',1;
   IF @channel=N'Giao hang' AND (NULLIF(LTRIM(RTRIM(@recipient)),N'') IS NULL OR NULLIF(@phone,'') IS NULL
     OR @phone COLLATE Latin1_General_100_BIN2 LIKE '%[^0-9]%' OR NULLIF(LTRIM(RTRIM(@address)),N'') IS NULL)
    THROW 52101,N'Thiếu thông tin giao.',1;
   DECLARE @balance int; SELECT @balance=DiemTichLuy FROM dbo.KHACHHANG WITH(UPDLOCK,HOLDLOCK) WHERE MaKH=@customer;
   -- Giữ khóa chính sách/đối tác/phương thức tới commit, kể cả khi catalog đổi đồng thời.
   DECLARE @partnerRevision binary(8),@lockedMethod char(5);
   SELECT @partnerRevision=dtt.CatalogRevision FROM dbo.DOITACGIAOHANG dtt WITH(HOLDLOCK)
    JOIN dbo.HOADON h ON h.MaDT=dtt.MaDT WHERE h.MaHD=@Code;
   DECLARE @token binary(32),@discount decimal(18,0),@payable decimal(38,0),@percent int,@tierPercent int,
    @promotionPercent int,@tierDiscount decimal(18,0),@promotionDiscount decimal(18,0),@promoCount int,
    @source varchar(20),@tier char(5),@promotion char(10),@partnerPercent decimal(5,2),@partnerFee decimal(18,0),
    @points bigint,@policyRevision binary(8),@customerRevision binary(8),@promotionRevision binary(8);
   SELECT @token=PreviewToken,@discount=Discount,@payable=Payable,@percent=PercentDiscount,@tierPercent=TierPercent,
    @promotionPercent=PromotionPercent,@tierDiscount=TierDiscount,@promotionDiscount=PromotionDiscount,
    @promoCount=PromotionCount,@source=Source,@tier=Tier,@promotion=Promotion,@partnerPercent=PartnerPercent,
    @partnerFee=PartnerFee,@points=Points,@policyRevision=PolicyRevision,@customerRevision=CustomerRevision,
    @promotionRevision=PromotionRevision FROM dbo.fn_CheckoutPreview(CONVERT(char(10),@Code));
   IF @ExpectedPreviewToken IS NULL OR @token<>@ExpectedPreviewToken THROW 52201,N'Preview đã thay đổi. Xác nhận lại số mới.',1;
   IF @payable>999999999999999999 OR @points>2147483647
    OR (@customer IS NOT NULL AND CONVERT(bigint,@balance)+@points>2147483647)
    THROW 52205,N'Tiền hoặc điểm vượt giới hạn.',1;
   SELECT @lockedMethod=p.MaPT FROM dbo.PHUONGTHUCTT p WITH(HOLDLOCK)
    JOIN dbo.PaymentMethodSupport s WITH(HOLDLOCK) ON s.MaPT=p.MaPT JOIN @ThanhToan t ON t.MaPT=p.MaPT;
   IF @payable<>COALESCE((SELECT SUM(CONVERT(decimal(38,0),SoTien)) FROM @ThanhToan),0)
    OR EXISTS(SELECT 1 FROM @ThanhToan t WHERE t.MaPT='PT010' OR NOT EXISTS(SELECT 1 FROM dbo.PHUONGTHUCTT p
     JOIN dbo.PaymentMethodSupport s ON s.MaPT=p.MaPT WHERE p.MaPT=t.MaPT AND s.IsSupported=1))
    THROW 52202,N'Phân bổ/phương thức không hợp lệ.',1;
   -- Ghi quà một lần/hóa đơn; dòng 0 ₫ vẫn vào công thức trừ kho và in hóa đơn.
   INSERT dbo.CHITIET_HD(MaHD,MaSP,SoLuong,GiaBan,GhiChu,MaKMQuaTang)
   SELECT @Code,q.MaSP,q.SoLuong,0,N'Tặng kèm',q.MaKM
   FROM dbo.fn_QuaTangApDung(CONVERT(date,@created),@customer) q;
   IF EXISTS(SELECT 1 FROM dbo.CHITIET_HD d JOIN dbo.SANPHAM s WITH(HOLDLOCK) ON s.MaSP=d.MaSP
     WHERE d.MaHD=@Code AND (s.TrangThai<>N'Dang ban' OR NOT EXISTS(SELECT 1 FROM dbo.CONGTHUC WHERE MaSP=d.MaSP)))
    THROW 52204,N'Món ngừng bán hoặc thiếu công thức.',1;
   DECLARE @consume TABLE(MaNVL char(5) PRIMARY KEY,Quantity decimal(38,4));
   INSERT @consume SELECT c.MaNVL,SUM(CONVERT(decimal(18,0),d.SoLuong)*c.DinhMuc) FROM dbo.CHITIET_HD d
    JOIN dbo.CONGTHUC c ON c.MaSP=d.MaSP WHERE d.MaHD=@Code GROUP BY c.MaNVL;
   IF EXISTS(SELECT 1 FROM @consume WHERE Quantity<=0 OR Quantity>99999999999999.9999) THROW 52205,N'Tiêu hao vượt giới hạn.',1;
   DECLARE @material char(5),@needed decimal(38,4),@available decimal(18,4);
   DECLARE stock CURSOR LOCAL FAST_FORWARD FOR SELECT MaNVL,Quantity FROM @consume ORDER BY MaNVL;
   OPEN stock; FETCH NEXT FROM stock INTO @material,@needed;
   WHILE @@FETCH_STATUS=0
   BEGIN
    SELECT @available=SoLuongTon FROM dbo.NGUYENVATLIEU WITH(UPDLOCK,HOLDLOCK) WHERE MaNVL=@material;
    IF @available IS NULL OR @available<@needed
     THROW 52203,N'Không đủ nguyên liệu. Đơn vẫn mở; kiểm tra tồn trước khi tiếp tục.',1;
    FETCH NEXT FROM stock INTO @material,@needed;
   END;
   CLOSE stock; DEALLOCATE stock;
   DECLARE @digest binary(32)=(SELECT Digest FROM dbo.fn_HoaDonUuDaiDigest(@Code));
   UPDATE dbo.HOADON SET GiamGia=@discount WHERE MaHD=@Code;
   INSERT dbo.HoaDonUuDaiSnapshot(MaHD,Kind,Source,TierCode,PromotionCode,PercentDiscount,TierPercent,PromotionPercent,
    TierDiscount,PromotionDiscount,PromotionCount,PromotionDigest,Discount,PolicyRevision,CustomerRevision,PromotionRevision)
   VALUES(@Code,'Checkout',@source,@tier,@promotion,@percent,@tierPercent,@promotionPercent,
    @tierDiscount,@promotionDiscount,@promoCount,@digest,@discount,@policyRevision,@customerRevision,@promotionRevision);
   INSERT dbo.HoaDonUuDaiApDung(MaHD,MaKM,PromotionRevision,Audience,Scope,[Percent],BaseAmount,Discount)
   SELECT @Code,MaKM,PromotionRevision,Audience,Scope,[Percent],BaseAmount,Discount FROM dbo.fn_HoaDonUuDaiApDung(@Code);
   INSERT dbo.TIEUHAO_HOADON(MaHD,MaNVL,SoLuong) SELECT @Code,MaNVL,CONVERT(decimal(18,4),Quantity) FROM @consume;
   INSERT dbo.CHITIET_THANHTOAN(MaHD,MaPT,SoTien,NgayTT,MaThamChieu)
   SELECT @Code,MaPT,SoTien,@date,MaThamChieu FROM @ThanhToan;
   UPDATE dbo.HOADON SET TrangThai=N'Da thanh toan',NgayThanhToan=@date,PhanTramPhiDoiTac=@partnerPercent,
    PhiDoiTac=@partnerFee WHERE MaHD=@Code;
   IF @customer IS NOT NULL AND @points>0
    INSERT dbo.LICHSU_DIEM(MaKH,MaHD,NgayGD,Loai,SoDiem,LyDo)
    VALUES(@customer,@Code,@date,N'Cong',CONVERT(int,@points),N'Tích điểm mua hàng');
   EXEC dbo.sp_SalesFinish @Actor,@RequestId,'SalesCheckout',@Code,@payload;
  END;
  COMMIT; SELECT EntityCode,ResultRevision FROM dbo.G03OperationReceipt WHERE RequestId=@RequestId;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
/* ===== 11. Đọc hóa đơn: dòng quà và quà dự kiến ===== */
ALTER PROCEDURE dbo.sp_InvoiceDetail @Actor int,@Stamp binary(8),@Code varchar(max) AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 BEGIN TRY
  BEGIN TRAN; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,CASH';
  DECLARE @resource nvarchar(255)=N'CafeManager:SalesOrder:'+CONVERT(nvarchar(10),@Code); EXEC dbo.sp_SalesLock @resource,'Shared';
  SELECT h.MaHD,h.NgayLap,h.MaNV,h.MaKH,h.MaBan,h.MaDT,h.MaKM,h.LoaiDon,h.TenNguoiNhan,h.SDTNguoiNhan,h.DiaChiGiao,
   h.TrangThaiGiao,h.NgayThanhToan,h.PhanTramPhiDoiTac,h.PhiDoiTac,h.TongTien,h.PhiShip,h.GiamGia,
   CONVERT(decimal(38,0),h.ThanhTien) Payable,h.TrangThai,h.Revision,CONVERT(bit,1) PreviewValid,
   s.Source PreviewSource,s.PercentDiscount PreviewPercent,SYSDATETIME() ReadAt,h.MaPhien,
   sessionRow.Revision SessionRevision,s.PromotionCount,s.TierPercent,s.PromotionPercent,s.TierDiscount,s.PromotionDiscount
  FROM dbo.HOADON h WITH(HOLDLOCK) LEFT JOIN dbo.HoaDonUuDaiSnapshot s ON s.MaHD=h.MaHD
  OUTER APPLY(SELECT TOP(1) p.Revision FROM dbo.PHIEN_BAN p WHERE p.MaPhien=h.MaPhien) sessionRow
  WHERE h.MaHD=@Code;
  SELECT d.MaDong,d.MaSP,s.TenSP,d.SoLuong,d.GiaBan,d.GhiChu,d.SoLuong*d.GiaBan Amount,d.MaKMQuaTang
  FROM dbo.CHITIET_HD d JOIN dbo.SANPHAM s ON s.MaSP=d.MaSP WHERE d.MaHD=@Code ORDER BY d.MaDong;
  SELECT t.MaTT,t.MaPT,p.TenPT,t.SoTien,t.MaThamChieu FROM dbo.CHITIET_THANHTOAN t
   JOIN dbo.PHUONGTHUCTT p ON p.MaPT=t.MaPT WHERE t.MaHD=@Code ORDER BY t.MaTT;
  SELECT MaKM Code,Name,[Percent],BaseAmount,Discount FROM dbo.fn_HoaDonUuDaiApDung(@Code) ORDER BY MaKM;
  SELECT s.Kind,s.Source,s.PercentDiscount,s.TierPercent,s.PromotionPercent,s.TierDiscount,s.PromotionDiscount,
   s.PromotionCount,COALESCE((SELECT SUM(SoDiem) FROM dbo.LICHSU_DIEM WHERE MaHD=@Code AND Loai=N'Cong'),0) Points,
   s.RecordedAt
  FROM dbo.HoaDonUuDaiSnapshot s WHERE s.MaHD=@Code;
  SELECT q.MaKM Code,q.TenKM Name,q.MaSP Product,s.TenSP ProductName,q.SoLuong Quantity
  FROM dbo.fn_HoaDonQuaTang(@Code) q JOIN dbo.SANPHAM s ON s.MaSP=q.MaSP
  WHERE EXISTS(SELECT 1 FROM dbo.HOADON h WHERE h.MaHD=@Code AND h.TrangThai IN(N'Chua thanh toan',N'Cho thanh toan'))
  ORDER BY q.MaKM,q.MaSP;
  COMMIT;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
ALTER PROCEDURE dbo.sp_SalesReadCore @Code varchar(max) AS
BEGIN
 SET NOCOUNT ON;
 SELECT h.MaHD,h.NgayLap,h.MaNV,h.MaKH,h.MaBan,h.MaDT,h.MaKM,h.LoaiDon,h.TenNguoiNhan,h.SDTNguoiNhan,h.DiaChiGiao,
  h.TrangThaiGiao,h.NgayThanhToan,h.PhanTramPhiDoiTac,h.PhiDoiTac,h.TongTien,h.PhiShip,
  d.Discount GiamGia,CONVERT(decimal(38,0),h.TongTien+h.PhiShip-d.Discount) Payable,h.TrangThai,h.Revision,v.Valid PreviewValid,
  CASE WHEN h.TrangThai IN(N'Chua thanh toan',N'Cho thanh toan')
   THEN CASE WHEN d.TierPercent>0 AND src.PromotionCount>0 THEN 'TierAndPromotion' WHEN src.PromotionCount>0 THEN 'Promotion'
    WHEN d.TierPercent>0 THEN 'Tier' ELSE 'None' END ELSE snapshot.Source END PreviewSource,
  CASE WHEN h.TrangThai IN(N'Chua thanh toan',N'Cho thanh toan')
   THEN CASE WHEN COALESCE(d.TierPercent,0)+COALESCE(d.PromotionPercent,0)>100 THEN 100
    ELSE COALESCE(d.TierPercent,0)+COALESCE(d.PromotionPercent,0) END ELSE snapshot.PercentDiscount END PreviewPercent,
  SYSDATETIME() ReadAt,h.MaPhien,sessionRow.Revision SessionRevision,
  CASE WHEN h.TrangThai IN(N'Chua thanh toan',N'Cho thanh toan') THEN src.PromotionCount ELSE snapshot.PromotionCount END PromotionCount
 FROM dbo.HOADON h WITH(HOLDLOCK) LEFT JOIN dbo.HoaDonUuDaiSnapshot snapshot ON snapshot.MaHD=h.MaHD
 CROSS APPLY dbo.fn_HoaDonGiamGia(h.MaHD) d
 CROSS APPLY(SELECT (SELECT COUNT(*) FROM dbo.fn_HoaDonUuDaiApDung(h.MaHD)) PromotionCount) src
 CROSS APPLY(SELECT CONVERT(bit,CASE WHEN h.MaKM IS NULL OR EXISTS(SELECT 1 FROM dbo.KHUYENMAI k
   WHERE k.MaKM=h.MaKM AND dbo.fn_NgayTrongKy(CONVERT(date,h.NgayLap),k.NgayBD,k.NgayKT)=1)
   THEN 1 ELSE 0 END) Valid) v
 OUTER APPLY(SELECT TOP(1) s.Revision FROM dbo.PHIEN_BAN s WHERE s.MaPhien=h.MaPhien) sessionRow
 WHERE h.MaHD=@Code;
 SELECT d.MaDong,d.MaSP,s.TenSP,d.SoLuong,d.GiaBan,d.GhiChu,d.SoLuong*d.GiaBan Amount,d.MaKMQuaTang
 FROM dbo.CHITIET_HD d JOIN dbo.SANPHAM s ON s.MaSP=d.MaSP WHERE d.MaHD=@Code ORDER BY d.MaDong;
 SELECT t.MaTT,t.MaPT,p.TenPT,t.SoTien,t.MaThamChieu FROM dbo.CHITIET_THANHTOAN t
 JOIN dbo.PHUONGTHUCTT p ON p.MaPT=t.MaPT WHERE t.MaHD=@Code ORDER BY t.MaTT;
 SELECT MaKM Code,Name,[Percent],BaseAmount,Discount FROM dbo.fn_HoaDonUuDaiApDung(@Code) ORDER BY MaKM;
END;
GO
/* ===== 12. Grant ===== */
GRANT EXECUTE ON dbo.sp_DiscountPreview TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_PromotionWrite TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_PromotionList TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_PromotionGet TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_PromotionCreate TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_PromotionUpdate TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_PromotionDelete TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_SalesCheckoutPreview TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_ThanhToanHoaDon TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_InvoiceDetail TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_SalesReadCore TO CafeManagerApp;
GO
