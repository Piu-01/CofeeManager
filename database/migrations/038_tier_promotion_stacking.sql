-- Cộng dồn ưu đãi hạng + tối đa 1 khuyến mãi mỗi hóa đơn (chốt 21/11/2026).
-- Giảm = ROUND(tiền món × min(100, %hạng + %KM) / 100, 0); ship không giảm.
-- Chỉ đổi cách tính preview/checkout; không đổi điểm, ngưỡng hạng, không backfill
-- snapshot hóa đơn lịch sử (cột mới để NULL cho dòng cũ).
SET NOCOUNT ON; SET XACT_ABORT ON;
IF @@TRANCOUNT=0 OR NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=37)
 THROW 52307,N'Cần schema037 và runner migration.',1;
IF OBJECT_ID(N'dbo.HoaDonUuDaiSnapshot',N'U') IS NULL OR OBJECT_ID(N'dbo.HANGTHANHVIEN',N'U') IS NULL
 OR OBJECT_ID(N'dbo.fn_DiscountPreview',N'IF') IS NULL OR OBJECT_ID(N'dbo.fn_CheckoutPreview',N'IF') IS NULL
 OR OBJECT_ID(N'dbo.sp_DiscountPreview',N'P') IS NULL OR OBJECT_ID(N'dbo.sp_SalesCheckoutPreview',N'P') IS NULL
 OR OBJECT_ID(N'dbo.sp_ThanhToanHoaDon',N'P') IS NULL OR OBJECT_ID(N'dbo.sp_InvoiceDetail',N'P') IS NULL
 THROW 52307,N'Thiếu object ưu đãi/snapshot; cần đối chiếu trước nâng cấp.',1;
