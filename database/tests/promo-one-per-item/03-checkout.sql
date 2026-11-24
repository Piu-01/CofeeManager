-- 03 (PP06/PP07): preview -> pause KM -> token cũ bị từ chối -> preview lại -> thanh toán snapshot;
-- quà tặng độc lập; đơn mở có MaKM legacy dùng như ứng viên ALL; hóa đơn đã trả khóa theo snapshot.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF DB_NAME() NOT LIKE 'CoffeeManager_Test_PromoOnePerItem[_]%' OR OBJECT_ID('dbo.PromoOnePerItemTestMarker') IS NULL
 THROW 53804,N'Chỉ DB test PromoOnePerItem có marker.',1;
IF OBJECT_ID('dbo.PromoOnePerItemBaseline') IS NULL THROW 53804,N'Thiếu baseline fixture.',1;
DECLARE @a int,@s binary(8),@today date=CONVERT(date,SYSDATETIME()),@id uniqueidentifier,@code varchar(10);
SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001';
DECLARE @codes TABLE(Loai varchar(20),Ma char(10));
-- Không capture proc preview bằng INSERT..EXEC (applock + ROLLBACK trong CATCH gây 3915);
-- đọc fn_CheckoutPreview và gọi sp_SalesCheckoutPreview không capture làm smoke.
DECLARE @payments dbo.DanhSachThanhToan;
DECLARE @rev binary(8),@token binary(32),@kmRev binary(8),@err int=0,@ok bit=0;
DECLARE @stock93 decimal(18,4)=(SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL93');
DECLARE @stock94 decimal(18,4)=(SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL94');

-- Đơn 1: A+B+C cho KH932 (VÀNG 5%).
DECLARE @lines1 dbo.SalesDraftLines;
INSERT @lines1 VALUES(1,'SP940',1,NULL,100000),(2,'SP941',1,NULL,60000),(3,'SP942',1,NULL,40000);
DELETE @codes; INSERT @codes EXEC dbo.sp_CapMa 'HOADON'; SET @code=(SELECT Ma FROM @codes); SET @id=NEWID();
EXEC dbo.sp_TaoHoaDonTuNhap @Actor=@a,@Stamp=@s,@Code=@code,@RequestId=@id,@Channel=N'Mang di',
 @Customer='KH932',@Lines=@lines1;
DECLARE @code1 char(10)=@code;
SELECT @rev=Revision FROM dbo.HOADON WHERE MaHD=@code1;

-- PP06a: preview trước khi đổi KM: KM 50 000 + hạng 7 500 = 57 500; 2 KM; 142 điểm.
IF EXISTS(SELECT 1 FROM dbo.fn_CheckoutPreview(@code1) WHERE Discount<>57500 OR Payable<>142500
 OR PercentDiscount<>29 OR TierPercent<>5 OR PromotionPercent<>25 OR TierDiscount<>7500
 OR PromotionDiscount<>50000 OR PromotionCount<>2 OR Points<>142 OR GiftCount<>1)
 OR (SELECT TongTien FROM dbo.HOADON WHERE MaHD=@code1)<>200000
 THROW 53804,N'PP06a: preview A+B+C trước khi đổi KM sai.',1;
EXEC dbo.sp_SalesCheckoutPreview @Actor=@a,@Stamp=@s,@Code=@code1,@ExpectedRevision=@rev;
SELECT @token=PreviewToken FROM dbo.fn_CheckoutPreview(@code1);

-- PP06b: tạm dừng KM3; preview cũ (token cũ) bị từ chối, đơn/stock không đổi.
SELECT @kmRev=Revision FROM dbo.KHUYENMAI WHERE MaKM='KM93000003';
SET @id=NEWID();
EXEC dbo.sp_PromotionWrite @Actor=@a,@Stamp=@s,@Action='Pause',@Code='KM93000003',
 @RequestId=@id,@ExpectedRevision=@kmRev;
DELETE @payments; INSERT @payments VALUES('PT001',142500,NULL);
SET @id=NEWID();
BEGIN TRY
 EXEC dbo.sp_ThanhToanHoaDon @Actor=@a,@Stamp=@s,@Code=@code1,@ExpectedRevision=@rev,
  @RequestId=@id,@ExpectedPreviewToken=@token,@ThanhToan=@payments;
END TRY BEGIN CATCH SET @err=ERROR_NUMBER(); END CATCH;
IF @err<>52201 THROW 53804,N'PP06b: token cũ phải bị từ chối bằng 52201.',1;
IF EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD=@code1 AND TrangThai<>N'Chua thanh toan')
 OR EXISTS(SELECT 1 FROM dbo.CHITIET_THANHTOAN WHERE MaHD=@code1)
 OR (SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL93')<>@stock93
 OR (SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL94')<>@stock94
 THROW 53804,N'PP06b: thanh toán lỗi phải rollback toàn bộ.',1;

-- PP06c: preview lại sau khi dừng KM3: KM1+KM2 = 28 000; hạng 5% của 172 000 = 8 600; tổng 36 600.
SELECT @rev=Revision FROM dbo.HOADON WHERE MaHD=@code1;
IF EXISTS(SELECT 1 FROM dbo.fn_CheckoutPreview(@code1) WHERE Discount<>36600 OR Payable<>163400
 OR PercentDiscount<>18 OR TierDiscount<>8600 OR PromotionDiscount<>28000 OR PromotionCount<>2 OR Points<>163)
 THROW 53804,N'PP06c: preview sau khi dừng KM3 sai.',1;
EXEC dbo.sp_SalesCheckoutPreview @Actor=@a,@Stamp=@s,@Code=@code1,@ExpectedRevision=@rev;
SELECT @token=PreviewToken FROM dbo.fn_CheckoutPreview(@code1);

-- PP06d: thanh toán, snapshot/hóa đơn/lịch sử điểm/tiêu hao khớp preview.
DELETE @payments; INSERT @payments VALUES('PT001',163400,NULL); SET @id=NEWID();
EXEC dbo.sp_ThanhToanHoaDon @Actor=@a,@Stamp=@s,@Code=@code1,@ExpectedRevision=@rev,
 @RequestId=@id,@ExpectedPreviewToken=@token,@ThanhToan=@payments;
IF NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD=@code1 AND TrangThai=N'Da thanh toan'
 AND GiamGia=36600 AND CONVERT(decimal(38,0),ThanhTien)=163400)
 THROW 53804,N'PP06d: hóa đơn đã trả sai tiền/giảm.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.HoaDonUuDaiSnapshot WHERE MaHD=@code1 AND Kind='Checkout'
 AND Source='TierAndPromotion' AND PercentDiscount=18 AND TierPercent=5 AND PromotionPercent=14
 AND TierDiscount=8600 AND PromotionDiscount=28000 AND PromotionCount=2 AND Discount=36600)
 THROW 53804,N'PP06d: snapshot không khớp preview tuần tự.',1;
IF (SELECT COUNT(*) FROM dbo.HoaDonUuDaiApDung WHERE MaHD=@code1)<>2
 OR (SELECT SUM(Discount) FROM dbo.HoaDonUuDaiApDung WHERE MaHD=@code1)<>28000
 OR NOT EXISTS(SELECT 1 FROM dbo.HoaDonUuDaiApDung WHERE MaHD=@code1 AND MaKM='KM93000001' AND Discount=10000)
 OR NOT EXISTS(SELECT 1 FROM dbo.HoaDonUuDaiApDung WHERE MaHD=@code1 AND MaKM='KM93000002' AND Discount=18000)
 OR EXISTS(SELECT 1 FROM dbo.HoaDonUuDaiApDung WHERE MaHD=@code1 AND MaKM IN('KM93000003','KM93000004','KM93000005'))
 THROW 53804,N'PP06d: danh sách KM đã áp sai (mỗi món một KM, quà không tính KM giảm).',1;
IF (SELECT COUNT(*) FROM dbo.CHITIET_THANHTOAN WHERE MaHD=@code1)<>1
 OR (SELECT SUM(SoTien) FROM dbo.CHITIET_THANHTOAN WHERE MaHD=@code1)<>163400
 THROW 53804,N'PP06d: thanh toán phải đúng một dòng 163 400.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.LICHSU_DIEM WHERE MaHD=@code1 AND MaKH='KH932' AND Loai=N'Cong' AND SoDiem=163)
 OR (SELECT DiemTichLuy FROM dbo.KHACHHANG WHERE MaKH='KH932')<>163
 THROW 53804,N'PP06d: điểm mua phải là 163.',1;

-- PP07: quà tặng độc lập với KM giảm: dòng SP944 0 ₫, trừ NVL94, không nằm trong danh sách KM giảm.
IF NOT EXISTS(SELECT 1 FROM dbo.CHITIET_HD WHERE MaHD=@code1 AND MaSP='SP944' AND SoLuong=1
 AND GiaBan=0 AND MaKMQuaTang='KM93000005')
 THROW 53804,N'PP07: thiếu dòng quà SP944.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.TIEUHAO_HOADON WHERE MaHD=@code1 AND MaNVL='NVL93' AND SoLuong=3)
 OR NOT EXISTS(SELECT 1 FROM dbo.TIEUHAO_HOADON WHERE MaHD=@code1 AND MaNVL='NVL94' AND SoLuong=1)
 THROW 53804,N'PP07: tiêu hao phải gồm 3 NVL93 + 1 NVL94.',1;
IF (SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL93')<>@stock93-3
 OR (SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL94')<>@stock94-1
 THROW 53804,N'PP07: tồn kho sau thanh toán sai.',1;

-- PP07b: đơn mở có MaKM legacy KM93000006 (tạm dừng, ALL 50%) vẫn được xét theo ngày và thắng ALL 5%.
-- Từ 039 sp_TaoHoaDonTuNhap không còn @Promotion; mô phỏng bản nháp legacy bằng cách
-- gán MaKM trực tiếp cho đơn đang mở (dữ liệu legacy của app cũ), không đụng đơn đã trả.
DECLARE @lines2 dbo.SalesDraftLines; INSERT @lines2 VALUES(1,'SP940',1,NULL,100000);
DELETE @codes; INSERT @codes EXEC dbo.sp_CapMa 'HOADON'; SET @code=(SELECT Ma FROM @codes); SET @id=NEWID();
EXEC dbo.sp_TaoHoaDonTuNhap @Actor=@a,@Stamp=@s,@Code=@code,@RequestId=@id,@Channel=N'Mang di',
 @Customer='KH931',@Lines=@lines2;
DECLARE @code2 char(10)=@code;
UPDATE dbo.HOADON SET MaKM='KM93000006' WHERE MaHD=@code2 AND TrangThai=N'Chua thanh toan';
IF @@ROWCOUNT<>1 THROW 53804,N'PP07b: không gán được MaKM legacy cho đơn mở.',1;
SELECT @rev=Revision FROM dbo.HOADON WHERE MaHD=@code2;
IF EXISTS(SELECT 1 FROM dbo.fn_CheckoutPreview(@code2) WHERE Discount<>50000 OR PromotionDiscount<>50000
 OR TierDiscount<>0 OR PercentDiscount<>50 OR PromotionCount<>1 OR Points<>50)
 THROW 53804,N'PP07b: KM legacy 50% phải thắng ALL 5%.',1;
EXEC dbo.sp_SalesCheckoutPreview @Actor=@a,@Stamp=@s,@Code=@code2,@ExpectedRevision=@rev;
SELECT @token=PreviewToken FROM dbo.fn_CheckoutPreview(@code2);
DELETE @payments; INSERT @payments VALUES('PT001',50000,NULL); SET @id=NEWID();
EXEC dbo.sp_ThanhToanHoaDon @Actor=@a,@Stamp=@s,@Code=@code2,@ExpectedRevision=@rev,
 @RequestId=@id,@ExpectedPreviewToken=@token,@ThanhToan=@payments;
IF NOT EXISTS(SELECT 1 FROM dbo.HoaDonUuDaiSnapshot WHERE MaHD=@code2 AND Kind='Checkout'
 AND PercentDiscount=50 AND TierPercent=0 AND PromotionPercent=50 AND TierDiscount=0
 AND PromotionDiscount=50000 AND PromotionCount=1 AND Discount=50000)
 OR NOT EXISTS(SELECT 1 FROM dbo.HoaDonUuDaiApDung WHERE MaHD=@code2 AND MaKM='KM93000006' AND Discount=50000)
 THROW 53804,N'PP07b: snapshot legacy sai.',1;
IF (SELECT DiemTichLuy FROM dbo.KHACHHANG WHERE MaKH='KH931')<>50
 THROW 53804,N'PP07b: KH931 phải được 50 điểm.',1;

-- PP06e: đơn đã trả khóa theo snapshot; bật lại KM3 không làm đổi số đã chốt.
SELECT @kmRev=Revision FROM dbo.KHUYENMAI WHERE MaKM='KM93000003';
SET @id=NEWID();
EXEC dbo.sp_PromotionWrite @Actor=@a,@Stamp=@s,@Action='Resume',@Code='KM93000003',
 @RequestId=@id,@ExpectedRevision=@kmRev;
IF EXISTS(SELECT 1 FROM dbo.fn_HoaDonGiamGia(@code1) WHERE Discount<>36600)
 OR (SELECT COUNT(*) FROM dbo.fn_HoaDonUuDaiApDung(@code1))<>2
 OR NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD=@code1 AND GiamGia=36600)
 THROW 53804,N'PP06e: hóa đơn đã trả đổi số sau khi bật lại KM.',1;
IF @@TRANCOUNT<>0 OR XACT_STATE()<>0 THROW 53804,N'Giao dịch không sạch.',1;
