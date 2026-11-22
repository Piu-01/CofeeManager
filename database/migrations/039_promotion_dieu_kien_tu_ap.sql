-- Ưu đãi có điều kiện, tự áp dụng, nhiều ưu đãi trên một hóa đơn (chốt 21/11/2026).
-- Thay cơ chế chọn tay một chương trình ở 038: mọi ưu đãi đang chạy, còn hạn,
-- đúng đối tượng khách và đúng món đều được cộng dồn; giảm theo từng dòng món,
-- chặn 100%/dòng và GiamGia <= TongTien; ship không giảm.
-- Không backfill dữ liệu lịch sử; đơn mở cũ có MaKM tiếp tục tính như ưu đãi legacy.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF @@TRANCOUNT=0 OR NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=38)
 THROW 53307,N'Cần schema038 và runner migration.',1;
IF OBJECT_ID(N'dbo.KHUYENMAI',N'U') IS NULL OR OBJECT_ID(N'dbo.HoaDonUuDaiSnapshot',N'U') IS NULL
 OR OBJECT_ID(N'dbo.sp_PromotionWrite',N'P') IS NULL OR OBJECT_ID(N'dbo.sp_SalesReadCore',N'P') IS NULL
 OR OBJECT_ID(N'dbo.sp_SalesCheckoutPreview',N'P') IS NULL OR OBJECT_ID(N'dbo.sp_ThanhToanHoaDon',N'P') IS NULL
 OR OBJECT_ID(N'dbo.sp_InvoiceDetail',N'P') IS NULL OR OBJECT_ID(N'dbo.sp_DiscountPreview',N'P') IS NULL
 OR OBJECT_ID(N'dbo.fn_CheckoutPreview',N'IF') IS NULL
 THROW 53307,N'Thiếu object ưu đãi/bán hàng của 038; cần đối chiếu trước nâng cấp.',1;
GO
/* ===== 1. DDL ===== */
-- Mỗi ALTER TABLE ở batch riêng: SQL Server biên dịch cả batch trước khi chạy,
-- nên cột mới thêm chưa thấy được cho constraint/ALTER cùng batch.
ALTER TABLE dbo.KHUYENMAI ADD TrangThai varchar(10) NOT NULL CONSTRAINT DF_KHUYENMAI_TrangThai DEFAULT 'DangChay',
 DoiTuongKhach varchar(10) NOT NULL CONSTRAINT DF_KHUYENMAI_DoiTuong DEFAULT 'ALL',
 PhamVi varchar(4) NOT NULL CONSTRAINT DF_KHUYENMAI_PhamVi DEFAULT 'ALL';
GO
ALTER TABLE dbo.KHUYENMAI ADD CONSTRAINT CK_KHUYENMAI_TrangThai CHECK(TrangThai IN('Nhap','DangChay','TamDung'));
ALTER TABLE dbo.KHUYENMAI ADD CONSTRAINT CK_KHUYENMAI_DoiTuong CHECK(DoiTuongKhach IN('ALL','FIRST','MEMBER','TIER'));
ALTER TABLE dbo.KHUYENMAI ADD CONSTRAINT CK_KHUYENMAI_PhamVi CHECK(PhamVi IN('ALL','ITEM'));
GO
ALTER TABLE dbo.KHUYENMAI DROP CONSTRAINT CHK_KM_NgayHopLe;
GO
ALTER TABLE dbo.KHUYENMAI ALTER COLUMN NgayKT date NULL;
GO
ALTER TABLE dbo.KHUYENMAI ADD CONSTRAINT CK_KHUYENMAI_NgayHopLe CHECK(NgayKT IS NULL OR NgayKT>=NgayBD);
GO
CREATE TABLE dbo.KHUYENMAI_HANG(
 MaKM char(10) NOT NULL CONSTRAINT FK_KM_HANG_KM REFERENCES dbo.KHUYENMAI(MaKM) ON DELETE CASCADE,
 MaHang char(5) NOT NULL CONSTRAINT FK_KM_HANG_HANG REFERENCES dbo.HANGTHANHVIEN(MaHang),
 CONSTRAINT PK_KHUYENMAI_HANG PRIMARY KEY(MaKM,MaHang));
CREATE TABLE dbo.KHUYENMAI_MON(
 MaKM char(10) NOT NULL CONSTRAINT FK_KM_MON_KM REFERENCES dbo.KHUYENMAI(MaKM) ON DELETE CASCADE,
 MaSP char(5) NOT NULL CONSTRAINT FK_KM_MON_SP REFERENCES dbo.SANPHAM(MaSP),
 CONSTRAINT PK_KHUYENMAI_MON PRIMARY KEY(MaKM,MaSP));
CREATE TABLE dbo.HoaDonUuDaiApDung(
 MaHD char(10) NOT NULL CONSTRAINT FK_HDUD_HD REFERENCES dbo.HOADON(MaHD),
 MaKM char(10) NOT NULL CONSTRAINT FK_HDUD_KM REFERENCES dbo.KHUYENMAI(MaKM),
 PromotionRevision binary(8) NOT NULL,
 Audience varchar(10) NOT NULL,
 Scope varchar(4) NOT NULL,
 [Percent] int NOT NULL CONSTRAINT CK_HDUD_Percent CHECK([Percent] BETWEEN 0 AND 100),
 BaseAmount decimal(18,0) NOT NULL CONSTRAINT CK_HDUD_Base CHECK(BaseAmount>=0),
 Discount decimal(18,0) NOT NULL CONSTRAINT CK_HDUD_Discount CHECK(Discount>=0),
 RecordedAt datetime2(7) NOT NULL CONSTRAINT DF_HDUD_RecordedAt DEFAULT SYSDATETIME(),
 CONSTRAINT PK_HoaDonUuDaiApDung PRIMARY KEY(MaHD,MaKM),
 CONSTRAINT CK_HDUD_Audience CHECK(Audience IN('ALL','FIRST','MEMBER','TIER')),
 CONSTRAINT CK_HDUD_Scope CHECK(Scope IN('ALL','ITEM')));
CREATE INDEX IX_HoaDonUuDaiApDung_MaKM ON dbo.HoaDonUuDaiApDung(MaKM);
DENY INSERT,UPDATE,DELETE ON dbo.KHUYENMAI_HANG TO CafeManagerApp;
DENY INSERT,UPDATE,DELETE ON dbo.KHUYENMAI_MON TO CafeManagerApp;
DENY INSERT,UPDATE,DELETE ON dbo.HoaDonUuDaiApDung TO CafeManagerApp;
GO
ALTER TABLE dbo.HoaDonUuDaiSnapshot ADD TierDiscount decimal(18,0) NULL,PromotionDiscount decimal(18,0) NULL,
 PromotionCount int NULL,PromotionDigest binary(32) NULL;
GO
ALTER TABLE dbo.HoaDonUuDaiSnapshot DROP CONSTRAINT CK_DiscountSnapshot_Known;
ALTER TABLE dbo.HoaDonUuDaiSnapshot DROP CONSTRAINT CK_DiscountSnapshot_Source;
GO
ALTER TABLE dbo.HoaDonUuDaiSnapshot ADD CONSTRAINT CK_DiscountSnapshot_Source
 CHECK(Source IN('None','Tier','Promotion','TierAndPromotion') OR Source IS NULL);
-- Dòng Checkout của 038 chưa có TierDiscount/PromotionDiscount; giữ NOCHECK để không
-- backfill lịch sử, dòng mới vẫn phải đủ breakdown và tổng khớp.
ALTER TABLE dbo.HoaDonUuDaiSnapshot WITH NOCHECK ADD CONSTRAINT CK_DiscountSnapshot_Known CHECK(
 (Kind='Checkout' AND Source IS NOT NULL AND PercentDiscount IS NOT NULL
  AND TierPercent IS NOT NULL AND PromotionPercent IS NOT NULL
  AND TierPercent BETWEEN 0 AND 100 AND PromotionPercent BETWEEN 0 AND 100
  AND PercentDiscount=CASE WHEN TierPercent+PromotionPercent>100 THEN 100 ELSE TierPercent+PromotionPercent END
  AND PolicyRevision IS NOT NULL
  AND (TierDiscount IS NULL OR PromotionDiscount IS NULL OR Discount=TierDiscount+PromotionDiscount))
 OR (Kind='LegacyUnknown' AND Source IS NULL AND PercentDiscount IS NULL));