GO
ALTER FUNCTION dbo.fn_DiscountPreview(@Money decimal(18,0),@Ship decimal(18,0),@BusinessDate date,@Customer char(5),@Promotion char(10))
RETURNS TABLE AS RETURN
(
 SELECT y.Source,y.Tier,y.Promotion,y.TierPercent,y.PromotionPercent,y.PercentDiscount,
 CONVERT(decimal(18,0),ROUND(CONVERT(decimal(28,4),@Money)*CONVERT(decimal(5,2),y.PercentDiscount)/100,0)) Discount,
 CONVERT(decimal(38,0),@Money)+@Ship-CONVERT(decimal(18,0),ROUND(CONVERT(decimal(28,4),@Money)*CONVERT(decimal(5,2),y.PercentDiscount)/100,0)) Payable
 FROM (SELECT x.Source,x.Tier,x.Promotion,x.TierPercent,x.PromotionPercent,
        CASE WHEN x.TierPercent+x.PromotionPercent>100 THEN 100 ELSE x.TierPercent+x.PromotionPercent END PercentDiscount
       FROM (SELECT CASE WHEN p.MaKM IS NOT NULL AND k.MaHang IS NOT NULL THEN 'TierAndPromotion'
                         WHEN p.MaKM IS NOT NULL THEN 'Promotion'
                         WHEN k.MaHang IS NOT NULL THEN 'Tier' ELSE 'None' END Source,
              k.MaHang Tier,p.MaKM Promotion,
              COALESCE(CASE WHEN @Customer IS NOT NULL THEN h.PhanTramGiam END,0) TierPercent,
              COALESCE(CASE WHEN @Promotion IS NOT NULL THEN p.PhanTramGiam END,0) PromotionPercent
             FROM (VALUES(1)) seed(n)
             LEFT JOIN dbo.KHACHHANG k ON k.MaKH=@Customer
             LEFT JOIN dbo.HANGTHANHVIEN h ON h.MaHang=k.MaHang
             LEFT JOIN dbo.KHUYENMAI p ON p.MaKM=@Promotion AND @BusinessDate BETWEEN p.NgayBD AND p.NgayKT) x) y
);
GO
ALTER PROCEDURE dbo.sp_DiscountPreview @Actor int,@Stamp binary(8),@Money decimal(38,18),@Ship decimal(38,18),@BusinessDate date,@Customer varchar(max)=NULL,@Promotion varchar(max)=NULL AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 BEGIN TRY
  BEGIN TRAN; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,CASH';
  DECLARE @lock int; EXEC @lock=sys.sp_getapplock @Resource=N'CafeManager:LoyaltyPolicy',@LockMode='Shared',@LockOwner='Transaction',@LockTimeout=10000;
  IF @lock<0 THROW 52004,N'Chính sách đang thay đổi.',1;
  IF @Money IS NULL OR @Ship IS NULL OR @Money<0 OR @Money>999999999999999999 OR ROUND(@Money,0)<>@Money OR @Ship<0 OR @Ship>999999999999999999 OR ROUND(@Ship,0)<>@Ship OR @BusinessDate IS NULL
   OR (@Customer IS NOT NULL AND (DATALENGTH(@Customer)>5 OR NOT EXISTS(SELECT 1 FROM dbo.KHACHHANG WITH(HOLDLOCK) WHERE MaKH=@Customer)))
   OR (@Promotion IS NOT NULL AND (DATALENGTH(@Promotion)>10 OR NOT EXISTS(SELECT 1 FROM dbo.KHUYENMAI WITH(HOLDLOCK) WHERE MaKM=@Promotion AND @BusinessDate BETWEEN NgayBD AND NgayKT))) THROW 52001,N'Dữ liệu preview hoặc chương trình không hiệu lực.',1;
  DECLARE @result TABLE(Source varchar(20),Tier char(5),Promotion char(10),TierPercent int,PromotionPercent int,PercentDiscount int,Discount decimal(18,0),Payable decimal(38,0));
  INSERT @result(Source,Tier,Promotion,TierPercent,PromotionPercent,PercentDiscount,Discount,Payable)
   SELECT Source,Tier,Promotion,TierPercent,PromotionPercent,PercentDiscount,Discount,Payable
   FROM dbo.fn_DiscountPreview(CONVERT(decimal(18,0),@Money),CONVERT(decimal(18,0),@Ship),@BusinessDate,CONVERT(char(5),@Customer),CONVERT(char(10),@Promotion));
  IF EXISTS(SELECT 1 FROM @result WHERE Payable>999999999999999999) THROW 52001,N'Phải trả vượt giới hạn tiền.',1;
  SELECT r.Source,r.Tier,r.Promotion,r.PercentDiscount,r.TierPercent,r.PromotionPercent,r.Discount,CONVERT(decimal(18,0),r.Payable) Payable,SYSDATETIME() ReadAt,s.Revision PolicyRevision FROM @result r CROSS JOIN dbo.LoyaltyPolicyState s;
  COMMIT;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
ALTER FUNCTION dbo.fn_CheckoutPreview(@Code char(10)) RETURNS TABLE AS RETURN
(
 SELECT p.Source,p.Tier,p.Promotion,p.PercentDiscount,p.TierPercent,p.PromotionPercent,p.Discount,p.Payable,
 COALESCE(d.ChietKhau,CONVERT(decimal(5,2),0)) PartnerPercent,
 CONVERT(decimal(18,0),ROUND(CONVERT(decimal(28,4),h.TongTien-p.Discount)*COALESCE(d.ChietKhau,0)/100,0)) PartnerFee,
 CONVERT(bigint,FLOOR((h.TongTien-p.Discount)/1000)) Points,
 s.Revision PolicyRevision,k.Revision CustomerRevision,m.Revision PromotionRevision,
 CONVERT(binary(32),HASHBYTES('SHA2_256',
 CONVERT(varbinary(max),h.Revision)+s.Revision+COALESCE(k.Revision,0x0000000000000000)+COALESCE(m.Revision,0x0000000000000000)+COALESCE(d.CatalogRevision,0x0000000000000000)
 +CONVERT(binary(8),CONVERT(bigint,p.Discount))+CONVERT(binary(8),CONVERT(bigint,p.Payable))+CONVERT(binary(4),p.PercentDiscount)
 +CONVERT(binary(4),p.TierPercent)+CONVERT(binary(4),p.PromotionPercent))) PreviewToken
 FROM dbo.HOADON h CROSS JOIN dbo.LoyaltyPolicyState s
 LEFT JOIN dbo.KHACHHANG k ON k.MaKH=h.MaKH LEFT JOIN dbo.KHUYENMAI m ON m.MaKM=h.MaKM LEFT JOIN dbo.DOITACGIAOHANG d ON d.MaDT=h.MaDT
 CROSS APPLY dbo.fn_DiscountPreview(h.TongTien,h.PhiShip,CONVERT(date,h.NgayLap),h.MaKH,h.MaKM) p WHERE h.MaHD=@Code
);
GO
ALTER TABLE dbo.HoaDonUuDaiSnapshot ADD TierPercent int NULL,PromotionPercent int NULL;
GO
DECLARE @sourceCheck sysname;
SELECT @sourceCheck=c.name FROM sys.check_constraints c
 WHERE c.parent_object_id=OBJECT_ID(N'dbo.HoaDonUuDaiSnapshot')
   AND c.parent_column_id=COLUMNPROPERTY(OBJECT_ID(N'dbo.HoaDonUuDaiSnapshot'),'Source','ColumnId');
