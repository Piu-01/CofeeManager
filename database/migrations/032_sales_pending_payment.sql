-- G04 bổ sung: trạng thái "Chờ thanh toán" (khóa sửa, vẫn thu/hủy được).
SET NOCOUNT ON; SET XACT_ABORT ON;
IF @@TRANCOUNT=0 OR NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=31)
 THROW 52107,N'Cần schema031 và runner migration.',1;
IF EXISTS(SELECT 1 FROM dbo.HOADON h WITH(TABLOCKX,HOLDLOCK)
 WHERE h.TrangThai=N'Chua thanh toan'
   AND (EXISTS(SELECT 1 FROM dbo.CHITIET_THANHTOAN t WHERE t.MaHD=h.MaHD)
     OR EXISTS(SELECT 1 FROM dbo.TIEUHAO_HOADON x WHERE x.MaHD=h.MaHD)))
 THROW 52107,N'Đơn mở có tiền/tiêu hao; cần đối chiếu trước nâng cấp.',1;
GO
ALTER TABLE dbo.HOADON DROP CONSTRAINT CHK_HD_TrangThai;
ALTER TABLE dbo.HOADON ADD CONSTRAINT CHK_HD_TrangThai CHECK (TrangThai IN
 (N'Chua thanh toan', N'Cho thanh toan', N'Da thanh toan', N'Huy'));
DROP INDEX UX_HD_BanMo ON dbo.HOADON;
CREATE UNIQUE INDEX UX_HD_BanMo ON dbo.HOADON(MaBan)
 WHERE MaBan IS NOT NULL AND TrangThai IN (N'Chua thanh toan', N'Cho thanh toan');
GO
ALTER VIEW dbo.vw_TrangThaiBan AS
SELECT b.MaBan,b.TenBan,b.MaKV,
       CASE WHEN h.MaHD IS NULL THEN N'Trong'
            WHEN h.TrangThai=N'Cho thanh toan' THEN N'Cho thanh toan'
            ELSE N'Dang phuc vu' END AS TrangThai,
       h.MaHD
FROM BAN b LEFT JOIN HOADON h ON h.MaBan=b.MaBan
 AND h.TrangThai IN (N'Chua thanh toan', N'Cho thanh toan');
GO
-- Cổng "còn thu/hủy": Chua hoặc Cho; vẫn kiểm revision và chưa phát sinh tiền/tiêu hao.
CREATE PROCEDURE dbo.sp_SalesCheckPayable @Code varchar(max),@ExpectedRevision binary(8) AS
BEGIN
 SET NOCOUNT ON;
 IF DATALENGTH(@Code)<>10 OR NOT EXISTS(SELECT 1 FROM dbo.HOADON WITH(UPDLOCK,HOLDLOCK)
  WHERE MaHD=@Code AND TrangThai IN (N'Chua thanh toan',N'Cho thanh toan'))
  THROW 52102,N'Đơn không có hoặc đã chốt/hủy.',1;
 IF @ExpectedRevision IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD=@Code AND Revision=@ExpectedRevision)
  THROW 52103,N'Đơn đã thay đổi ở phiên khác. Tải lại trước khi tiếp tục.',1;
 IF EXISTS(SELECT 1 FROM dbo.CHITIET_THANHTOAN WHERE MaHD=@Code) OR EXISTS(SELECT 1 FROM dbo.TIEUHAO_HOADON WHERE MaHD=@Code)
  THROW 52102,N'Đơn đã có tiền/tiêu hao; cần đối chiếu.',1;
END;
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
ALTER PROCEDURE dbo.sp_HuyHoaDon @Actor int,@Stamp binary(8),@Code varchar(max),@ExpectedRevision binary(8),@RequestId uniqueidentifier AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 52101,N'Không gọi ghi trong transaction bên ngoài.',1;
 BEGIN TRY
  BEGIN TRAN;
  DECLARE @payload nvarchar(max)=(SELECT @Code c,@ExpectedRevision r FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES),@replay bit;
  EXEC dbo.sp_SalesBegin @Actor,@Stamp,@RequestId,'SalesCancel',@Code,@payload,@replay OUTPUT;
  IF @replay=0
  BEGIN
   DECLARE @resource nvarchar(255)=N'CafeManager:SalesOrder:'+CONVERT(nvarchar(10),@Code); EXEC dbo.sp_SalesLock @resource;
   EXEC dbo.sp_SalesCheckPayable @Code,@ExpectedRevision;
   UPDATE dbo.HOADON SET TrangThai=N'Huy',TrangThaiGiao=CASE WHEN LoaiDon=N'Giao hang' THEN N'Huy giao' END WHERE MaHD=@Code;
   EXEC dbo.sp_SalesFinish @Actor,@RequestId,'SalesCancel',@Code,@payload;
  END;
  COMMIT; SELECT EntityCode,ResultRevision FROM dbo.G03OperationReceipt WHERE RequestId=@RequestId;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
CREATE PROCEDURE dbo.sp_YeuCauThanhToan @Actor int,@Stamp binary(8),@Code varchar(max),@ExpectedRevision binary(8),@RequestId uniqueidentifier AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 52101,N'Không gọi ghi trong transaction bên ngoài.',1;
 BEGIN TRY
  BEGIN TRAN;
  DECLARE @payload nvarchar(max)=(SELECT @Code c,@ExpectedRevision r FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES),@replay bit;
  EXEC dbo.sp_SalesBegin @Actor,@Stamp,@RequestId,'SalesRequestPay',@Code,@payload,@replay OUTPUT;
  IF @replay=0
  BEGIN
   DECLARE @resource nvarchar(255)=N'CafeManager:SalesOrder:'+CONVERT(nvarchar(10),@Code); EXEC dbo.sp_SalesLock @resource;
   EXEC dbo.sp_SalesCheckOpen @Code,@ExpectedRevision;
   IF NOT EXISTS(SELECT 1 FROM dbo.HOADON h WHERE h.MaHD=@Code AND h.TongTien>0) THROW 52108,N'Đơn cần có món trước khi yêu cầu thanh toán.',1;
   UPDATE dbo.HOADON SET TrangThai=N'Cho thanh toan' WHERE MaHD=@Code;
   EXEC dbo.sp_SalesFinish @Actor,@RequestId,'SalesRequestPay',@Code,@payload;
  END;
  COMMIT; SELECT EntityCode,ResultRevision FROM dbo.G03OperationReceipt WHERE RequestId=@RequestId;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