GO
CREATE TRIGGER dbo.TR_HoaDonUuDaiApDung ON dbo.HoaDonUuDaiApDung AFTER INSERT,UPDATE,DELETE AS
BEGIN
 SET NOCOUNT ON;
 IF EXISTS(SELECT 1 FROM deleted) THROW 53307,N'Danh sách ưu đãi đã áp không sửa/xóa.',1;
 IF EXISTS(SELECT 1 FROM inserted i JOIN dbo.HOADON h ON h.MaHD=i.MaHD
   WHERE h.TrangThai NOT IN (N'Chua thanh toan',N'Cho thanh toan'))
  THROW 53307,N'Chỉ ghi ưu đãi áp dụng khi chốt đơn đang mở.',1;
 IF EXISTS(SELECT i.MaHD FROM inserted i JOIN dbo.HoaDonUuDaiSnapshot s ON s.MaHD=i.MaHD
   GROUP BY i.MaHD,s.PromotionDiscount HAVING SUM(i.Discount)<>COALESCE(s.PromotionDiscount,-1))
  THROW 53307,N'Tổng ưu đãi áp dụng lệch snapshot.',1;
END;
GO
CREATE TYPE dbo.PreviewLines AS TABLE(MaSP char(5) NOT NULL,SoLuong int NOT NULL);
GRANT EXECUTE,REFERENCES ON TYPE::dbo.PreviewLines TO CafeManagerApp;
GO
/* ===== 2. Hàm dùng chung ===== */
CREATE FUNCTION dbo.fn_HoaDonLinesJson(@Code char(10)) RETURNS TABLE AS RETURN
(
 SELECT CONVERT(nvarchar(max),(SELECT d.MaSP Product,CONVERT(decimal(18,0),d.SoLuong)*d.GiaBan Amount
  FROM dbo.CHITIET_HD d WHERE d.MaHD=@Code ORDER BY d.MaDong FOR JSON PATH,INCLUDE_NULL_VALUES)) Json
);
GO
CREATE FUNCTION dbo.fn_UuDaiCap(@BusinessDate date,@Customer char(5),@LegacyPromotion char(10),@LinesJson nvarchar(max))
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
 WHERE k.MaKM<>COALESCE(@LegacyPromotion,'') AND k.TrangThai='DangChay'
  AND @BusinessDate BETWEEN k.NgayBD AND COALESCE(k.NgayKT,CONVERT(date,'99991231',112))
  AND (k.DoiTuongKhach='ALL'
   OR (k.DoiTuongKhach='MEMBER' AND @Customer IS NOT NULL)
   OR (k.DoiTuongKhach='FIRST' AND @Customer IS NOT NULL
       AND NOT EXISTS(SELECT 1 FROM dbo.HOADON h WHERE h.MaKH=@Customer AND h.TrangThai=N'Da thanh toan'))
   OR (k.DoiTuongKhach='TIER' AND @Customer IS NOT NULL
       AND EXISTS(SELECT 1 FROM dbo.KHUYENMAI_HANG x JOIN dbo.KHACHHANG c ON c.MaKH=@Customer
                  WHERE x.MaKM=k.MaKM AND x.MaHang=c.MaHang)));
 IF @LegacyPromotion IS NOT NULL AND NOT EXISTS(SELECT 1 FROM @promo WHERE MaKM=@LegacyPromotion)
  INSERT @promo SELECT k.MaKM,k.PhanTramGiam,0 FROM dbo.KHUYENMAI k
  WHERE k.MaKM=@LegacyPromotion AND @BusinessDate BETWEEN k.NgayBD AND COALESCE(k.NgayKT,CONVERT(date,'99991231',112));
 INSERT @result(MaKM,[Percent],No,Amount)
 SELECT p.MaKM,p.[Percent],l.No,l.Amount
 FROM @promo p JOIN @line l ON p.IsItem=0
   OR (l.MaSP IS NOT NULL AND EXISTS(SELECT 1 FROM dbo.KHUYENMAI_MON m WHERE m.MaKM=p.MaKM AND m.MaSP=l.MaSP))
 WHERE p.[Percent]>0;
 RETURN;
END;
GO
CREATE FUNCTION dbo.fn_UuDaiApDung(@BusinessDate date,@Customer char(5),@LegacyPromotion char(10),@LinesJson nvarchar(max))
RETURNS @result TABLE(MaKM char(10) NOT NULL,[Percent] int NOT NULL,BaseAmount decimal(18,0) NOT NULL,RawShare decimal(28,4) NOT NULL)
AS
BEGIN
 INSERT @result
 SELECT MaKM,MAX([Percent]),SUM(Amount),SUM(CONVERT(decimal(28,4),Amount)*CONVERT(decimal(5,2),[Percent])/100)
 FROM dbo.fn_UuDaiCap(@BusinessDate,@Customer,@LegacyPromotion,@LinesJson) GROUP BY MaKM;
 RETURN;
END;
GO
CREATE FUNCTION dbo.fn_UuDaiDong(@BusinessDate date,@Customer char(5),@LegacyPromotion char(10),@LinesJson nvarchar(max))
RETURNS @result TABLE(No int NOT NULL,Amount decimal(18,0) NOT NULL,[Percent] int NOT NULL,Discount decimal(18,0) NOT NULL)
AS
BEGIN
 IF @LinesJson IS NULL OR LTRIM(RTRIM(@LinesJson))='' SET @LinesJson=N'[]';
 DECLARE @line TABLE(No int IDENTITY(1,1) NOT NULL,Amount decimal(18,0) NOT NULL);
 INSERT @line(Amount) SELECT CONVERT(decimal(18,0),JSON_VALUE(v.value,'$.Amount'))
 FROM OPENJSON(@LinesJson) v WHERE JSON_VALUE(v.value,'$.Amount') IS NOT NULL;
 DECLARE @tier int=0;
 IF @Customer IS NOT NULL
  SELECT @tier=COALESCE(h.PhanTramGiam,0) FROM dbo.KHACHHANG k JOIN dbo.HANGTHANHVIEN h ON h.MaHang=k.MaHang WHERE k.MaKH=@Customer;
 INSERT @result(No,Amount,[Percent],Discount)
 SELECT l.No,l.Amount,
  CASE WHEN @tier+COALESCE(p.Pct,0)>100 THEN 100 ELSE @tier+COALESCE(p.Pct,0) END,
  CONVERT(decimal(18,0),ROUND(CONVERT(decimal(28,4),l.Amount)
   *CONVERT(decimal(5,2),CASE WHEN @tier+COALESCE(p.Pct,0)>100 THEN 100 ELSE @tier+COALESCE(p.Pct,0) END)/100,0))
 FROM @line l LEFT JOIN(SELECT No,SUM([Percent]) Pct FROM dbo.fn_UuDaiCap(@BusinessDate,@Customer,@LegacyPromotion,@LinesJson) GROUP BY No) p ON p.No=l.No;
 RETURN;
END;
GO
CREATE FUNCTION dbo.fn_UuDaiTong(@BusinessDate date,@Customer char(5),@LegacyPromotion char(10),@LinesJson nvarchar(max))
RETURNS @result TABLE(TierPercent int NOT NULL,PromotionPercent int NOT NULL,
 TierDiscount decimal(18,0) NOT NULL,PromotionDiscount decimal(18,0) NOT NULL,Discount decimal(18,0) NOT NULL)