IF @sourceCheck IS NOT NULL
BEGIN
 DECLARE @dropCheck nvarchar(max)=N'ALTER TABLE dbo.HoaDonUuDaiSnapshot DROP CONSTRAINT '+QUOTENAME(@sourceCheck)+N';';
 EXEC sys.sp_executesql @dropCheck;
END;
ALTER TABLE dbo.HoaDonUuDaiSnapshot DROP CONSTRAINT CK_DiscountSnapshot_Known;
ALTER TABLE dbo.HoaDonUuDaiSnapshot ADD CONSTRAINT CK_DiscountSnapshot_Source
 CHECK(Source IN('None','Tier','Promotion','TierAndPromotion') OR Source IS NULL);
-- Dòng Checkout cũ đã ghi trước 038 không có breakdown; thêm NOCHECK để không
-- backfill lịch sử, nhưng mọi dòng Checkout mới vẫn bị kiểm đủ theo biểu thức.
ALTER TABLE dbo.HoaDonUuDaiSnapshot WITH NOCHECK ADD CONSTRAINT CK_DiscountSnapshot_Known CHECK(
 (Kind='Checkout' AND Source IS NOT NULL AND PercentDiscount IS NOT NULL
  AND TierPercent IS NOT NULL AND PromotionPercent IS NOT NULL
  AND TierPercent BETWEEN 0 AND 100 AND PromotionPercent BETWEEN 0 AND 100
  AND PercentDiscount=CASE WHEN TierPercent+PromotionPercent>100 THEN 100 ELSE TierPercent+PromotionPercent END
  AND PolicyRevision IS NOT NULL)
 OR (Kind='LegacyUnknown' AND Source IS NULL AND PercentDiscount IS NULL));
GO
ALTER PROCEDURE dbo.sp_SalesCheckoutPreview @Actor int,@Stamp binary(8),@Code varchar(max),@ExpectedRevision binary(8) AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 BEGIN TRY
  BEGIN TRAN; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,CASH'; EXEC dbo.sp_SalesLock N'CafeManager:LoyaltyPolicy','Shared';
  DECLARE @resource nvarchar(255)=N'CafeManager:SalesOrder:'+CONVERT(nvarchar(10),@Code); EXEC dbo.sp_SalesLock @resource,'Shared';
  EXEC dbo.sp_SalesCheckPayable @Code,@ExpectedRevision;
  IF EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD=@Code AND TongTien<=0) THROW 51102,N'Hóa đơn phải có món.',1;
  IF EXISTS(SELECT 1 FROM dbo.HOADON h WHERE h.MaHD=@Code AND h.MaKM IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.KHUYENMAI WHERE MaKM=h.MaKM AND CONVERT(date,h.NgayLap) BETWEEN NgayBD AND NgayKT)) THROW 51105,N'Khuyến mãi không hợp lệ.',1;
  SELECT h.MaHD Code,h.Revision,h.TongTien Total,h.PhiShip Ship,p.Discount,p.Payable,p.Source,p.PercentDiscount,p.TierPercent,p.PromotionPercent,p.PartnerPercent,p.PartnerFee,p.Points,p.PreviewToken
  FROM dbo.HOADON h CROSS APPLY dbo.fn_CheckoutPreview(h.MaHD) p WHERE h.MaHD=@Code;
  COMMIT;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