CREATE PROCEDURE dbo.sp_MoLaiHoaDon @Actor int,@Stamp binary(8),@Code varchar(max),@ExpectedRevision binary(8),@RequestId uniqueidentifier AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 52101,N'Không gọi ghi trong transaction bên ngoài.',1;
 BEGIN TRY
  BEGIN TRAN;
  EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN';
  DECLARE @payload nvarchar(max)=(SELECT @Code c,@ExpectedRevision r FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES),@replay bit;
  EXEC dbo.sp_SalesBegin @Actor,@Stamp,@RequestId,'SalesReopen',@Code,@payload,@replay OUTPUT;
  IF @replay=0
  BEGIN
   DECLARE @resource nvarchar(255)=N'CafeManager:SalesOrder:'+CONVERT(nvarchar(10),@Code); EXEC dbo.sp_SalesLock @resource;
   IF NOT EXISTS(SELECT 1 FROM dbo.HOADON WITH(UPDLOCK,HOLDLOCK) WHERE MaHD=@Code AND TrangThai=N'Cho thanh toan')
    THROW 52109,N'Chỉ mở lại được đơn đang chờ thanh toán.',1;
   IF @ExpectedRevision IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD=@Code AND Revision=@ExpectedRevision)
    THROW 52103,N'Đơn đã thay đổi ở phiên khác. Tải lại trước khi tiếp tục.',1;
   IF EXISTS(SELECT 1 FROM dbo.CHITIET_THANHTOAN WHERE MaHD=@Code) OR EXISTS(SELECT 1 FROM dbo.TIEUHAO_HOADON WHERE MaHD=@Code)
    THROW 52102,N'Đơn đã có tiền/tiêu hao; cần đối chiếu.',1;
   UPDATE dbo.HOADON SET TrangThai=N'Chua thanh toan' WHERE MaHD=@Code;
   EXEC dbo.sp_SalesFinish @Actor,@RequestId,'SalesReopen',@Code,@payload;
  END;
  COMMIT; SELECT EntityCode,ResultRevision FROM dbo.G03OperationReceipt WHERE RequestId=@RequestId;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
ALTER PROCEDURE dbo.sp_SalesReconcile @Actor int,@Stamp binary(8),@RequestId uniqueidentifier,@Code varchar(max),@Operation varchar(40) AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 BEGIN TRY
  BEGIN TRAN; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,CASH';
  IF @RequestId IS NULL OR @Operation NOT IN('SalesCreate','SalesLine','SalesHeader','SalesCheckout','SalesCancel','SalesDelivery','SalesRequestPay','SalesReopen') THROW 52101,N'Yêu cầu đối chiếu không hợp lệ.',1;
  DECLARE @resource nvarchar(255)=N'CafeManager:SalesRequest:'+CONVERT(nvarchar(36),@RequestId); EXEC dbo.sp_SalesLock @resource;
  SELECT EntityCode,ResultRevision,Operation,CompletedAt FROM dbo.G03OperationReceipt WITH(HOLDLOCK) WHERE RequestId=@RequestId AND ActorAccountId=@Actor AND Operation=@Operation AND EntityCode=@Code;
  COMMIT;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
ALTER PROCEDURE dbo.sp_SalesReadCore @Code varchar(max) AS
BEGIN
 SET NOCOUNT ON;
 SELECT h.MaHD,h.NgayLap,h.MaNV,h.MaKH,h.MaBan,h.MaDT,h.MaKM,h.LoaiDon,h.TenNguoiNhan,h.SDTNguoiNhan,h.DiaChiGiao,h.TrangThaiGiao,h.NgayThanhToan,h.PhanTramPhiDoiTac,h.PhiDoiTac,h.TongTien,h.PhiShip,
 CASE WHEN h.TrangThai IN (N'Chua thanh toan',N'Cho thanh toan') AND v.Valid=1 THEN p.Discount ELSE h.GiamGia END GiamGia,
 CASE WHEN h.TrangThai IN (N'Chua thanh toan',N'Cho thanh toan') AND v.Valid=1 THEN p.Payable ELSE CONVERT(decimal(38,0),h.ThanhTien) END Payable,h.TrangThai,h.Revision,v.Valid PreviewValid,
 CASE WHEN h.TrangThai IN (N'Chua thanh toan',N'Cho thanh toan') AND v.Valid=1 THEN p.Source ELSE snapshot.Source END PreviewSource,CASE WHEN h.TrangThai IN (N'Chua thanh toan',N'Cho thanh toan') AND v.Valid=1 THEN p.PercentDiscount ELSE snapshot.PercentDiscount END PreviewPercent,SYSDATETIME() ReadAt
 FROM dbo.HOADON h WITH(HOLDLOCK) LEFT JOIN dbo.HoaDonUuDaiSnapshot snapshot ON snapshot.MaHD=h.MaHD
 CROSS APPLY dbo.fn_DiscountPreview(CASE WHEN h.TrangThai IN (N'Chua thanh toan',N'Cho thanh toan') THEN h.TongTien ELSE 0 END,CASE WHEN h.TrangThai IN (N'Chua thanh toan',N'Cho thanh toan') THEN h.PhiShip ELSE 0 END,CONVERT(date,h.NgayLap),CASE WHEN h.TrangThai IN (N'Chua thanh toan',N'Cho thanh toan') THEN h.MaKH END,CASE WHEN h.TrangThai IN (N'Chua thanh toan',N'Cho thanh toan') THEN h.MaKM END) p
 CROSS APPLY(SELECT CONVERT(bit,CASE WHEN h.MaKM IS NULL OR EXISTS(SELECT 1 FROM dbo.KHUYENMAI k WHERE k.MaKM=h.MaKM AND CONVERT(date,h.NgayLap) BETWEEN k.NgayBD AND k.NgayKT) THEN 1 ELSE 0 END) Valid) v WHERE h.MaHD=@Code;
 SELECT d.MaDong,d.MaSP,s.TenSP,d.SoLuong,d.GiaBan,d.GhiChu,d.SoLuong*d.GiaBan Amount FROM dbo.CHITIET_HD d JOIN dbo.SANPHAM s ON s.MaSP=d.MaSP WHERE d.MaHD=@Code ORDER BY d.MaDong;
 SELECT t.MaTT,t.MaPT,p.TenPT,t.SoTien,t.MaThamChieu FROM dbo.CHITIET_THANHTOAN t JOIN dbo.PHUONGTHUCTT p ON p.MaPT=t.MaPT WHERE t.MaHD=@Code ORDER BY t.MaTT;