AS
BEGIN
 IF @LinesJson IS NULL OR LTRIM(RTRIM(@LinesJson))='' SET @LinesJson=N'[]';
 DECLARE @total decimal(18,0)=(SELECT COALESCE(SUM(Amount),0) FROM dbo.fn_UuDaiDong(@BusinessDate,@Customer,@LegacyPromotion,@LinesJson));
 DECLARE @raw decimal(38,0)=(SELECT COALESCE(SUM(Discount),0) FROM dbo.fn_UuDaiDong(@BusinessDate,@Customer,@LegacyPromotion,@LinesJson));
 DECLARE @discount decimal(18,0)=CASE WHEN @raw>CONVERT(decimal(38,0),@total) THEN @total ELSE CONVERT(decimal(18,0),@raw) END;
 DECLARE @tier int=0;
 IF @Customer IS NOT NULL
  SELECT @tier=COALESCE(h.PhanTramGiam,0) FROM dbo.KHACHHANG k JOIN dbo.HANGTHANHVIEN h ON h.MaHang=k.MaHang WHERE k.MaKH=@Customer;
 DECLARE @tierDiscount decimal(18,0)=CASE WHEN @tier=0 THEN 0
  ELSE CONVERT(decimal(18,0),ROUND(CONVERT(decimal(28,4),@total)*@tier/100,0)) END;
 IF @tierDiscount>@discount SET @tierDiscount=@discount;
 DECLARE @promoPercent int=(SELECT CASE WHEN COALESCE(SUM([Percent]),0)>100 THEN 100 ELSE COALESCE(SUM([Percent]),0) END
  FROM dbo.fn_UuDaiApDung(@BusinessDate,@Customer,@LegacyPromotion,@LinesJson));
 INSERT @result VALUES(@tier,@promoPercent,@tierDiscount,@discount-@tierDiscount,@discount);
 RETURN;
END;
GO
CREATE FUNCTION dbo.fn_UuDaiChia(@BusinessDate date,@Customer char(5),@LegacyPromotion char(10),@LinesJson nvarchar(max))
RETURNS @result TABLE(MaKM char(10) NOT NULL,[Percent] int NOT NULL,BaseAmount decimal(18,0) NOT NULL,Discount decimal(18,0) NOT NULL)
AS
BEGIN
 DECLARE @promoTotal decimal(18,0)=(SELECT PromotionDiscount FROM dbo.fn_UuDaiTong(@BusinessDate,@Customer,@LegacyPromotion,@LinesJson));
 DECLARE @rawTotal decimal(28,4)=(SELECT COALESCE(SUM(RawShare),0) FROM dbo.fn_UuDaiApDung(@BusinessDate,@Customer,@LegacyPromotion,@LinesJson));
 IF @rawTotal<=0 RETURN;
 ;WITH x AS(
  SELECT MaKM,[Percent],BaseAmount,
   CASE WHEN @promoTotal<=0 THEN CONVERT(decimal(18,0),0)
    ELSE CONVERT(decimal(18,0),FLOOR(CONVERT(decimal(38,8),@promoTotal)*RawShare/@rawTotal)) END Base,
   ROW_NUMBER() OVER(ORDER BY MaKM) rn,COUNT(*) OVER() cnt
  FROM dbo.fn_UuDaiApDung(@BusinessDate,@Customer,@LegacyPromotion,@LinesJson) WHERE RawShare>0),
 y AS(SELECT *,SUM(Base) OVER(ORDER BY MaKM ROWS UNBOUNDED PRECEDING) Cum FROM x)
 INSERT @result(MaKM,[Percent],BaseAmount,Discount)
 SELECT MaKM,[Percent],BaseAmount,CASE WHEN rn=cnt THEN @promoTotal-(Cum-Base) ELSE Base END FROM y;
 DELETE @result WHERE Discount<=0;
 RETURN;
END;
GO
CREATE FUNCTION dbo.fn_HoaDonGiamGia(@Code char(10))
RETURNS @result TABLE(TierPercent int NULL,PromotionPercent int NULL,TierDiscount decimal(18,0) NULL,
 PromotionDiscount decimal(18,0) NULL,Discount decimal(18,0) NOT NULL)
AS
BEGIN
 DECLARE @status nvarchar(30),@customer char(5),@legacy char(10),@date date,@json nvarchar(max);
 SELECT @status=h.TrangThai,@customer=h.MaKH,@legacy=h.MaKM,@date=CONVERT(date,h.NgayLap)
 FROM dbo.HOADON h WHERE h.MaHD=@Code;
 IF @@ROWCOUNT=0 RETURN;
 IF @status IN(N'Chua thanh toan',N'Cho thanh toan')
 BEGIN
  SELECT @json=Json FROM dbo.fn_HoaDonLinesJson(@Code);
  INSERT @result SELECT TierPercent,PromotionPercent,TierDiscount,PromotionDiscount,Discount
  FROM dbo.fn_UuDaiTong(@date,@customer,@legacy,@json);
 END
 ELSE
  INSERT @result SELECT s.TierPercent,s.PromotionPercent,s.TierDiscount,s.PromotionDiscount,h.GiamGia
  FROM dbo.HOADON h LEFT JOIN dbo.HoaDonUuDaiSnapshot s ON s.MaHD=h.MaHD WHERE h.MaHD=@Code;
 RETURN;
END;
GO
CREATE FUNCTION dbo.fn_HoaDonUuDaiApDung(@Code char(10))
RETURNS @result TABLE(MaKM char(10) NOT NULL,Name nvarchar(200) NOT NULL,[Percent] int NOT NULL,
 BaseAmount decimal(18,0) NOT NULL,Discount decimal(18,0) NOT NULL,
 PromotionRevision binary(8) NOT NULL,Audience varchar(10) NOT NULL,Scope varchar(4) NOT NULL)
AS
BEGIN
 DECLARE @status nvarchar(30),@customer char(5),@legacy char(10),@date date,@json nvarchar(max);
 SELECT @status=h.TrangThai,@customer=h.MaKH,@legacy=h.MaKM,@date=CONVERT(date,h.NgayLap)
 FROM dbo.HOADON h WHERE h.MaHD=@Code;
 IF @@ROWCOUNT=0 RETURN;
 IF @status IN(N'Chua thanh toan',N'Cho thanh toan')
 BEGIN
  SELECT @json=Json FROM dbo.fn_HoaDonLinesJson(@Code);
  INSERT @result
  SELECT c.MaKM,k.TenKM,c.[Percent],c.BaseAmount,c.Discount,k.Revision,k.DoiTuongKhach,k.PhamVi
  FROM dbo.fn_UuDaiChia(@date,@customer,@legacy,@json) c JOIN dbo.KHUYENMAI k ON k.MaKM=c.MaKM;
 END
 ELSE
  INSERT @result
  SELECT a.MaKM,k.TenKM,a.[Percent],a.BaseAmount,a.Discount,a.PromotionRevision,a.Audience,a.Scope
  FROM dbo.HoaDonUuDaiApDung a JOIN dbo.KHUYENMAI k ON k.MaKM=a.MaKM WHERE a.MaHD=@Code;
 RETURN;
END;
GO
CREATE FUNCTION dbo.fn_PromotionCatalogDigest() RETURNS TABLE AS RETURN
(
 SELECT CONVERT(binary(32),HASHBYTES('SHA2_256',CONVERT(varbinary(max),COALESCE((
  SELECT STRING_AGG(CONVERT(nvarchar(60),k.MaKM)+':'+CONVERT(nvarchar(20),CONVERT(bigint,k.Revision)),N',')
   WITHIN GROUP(ORDER BY k.MaKM) FROM dbo.KHUYENMAI k),N'')))) Digest
);
GO
CREATE FUNCTION dbo.fn_HoaDonUuDaiDigest(@Code char(10)) RETURNS TABLE AS RETURN
(
 SELECT CONVERT(binary(32),HASHBYTES('SHA2_256',CONVERT(varbinary(max),COALESCE((
  SELECT STRING_AGG(CONVERT(nvarchar(40),x.MaKM)+':'+CONVERT(nvarchar(20),x.Discount),N',')
   WITHIN GROUP(ORDER BY x.MaKM) FROM dbo.fn_HoaDonUuDaiApDung(@Code) x),N'')))) Digest
);
GO
/* ===== 3. Preview tạm tính ===== */
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
   SYSDATETIME() ReadAt,s.Revision PolicyRevision
  FROM dbo.LoyaltyPolicyState s LEFT JOIN dbo.KHACHHANG k ON k.MaKH=@Customer;
  SELECT c.MaKM Code,k.TenKM Name,c.[Percent],c.BaseAmount,c.Discount
  FROM dbo.fn_UuDaiChia(@BusinessDate,CONVERT(char(5),@Customer),NULL,@json) c JOIN dbo.KHUYENMAI k ON k.MaKM=c.MaKM
  ORDER BY c.MaKM;
  COMMIT;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
