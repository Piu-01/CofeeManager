-- 044: Mỗi dòng món chỉ nhận một khuyến mãi giảm; chọn tổ hợp rời nhau có tổng
-- giảm lớn nhất. Hạng thành viên tính trên hóa đơn sau khuyến mãi (thay luật
-- cộng dồn additive của 038/039).
-- Chạy sau 043 bằng tools/database/Apply-Migration.ps1 -Version 44.
-- Không backfill snapshot/lịch sử hóa đơn đã chốt.
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF @@TRANCOUNT=0 OR NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=43)
    THROW 54401,N'Migration 043 bắt buộc.',1;
IF OBJECT_ID(N'dbo.KHUYENMAI',N'U') IS NULL OR OBJECT_ID(N'dbo.KHUYENMAI_MON',N'U') IS NULL
 OR OBJECT_ID(N'dbo.KHUYENMAI_MON_TANG',N'U') IS NULL OR OBJECT_ID(N'dbo.HoaDonUuDaiApDung',N'U') IS NULL
 OR OBJECT_ID(N'dbo.HoaDonUuDaiSnapshot',N'U') IS NULL
 OR OBJECT_ID(N'dbo.fn_UuDaiCap',N'TF') IS NULL OR OBJECT_ID(N'dbo.fn_UuDaiTong',N'TF') IS NULL
 OR OBJECT_ID(N'dbo.fn_CheckoutPreview',N'IF') IS NULL OR OBJECT_ID(N'dbo.sp_DiscountPreview',N'P') IS NULL
 OR OBJECT_ID(N'dbo.sp_ThanhToanHoaDon',N'P') IS NULL OR OBJECT_ID(N'dbo.sp_InvoiceDetail',N'P') IS NULL
 OR OBJECT_ID(N'dbo.sp_SalesReadCore',N'P') IS NULL
    THROW 54401,N'Thiếu object khuyến mãi/bán hàng của 043; cần đối chiếu trước nâng cấp.',1;
GO
/* ===== 1. Hàm chọn tổ hợp: mỗi dòng tối đa một KM, tổng giảm lớn nhất =====
   Ứng viên: KM Giam đang chạy, đúng ngày/đối tượng, % > 0; KM legacy (HOADON.MaKM)
   được thêm như ứng viên phạm vi ALL, chỉ xét hiệu lực ngày như 039.
   Mỗi nhóm xung đột (các KM cùng phủ ít nhất một dòng) giải độc lập:
   - <= 15 KM: duyệt toàn bộ tập con rời nhau, chọn tổng ROUND(amount*pct/100) lớn
     nhất; hòa chọn ít KM hơn rồi theo chuỗi mã tăng dần.
   - > 15 KM: tham lam tất định theo phần giảm riêng, thử thay thế một lượt; giới hạn
     cấu hình có ghi trong tài liệu, không cộng dồn sai luật.
   Kết quả tất định, không phụ thuộc thứ tự đọc bảng. */