END;
GO
ALTER PROCEDURE dbo.sp_SalesOpenOrders @Actor int,@Stamp binary(8),@Search nvarchar(100)=N'',@Channel nvarchar(20)=NULL,@Skip int=0 AS
BEGIN
 SET NOCOUNT ON; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,CASH';
 IF @Skip<0 THROW 52101,N'Trang không hợp lệ.',1;
 SET @Search=REPLACE(REPLACE(REPLACE(REPLACE(@Search,N'~',N'~~'),N'%',N'~%'),N'_',N'~_'),N'[',N'~[');
 SELECT h.MaHD Code,h.NgayLap Created,h.LoaiDon Channel,h.MaBan TableCode,k.TenKH Customer,h.ThanhTien Payable,h.TrangThai Status,h.TrangThaiGiao Delivery,h.Revision,COUNT(*) OVER() TotalCount
 FROM dbo.HOADON h LEFT JOIN dbo.KHACHHANG k ON k.MaKH=h.MaKH WHERE h.TrangThai IN (N'Chua thanh toan',N'Cho thanh toan') AND (@Channel IS NULL OR h.LoaiDon=@Channel) AND (h.MaHD LIKE N'%'+@Search+N'%' ESCAPE N'~' OR k.TenKH LIKE N'%'+@Search+N'%' ESCAPE N'~') ORDER BY h.NgayLap DESC,h.MaHD OFFSET @Skip ROWS FETCH NEXT 50 ROWS ONLY;
END;
GO
ALTER PROCEDURE dbo.sp_TaoHoaDon @Actor int,@Stamp binary(8),@Code varchar(max),@RequestId uniqueidentifier,@Channel nvarchar(max),@TableCode varchar(max)=NULL,@Customer varchar(max)=NULL,@Promotion varchar(max)=NULL,@Partner varchar(max)=NULL,@Recipient nvarchar(max)=NULL,@Phone varchar(max)=NULL,@Address nvarchar(max)=NULL,@Ship decimal(38,18)=0,@Product varchar(max),@Quantity bigint,@Note nvarchar(max)=NULL AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 52101,N'Không gọi ghi trong transaction bên ngoài.',1;
 BEGIN TRY
  BEGIN TRAN;
  IF @Code IS NULL OR DATALENGTH(@Code)<>10 OR @Code COLLATE Latin1_General_100_BIN2 NOT LIKE 'HD[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]' THROW 52101,N'Mã hóa đơn không hợp lệ.',1;
  SELECT @Recipient=NULLIF(LTRIM(RTRIM(@Recipient)),N''),@Phone=NULLIF(LTRIM(RTRIM(@Phone)),''),@Address=NULLIF(LTRIM(RTRIM(@Address)),N''),@Note=NULLIF(LTRIM(RTRIM(@Note)),N'');
  SELECT @Customer=NULLIF(LTRIM(RTRIM(@Customer)),''),@Promotion=NULLIF(LTRIM(RTRIM(@Promotion)),''),@Partner=NULLIF(LTRIM(RTRIM(@Partner)),'');
  SET @TableCode=NULLIF(LTRIM(RTRIM(@TableCode)),'');
  DECLARE @payload nvarchar(max)=(SELECT @Code c,@Channel ch,@TableCode b,@Customer kh,@Promotion km,@Partner dt,@Recipient n,@Phone ph,@Address a,@Ship ship,@Product sp,@Quantity q,@Note note FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES),@replay bit;
  EXEC dbo.sp_SalesBegin @Actor,@Stamp,@RequestId,'SalesCreate',@Code,@payload,@replay OUTPUT;
  IF @replay=0
  BEGIN
   EXEC dbo.sp_SalesLock N'CafeManager:LoyaltyPolicy','Shared'; EXEC dbo.sp_SalesLock N'CafeManager:Recipes','Shared';
   DECLARE @resource nvarchar(255)=N'CafeManager:SalesOrder:'+CONVERT(nvarchar(10),@Code); EXEC dbo.sp_SalesLock @resource;
   DECLARE @date date=CONVERT(date,SYSDATETIME());
   EXEC dbo.sp_SalesValidateHeader @Channel,@TableCode,@Customer,@Promotion,@Partner,@Recipient,@Phone,@Address,@Ship,@date;
   IF @TableCode IS NOT NULL
   BEGIN
    DECLARE @lockedTable char(5); SELECT @lockedTable=MaBan FROM dbo.BAN WITH(UPDLOCK,HOLDLOCK) WHERE MaBan=@TableCode;
    IF EXISTS(SELECT 1 FROM dbo.HOADON WITH(UPDLOCK,HOLDLOCK) WHERE MaBan=@TableCode AND TrangThai IN (N'Chua thanh toan',N'Cho thanh toan')) THROW 52104,N'Bàn đã có đơn. Tải đơn hiện tại trước khi thêm món.',1;
   END;
   IF @Quantity IS NULL OR @Quantity NOT BETWEEN 1 AND 2147483647 OR DATALENGTH(@Product)>5 OR DATALENGTH(@Note)>200 THROW 52101,N'Số lượng/ghi chú/mã món không hợp lệ.',1;
   DECLARE @price decimal(18,0); SELECT @price=GiaBan FROM dbo.SANPHAM WITH(HOLDLOCK) WHERE MaSP=@Product AND TrangThai=N'Dang ban' AND EXISTS(SELECT 1 FROM dbo.CONGTHUC WHERE MaSP=@Product);
   IF @price IS NULL THROW 52105,N'Món ngừng bán hoặc chưa có công thức.',1;
   IF CONVERT(decimal(38,0),@price)*@Quantity>999999999999999999 THROW 52101,N'Tổng tiền vượt giới hạn.',1;
   INSERT dbo.HOADON(MaHD,MaNV,MaKH,MaBan,MaDT,MaKM,LoaiDon,TenNguoiNhan,SDTNguoiNhan,DiaChiGiao,TrangThaiGiao,PhiShip)
   SELECT @Code,MaNV,@Customer,@TableCode,@Partner,@Promotion,@Channel,@Recipient,@Phone,@Address,CASE WHEN @Channel=N'Giao hang' THEN N'Cho giao' END,CONVERT(decimal(18,0),@Ship) FROM dbo.TAIKHOAN WHERE MaTK=@Actor;
   INSERT dbo.CHITIET_HD(MaHD,MaSP,SoLuong,GiaBan,GhiChu) VALUES(@Code,@Product,CONVERT(int,@Quantity),@price,@Note);
   EXEC dbo.sp_SalesRefreshDiscount @Code;
   EXEC dbo.sp_SalesFinish @Actor,@RequestId,'SalesCreate',@Code,@payload;
  END;
  COMMIT; SELECT EntityCode,ResultRevision FROM dbo.G03OperationReceipt WHERE RequestId=@RequestId;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