/* ===== 4. Danh mục ưu đãi ===== */
ALTER PROCEDURE dbo.sp_PromotionList @Actor int,@Stamp binary(8),@Search nvarchar(max)=N'',@Skip int=0,@OnDate date=NULL AS
BEGIN
 SET NOCOUNT ON; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,CASH';
 IF LEN(@Search)>100 OR @Skip<0 THROW 52001,N'Bộ lọc không hợp lệ.',1;
 IF EXISTS(SELECT 1 FROM dbo.TAIKHOAN WHERE MaTK=@Actor AND MaVT='CASH')
 BEGIN
  SET @OnDate=COALESCE(@OnDate,CONVERT(date,SYSDATETIME()));
  SET @Search=REPLACE(REPLACE(REPLACE(REPLACE(@Search,N'~',N'~~'),N'%',N'~%'),N'_',N'~_'),N'[',N'~[');
  SELECT k.MaKM Code,k.TenKM Name,k.NgayBD StartDate,k.NgayKT EndDate,k.PhanTramGiam Discount,
   CASE WHEN @OnDate>COALESCE(k.NgayKT,CONVERT(date,'99991231',112)) THEN 'HetHan' ELSE k.TrangThai END TrangThai,
   k.DoiTuongKhach Audience,k.PhamVi Scope,
   (SELECT COUNT(*) FROM dbo.KHUYENMAI_MON m WHERE m.MaKM=k.MaKM) ItemCount,k.Revision
  FROM dbo.KHUYENMAI k
  WHERE (k.TenKM LIKE N'%'+@Search+N'%' ESCAPE N'~' OR k.MaKM LIKE N'%'+@Search+N'%' ESCAPE N'~')
   AND k.TrangThai='DangChay'
   AND @OnDate BETWEEN k.NgayBD AND COALESCE(k.NgayKT,CONVERT(date,'99991231',112))
  ORDER BY k.MaKM OFFSET @Skip ROWS FETCH NEXT 50 ROWS ONLY;
  RETURN;
 END;
 SET @Search=REPLACE(REPLACE(REPLACE(REPLACE(@Search,N'~',N'~~'),N'%',N'~%'),N'_',N'~_'),N'[',N'~[');
 SELECT k.MaKM Code,k.TenKM Name,k.NgayBD StartDate,k.NgayKT EndDate,k.PhanTramGiam Discount,
  CASE WHEN k.TrangThai='DangChay' AND k.NgayKT IS NOT NULL AND COALESCE(@OnDate,CONVERT(date,SYSDATETIME()))>k.NgayKT
   THEN 'HetHan' ELSE k.TrangThai END TrangThai,
  k.DoiTuongKhach Audience,k.PhamVi Scope,
  (SELECT COUNT(*) FROM dbo.KHUYENMAI_MON m WHERE m.MaKM=k.MaKM) ItemCount,k.Revision
 FROM dbo.KHUYENMAI k
 WHERE (k.TenKM LIKE N'%'+@Search+N'%' ESCAPE N'~' OR k.MaKM LIKE N'%'+@Search+N'%' ESCAPE N'~')
  AND (@OnDate IS NULL OR @OnDate BETWEEN k.NgayBD AND COALESCE(k.NgayKT,CONVERT(date,'99991231',112)))
 ORDER BY k.MaKM OFFSET @Skip ROWS FETCH NEXT 50 ROWS ONLY;
END;
GO
ALTER PROCEDURE dbo.sp_PromotionGet @Actor int,@Stamp binary(8),@Code varchar(max) AS
BEGIN
 SET NOCOUNT ON; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,CASH';
 SELECT k.MaKM Code,k.TenKM Name,k.NgayBD StartDate,k.NgayKT EndDate,k.PhanTramGiam Discount,
  k.TrangThai,k.DoiTuongKhach Audience,k.PhamVi Scope,
  (SELECT COUNT(*) FROM dbo.KHUYENMAI_MON m WHERE m.MaKM=k.MaKM) ItemCount,k.Revision
 FROM dbo.KHUYENMAI k WHERE k.MaKM=@Code;
 SELECT h.MaHang,h.DiemToiThieu,h.PhanTramGiam FROM dbo.KHUYENMAI_HANG x
  JOIN dbo.HANGTHANHVIEN h ON h.MaHang=x.MaHang WHERE x.MaKM=@Code ORDER BY h.DiemToiThieu;
 SELECT m.MaSP,s.TenSP,s.GiaBan,s.TrangThai FROM dbo.KHUYENMAI_MON m
  JOIN dbo.SANPHAM s ON s.MaSP=m.MaSP WHERE m.MaKM=@Code ORDER BY m.MaSP;
END;
GO
ALTER PROCEDURE dbo.sp_PromotionWrite @Actor int,@Stamp binary(8),@Action varchar(10),@Code varchar(max),
 @RequestId uniqueidentifier,@ExpectedRevision binary(8)=NULL,@Name nvarchar(max)=NULL,@StartDate date=NULL,
 @EndDate date=NULL,@Discount bigint=NULL,@Audience varchar(10)=NULL,@Status varchar(10)=NULL,
 @TiersJson nvarchar(max)=NULL,@ItemsJson nvarchar(max)=NULL AS
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
  IF @Action IN('Create','Update')
  BEGIN
   SET @Name=LTRIM(RTRIM(@Name)); SET @Audience=UPPER(LTRIM(RTRIM(@Audience)));
   IF @Status IS NULL AND @Action='Create' SET @Status='Nhap';
   IF @Status IS NULL SET @Status=(SELECT k.TrangThai FROM dbo.KHUYENMAI k WITH(HOLDLOCK) WHERE k.MaKM=@Code);
   IF @Status IS NULL THROW 51603,N'Chương trình không tồn tại hoặc thiếu trạng thái.',1;
   IF @Name IS NULL OR LEN(@Name)=0 OR DATALENGTH(@Name)>200 OR @Name LIKE N'%'+NCHAR(9)+N'%'
    OR @Name LIKE N'%'+NCHAR(10)+N'%' OR @Name LIKE N'%'+NCHAR(13)+N'%'
    OR @StartDate IS NULL OR (@EndDate IS NOT NULL AND @EndDate<@StartDate)
    OR @Discount IS NULL OR @Discount<0 OR @Discount>100
    OR @Status NOT IN('Nhap','DangChay','TamDung') OR @Audience NOT IN('ALL','FIRST','MEMBER','TIER')
    THROW 52001,N'Thông tin chương trình không hợp lệ.',1;
   IF @Audience='TIER'
   BEGIN
    IF @TiersJson IS NULL OR ISJSON(@TiersJson)<>1 THROW 52001,N'Cần danh sách hạng hợp lệ.',1;
    INSERT @tiers SELECT DISTINCT UPPER(LTRIM(RTRIM(CONVERT(char(5),value)))) FROM OPENJSON(@TiersJson)
     WHERE value IS NOT NULL AND LEN(value) BETWEEN 1 AND 5;
    IF NOT EXISTS(SELECT 1 FROM @tiers) THROW 52001,N'Ưu đãi theo hạng cần ít nhất một hạng.',1;
    IF EXISTS(SELECT 1 FROM @tiers t WHERE NOT EXISTS(SELECT 1 FROM dbo.HANGTHANHVIEN h WHERE h.MaHang=t.MaHang))
     THROW 52005,N'Hạng không tồn tại.',1;
   END;
   IF @ItemsJson IS NOT NULL AND ISJSON(@ItemsJson)<>1 THROW 52001,N'Danh sách món không hợp lệ.',1;
   IF @ItemsJson IS NOT NULL
    INSERT @items SELECT DISTINCT CONVERT(char(5),value) FROM OPENJSON(@ItemsJson)
     WHERE value IS NOT NULL AND LEN(value)=5;
   IF (SELECT COUNT(*) FROM @items)>500 THROW 52001,N'Tối đa 500 món mỗi ưu đãi.',1;
   IF EXISTS(SELECT 1 FROM @items i WHERE NOT EXISTS(SELECT 1 FROM dbo.SANPHAM s WHERE s.MaSP=i.MaSP))
    THROW 52005,N'Món không tồn tại.',1;
  END;
  DECLARE @payload nvarchar(max)=(SELECT @Code c,@Action a,@Name n,@StartDate sd,@EndDate ed,@Discount d,
   @Audience au,@Status st,@TiersJson tj,@ItemsJson ij,@ExpectedRevision r FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);
  DECLARE @hash binary(32)=HASHBYTES('SHA2_256',CONVERT(varbinary(max),@payload)),@resultCode varchar(10)=@Code,
   @revision binary(8),@replay bit,@operation varchar(40)='Promotion'+@Action;
  EXEC dbo.sp_G03BeginReceipt @Actor,@operation,@RequestId,@hash,@resultCode OUTPUT,@revision OUTPUT,@replay OUTPUT;
  IF @replay=0
  BEGIN
   IF @Action='Create'
   BEGIN
    IF EXISTS(SELECT 1 FROM dbo.KHUYENMAI WITH(UPDLOCK,HOLDLOCK) WHERE MaKM=@Code) THROW 52003,N'Mã KM đã tồn tại.',1;
    INSERT dbo.KHUYENMAI(MaKM,TenKM,NgayBD,NgayKT,PhanTramGiam,TrangThai,DoiTuongKhach,PhamVi)
    VALUES(@Code,@Name,@StartDate,@EndDate,CONVERT(int,@Discount),@Status,@Audience,CASE WHEN EXISTS(SELECT 1 FROM @items) THEN 'ITEM' ELSE 'ALL' END);
   END
   ELSE
   BEGIN
    IF NOT EXISTS(SELECT 1 FROM dbo.KHUYENMAI WITH(UPDLOCK,HOLDLOCK) WHERE MaKM=@Code AND Revision=@ExpectedRevision)
     THROW 51603,N'Chương trình đã thay đổi.',1;
    IF @Action='Delete'
    BEGIN
     IF EXISTS(SELECT 1 FROM dbo.HoaDonUuDaiApDung WHERE MaKM=@Code)
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
      PhamVi=CASE WHEN EXISTS(SELECT 1 FROM @items) THEN 'ITEM' ELSE 'ALL' END WHERE MaKM=@Code;
    END;
   END;
   IF @Action IN('Create','Update')
   BEGIN
    DELETE dbo.KHUYENMAI_HANG WHERE MaKM=@Code;
    INSERT dbo.KHUYENMAI_HANG(MaKM,MaHang) SELECT @Code,MaHang FROM @tiers;
    DELETE dbo.KHUYENMAI_MON WHERE MaKM=@Code;
    INSERT dbo.KHUYENMAI_MON(MaKM,MaSP) SELECT @Code,MaSP FROM @items;
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
 @Audience varchar(10)=NULL,@Status varchar(10)=NULL,@TiersJson nvarchar(max)=NULL,@ItemsJson nvarchar(max)=NULL AS