CREATE FUNCTION dbo.fn_UuDaiChon(@BusinessDate date,@Customer char(5),@LegacyPromotion char(10),@LinesJson nvarchar(max))
RETURNS @result TABLE(No int NOT NULL,MaKM char(10) NOT NULL,[Percent] int NOT NULL,Amount decimal(18,0) NOT NULL)
AS
BEGIN
 IF @LinesJson IS NULL OR LTRIM(RTRIM(@LinesJson))='' SET @LinesJson=N'[]';
 DECLARE @line TABLE(No int IDENTITY(1,1) PRIMARY KEY,MaSP char(5) NULL,Amount decimal(18,0) NOT NULL);
 INSERT @line(MaSP,Amount)
 SELECT TRY_CONVERT(char(5),JSON_VALUE(v.value,'$.Product')),CONVERT(decimal(18,0),JSON_VALUE(v.value,'$.Amount'))
 FROM OPENJSON(@LinesJson) v WHERE JSON_VALUE(v.value,'$.Amount') IS NOT NULL;
 IF NOT EXISTS(SELECT 1 FROM @line) RETURN;

 DECLARE @cand TABLE(Id int IDENTITY(0,1) PRIMARY KEY,MaKM char(10) NOT NULL,Pct int NOT NULL,
  IsAll bit NOT NULL,Comp int NULL);
 INSERT @cand(MaKM,Pct,IsAll)
 SELECT k.MaKM,k.PhanTramGiam,CONVERT(bit,CASE WHEN k.PhamVi='ITEM' THEN 0 ELSE 1 END)
 FROM dbo.KHUYENMAI k
 WHERE k.LoaiUuDai='Giam' AND k.MaKM<>COALESCE(@LegacyPromotion,'') AND k.TrangThai='DangChay'
  AND k.PhanTramGiam>0
  AND dbo.fn_NgayTrongKy(@BusinessDate,k.NgayBD,k.NgayKT)=1
  AND dbo.fn_KhachThoaDoiTuong(k.DoiTuongKhach,@Customer,k.MaKM)=1;
 -- Legacy: chỉ xét hiệu lực ngày như 039, luôn phạm vi ALL, không ép cộng ngoài tổ hợp.
 IF @LegacyPromotion IS NOT NULL
  AND NOT EXISTS(SELECT 1 FROM @cand WHERE MaKM=@LegacyPromotion)
  INSERT @cand(MaKM,Pct,IsAll)
  SELECT k.MaKM,k.PhanTramGiam,1 FROM dbo.KHUYENMAI k
  WHERE k.MaKM=@LegacyPromotion AND k.PhanTramGiam>0
   AND dbo.fn_NgayTrongKy(@BusinessDate,k.NgayBD,k.NgayKT)=1;

 DECLARE @cov TABLE(Id int NOT NULL,No int NOT NULL,Amt decimal(18,0) NOT NULL,
  W decimal(18,0) NOT NULL,PRIMARY KEY(Id,No));
 INSERT @cov(Id,No,Amt,W)
 SELECT c.Id,l.No,l.Amount,
  CONVERT(decimal(18,0),ROUND(CONVERT(decimal(28,4),l.Amount)*c.Pct/100,0))
 FROM @cand c JOIN @line l ON c.IsAll=1
   OR (l.MaSP IS NOT NULL AND EXISTS(SELECT 1 FROM dbo.KHUYENMAI_MON m
        WHERE m.MaKM=c.MaKM AND m.MaSP=l.MaSP))
 WHERE l.Amount>0;
 DELETE c FROM @cand c WHERE NOT EXISTS(SELECT 1 FROM @cov v WHERE v.Id=c.Id);
 IF NOT EXISTS(SELECT 1 FROM @cand) RETURN;

 -- Tách nhóm xung đột: dựng cạnh hai chiều rồi lan nhãn nhóm.
 DECLARE @edge TABLE(A int NOT NULL,B int NOT NULL,PRIMARY KEY(A,B));
 INSERT @edge(A,B)
 SELECT DISTINCT a.Id,b.Id FROM @cov a JOIN @cov b ON b.No=a.No AND a.Id<>b.Id;

 DECLARE @comp int=0,@cid int;
 WHILE 1=1
 BEGIN
  SELECT @cid=MIN(Id) FROM @cand WHERE Comp IS NULL;
  IF @cid IS NULL BREAK;
  SET @comp=@comp+1;
  UPDATE @cand SET Comp=@comp WHERE Id=@cid;
  WHILE 1=1
  BEGIN
   UPDATE c SET Comp=@comp
   FROM @cand c
   WHERE c.Comp IS NULL AND EXISTS(
     SELECT 1 FROM @edge e JOIN @cand d ON d.Id=e.A AND d.Comp=@comp
     WHERE e.B=c.Id);
   IF @@ROWCOUNT=0 BREAK;
  END;
 END;

 DECLARE @ids TABLE(Ord int PRIMARY KEY,Id int NOT NULL UNIQUE,MaKM char(10) NOT NULL,
  Pct int NOT NULL,W decimal(18,0) NOT NULL,Bit bigint NOT NULL);
 DECLARE @cm TABLE(Ord int PRIMARY KEY,Mask bigint NOT NULL);
 DECLARE @sel TABLE(Id int PRIMARY KEY);
 DECLARE @pick TABLE(Id int PRIMARY KEY);
 DECLARE @todo TABLE(Seq int IDENTITY(1,1) PRIMARY KEY,Ord int NOT NULL);
 DECLARE @c int=1,@m int,@i int,@seq int,@cntTodo int;
 DECLARE @mask bigint,@limit bigint,@bit bigint,@bestMask bigint,@bestW decimal(18,0),
  @bestCnt int,@bestKey nvarchar(400),@ok bit,@w decimal(18,0),@cnt int,@key nvarchar(400);
 DECLARE @uid int,@uw decimal(18,0),@cw decimal(18,0);

 WHILE @c<=@comp
 BEGIN
  DELETE @ids; DELETE @cm; DELETE @pick; DELETE @todo;
  INSERT @ids(Ord,Id,MaKM,Pct,W,Bit)
  SELECT ROW_NUMBER() OVER(ORDER BY c.MaKM)-1,c.Id,c.MaKM,c.Pct,
   (SELECT COALESCE(SUM(v.W),0) FROM @cov v WHERE v.Id=c.Id),
   CONVERT(bigint,POWER(CONVERT(float,2),ROW_NUMBER() OVER(ORDER BY c.MaKM)-1))
  FROM @cand c WHERE c.Comp=@c;
  SET @m=(SELECT COUNT(*) FROM @ids);
  IF @m>0
  BEGIN
   INSERT @cm(Ord,Mask)
   SELECT x.Ord,COALESCE(SUM(y.Bit),0)
   FROM @edge e JOIN @ids x ON x.Id=e.A JOIN @ids y ON y.Id=e.B
   GROUP BY x.Ord;

   IF @m<=15
   BEGIN
    SET @bestMask=NULL; SET @bestW=-1; SET @bestCnt=0; SET @bestKey=NULL;
    SET @limit=1; SET @i=0;
    WHILE @i<@m BEGIN SET @limit=@limit*2; SET @i=@i+1; END;
    SET @mask=0;
    WHILE @mask<@limit
    BEGIN
     SET @ok=1; SET @w=0; SET @cnt=0; SET @key=N'';
     SET @i=0;
     WHILE @i<@m AND @ok=1
     BEGIN
      SELECT @bit=Bit FROM @ids WHERE Ord=@i;
      IF (@mask & @bit)<>0
      BEGIN
       IF EXISTS(SELECT 1 FROM @cm WHERE Ord=@i AND (@mask & Mask)<>0) SET @ok=0;
       ELSE
       BEGIN
        SET @w=@w+(SELECT W FROM @ids WHERE Ord=@i);
        SET @key=@key+(SELECT MaKM FROM @ids WHERE Ord=@i);
        SET @cnt=@cnt+1;
       END;
      END;
      SET @i=@i+1;
     END;
     IF @ok=1 AND (@bestMask IS NULL OR @w>@bestW
       OR (@w=@bestW AND (@cnt<@bestCnt OR (@cnt=@bestCnt AND @key<@bestKey))))
      SELECT @bestMask=@mask,@bestW=@w,@bestCnt=@cnt,@bestKey=@key;
     SET @mask=@mask+1;
    END;
    IF @bestMask IS NOT NULL
     INSERT @sel(Id) SELECT Id FROM @ids WHERE (@bestMask & Bit)<>0;
   END
   ELSE
   BEGIN
    -- Tham lam: KM có phần giảm riêng cao trước, bỏ KM xung đột đã chọn.
    INSERT @todo(Ord) SELECT Ord FROM @ids ORDER BY W DESC,MaKM;
    SET @seq=1; SET @cntTodo=(SELECT COUNT(*) FROM @todo);
    WHILE @seq<=@cntTodo
    BEGIN
     SET @uid=(SELECT Id FROM @ids WHERE Ord=(SELECT Ord FROM @todo WHERE Seq=@seq));
     IF NOT EXISTS(SELECT 1 FROM @pick p JOIN @edge e ON e.A=p.Id AND e.B=@uid)
      INSERT @pick(Id) VALUES(@uid);
     SET @seq=@seq+1;
    END;
    -- Một lượt thay thế nếu KM ngoài tập lớn hơn tổng các KM xung đột đang chọn.
    SET @seq=1;
    WHILE @seq<=@cntTodo
    BEGIN
     SET @uid=(SELECT Id FROM @ids WHERE Ord=(SELECT Ord FROM @todo WHERE Seq=@seq));
     IF NOT EXISTS(SELECT 1 FROM @pick WHERE Id=@uid)
     BEGIN
      SET @uw=(SELECT W FROM @ids WHERE Id=@uid);
      SET @cw=(SELECT COALESCE(SUM(x.W),0) FROM @pick p JOIN @ids x ON x.Id=p.Id
        WHERE EXISTS(SELECT 1 FROM @edge e WHERE e.A=p.Id AND e.B=@uid));
      IF @uw>@cw
      BEGIN
       DELETE p FROM @pick p WHERE EXISTS(SELECT 1 FROM @edge e WHERE e.A=p.Id AND e.B=@uid);
       INSERT @pick(Id) VALUES(@uid);
      END;
     END;
     SET @seq=@seq+1;
    END;
    INSERT @sel(Id) SELECT Id FROM @pick;
   END;
  END;
  SET @c=@c+1;
 END;

 INSERT @result(No,MaKM,[Percent],Amount)
 SELECT v.No,c.MaKM,c.Pct,v.Amt
 FROM @sel s JOIN @cand c ON c.Id=s.Id JOIN @cov v ON v.Id=c.Id
 ORDER BY v.No,c.MaKM;
 RETURN;