ALTER PROCEDURE dbo.sp_TaoHoaDonTuNhap
 @Actor int,@Stamp binary(8),@Code varchar(max),@RequestId uniqueidentifier,
 @Channel nvarchar(max),@TableCode varchar(max)=NULL,@Customer varchar(max)=NULL,@Promotion varchar(max)=NULL,
 @Partner varchar(max)=NULL,@Recipient nvarchar(max)=NULL,@Phone varchar(max)=NULL,@Address nvarchar(max)=NULL,
 @Ship decimal(38,18)=0,@Lines dbo.SalesDraftLines READONLY AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 52101,N'Không gọi ghi trong transaction bên ngoài.',1;
 BEGIN TRY
  BEGIN TRAN;
  IF @Code IS NULL OR DATALENGTH(@Code)<>10 OR @Code COLLATE Latin1_General_100_BIN2 NOT LIKE 'HD[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]' THROW 52101,N'Mã hóa đơn không hợp lệ.',1;
  SELECT @Recipient=NULLIF(LTRIM(RTRIM(@Recipient)),N''),@Phone=NULLIF(LTRIM(RTRIM(@Phone)),''),@Address=NULLIF(LTRIM(RTRIM(@Address)),N'');
  DECLARE @lineJson nvarchar(max)=(SELECT RowNo,Product,Quantity,Note,Price FROM @Lines ORDER BY RowNo FOR JSON PATH,INCLUDE_NULL_VALUES);
  DECLARE @payload nvarchar(max)=(SELECT @Code c,@Channel ch,@TableCode b,@Customer kh,@Promotion km,@Partner dt,@Recipient n,@Phone ph,@Address a,@Ship ship,JSON_QUERY(@lineJson) lines FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES),@replay bit;
  EXEC dbo.sp_SalesBegin @Actor,@Stamp,@RequestId,'SalesCreate',@Code,@payload,@replay OUTPUT;
  IF @replay=0
  BEGIN
   EXEC dbo.sp_SalesLock N'CafeManager:LoyaltyPolicy','Shared'; EXEC dbo.sp_SalesLock N'CafeManager:Recipes','Shared';
   DECLARE @resource nvarchar(255)=N'CafeManager:SalesOrder:'+CONVERT(nvarchar(10),@Code); EXEC dbo.sp_SalesLock @resource;
   DECLARE @date date=CONVERT(date,SYSDATETIME());
   EXEC dbo.sp_SalesValidateHeader @Channel,@TableCode,@Customer,@Promotion,@Partner,@Recipient,@Phone,@Address,@Ship,@date;
   IF @TableCode IS NOT NULL
   BEGIN
    DECLARE @lockedTable char(5); SELECT @lockedTable=MaBan FROM dbo.BAN WITH(UPDLOCK,HOLDLOCK) WHERE MaBan=@TableCode;
    IF EXISTS(SELECT 1 FROM dbo.HOADON WITH(UPDLOCK,HOLDLOCK) WHERE MaBan=@TableCode AND TrangThai IN (N'Chua thanh toan',N'Cho thanh toan')) THROW 52104,N'Bàn đã có đơn. Tải đơn hiện tại trước khi thêm món.',1;
   END;
   IF NOT EXISTS(SELECT 1 FROM @Lines) THROW 52101,N'Đơn nháp không có dòng món.',1;
   IF EXISTS(SELECT 1 FROM @Lines WHERE Product IS NULL OR Quantity NOT BETWEEN 1 AND 2147483647 OR Price<0 OR Price>999999999999999999 OR (Note IS NOT NULL AND DATALENGTH(Note)>200))
    THROW 52101,N'Số lượng/ghi chú/mã món không hợp lệ.',1;
   IF EXISTS(SELECT 1 FROM @Lines l WHERE NOT EXISTS(SELECT 1 FROM dbo.SANPHAM s WITH(HOLDLOCK) WHERE s.MaSP=l.Product AND s.TrangThai=N'Dang ban' AND EXISTS(SELECT 1 FROM dbo.CONGTHUC c WHERE c.MaSP=l.Product)))
    THROW 52105,N'Món ngừng bán hoặc chưa có công thức.',1;
   IF EXISTS(SELECT 1 FROM @Lines l JOIN dbo.SANPHAM s ON s.MaSP=l.Product WHERE l.Price<>s.GiaBan)
    THROW 52105,N'Giá món đã thay đổi; tải lại menu và thêm lại dòng.',1;
   IF EXISTS(SELECT 1 FROM @Lines WHERE CONVERT(decimal(38,0),Price)*Quantity>999999999999999999)
    OR (SELECT SUM(CONVERT(decimal(38,0),Price)*Quantity) FROM @Lines)>999999999999999999
    THROW 52101,N'Tổng tiền vượt giới hạn.',1;
   INSERT dbo.HOADON(MaHD,MaNV,MaKH,MaBan,MaDT,MaKM,LoaiDon,TenNguoiNhan,SDTNguoiNhan,DiaChiGiao,TrangThaiGiao,PhiShip)
   SELECT @Code,MaNV,@Customer,@TableCode,@Partner,@Promotion,@Channel,@Recipient,@Phone,@Address,CASE WHEN @Channel=N'Giao hang' THEN N'Cho giao' END,CONVERT(decimal(18,0),@Ship) FROM dbo.TAIKHOAN WHERE MaTK=@Actor;
   INSERT dbo.CHITIET_HD(MaHD,MaSP,SoLuong,GiaBan,GhiChu)
   SELECT @Code,Product,Quantity,Price,NULLIF(LTRIM(RTRIM(Note)),N'') FROM @Lines ORDER BY RowNo;
   EXEC dbo.sp_SalesRefreshDiscount @Code;
   EXEC dbo.sp_SalesFinish @Actor,@RequestId,'SalesCreate',@Code,@payload;
  END;
  COMMIT; SELECT EntityCode,ResultRevision FROM dbo.G03OperationReceipt WHERE RequestId=@RequestId;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END