BEGIN SET NOCOUNT ON; EXEC dbo.sp_PromotionWrite @Actor,@Stamp,'Create',@Code,@RequestId,@ExpectedRevision,@Name,@StartDate,@EndDate,@Discount,@Audience,@Status,@TiersJson,@ItemsJson; END;
GO
ALTER PROCEDURE dbo.sp_PromotionUpdate @Actor int,@Stamp binary(8),@Code varchar(max),@RequestId uniqueidentifier,
 @ExpectedRevision binary(8)=NULL,@Name nvarchar(max)=NULL,@StartDate date=NULL,@EndDate date=NULL,@Discount bigint=NULL,
 @Audience varchar(10)=NULL,@Status varchar(10)=NULL,@TiersJson nvarchar(max)=NULL,@ItemsJson nvarchar(max)=NULL AS
BEGIN SET NOCOUNT ON; EXEC dbo.sp_PromotionWrite @Actor,@Stamp,'Update',@Code,@RequestId,@ExpectedRevision,@Name,@StartDate,@EndDate,@Discount,@Audience,@Status,@TiersJson,@ItemsJson; END;
GO
ALTER PROCEDURE dbo.sp_PromotionDelete @Actor int,@Stamp binary(8),@Code varchar(max),@RequestId uniqueidentifier,
 @ExpectedRevision binary(8)=NULL,@Name nvarchar(max)=NULL,@StartDate date=NULL,@EndDate date=NULL,@Discount bigint=NULL,
 @Audience varchar(10)=NULL,@Status varchar(10)=NULL,@TiersJson nvarchar(max)=NULL,@ItemsJson nvarchar(max)=NULL AS
BEGIN SET NOCOUNT ON; EXEC dbo.sp_PromotionWrite @Actor,@Stamp,'Delete',@Code,@RequestId,@ExpectedRevision,@Name,@StartDate,@EndDate,@Discount,@Audience,@Status,@TiersJson,@ItemsJson; END;
GO
/* ===== 5. Bán hàng ===== */
ALTER PROCEDURE dbo.sp_SalesValidateHeader @Channel nvarchar(max),@TableCode varchar(max),@Customer varchar(max),
 @Partner varchar(max),@Recipient nvarchar(max),@Phone varchar(max),@Address nvarchar(max),@Ship decimal(38,18),@BusinessDate date AS
BEGIN
 SET NOCOUNT ON;
 IF @Channel IS NULL OR @Channel NOT IN(N'Tai quan',N'Mang di',N'Giao hang') OR @Ship IS NULL OR @Ship<0
  OR @Ship>999999999999999999 OR ROUND(@Ship,0)<>@Ship THROW 52101,N'Loại đơn/phí ship không hợp lệ.',1;
 IF (@TableCode IS NOT NULL AND (DATALENGTH(@TableCode)>5 OR NOT EXISTS(SELECT 1 FROM dbo.BAN WITH(HOLDLOCK) WHERE MaBan=@TableCode)))
  OR (@Customer IS NOT NULL AND (DATALENGTH(@Customer)>5 OR NOT EXISTS(SELECT 1 FROM dbo.KHACHHANG WITH(HOLDLOCK) WHERE MaKH=@Customer)))
  OR (@Partner IS NOT NULL AND (DATALENGTH(@Partner)>5 OR NOT EXISTS(SELECT 1 FROM dbo.DOITACGIAOHANG WITH(HOLDLOCK) WHERE MaDT=@Partner)))
  THROW 52105,N'Bàn/khách/đối tác không hợp lệ.',1;
 IF (@Channel=N'Tai quan' AND (@TableCode IS NULL OR @Partner IS NOT NULL)) OR (@Channel<>N'Tai quan' AND @TableCode IS NOT NULL)
  OR (@Channel<>N'Giao hang' AND (@Partner IS NOT NULL OR @Ship<>0 OR @Recipient IS NOT NULL OR @Phone IS NOT NULL OR @Address IS NOT NULL))
  THROW 52101,N'Thông tin không phù hợp loại đơn.',1;
 IF @Channel=N'Giao hang' AND (@Recipient IS NULL OR LEN(@Recipient)=0 OR DATALENGTH(@Recipient)>200
  OR @Phone IS NULL OR DATALENGTH(@Phone) NOT BETWEEN 1 AND 15 OR @Phone COLLATE Latin1_General_100_BIN2 LIKE '%[^0-9]%'
  OR @Address IS NULL OR LEN(@Address)=0 OR DATALENGTH(@Address)>500) THROW 52101,N'Cần tên, số điện thoại và địa chỉ giao hợp lệ.',1;
END;
GO
ALTER PROCEDURE dbo.sp_SalesRefreshDiscount @Code char(10) AS
BEGIN
 SET NOCOUNT ON;
 IF EXISTS(SELECT 1 FROM dbo.HOADON h CROSS APPLY dbo.fn_HoaDonGiamGia(h.MaHD) d
   WHERE h.MaHD=@Code AND CONVERT(decimal(38,0),h.TongTien+h.PhiShip-d.Discount)>999999999999999999)
  THROW 52101,N'Tổng tiền vượt giới hạn.',1;
 UPDATE h SET GiamGia=d.Discount FROM dbo.HOADON h CROSS APPLY dbo.fn_HoaDonGiamGia(h.MaHD) d WHERE h.MaHD=@Code;