END;
GO
/* ===== 2. fn_UuDaiCap giữ chữ ký, chỉ trả cặp của tổ hợp được chọn ===== */
ALTER FUNCTION dbo.fn_UuDaiCap(@BusinessDate date,@Customer char(5),@LegacyPromotion char(10),@LinesJson nvarchar(max))
RETURNS @result TABLE(MaKM char(10) NOT NULL,[Percent] int NOT NULL,No int NOT NULL,Amount decimal(18,0) NOT NULL)
AS
BEGIN
 INSERT @result(MaKM,[Percent],No,Amount)
 SELECT MaKM,[Percent],No,Amount
 FROM dbo.fn_UuDaiChon(@BusinessDate,@Customer,@LegacyPromotion,@LinesJson);
 RETURN;
END;
GO
/* ===== 3. fn_UuDaiDong: phần KM của dòng, KHÔNG cộng hạng =====
   Hạng tính một lần trên tổng còn lại ở fn_UuDaiTong. */
ALTER FUNCTION dbo.fn_UuDaiDong(@BusinessDate date,@Customer char(5),@LegacyPromotion char(10),@LinesJson nvarchar(max))
RETURNS @result TABLE(No int NOT NULL,Amount decimal(18,0) NOT NULL,[Percent] int NOT NULL,Discount decimal(18,0) NOT NULL)
AS
BEGIN
 IF @LinesJson IS NULL OR LTRIM(RTRIM(@LinesJson))='' SET @LinesJson=N'[]';
 DECLARE @line TABLE(No int IDENTITY(1,1) NOT NULL,Amount decimal(18,0) NOT NULL);
 INSERT @line(Amount) SELECT CONVERT(decimal(18,0),JSON_VALUE(v.value,'$.Amount'))
 FROM OPENJSON(@LinesJson) v WHERE JSON_VALUE(v.value,'$.Amount') IS NOT NULL;
 INSERT @result(No,Amount,[Percent],Discount)
 SELECT l.No,l.Amount,COALESCE(p.Pct,0),
  CONVERT(decimal(18,0),ROUND(CONVERT(decimal(28,4),l.Amount)*COALESCE(p.Pct,0)/100,0))
 FROM @line l LEFT JOIN(
   SELECT No,MAX([Percent]) Pct FROM dbo.fn_UuDaiCap(@BusinessDate,@Customer,@LegacyPromotion,@LinesJson)
   GROUP BY No) p ON p.No=l.No;
 RETURN;