GO
ALTER PROCEDURE dbo.sp_ReportDashboard @Actor int,@Stamp binary(8) AS
BEGIN
 SET NOCOUNT ON;EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN';DECLARE @today date=CONVERT(date,SYSDATETIME()),@until datetime2(7)=DATEADD(day,1,CONVERT(datetime2(7),CONVERT(date,SYSDATETIME())));
 SELECT @today BusinessDate,SYSDATETIME() ReadAt,
 (SELECT COALESCE(SUM(CONVERT(decimal(38,0),ThanhTien)),0) FROM dbo.HOADON WHERE TrangThai=N'Da thanh toan' AND NgayThanhToan>=@today AND NgayThanhToan<@until) Revenue,
 (SELECT COUNT(*) FROM dbo.HOADON WHERE TrangThai IN (N'Chua thanh toan',N'Cho thanh toan')) OpenOrders,
 (SELECT COALESCE(SUM(CONVERT(decimal(38,0),ConNo)),0) FROM dbo.vw_CongNoNCC) Debt;
END;
GO
ALTER PROCEDURE dbo.sp_DashboardAnalyticsRead
 @Actor int,@Stamp binary(8),@Month bit,@At datetime2(7)=NULL
WITH EXECUTE AS OWNER AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN';
 DECLARE @read datetime2(7)=SYSDATETIME();
 SET @At=COALESCE(@At,@read);
 IF @Month IS NULL OR @At<'20000101' OR @At>@read OR @@TRANCOUNT<>0
  THROW 53501,N'Kỳ tổng quan hoặc transaction không hợp lệ.',1;
 DECLARE @from datetime2(7)=CONVERT(datetime2(7),CASE WHEN @Month=1 THEN DATEFROMPARTS(YEAR(@At),MONTH(@At),1) ELSE CONVERT(date,@At) END),
  @previousFrom datetime2(7),@previousUntil datetime2(7);
 IF @Month=0 BEGIN SET @previousFrom=DATEADD(day,-1,@from);SET @previousUntil=DATEADD(day,-1,@At);END;
 ELSE IF DAY(@At)<=DAY(EOMONTH(DATEADD(month,-1,CONVERT(date,@At))))
 BEGIN SET @previousFrom=DATEADD(month,-1,@from);SET @previousUntil=DATEADD(month,-1,@At);END;
 CREATE TABLE #indicators([Key] varchar(80) NOT NULL,Value decimal(38,8) NULL);
 CREATE TABLE #rows(PageKey varchar(30) NOT NULL,Code nvarchar(100) NOT NULL,Name nvarchar(200) NOT NULL,
  Bills int NOT NULL,Quantity decimal(38,4) NOT NULL,Gross decimal(38,0) NOT NULL,Discount decimal(38,0) NOT NULL,
  Ship decimal(38,0) NOT NULL,Amount decimal(38,0) NOT NULL,Fees decimal(38,0) NOT NULL,Position int NOT NULL);
 BEGIN TRY
  SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
  BEGIN TRAN;
  -- Một snapshot header cho toàn bộ phần bán hàng, không JOIN phương thức thanh toán vào dòng món.
  SELECT CASE WHEN h.NgayThanhToan>=@from THEN 'C' ELSE 'P' END Period,
   h.MaHD,h.NgayThanhToan,h.LoaiDon,CONVERT(decimal(38,0),h.TongTien) Gross,
   CONVERT(decimal(38,0),h.GiamGia) Discount,CONVERT(decimal(38,0),h.PhiShip) Ship,
   CONVERT(decimal(38,0),h.ThanhTien) Amount,CONVERT(decimal(38,0),h.PhiDoiTac) Fees
  INTO #bills FROM dbo.HOADON h
  WHERE h.TrangThai=N'Da thanh toan' AND
   ((h.NgayThanhToan>=@from AND h.NgayThanhToan<@At) OR
    (@previousFrom IS NOT NULL AND h.NgayThanhToan>=@previousFrom AND h.NgayThanhToan<@previousUntil));
  SELECT 'C' Period,COUNT(*) Bills,COALESCE(SUM(Amount),0) Amount INTO #sales FROM #bills WHERE Period='C';
  INSERT #sales SELECT 'P',COUNT(*),COALESCE(SUM(Amount),0) FROM #bills WHERE Period='P';
  SELECT l.MaSP,s.TenSP,COUNT(DISTINCT b.MaHD) Bills,SUM(CONVERT(decimal(38,4),l.SoLuong)) Quantity,
   SUM(CONVERT(decimal(38,0),l.SoLuong*l.GiaBan)) Amount
  INTO #products FROM #bills b JOIN dbo.CHITIET_HD l ON l.MaHD=b.MaHD JOIN dbo.SANPHAM s ON s.MaSP=l.MaSP
  WHERE b.Period='C' GROUP BY l.MaSP,s.TenSP;
  SELECT CASE WHEN p.NgayLap>=@from THEN 'C' ELSE 'P' END Period,CONVERT(decimal(38,0),p.SoTien) Amount
  INTO #expenses FROM dbo.PHIEUTHUCHI p JOIN dbo.DANHMUC_THUCHI d ON d.MaDMTC=p.MaDMTC
  WHERE d.Loai=N'Chi' AND ((p.NgayLap>=@from AND p.NgayLap<@At) OR
   (@previousFrom IS NOT NULL AND p.NgayLap>=@previousFrom AND p.NgayLap<@previousUntil));
  DECLARE @revenue decimal(38,8),@previousRevenue decimal(38,8),@bills int,@previousBills int,
   @average decimal(38,8),@previousAverage decimal(38,8),@expense decimal(38,8),@previousExpense decimal(38,8);
  SELECT @revenue=Amount,@bills=Bills FROM #sales WHERE Period='C';
  SELECT @previousRevenue=CASE WHEN @previousFrom IS NULL THEN NULL ELSE Amount END,
   @previousBills=CASE WHEN @previousFrom IS NULL THEN NULL ELSE Bills END FROM #sales WHERE Period='P';
  SET @average=@revenue/NULLIF(@bills,0);SET @previousAverage=@previousRevenue/NULLIF(@previousBills,0);
  SELECT @expense=COALESCE(SUM(Amount),0) FROM #expenses WHERE Period='C';
  SELECT @previousExpense=CASE WHEN @previousFrom IS NULL THEN NULL ELSE COALESCE(SUM(Amount),0) END FROM #expenses WHERE Period='P';
  INSERT #indicators VALUES
   ('invoice-count.current',@bills),('invoice-count.previous',@previousBills),
   ('average.current',@average),('average.previous',@previousAverage),
   ('expense.current',@expense),('expense.previous',@previousExpense),
   ('difference.revenue',@revenue-@previousRevenue),('difference.invoices',@bills-@previousBills),
   ('difference.average',@average-@previousAverage),('difference.expense',@expense-@previousExpense),
   ('change.revenue',CASE WHEN @previousRevenue IS NULL THEN NULL WHEN @previousRevenue=0 AND @revenue=0 THEN 0 ELSE (@revenue/NULLIF(@previousRevenue,0)-1)*100 END),
   ('change.invoices',CASE WHEN @previousBills IS NULL THEN NULL WHEN @previousBills=0 AND @bills=0 THEN 0 ELSE (CONVERT(decimal(38,8),@bills)/NULLIF(@previousBills,0)-1)*100 END),
   ('change.average',CASE WHEN @average IS NULL OR @previousAverage IS NULL THEN NULL WHEN @previousAverage=0 AND @average=0 THEN 0 ELSE (@average/NULLIF(@previousAverage,0)-1)*100 END),
   ('change.expense',CASE WHEN @previousExpense IS NULL THEN NULL WHEN @previousExpense=0 AND @expense=0 THEN 0 ELSE (@expense/NULLIF(@previousExpense,0)-1)*100 END);
  INSERT #indicators SELECT 'quantity.total',COALESCE(SUM(Quantity),0) FROM #products;
  INSERT #indicators SELECT 'orders.open',COUNT(*) FROM dbo.HOADON WHERE TrangThai IN (N'Chua thanh toan',N'Cho thanh toan');
  INSERT #indicators SELECT 'delivery.pending',COUNT(*) FROM dbo.HOADON WHERE LoaiDon=N'Giao hang' AND TrangThai=N'Da thanh toan' AND TrangThaiGiao IN(N'Cho giao',N'Dang giao');
  INSERT #indicators SELECT 'material.zero',COUNT(*) FROM dbo.NGUYENVATLIEU WHERE SoLuongTon=0;
  INSERT #indicators SELECT 'debt.current',COALESCE(SUM(CONVERT(decimal(38,0),ConNo)),0) FROM dbo.vw_CongNoNCC WHERE ConNo>0;

  -- Chỉ sinh bucket đã bắt đầu; bucket cuối kỳ được cắt tại cùng mốc giờ/phút.
  ;WITH numbers AS(SELECT 0 n UNION ALL SELECT n+1 FROM numbers WHERE n<30),
  periods AS(SELECT 'C' Period,@from StartAt,@At UntilAt UNION ALL SELECT 'P',@previousFrom,@previousUntil WHERE @previousFrom IS NOT NULL)
  SELECT p.Period,n.n,CASE WHEN @Month=1 THEN DATEADD(day,n.n,p.StartAt) ELSE DATEADD(hour,n.n,p.StartAt) END Bucket,
   CASE WHEN @Month=1 THEN DATEADD(day,n.n+1,p.StartAt) ELSE DATEADD(hour,n.n+1,p.StartAt) END BucketUntil
  INTO #buckets FROM periods p CROSS JOIN numbers n
  WHERE CASE WHEN @Month=1 THEN DATEADD(day,n.n,p.StartAt) ELSE DATEADD(hour,n.n,p.StartAt) END<p.UntilAt OPTION(MAXRECURSION 31);
  INSERT #rows
  SELECT CASE k.Period WHEN 'C' THEN 'trend.current' ELSE 'trend.previous' END,CONVERT(nvarchar(33),k.Bucket,126),
   CASE WHEN @Month=1 THEN CONVERT(nvarchar(10),CONVERT(date,k.Bucket),103) ELSE CONVERT(nvarchar(5),k.Bucket,108) END,
   COUNT(b.MaHD),0,COALESCE(SUM(b.Gross),0),COALESCE(SUM(b.Discount),0),COALESCE(SUM(b.Ship),0),COALESCE(SUM(b.Amount),0),COALESCE(SUM(b.Fees),0),k.n
  FROM #buckets k LEFT JOIN #bills b ON b.Period=k.Period AND b.NgayThanhToan>=k.Bucket AND b.NgayThanhToan<k.BucketUntil
  GROUP BY k.Period,k.Bucket,k.n;
  INSERT #rows
  SELECT 'channels',c.Code,c.Name,COUNT(b.MaHD),0,COALESCE(SUM(b.Gross),0),COALESCE(SUM(b.Discount),0),COALESCE(SUM(b.Ship),0),COALESCE(SUM(b.Amount),0),COALESCE(SUM(b.Fees),0),c.Position
  FROM(VALUES(N'Tai quan',N'Tại quán',0),(N'Mang di',N'Mang đi',1),(N'Giao hang',N'Giao hàng',2))c(Code,Name,Position)
  LEFT JOIN #bills b ON b.Period='C' AND b.LoaiDon=c.Code GROUP BY c.Code,c.Name,c.Position;
  INSERT #indicators SELECT 'share.channel.'+CONVERT(varchar(20),Code),CASE WHEN @revenue=0 THEN NULL ELSE CONVERT(decimal(38,8),Amount)/@revenue*100 END FROM #rows WHERE PageKey='channels';
  INSERT #rows SELECT TOP(5) 'products',MaSP,TenSP,Bills,Quantity,0,0,0,Amount,0,CONVERT(int,ROW_NUMBER() OVER(ORDER BY Quantity DESC,MaSP)) FROM #products ORDER BY Quantity DESC,MaSP;
  COMMIT;
 END TRY BEGIN CATCH
  IF @@TRANCOUNT>0 ROLLBACK;
  THROW;
 END CATCH;
 SELECT @from [From],@At Until,@previousFrom PreviousFrom,@previousUntil PreviousUntil,@read ReadAt;
 SELECT [Key],Value FROM #indicators ORDER BY [Key];
 SELECT PageKey,Code,Name,Bills,Quantity,Gross,Discount,Ship,Amount,Fees,Gross-Discount AfterDiscount FROM #rows ORDER BY PageKey,Position;
 -- Tổng vẫn đầy đủ khi Top5 hoặc danh sách bucket rỗng.
 SELECT 'trend.current' PageKey,COALESCE(SUM(Gross),0) Gross,COALESCE(SUM(Discount),0) Discount,COALESCE(SUM(Ship),0) Ship,COALESCE(SUM(Amount),0) Amount,COALESCE(SUM(Fees),0) Fees FROM #bills WHERE Period='C'
 UNION ALL SELECT 'trend.previous',COALESCE(SUM(Gross),0),COALESCE(SUM(Discount),0),COALESCE(SUM(Ship),0),COALESCE(SUM(Amount),0),COALESCE(SUM(Fees),0) FROM #bills WHERE Period='P' AND @previousFrom IS NOT NULL
 UNION ALL SELECT 'channels',COALESCE(SUM(Gross),0),COALESCE(SUM(Discount),0),COALESCE(SUM(Ship),0),COALESCE(SUM(Amount),0),COALESCE(SUM(Fees),0) FROM #bills WHERE Period='C'
 UNION ALL SELECT 'products',0,0,0,COALESCE(SUM(Amount),0),0 FROM #products;