END;
GO
ALTER PROCEDURE dbo.sp_TaoHoaDon @Actor int,@Stamp binary(8),@Code varchar(max),@RequestId uniqueidentifier,
 @Channel nvarchar(max),@TableCode varchar(max)=NULL,@Customer varchar(max)=NULL,@Partner varchar(max)=NULL,
 @Recipient nvarchar(max)=NULL,@Phone varchar(max)=NULL,@Address varchar(max)=NULL,@Ship decimal(38,18)=0,
 @Product varchar(max),@Quantity bigint,@Note nvarchar(max)=NULL AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 52101,N'Không gọi ghi trong transaction bên ngoài.',1;
 BEGIN TRY
  BEGIN TRAN;
  IF @Code IS NULL OR DATALENGTH(@Code)<>10 OR @Code COLLATE Latin1_General_100_BIN2 NOT LIKE 'HD[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'
   THROW 52101,N'Mã hóa đơn không hợp lệ.',1;
  SELECT @Recipient=NULLIF(LTRIM(RTRIM(@Recipient)),N''),@Phone=NULLIF(LTRIM(RTRIM(@Phone)),''),
   @Address=NULLIF(LTRIM(RTRIM(@Address)),N''),@Note=NULLIF(LTRIM(RTRIM(@Note)),N'');
  SELECT @Customer=NULLIF(LTRIM(RTRIM(@Customer)),''),@Partner=NULLIF(LTRIM(RTRIM(@Partner)),'');
  SET @TableCode=NULLIF(LTRIM(RTRIM(@TableCode)),'');
  DECLARE @payload nvarchar(max)=(SELECT @Code c,@Channel ch,@TableCode b,@Customer kh,@Partner dt,@Recipient n,
   @Phone ph,@Address a,@Ship ship,@Product sp,@Quantity q,@Note note FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES),@replay bit;
  EXEC dbo.sp_SalesBegin @Actor,@Stamp,@RequestId,'SalesCreate',@Code,@payload,@replay OUTPUT;
  IF @replay=0
  BEGIN
   EXEC dbo.sp_SalesLock N'CafeManager:LoyaltyPolicy','Shared'; EXEC dbo.sp_SalesLock N'CafeManager:Recipes','Shared';
   DECLARE @resource nvarchar(255)=N'CafeManager:SalesOrder:'+CONVERT(nvarchar(10),@Code); EXEC dbo.sp_SalesLock @resource;
   DECLARE @date date=CONVERT(date,SYSDATETIME());
   EXEC dbo.sp_SalesValidateHeader @Channel,@TableCode,@Customer,@Partner,@Recipient,@Phone,@Address,@Ship,@date;
   IF @TableCode IS NOT NULL
   BEGIN
    DECLARE @lockedTable char(5); SELECT @lockedTable=MaBan FROM dbo.BAN WITH(UPDLOCK,HOLDLOCK) WHERE MaBan=@TableCode;
    IF EXISTS(SELECT 1 FROM dbo.HOADON WITH(UPDLOCK,HOLDLOCK) WHERE MaBan=@TableCode AND TrangThai IN(N'Chua thanh toan',N'Cho thanh toan'))
     THROW 52104,N'Bàn đã có đơn. Tải đơn hiện tại trước khi thêm món.',1;
   END;
   DECLARE @session int;
   IF @TableCode IS NOT NULL
   BEGIN
    SELECT @session=MaPhien FROM dbo.PHIEN_BAN WITH(UPDLOCK,HOLDLOCK) WHERE MaBan=@TableCode AND GioRa IS NULL;
    IF @session IS NULL
    BEGIN
     INSERT dbo.PHIEN_BAN(MaBan,MaNVMo) SELECT @TableCode,MaNV FROM dbo.TAIKHOAN WHERE MaTK=@Actor;
     SET @session=SCOPE_IDENTITY();
    END;
   END;
   IF @Quantity IS NULL OR @Quantity NOT BETWEEN 1 AND 2147483647 OR DATALENGTH(@Product)>5 OR DATALENGTH(@Note)>200
    THROW 52101,N'Số lượng/ghi chú/mã món không hợp lệ.',1;
   DECLARE @price decimal(18,0); SELECT @price=GiaBan FROM dbo.SANPHAM WITH(HOLDLOCK)
    WHERE MaSP=@Product AND TrangThai=N'Dang ban' AND EXISTS(SELECT 1 FROM dbo.CONGTHUC WHERE MaSP=@Product);
   IF @price IS NULL THROW 52105,N'Món ngừng bán hoặc chưa có công thức.',1;
   IF CONVERT(decimal(38,0),@price)*@Quantity>999999999999999999 THROW 52101,N'Tổng tiền vượt giới hạn.',1;
   INSERT dbo.HOADON(MaHD,MaNV,MaKH,MaBan,MaDT,LoaiDon,TenNguoiNhan,SDTNguoiNhan,DiaChiGiao,TrangThaiGiao,PhiShip,MaPhien)
   SELECT @Code,MaNV,@Customer,@TableCode,@Partner,@Channel,@Recipient,@Phone,@Address,
    CASE WHEN @Channel=N'Giao hang' THEN N'Cho giao' END,CONVERT(decimal(18,0),@Ship),@session
   FROM dbo.TAIKHOAN WHERE MaTK=@Actor;
   INSERT dbo.CHITIET_HD(MaHD,MaSP,SoLuong,GiaBan,GhiChu) VALUES(@Code,@Product,CONVERT(int,@Quantity),@price,@Note);
   EXEC dbo.sp_SalesRefreshDiscount @Code;
   EXEC dbo.sp_SalesFinish @Actor,@RequestId,'SalesCreate',@Code,@payload;
  END;
  COMMIT; SELECT EntityCode,ResultRevision FROM dbo.G03OperationReceipt WHERE RequestId=@RequestId;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