ALTER PROCEDURE dbo.sp_ThanhToanHoaDon @Actor int,@Stamp binary(8),@Code varchar(max),@ExpectedRevision binary(8),@RequestId uniqueidentifier,@ExpectedPreviewToken binary(32),@ThanhToan dbo.DanhSachThanhToan READONLY AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 52101,N'Không gọi ghi trong transaction bên ngoài.',1;
 BEGIN TRY
  BEGIN TRAN;
  DECLARE @payments nvarchar(max)=(SELECT MaPT,SoTien,MaThamChieu FROM @ThanhToan ORDER BY MaPT,MaThamChieu,SoTien FOR JSON PATH,INCLUDE_NULL_VALUES);
  DECLARE @payload nvarchar(max)=(SELECT @Code c,@ExpectedRevision r,@ExpectedPreviewToken p,@payments a FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES),@replay bit;
  EXEC dbo.sp_SalesBegin @Actor,@Stamp,@RequestId,'SalesCheckout',@Code,@payload,@replay OUTPUT;
  IF @replay=0
  BEGIN
   EXEC dbo.sp_SalesLock N'CafeManager:LoyaltyPolicy','Shared'; EXEC dbo.sp_SalesLock N'CafeManager:Recipes','Shared';
   DECLARE @resource nvarchar(255)=N'CafeManager:SalesOrder:'+CONVERT(nvarchar(10),@Code); EXEC dbo.sp_SalesLock @resource;
   EXEC dbo.sp_SalesCheckPayable @Code,@ExpectedRevision;
   DECLARE @customer char(5),@total decimal(18,0),@date datetime2(0),@created datetime2(0),@channel nvarchar(20),@recipient nvarchar(100),@phone varchar(15),@address nvarchar(250);
   SELECT @customer=MaKH,@total=TongTien,@created=NgayLap,@channel=LoaiDon,@recipient=TenNguoiNhan,@phone=SDTNguoiNhan,@address=DiaChiGiao FROM dbo.HOADON WHERE MaHD=@Code;
   SET @date=SYSDATETIME();
   IF @date<@created THROW 52205,N'Ngày thanh toán trước lập đơn.',1;
   IF @total<=0 OR NOT EXISTS(SELECT 1 FROM dbo.CHITIET_HD WHERE MaHD=@Code) THROW 51102,N'Hóa đơn phải có món.',1;
   IF @channel=N'Giao hang' AND (NULLIF(LTRIM(RTRIM(@recipient)),N'') IS NULL OR NULLIF(@phone,'') IS NULL OR @phone COLLATE Latin1_General_100_BIN2 LIKE '%[^0-9]%' OR NULLIF(LTRIM(RTRIM(@address)),N'') IS NULL) THROW 52101,N'Thiếu thông tin giao.',1;
   DECLARE @balance int; SELECT @balance=DiemTichLuy FROM dbo.KHACHHANG WITH(UPDLOCK,HOLDLOCK) WHERE MaKH=@customer;
   IF EXISTS(SELECT 1 FROM dbo.HOADON h WHERE h.MaHD=@Code AND h.MaKM IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.KHUYENMAI WITH(HOLDLOCK) WHERE MaKM=h.MaKM AND CONVERT(date,h.NgayLap) BETWEEN NgayBD AND NgayKT)) THROW 51105,N'Khuyến mãi không hợp lệ.',1;
   -- Retain policy/customer/partner/payment locks through commit, including concurrent catalog changes.
   DECLARE @promotionRevision binary(8),@partnerRevision binary(8),@lockedMethod char(5);
   SELECT @promotionRevision=m.Revision FROM dbo.KHUYENMAI m WITH(HOLDLOCK) JOIN dbo.HOADON h ON h.MaKM=m.MaKM WHERE h.MaHD=@Code;
   SELECT @partnerRevision=d.CatalogRevision FROM dbo.DOITACGIAOHANG d WITH(HOLDLOCK) JOIN dbo.HOADON h ON h.MaDT=d.MaDT WHERE h.MaHD=@Code;
   DECLARE @token binary(32),@discount decimal(18,0),@payable decimal(38,0),@percent int,@tierPercent int,@promotionPercent int,@source varchar(20),@tier char(5),@promotion char(10),@partnerPercent decimal(5,2),@partnerFee decimal(18,0),@points bigint,@policyRevision binary(8),@customerRevision binary(8);
   SELECT @token=PreviewToken,@discount=Discount,@payable=Payable,@percent=PercentDiscount,@tierPercent=TierPercent,@promotionPercent=PromotionPercent,@source=Source,@tier=Tier,@promotion=Promotion,@partnerPercent=PartnerPercent,@partnerFee=PartnerFee,@points=Points,@policyRevision=PolicyRevision,@customerRevision=CustomerRevision FROM dbo.fn_CheckoutPreview(CONVERT(char(10),@Code));
   IF @ExpectedPreviewToken IS NULL OR @token<>@ExpectedPreviewToken THROW 52201,N'Preview đã thay đổi. Xác nhận lại số mới.',1;
   IF @payable>999999999999999999 OR @points>2147483647 OR (@customer IS NOT NULL AND CONVERT(bigint,@balance)+@points>2147483647) THROW 52205,N'Tiền hoặc điểm vượt giới hạn.',1;
   SELECT @lockedMethod=p.MaPT FROM dbo.PHUONGTHUCTT p WITH(HOLDLOCK) JOIN dbo.PaymentMethodSupport s WITH(HOLDLOCK) ON s.MaPT=p.MaPT JOIN @ThanhToan t ON t.MaPT=p.MaPT;
   IF @payable<>COALESCE((SELECT SUM(CONVERT(decimal(38,0),SoTien)) FROM @ThanhToan),0)
    OR EXISTS(SELECT 1 FROM @ThanhToan t WHERE t.MaPT='PT010' OR NOT EXISTS(SELECT 1 FROM dbo.PHUONGTHUCTT p JOIN dbo.PaymentMethodSupport s ON s.MaPT=p.MaPT WHERE p.MaPT=t.MaPT AND s.IsSupported=1)) THROW 52202,N'Phân bổ/phương thức không hợp lệ.',1;
   IF EXISTS(SELECT 1 FROM dbo.CHITIET_HD d JOIN dbo.SANPHAM s WITH(HOLDLOCK) ON s.MaSP=d.MaSP WHERE d.MaHD=@Code AND (s.TrangThai<>N'Dang ban' OR NOT EXISTS(SELECT 1 FROM dbo.CONGTHUC WHERE MaSP=d.MaSP))) THROW 52204,N'Món ngừng bán hoặc thiếu công thức.',1;
   DECLARE @consume TABLE(MaNVL char(5) PRIMARY KEY,Quantity decimal(38,4));
   INSERT @consume SELECT c.MaNVL,SUM(CONVERT(decimal(18,0),d.SoLuong)*c.DinhMuc) FROM dbo.CHITIET_HD d JOIN dbo.CONGTHUC c ON c.MaSP=d.MaSP WHERE d.MaHD=@Code GROUP BY c.MaNVL;
   IF EXISTS(SELECT 1 FROM @consume WHERE Quantity<=0 OR Quantity>99999999999999.9999) THROW 52205,N'Tiêu hao vượt giới hạn.',1;
   DECLARE @material char(5),@needed decimal(38,4),@available decimal(18,4);
   DECLARE stock CURSOR LOCAL FAST_FORWARD FOR SELECT MaNVL,Quantity FROM @consume ORDER BY MaNVL;
   OPEN stock; FETCH NEXT FROM stock INTO @material,@needed;
   WHILE @@FETCH_STATUS=0
   BEGIN
    SELECT @available=SoLuongTon FROM dbo.NGUYENVATLIEU WITH(UPDLOCK,HOLDLOCK) WHERE MaNVL=@material;
    IF @available IS NULL OR @available<@needed THROW 52203,N'Không đủ nguyên liệu. Đơn vẫn mở; kiểm tra tồn trước khi tiếp tục.',1;
    FETCH NEXT FROM stock INTO @material,@needed;
   END;
   CLOSE stock; DEALLOCATE stock;
   UPDATE dbo.HOADON SET GiamGia=@discount WHERE MaHD=@Code;
   INSERT dbo.HoaDonUuDaiSnapshot(MaHD,Kind,Source,TierCode,PromotionCode,PercentDiscount,TierPercent,PromotionPercent,Discount,PolicyRevision,CustomerRevision,PromotionRevision)
    VALUES(@Code,'Checkout',@source,@tier,@promotion,@percent,@tierPercent,@promotionPercent,@discount,@policyRevision,@customerRevision,@promotionRevision);
   INSERT dbo.TIEUHAO_HOADON(MaHD,MaNVL,SoLuong) SELECT @Code,MaNVL,CONVERT(decimal(18,4),Quantity) FROM @consume;
   INSERT dbo.CHITIET_THANHTOAN(MaHD,MaPT,SoTien,NgayTT,MaThamChieu) SELECT @Code,MaPT,SoTien,@date,MaThamChieu FROM @ThanhToan;
   UPDATE dbo.HOADON SET TrangThai=N'Da thanh toan',NgayThanhToan=@date,PhanTramPhiDoiTac=@partnerPercent,PhiDoiTac=@partnerFee WHERE MaHD=@Code;
   IF @customer IS NOT NULL AND @points>0 INSERT dbo.LICHSU_DIEM(MaKH,MaHD,NgayGD,Loai,SoDiem,LyDo) VALUES(@customer,@Code,@date,N'Cong',CONVERT(int,@points),N'Tích điểm mua hàng');
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
  SELECT h.MaHD,h.NgayLap,h.MaNV,h.MaKH,h.MaBan,h.MaDT,h.MaKM,h.LoaiDon,h.TenNguoiNhan,h.SDTNguoiNhan,h.DiaChiGiao,h.TrangThaiGiao,h.NgayThanhToan,h.PhanTramPhiDoiTac,h.PhiDoiTac,h.TongTien,h.PhiShip,h.GiamGia,CONVERT(decimal(38,0),h.ThanhTien) Payable,h.TrangThai,h.Revision,CONVERT(bit,1) PreviewValid,s.Source PreviewSource,s.PercentDiscount PreviewPercent,SYSDATETIME() ReadAt,h.MaPhien,sessionRow.Revision SessionRevision
  FROM dbo.HOADON h WITH(HOLDLOCK) LEFT JOIN dbo.HoaDonUuDaiSnapshot s ON s.MaHD=h.MaHD
  OUTER APPLY(SELECT TOP(1) p.Revision FROM dbo.PHIEN_BAN p WHERE p.MaPhien=h.MaPhien) sessionRow
  WHERE h.MaHD=@Code;
  SELECT d.MaDong,d.MaSP,s.TenSP,d.SoLuong,d.GiaBan,d.GhiChu,d.SoLuong*d.GiaBan Amount FROM dbo.CHITIET_HD d JOIN dbo.SANPHAM s ON s.MaSP=d.MaSP WHERE d.MaHD=@Code ORDER BY d.MaDong;
  SELECT t.MaTT,t.MaPT,p.TenPT,t.SoTien,t.MaThamChieu FROM dbo.CHITIET_THANHTOAN t JOIN dbo.PHUONGTHUCTT p ON p.MaPT=t.MaPT WHERE t.MaHD=@Code ORDER BY t.MaTT;
  SELECT s.Kind,s.Source,s.PercentDiscount,s.TierPercent,s.PromotionPercent,COALESCE((SELECT SUM(SoDiem) FROM dbo.LICHSU_DIEM WHERE MaHD=@Code AND Loai=N'Cong'),0) Points,s.RecordedAt FROM dbo.HoaDonUuDaiSnapshot s WHERE s.MaHD=@Code;
  COMMIT;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
GRANT EXECUTE ON dbo.sp_DiscountPreview TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_SalesCheckoutPreview TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_ThanhToanHoaDon TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_InvoiceDetail TO CafeManagerApp;
GO