END;
GO
ALTER PROCEDURE dbo.sp_DashboardAnalyticsSources
 @Actor int,@Stamp binary(8),@Kind varchar(20),@From datetime2(7)=NULL,@Until datetime2(7)=NULL,
 @Channel nvarchar(20)=NULL,@Product varchar(5)=NULL,@Skip int=0
WITH EXECUTE AS OWNER AS
BEGIN
 SET NOCOUNT ON;SET XACT_ABORT ON;
 EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN';
 DECLARE @read datetime2(7)=SYSDATETIME();
 IF @Kind IS NULL OR @Kind NOT IN('revenue','invoices','average','trend','channel','product','expense','orders','delivery','material','debt')
  OR @Skip<0 OR @@TRANCOUNT<>0 OR (@Kind NOT IN('orders','delivery','material','debt') AND (@From IS NULL OR @Until IS NULL OR @From>@Until OR @Until>@read))
  OR (@Channel IS NOT NULL AND @Channel NOT IN(N'Tai quan',N'Mang di',N'Giao hang'))
  OR (@Kind='channel' AND @Channel IS NULL) OR (@Channel IS NOT NULL AND @Kind<>'channel')
  OR (@Kind='product' AND (@Product IS NULL OR LEN(@Product)<>5)) OR (@Product IS NOT NULL AND @Kind<>'product')
  THROW 53501,N'Nguồn hoặc kỳ tổng quan không hợp lệ.',1;
 CREATE TABLE #sources(SourceType varchar(20) NOT NULL,Code varchar(100) NOT NULL,Name nvarchar(200) NOT NULL,
  SourceDate datetime2(7) NULL,Amount decimal(38,0) NULL,Quantity decimal(38,4) NULL);
 CREATE TABLE #headers(MaHD char(10) NOT NULL,LoaiDon nvarchar(20) NOT NULL,SourceDate datetime2(7) NULL,Amount decimal(38,0) NOT NULL);
 BEGIN TRY
  SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;BEGIN TRAN;
  IF @Kind IN('revenue','invoices','average','trend','channel','product','orders','delivery')
  BEGIN
   -- Chặng một chỉ đọc HOADON; EXISTS CHITIET_HD trước đây cho phép optimizer đảo thứ tự khóa.
   INSERT #headers(MaHD,LoaiDon,SourceDate,Amount)
   SELECT h.MaHD,h.LoaiDon,CASE WHEN @Kind IN('orders','delivery') THEN h.NgayLap ELSE h.NgayThanhToan END,h.ThanhTien
   FROM dbo.HOADON h WHERE
    (@Kind='orders' AND h.TrangThai IN (N'Chua thanh toan',N'Cho thanh toan')) OR
    (@Kind='delivery' AND h.LoaiDon=N'Giao hang' AND h.TrangThai=N'Da thanh toan' AND h.TrangThaiGiao IN(N'Cho giao',N'Dang giao')) OR
    (@Kind NOT IN('orders','delivery') AND h.TrangThai=N'Da thanh toan' AND h.NgayThanhToan>=@From AND h.NgayThanhToan<@Until
     AND (@Channel IS NULL OR h.LoaiDon=@Channel));
   IF @Kind='product'
    INSERT #sources(SourceType,Code,Name,SourceDate,Amount)
    SELECT 'Invoice',h.MaHD,h.LoaiDon,h.SourceDate,h.Amount FROM #headers h
    WHERE EXISTS(SELECT 1 FROM dbo.CHITIET_HD l WHERE l.MaHD=h.MaHD AND l.MaSP=@Product);
   ELSE
    INSERT #sources(SourceType,Code,Name,SourceDate,Amount)
    SELECT 'Invoice',h.MaHD,h.LoaiDon,h.SourceDate,h.Amount FROM #headers h;
  END
  IF @Kind='expense'
   INSERT #sources(SourceType,Code,Name,SourceDate,Amount)
   SELECT 'Finance',p.MaTC,d.TenDMTC,p.NgayLap,p.SoTien FROM dbo.PHIEUTHUCHI p JOIN dbo.DANHMUC_THUCHI d ON d.MaDMTC=p.MaDMTC
   WHERE d.Loai=N'Chi' AND p.NgayLap>=@From AND p.NgayLap<@Until;
  IF @Kind='material'
   INSERT #sources(SourceType,Code,Name,Quantity) SELECT 'Material',MaNVL,TenNVL+N' · '+DonViTinh,SoLuongTon FROM dbo.NGUYENVATLIEU WHERE SoLuongTon=0;
  IF @Kind='debt'
   INSERT #sources(SourceType,Code,Name,SourceDate,Amount)
   SELECT 'Debt',v.MaCN,n.TenNCC,p.NgayNhap,v.ConNo FROM dbo.vw_CongNoNCC v JOIN dbo.NHACUNGCAP n ON n.MaNCC=v.MaNCC JOIN dbo.PHIEUNHAP p ON p.MaPN=v.MaPN WHERE v.ConNo>0;
  COMMIT;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK;THROW;END CATCH;
 SELECT COUNT(*) TotalCount,@read ReadAt FROM #sources;
 SELECT SourceType,Code,Name,SourceDate,Amount,Quantity FROM #sources ORDER BY SourceDate DESC,SourceType,Code OFFSET @Skip ROWS FETCH NEXT 50 ROWS ONLY;