END;
GO
/* ===== 4. fn_UuDaiTong: KM trước, hạng trên phần còn lại ===== */
ALTER FUNCTION dbo.fn_UuDaiTong(@BusinessDate date,@Customer char(5),@LegacyPromotion char(10),@LinesJson nvarchar(max))
RETURNS @result TABLE(TierPercent int NOT NULL,PromotionPercent int NOT NULL,
 TierDiscount decimal(18,0) NOT NULL,PromotionDiscount decimal(18,0) NOT NULL,Discount decimal(18,0) NOT NULL)
AS
BEGIN
 IF @LinesJson IS NULL OR LTRIM(RTRIM(@LinesJson))='' SET @LinesJson=N'[]';
 DECLARE @d TABLE(No int NOT NULL,Amount decimal(18,0) NOT NULL,[Percent] int NOT NULL,Discount decimal(18,0) NOT NULL);
 INSERT @d SELECT No,Amount,[Percent],Discount
 FROM dbo.fn_UuDaiDong(@BusinessDate,@Customer,@LegacyPromotion,@LinesJson);
 DECLARE @total decimal(18,0)=(SELECT COALESCE(SUM(Amount),0) FROM @d);
 DECLARE @promoRaw decimal(38,0)=(SELECT COALESCE(SUM(Discount),0) FROM @d);
 DECLARE @promoDiscount decimal(18,0)=CASE WHEN @promoRaw>CONVERT(decimal(38,0),@total)
   THEN @total ELSE CONVERT(decimal(18,0),@promoRaw) END;
 DECLARE @tier int=0;
 IF @Customer IS NOT NULL
  SELECT @tier=COALESCE(h.PhanTramGiam,0) FROM dbo.KHACHHANG k
   JOIN dbo.HANGTHANHVIEN h ON h.MaHang=k.MaHang WHERE k.MaKH=@Customer;
 DECLARE @tierDiscount decimal(18,0)=CASE WHEN @tier=0 THEN 0
   ELSE CONVERT(decimal(18,0),ROUND(CONVERT(decimal(28,4),@total-@promoDiscount)*@tier/100,0)) END;
 DECLARE @discount decimal(18,0)=@promoDiscount+@tierDiscount;
 DECLARE @promoPercent int=CASE WHEN @total>0
   THEN CONVERT(int,ROUND(CONVERT(decimal(28,4),@promoDiscount)*100/@total,0)) ELSE 0 END;
 INSERT @result VALUES(@tier,@promoPercent,@tierDiscount,@promoDiscount,@discount);
 RETURN;