ALTER PROCEDURE dbo.sp_TaoHoaDonTuNhap @Actor int,@Stamp binary(8),@Code varchar(max),@RequestId uniqueidentifier,
 @Channel nvarchar(max),@TableCode varchar(max)=NULL,@Customer varchar(max)=NULL,@Partner varchar(max)=NULL,
 @Recipient nvarchar(max)=NULL,@Phone varchar(max)=NULL,@Address varchar(max)=NULL,@Ship decimal(38,18)=0,
 @Lines dbo.SalesDraftLines READONLY AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 52101,N'Không gọi ghi trong transaction bên ngoài.',1;
 BEGIN TRY
  BEGIN TRAN;
  IF @Code IS NULL OR DATALENGTH(@Code)<>10 OR @Code COLLATE Latin1_General_100_BIN2 NOT LIKE 'HD[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'
   THROW 52101,N'Mã hóa đơn không hợp lệ.',1;
  SELECT @Recipient=NULLIF(LTRIM(RTRIM(@Recipient)),N''),@Phone=NULLIF(LTRIM(RTRIM(@Phone)),''),@Address=NULLIF(LTRIM(RTRIM(@Address)),N'');
  SELECT @Customer=NULLIF(LTRIM(RTRIM(@Customer)),''),@Partner=NULLIF(LTRIM(RTRIM(@Partner)),'');
  DECLARE @lineJson nvarchar(max)=(SELECT RowNo,Product,Quantity,Note,Price FROM @Lines ORDER BY RowNo FOR JSON PATH,INCLUDE_NULL_VALUES);
  DECLARE @payload nvarchar(max)=(SELECT @Code c,@Channel ch,@TableCode b,@Customer kh,@Partner dt,@Recipient n,
   @Phone ph,@Address a,@Ship ship,JSON_QUERY(@lineJson) lines FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES),@replay bit;
  EXEC dbo.sp_SalesBegin @Actor,@Stamp,@RequestId,'SalesCreate',@Code,@payload,@replay OUTPUT;
  IF @replay=0
  BEGIN
   EXEC dbo.sp_SalesLock N'CafeManager:LoyaltyPolicy','Shared'; EXEC dbo.sp_SalesLock N'CafeManager:Recipes','Shared';
   DECLARE @resource nvarchar(255)=N'CafeManager:SalesOrder:'+CONVERT(nvarchar(10),@Code); EXEC dbo.sp_SalesLock @resource;
   DECLARE @date date=CONVERT(date,SYSDATETIME());
   EXEC dbo.sp_SalesValidateHeader @Channel,@TableCode,@Customer,@Partner,@Recipient,@Phone,@Address,@Ship,@date;
   IF @TableCode IS NOT NULL
   BEGIN
    DECLARE @lockedTable char(5); SELECT @lockedTable=MaBan FROM dbo.BAN WITH(UPDLOCK,HOLDLOCK) WHERE MaBan=@TableCode;
    IF EXISTS(SELECT 1 FROM dbo.HOADON WITH(UPDLOCK,HOLDLOCK) WHERE MaBan=@TableCode AND TrangThai IN(N'Chua thanh toan',N'Cho thanh toan'))
     THROW 52104,N'Bàn đã có đơn. Tải đơn hiện tại trước khi thêm món.',1;
   END;
   DECLARE @session int;
   IF @TableCode IS NOT NULL
   BEGIN
    SELECT @session=MaPhien FROM dbo.PHIEN_BAN WITH(UPDLOCK,HOLDLOCK) WHERE MaBan=@TableCode AND GioRa IS NULL;
    IF @session IS NULL
    BEGIN
     INSERT dbo.PHIEN_BAN(MaBan,MaNVMo) SELECT @TableCode,MaNV FROM dbo.TAIKHOAN WHERE MaTK=@Actor;
     SET @session=SCOPE_IDENTITY();
    END;
   END;
   IF NOT EXISTS(SELECT 1 FROM @Lines) THROW 52101,N'Đơn nháp không có dòng món.',1;
   IF EXISTS(SELECT 1 FROM @Lines WHERE Product IS NULL OR Quantity NOT BETWEEN 1 AND 2147483647 OR Price<0
     OR Price>999999999999999999 OR (Note IS NOT NULL AND DATALENGTH(Note)>200))
    THROW 52101,N'Số lượng/ghi chú/mã món không hợp lệ.',1;
   IF EXISTS(SELECT 1 FROM @Lines l WHERE NOT EXISTS(SELECT 1 FROM dbo.SANPHAM s WITH(HOLDLOCK)
     WHERE s.MaSP=l.Product AND s.TrangThai=N'Dang ban' AND EXISTS(SELECT 1 FROM dbo.CONGTHUC c WHERE c.MaSP=l.Product)))
    THROW 52105,N'Món ngừng bán hoặc chưa có công thức.',1;
   IF EXISTS(SELECT 1 FROM @Lines l JOIN dbo.SANPHAM s ON s.MaSP=l.Product WHERE l.Price<>s.GiaBan)
    THROW 52105,N'Giá món đã thay đổi; tải lại menu và thêm lại dòng.',1;
   IF EXISTS(SELECT 1 FROM @Lines WHERE CONVERT(decimal(38,0),Price)*Quantity>999999999999999999)
    OR (SELECT SUM(CONVERT(decimal(38,0),Price)*Quantity) FROM @Lines)>999999999999999999
    THROW 52101,N'Tổng tiền vượt giới hạn.',1;
   INSERT dbo.HOADON(MaHD,MaNV,MaKH,MaBan,MaDT,LoaiDon,TenNguoiNhan,SDTNguoiNhan,DiaChiGiao,TrangThaiGiao,PhiShip,MaPhien)
   SELECT @Code,MaNV,@Customer,@TableCode,@Partner,@Channel,@Recipient,@Phone,@Address,
    CASE WHEN @Channel=N'Giao hang' THEN N'Cho giao' END,CONVERT(decimal(18,0),@Ship),@session
   FROM dbo.TAIKHOAN WHERE MaTK=@Actor;
   INSERT dbo.CHITIET_HD(MaHD,MaSP,SoLuong,GiaBan,GhiChu)
   SELECT @Code,Product,Quantity,Price,NULLIF(LTRIM(RTRIM(Note)),N'') FROM @Lines ORDER BY RowNo;
   EXEC dbo.sp_SalesRefreshDiscount @Code;
   EXEC dbo.sp_SalesFinish @Actor,@RequestId,'SalesCreate',@Code,@payload;
  END;
  COMMIT; SELECT EntityCode,ResultRevision FROM dbo.G03OperationReceipt WHERE RequestId=@RequestId;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
ALTER PROCEDURE dbo.sp_SuaDongHoaDon @Actor int,@Stamp binary(8),@Code varchar(max),@ExpectedRevision binary(8),
 @RequestId uniqueidentifier,@Action varchar(10),@LineId int=NULL,@Product varchar(max)=NULL,@Quantity bigint=NULL,
 @Note nvarchar(max)=NULL AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 52101,N'Không gọi ghi trong transaction bên ngoài.',1;
 BEGIN TRY
  BEGIN TRAN; SET @Note=NULLIF(LTRIM(RTRIM(@Note)),N'');
  DECLARE @payload nvarchar(max)=(SELECT @Code c,@ExpectedRevision r,@Action a,@LineId l,@Product p,@Quantity q,@Note note
   FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES),@replay bit;
  EXEC dbo.sp_SalesBegin @Actor,@Stamp,@RequestId,'SalesLine',@Code,@payload,@replay OUTPUT;
  IF @replay=0
  BEGIN
   EXEC dbo.sp_SalesLock N'CafeManager:LoyaltyPolicy','Shared'; EXEC dbo.sp_SalesLock N'CafeManager:Recipes','Shared';
   DECLARE @resource nvarchar(255)=N'CafeManager:SalesOrder:'+CONVERT(nvarchar(10),@Code); EXEC dbo.sp_SalesLock @resource;
   EXEC dbo.sp_SalesCheckPayable @Code,@ExpectedRevision;
   IF @Action IS NULL OR @Action NOT IN('Add','Update','Delete') OR DATALENGTH(@Note)>200
    OR (@Action<>'Delete' AND (@Quantity IS NULL OR @Quantity NOT BETWEEN 1 AND 2147483647))
    THROW 52101,N'Dòng món không hợp lệ.',1;
   DECLARE @price decimal(18,0),@oldQuantity int=0,@oldNote nvarchar(100);
   IF @Action<>'Add'
   BEGIN
    SELECT @Product=MaSP,@price=GiaBan,@oldQuantity=SoLuong,@oldNote=GhiChu FROM dbo.CHITIET_HD
     WHERE MaHD=@Code AND MaDong=@LineId;
    IF @price IS NULL THROW 52101,N'Dòng không thuộc hóa đơn.',1;
   END;
   IF EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD=@Code AND TrangThai=N'Cho thanh toan')
    AND (@Action='Delete' OR (@Action='Update' AND (@Quantity<=@oldQuantity OR ISNULL(@Note,N'')<>ISNULL(@oldNote,N''))))
    THROW 52110,N'Đơn đã in tạm; chỉ thêm món hoặc tăng số lượng, không xóa/giảm/đổi ghi chú.',1;
   IF @Action='Add' OR (@Action='Update' AND @Quantity>@oldQuantity)
   BEGIN
    IF DATALENGTH(@Product)>5 OR NOT EXISTS(SELECT 1 FROM dbo.SANPHAM WITH(HOLDLOCK)
      WHERE MaSP=@Product AND TrangThai=N'Dang ban' AND EXISTS(SELECT 1 FROM dbo.CONGTHUC WHERE MaSP=@Product))
     THROW 52105,N'Món ngừng bán hoặc chưa có công thức.',1;
    IF @Action='Add' SELECT @price=GiaBan FROM dbo.SANPHAM WHERE MaSP=@Product;
   END;
   IF @Action<>'Delete' AND (SELECT CONVERT(decimal(38,0),TongTien)-CONVERT(decimal(38,0),@oldQuantity)*COALESCE(@price,0)
     +CONVERT(decimal(38,0),@Quantity)*@price FROM dbo.HOADON WHERE MaHD=@Code)>999999999999999999
    THROW 52101,N'Tổng tiền vượt giới hạn.',1;
   UPDATE dbo.HOADON SET GiamGia=0 WHERE MaHD=@Code;
   IF @Action='Add' INSERT dbo.CHITIET_HD(MaHD,MaSP,SoLuong,GiaBan,GhiChu)
    VALUES(@Code,@Product,CONVERT(int,@Quantity),@price,@Note);
   IF @Action='Update' UPDATE dbo.CHITIET_HD SET SoLuong=CONVERT(int,@Quantity),GhiChu=@Note
    WHERE MaHD=@Code AND MaDong=@LineId;
   IF @Action='Delete' DELETE dbo.CHITIET_HD WHERE MaHD=@Code AND MaDong=@LineId;
   EXEC dbo.sp_SalesRefreshDiscount @Code;
   EXEC dbo.sp_SalesFinish @Actor,@RequestId,'SalesLine',@Code,@payload;
  END;
  COMMIT; SELECT EntityCode,ResultRevision FROM dbo.G03OperationReceipt WHERE RequestId=@RequestId;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