END;
GO
ALTER TRIGGER dbo.TR_CTTT_KiemTra ON dbo.CHITIET_THANHTOAN AFTER INSERT,UPDATE,DELETE AS
BEGIN
 SET NOCOUNT ON;
 DECLARE @code char(10),@status nvarchar(30),@payable decimal(38,0),@allocated decimal(38,0);
 -- Khóa đúng khóa chính hiện hữu, không lọc trạng thái trước khi lấy khóa.
 DECLARE orders CURSOR LOCAL FAST_FORWARD FOR
  SELECT MaHD FROM inserted UNION SELECT MaHD FROM deleted ORDER BY MaHD;
 OPEN orders; FETCH NEXT FROM orders INTO @code;
 WHILE @@FETCH_STATUS=0
 BEGIN
  SELECT @status=NULL,@payable=NULL;
  SELECT @status=TrangThai,@payable=ThanhTien
  FROM dbo.HOADON WITH(UPDLOCK,HOLDLOCK,INDEX(PK_HOADON),FORCESEEK)
  WHERE MaHD=@code;
  IF @status IS NULL OR @status NOT IN (N'Chua thanh toan',N'Cho thanh toan')
   THROW 51002,N'Khong duoc sua thanh toan cua hoa don da chot/huy.',1;
  SELECT @allocated=SUM(SoTien) FROM dbo.CHITIET_THANHTOAN WHERE MaHD=@code;
  IF @allocated>@payable
   THROW 51003,N'Tien phan bo thanh toan vuot so phai tra (khong luu tien khach dua).',1;
  FETCH NEXT FROM orders INTO @code;
 END;
 CLOSE orders; DEALLOCATE orders;