END;
GO
/* ===== 5. Chia phần khuyến mãi cho từng KM: giữ cách phân bổ 039 ===== */
ALTER FUNCTION dbo.fn_UuDaiChia(@BusinessDate date,@Customer char(5),@LegacyPromotion char(10),@LinesJson nvarchar(max))
RETURNS @result TABLE(MaKM char(10) NOT NULL,[Percent] int NOT NULL,BaseAmount decimal(18,0) NOT NULL,Discount decimal(18,0) NOT NULL)
AS
BEGIN
 DECLARE @promoTotal decimal(18,0)=(SELECT PromotionDiscount
   FROM dbo.fn_UuDaiTong(@BusinessDate,@Customer,@LegacyPromotion,@LinesJson));
 DECLARE @rawTotal decimal(28,4)=(SELECT COALESCE(SUM(RawShare),0)
   FROM dbo.fn_UuDaiApDung(@BusinessDate,@Customer,@LegacyPromotion,@LinesJson));
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
/* ===== 6. Snapshot: cho phép công thức tuần tự, không backfill dòng cũ ===== */
ALTER TABLE dbo.HoaDonUuDaiSnapshot DROP CONSTRAINT CK_DiscountSnapshot_Known;
GO
ALTER TABLE dbo.HoaDonUuDaiSnapshot WITH NOCHECK ADD CONSTRAINT CK_DiscountSnapshot_Known CHECK(
 (Kind='Checkout' AND Source IS NOT NULL AND PercentDiscount IS NOT NULL
  AND TierPercent IS NOT NULL AND PromotionPercent IS NOT NULL
  AND TierPercent BETWEEN 0 AND 100 AND PromotionPercent BETWEEN 0 AND 100
  AND PolicyRevision IS NOT NULL
  AND (TierDiscount IS NULL OR PromotionDiscount IS NULL OR Discount=TierDiscount+PromotionDiscount))
 OR (Kind='LegacyUnknown' AND Source IS NULL AND PercentDiscount IS NULL));
GO
/* ===== 7. Preview: PercentDiscount là mức giảm thực tế sau hai bước ===== */
ALTER FUNCTION dbo.fn_CheckoutPreview(@Code char(10)) RETURNS TABLE AS RETURN
(
 SELECT CASE WHEN k.MaHang IS NOT NULL AND c.PromotionCount>0 THEN 'TierAndPromotion'
   WHEN c.PromotionCount>0 THEN 'Promotion' WHEN k.MaHang IS NOT NULL THEN 'Tier' ELSE 'None' END Source,
  k.MaHang Tier,m.MaKM Promotion,
  CASE WHEN h.TongTien>0
   THEN CONVERT(int,ROUND(CONVERT(decimal(28,4),d.Discount)*100/h.TongTien,0))
   ELSE 0 END PercentDiscount,
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
/* ===== 8. sp_DiscountPreview: % hiển thị theo mức giảm thực tế ===== */
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
   CASE WHEN @total>0 THEN CONVERT(int,ROUND(CONVERT(decimal(28,4),@discount)*100/@total,0)) ELSE 0 END PercentDiscount,
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
/* ===== 9. Grant ===== */
GRANT EXECUTE ON dbo.sp_DiscountPreview TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_ThanhToanHoaDon TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_InvoiceDetail TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_SalesReadCore TO CafeManagerApp;
GO