ALTER PROCEDURE dbo.sp_SuaThongTinHoaDon @Actor int,@Stamp binary(8),@Code varchar(max),@ExpectedRevision binary(8),
 @RequestId uniqueidentifier,@Customer varchar(max)=NULL,@Partner varchar(max)=NULL,@Recipient nvarchar(max)=NULL,
 @Phone varchar(max)=NULL,@Address nvarchar(max)=NULL,@Ship decimal(38,18)=0 AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 52101,N'Không gọi ghi trong transaction bên ngoài.',1;
 BEGIN TRY
  BEGIN TRAN;
  SELECT @Recipient=NULLIF(LTRIM(RTRIM(@Recipient)),N''),@Phone=NULLIF(LTRIM(RTRIM(@Phone)),''),@Address=NULLIF(LTRIM(RTRIM(@Address)),N'');
  SELECT @Customer=NULLIF(LTRIM(RTRIM(@Customer)),''),@Partner=NULLIF(LTRIM(RTRIM(@Partner)),'');
  DECLARE @payload nvarchar(max)=(SELECT @Code c,@ExpectedRevision r,@Customer kh,@Partner dt,@Recipient n,
   @Phone ph,@Address a,@Ship ship FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES),@replay bit;
  EXEC dbo.sp_SalesBegin @Actor,@Stamp,@RequestId,'SalesHeader',@Code,@payload,@replay OUTPUT;
  IF @replay=0
  BEGIN
   EXEC dbo.sp_SalesLock N'CafeManager:LoyaltyPolicy','Shared';
   DECLARE @resource nvarchar(255)=N'CafeManager:SalesOrder:'+CONVERT(nvarchar(10),@Code); EXEC dbo.sp_SalesLock @resource;
   EXEC dbo.sp_SalesCheckPayable @Code,@ExpectedRevision;
   DECLARE @channel nvarchar(20),@tableCode varchar(5),@date date,@delivery nvarchar(20);
   SELECT @channel=LoaiDon,@tableCode=MaBan,@date=CONVERT(date,NgayLap),@delivery=TrangThaiGiao FROM dbo.HOADON WHERE MaHD=@Code;
   IF @channel=N'Giao hang' AND @delivery<>N'Cho giao' THROW 52102,N'Đã bắt đầu giao, không sửa thông tin đơn.',1;
   EXEC dbo.sp_SalesValidateHeader @channel,@tableCode,@Customer,@Partner,@Recipient,@Phone,@Address,@Ship,@date;
   UPDATE dbo.HOADON SET MaKH=@Customer,MaDT=@Partner,TenNguoiNhan=@Recipient,SDTNguoiNhan=@Phone,
    DiaChiGiao=@Address,PhiShip=CONVERT(decimal(18,0),@Ship),GiamGia=0 WHERE MaHD=@Code;
   EXEC dbo.sp_SalesRefreshDiscount @Code;
   EXEC dbo.sp_SalesFinish @Actor,@RequestId,'SalesHeader',@Code,@payload;
  END;
  COMMIT; SELECT EntityCode,ResultRevision FROM dbo.G03OperationReceipt WHERE RequestId=@RequestId;
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
   WHERE k.MaKM=h.MaKM AND CONVERT(date,h.NgayLap) BETWEEN k.NgayBD AND COALESCE(k.NgayKT,CONVERT(date,'99991231',112)))
   THEN 1 ELSE 0 END) Valid) v
 OUTER APPLY(SELECT TOP(1) s.Revision FROM dbo.PHIEN_BAN s WHERE s.MaPhien=h.MaPhien) sessionRow
 WHERE h.MaHD=@Code;
 SELECT d.MaDong,d.MaSP,s.TenSP,d.SoLuong,d.GiaBan,d.GhiChu,d.SoLuong*d.GiaBan Amount
 FROM dbo.CHITIET_HD d JOIN dbo.SANPHAM s ON s.MaSP=d.MaSP WHERE d.MaHD=@Code ORDER BY d.MaDong;
 SELECT t.MaTT,t.MaPT,p.TenPT,t.SoTien,t.MaThamChieu FROM dbo.CHITIET_THANHTOAN t
 JOIN dbo.PHUONGTHUCTT p ON p.MaPT=t.MaPT WHERE t.MaHD=@Code ORDER BY t.MaTT;
 SELECT MaKM Code,Name,[Percent],BaseAmount,Discount FROM dbo.fn_HoaDonUuDaiApDung(@Code) ORDER BY MaKM;
END;
GO
/* ===== 6. Preview/checkout/hóa đơn ===== */
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
   +CONVERT(binary(4),c.PromotionCount)+cd.Digest+ad.Digest)) PreviewToken
 FROM dbo.HOADON h CROSS JOIN dbo.LoyaltyPolicyState s
 LEFT JOIN dbo.KHACHHANG k ON k.MaKH=h.MaKH LEFT JOIN dbo.KHUYENMAI m ON m.MaKM=h.MaKM
 LEFT JOIN dbo.DOITACGIAOHANG pt ON pt.MaDT=h.MaDT
 CROSS APPLY dbo.fn_HoaDonGiamGia(h.MaHD) d
 CROSS APPLY(SELECT (SELECT COUNT(*) FROM dbo.fn_HoaDonUuDaiApDung(h.MaHD)) PromotionCount) c
 CROSS APPLY dbo.fn_PromotionCatalogDigest() cd CROSS APPLY dbo.fn_HoaDonUuDaiDigest(h.MaHD) ad
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
   p.Points,p.PreviewToken
  FROM dbo.HOADON h CROSS APPLY dbo.fn_CheckoutPreview(h.MaHD) p WHERE h.MaHD=@Code;
  SELECT MaKM Code,Name,[Percent],BaseAmount,Discount FROM dbo.fn_HoaDonUuDaiApDung(@Code) ORDER BY MaKM;
  COMMIT;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
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
  SELECT d.MaDong,d.MaSP,s.TenSP,d.SoLuong,d.GiaBan,d.GhiChu,d.SoLuong*d.GiaBan Amount FROM dbo.CHITIET_HD d
   JOIN dbo.SANPHAM s ON s.MaSP=d.MaSP WHERE d.MaHD=@Code ORDER BY d.MaDong;
  SELECT t.MaTT,t.MaPT,p.TenPT,t.SoTien,t.MaThamChieu FROM dbo.CHITIET_THANHTOAN t
   JOIN dbo.PHUONGTHUCTT p ON p.MaPT=t.MaPT WHERE t.MaHD=@Code ORDER BY t.MaTT;
  SELECT MaKM Code,Name,[Percent],BaseAmount,Discount FROM dbo.fn_HoaDonUuDaiApDung(@Code) ORDER BY MaKM;
  SELECT s.Kind,s.Source,s.PercentDiscount,s.TierPercent,s.PromotionPercent,s.TierDiscount,s.PromotionDiscount,
   s.PromotionCount,COALESCE((SELECT SUM(SoDiem) FROM dbo.LICHSU_DIEM WHERE MaHD=@Code AND Loai=N'Cong'),0) Points,
   s.RecordedAt
  FROM dbo.HoaDonUuDaiSnapshot s WHERE s.MaHD=@Code;
  COMMIT;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
DROP FUNCTION dbo.fn_DiscountPreview;
GO
GRANT EXECUTE ON dbo.sp_DiscountPreview TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_PromotionWrite TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_SalesCheckoutPreview TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_ThanhToanHoaDon TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_InvoiceDetail TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_TaoHoaDon TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_TaoHoaDonTuNhap TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_SuaDongHoaDon TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_SuaThongTinHoaDon TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_SalesReadCore TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_SalesValidateHeader TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_PromotionList TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_PromotionGet TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_PromotionCreate TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_PromotionUpdate TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_PromotionDelete TO CafeManagerApp;
GO