END;
GO
ALTER TRIGGER dbo.TR_DiscountSnapshot ON dbo.HoaDonUuDaiSnapshot AFTER INSERT,UPDATE,DELETE AS
BEGIN
 SET NOCOUNT ON;
 IF EXISTS(SELECT 1 FROM deleted) THROW 52207,N'Snapshot ưu đãi không sửa/xóa.',1;
 IF EXISTS(SELECT 1 FROM inserted i JOIN dbo.HOADON h ON h.MaHD=i.MaHD WHERE i.Kind<>'Checkout' OR h.TrangThai NOT IN (N'Chua thanh toan',N'Cho thanh toan') OR h.GiamGia<>i.Discount) THROW 52207,N'Snapshot chỉ ghi trong checkout đơn mở.',1;
END;
GO
ALTER TRIGGER dbo.TR_TieuHao_Kho ON dbo.TIEUHAO_HOADON AFTER INSERT,UPDATE,DELETE AS
BEGIN
 SET NOCOUNT ON;
 IF EXISTS (SELECT 1 FROM deleted)
  THROW 51009,N'Tieu hao hoa don la lich su, khong sua/xoa.',1;
 IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.HOADON h WITH (UPDLOCK,HOLDLOCK) ON h.MaHD=i.MaHD
    WHERE h.TrangThai NOT IN (N'Chua thanh toan',N'Cho thanh toan'))
  THROW 51010,N'Chi ghi tieu hao trong giao dich thanh toan don dang mo.',1;
 ;WITH delta AS (SELECT MaNVL,CONVERT(DECIMAL(18,4),SUM(SoLuong)) SL FROM inserted GROUP BY MaNVL)
 UPDATE n WITH (UPDLOCK) SET SoLuongTon=n.SoLuongTon-d.SL
 FROM NGUYENVATLIEU n JOIN delta d ON d.MaNVL=n.MaNVL;
END;
GO
GRANT EXECUTE ON dbo.sp_YeuCauThanhToan TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_MoLaiHoaDon TO CafeManagerApp;
GO
