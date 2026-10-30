-- T15: SQL owns the complete checkout; no client supplied discount/date.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF @@TRANCOUNT=0 OR NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=10) THROW 52207,N'Migration010 bắt buộc.',1;
IF EXISTS(SELECT 1 FROM dbo.HOADON h WITH(TABLOCKX,HOLDLOCK) WHERE h.TrangThai=N'Chua thanh toan' AND (EXISTS(SELECT 1 FROM dbo.CHITIET_THANHTOAN t WHERE t.MaHD=h.MaHD) OR EXISTS(SELECT 1 FROM dbo.TIEUHAO_HOADON x WHERE x.MaHD=h.MaHD))) THROW 52207,N'Đơn mở có tiền/tiêu hao; cần đối chiếu trước nâng cấp.',1;
CREATE TABLE dbo.HoaDonUuDaiSnapshot(
 MaHD char(10) NOT NULL PRIMARY KEY REFERENCES dbo.HOADON(MaHD),
 Kind varchar(20) NOT NULL CHECK(Kind IN('Checkout','LegacyUnknown')),
 Source varchar(20) NULL CHECK(Source IN('None','Tier','Promotion')),
 TierCode char(5) NULL,PromotionCode char(10) NULL,PercentDiscount int NULL CHECK(PercentDiscount BETWEEN 0 AND 100),
 Discount decimal(18,0) NOT NULL CHECK(Discount>=0), RecordedAt datetime2(7) NOT NULL DEFAULT SYSDATETIME(),
 PolicyRevision binary(8) NULL,CustomerRevision binary(8) NULL,PromotionRevision binary(8) NULL,
 CONSTRAINT CK_DiscountSnapshot_Known CHECK((Kind='Checkout' AND Source IS NOT NULL AND PercentDiscount IS NOT NULL AND PolicyRevision IS NOT NULL) OR (Kind='LegacyUnknown' AND Source IS NULL AND PercentDiscount IS NULL)));
INSERT dbo.HoaDonUuDaiSnapshot(MaHD,Kind,Discount) SELECT MaHD,'LegacyUnknown',GiamGia FROM dbo.HOADON WHERE TrangThai=N'Da thanh toan';
DENY INSERT,UPDATE,DELETE ON dbo.HoaDonUuDaiSnapshot TO CafeManagerApp;
GO
CREATE TRIGGER dbo.TR_DiscountSnapshot ON dbo.HoaDonUuDaiSnapshot AFTER INSERT,UPDATE,DELETE AS
BEGIN
 SET NOCOUNT ON;
 IF EXISTS(SELECT 1 FROM deleted) THROW 52207,N'Snapshot ưu đãi không sửa/xóa.',1;
 IF EXISTS(SELECT 1 FROM inserted i JOIN dbo.HOADON h ON h.MaHD=i.MaHD WHERE i.Kind<>'Checkout' OR h.TrangThai<>N'Chua thanh toan' OR h.GiamGia<>i.Discount) THROW 52207,N'Snapshot chỉ ghi trong checkout đơn mở.',1;
END;
GO
CREATE FUNCTION dbo.fn_CheckoutPreview(@Code char(10)) RETURNS TABLE AS RETURN
(
 SELECT p.Source,p.Tier,p.Promotion,p.PercentDiscount,p.Discount,p.Payable,
 COALESCE(d.ChietKhau,CONVERT(decimal(5,2),0)) PartnerPercent,
 CONVERT(decimal(18,0),ROUND(CONVERT(decimal(28,4),h.TongTien-p.Discount)*COALESCE(d.ChietKhau,0)/100,0)) PartnerFee,
 CONVERT(bigint,FLOOR((h.TongTien-p.Discount)/1000)) Points,
 s.Revision PolicyRevision,k.Revision CustomerRevision,m.Revision PromotionRevision,
 CONVERT(binary(32),HASHBYTES('SHA2_256',
 CONVERT(varbinary(max),h.Revision)+s.Revision+COALESCE(k.Revision,0x0000000000000000)+COALESCE(m.Revision,0x0000000000000000)+COALESCE(d.CatalogRevision,0x0000000000000000)
 +CONVERT(binary(8),CONVERT(bigint,p.Discount))+CONVERT(binary(8),CONVERT(bigint,p.Payable))+CONVERT(binary(4),p.PercentDiscount))) PreviewToken
 FROM dbo.HOADON h CROSS JOIN dbo.LoyaltyPolicyState s
 LEFT JOIN dbo.KHACHHANG k ON k.MaKH=h.MaKH LEFT JOIN dbo.KHUYENMAI m ON m.MaKM=h.MaKM LEFT JOIN dbo.DOITACGIAOHANG d ON d.MaDT=h.MaDT
 CROSS APPLY dbo.fn_DiscountPreview(h.TongTien,h.PhiShip,CONVERT(date,h.NgayLap),h.MaKH,h.MaKM) p WHERE h.MaHD=@Code
);
GO
CREATE PROCEDURE dbo.sp_SalesCheckoutPreview @Actor int,@Stamp binary(8),@Code varchar(max),@ExpectedRevision binary(8) AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 BEGIN TRY
  BEGIN TRAN; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,CASH'; EXEC dbo.sp_SalesLock N'CafeManager:LoyaltyPolicy','Shared';
  DECLARE @resource nvarchar(255)=N'CafeManager:SalesOrder:'+CONVERT(nvarchar(10),@Code); EXEC dbo.sp_SalesLock @resource,'Shared';
  EXEC dbo.sp_SalesCheckOpen @Code,@ExpectedRevision;
  IF EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD=@Code AND TongTien<=0) THROW 51102,N'Hóa đơn phải có món.',1;
  IF EXISTS(SELECT 1 FROM dbo.HOADON h WHERE h.MaHD=@Code AND h.MaKM IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.KHUYENMAI WHERE MaKM=h.MaKM AND CONVERT(date,h.NgayLap) BETWEEN NgayBD AND NgayKT)) THROW 51105,N'Khuyến mãi không hợp lệ.',1;
  SELECT h.MaHD Code,h.Revision,h.TongTien Total,h.PhiShip Ship,p.Discount,p.Payable,p.Source,p.PercentDiscount,p.PartnerPercent,p.PartnerFee,p.Points,p.PreviewToken
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
   EXEC dbo.sp_SalesCheckOpen @Code,@ExpectedRevision;
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
   DECLARE @token binary(32),@discount decimal(18,0),@payable decimal(38,0),@percent int,@source varchar(20),@tier char(5),@promotion char(10),@partnerPercent decimal(5,2),@partnerFee decimal(18,0),@points bigint,@policyRevision binary(8),@customerRevision binary(8);
   SELECT @token=PreviewToken,@discount=Discount,@payable=Payable,@percent=PercentDiscount,@source=Source,@tier=Tier,@promotion=Promotion,@partnerPercent=PartnerPercent,@partnerFee=PartnerFee,@points=Points,@policyRevision=PolicyRevision,@customerRevision=CustomerRevision FROM dbo.fn_CheckoutPreview(CONVERT(char(10),@Code));
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
   INSERT dbo.HoaDonUuDaiSnapshot(MaHD,Kind,Source,TierCode,PromotionCode,PercentDiscount,Discount,PolicyRevision,CustomerRevision,PromotionRevision)
    VALUES(@Code,'Checkout',@source,@tier,@promotion,@percent,@discount,@policyRevision,@customerRevision,@promotionRevision);
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
GRANT EXECUTE ON dbo.sp_SalesCheckoutPreview TO CafeManagerApp;
