-- 03: chốt đơn có quà, snapshot, token khi tạm ngưng, thiếu tồn, bảo vệ lịch sử.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF DB_NAME() NOT LIKE 'CoffeeManager_Test_PromoV2[_]%' OR OBJECT_ID('dbo.PromoV2TestMarker') IS NULL
 THROW 53704,N'Chỉ DB test PromoV2 có marker.',1;
DECLARE @a int,@s binary(8),@id uniqueidentifier,@today date=CONVERT(date,SYSDATETIME());
SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001';
DECLARE @codes TABLE(Loai varchar(20),Ma char(10));
DECLARE @hdA char(10),@hdB char(10),@hdC char(10),@hdD char(10);

-- ===== A: khách vãng lai, SP920 x2, giảm 10% + 20% món + 5% vãng lai = 35000 =====
DELETE @codes; INSERT @codes EXEC dbo.sp_CapMa 'HOADON'; SELECT @hdA=Ma FROM @codes;
SET @id=NEWID();
EXEC dbo.sp_TaoHoaDon @Actor=@a,@Stamp=@s,@Code=@hdA,@RequestId=@id,@Channel=N'Mang di',
 @Product='SP920',@Quantity=2;
IF (SELECT TongTien FROM dbo.HOADON WHERE MaHD=@hdA)<>100000
 OR (SELECT GiamGia FROM dbo.HOADON WHERE MaHD=@hdA)<>35000
 OR (SELECT ThanhTien FROM dbo.HOADON WHERE MaHD=@hdA)<>65000
 THROW 53704,N'Đơn A tạo sai tiền trước chốt.',1;
DECLARE @revA binary(8)=(SELECT Revision FROM dbo.HOADON WHERE MaHD=@hdA);
DECLARE @tokenA binary(32),@discA decimal(18,0),@tierA int,@ppA int,@tierDiscA decimal(18,0),@promoDiscA decimal(18,0),
 @countA int,@giftA int,@payA decimal(38,0);
SELECT @tokenA=PreviewToken,@discA=Discount,@tierA=TierPercent,@ppA=PromotionPercent,@tierDiscA=TierDiscount,
 @promoDiscA=PromotionDiscount,@countA=PromotionCount,@giftA=GiftCount,@payA=Payable FROM dbo.fn_CheckoutPreview(@hdA);
IF @tokenA IS NULL OR @discA<>35000 OR @tierA<>0 OR @ppA<>35 OR @tierDiscA<>0 OR @promoDiscA<>35000
 OR @countA<>3 OR @giftA<>1 OR @payA<>65000
 THROW 53704,N'Preview đơn A sai.',1;
EXEC dbo.sp_SalesCheckoutPreview @Actor=@a,@Stamp=@s,@Code=@hdA,@ExpectedRevision=@revA;
DECLARE @pay dbo.DanhSachThanhToan; INSERT @pay(MaPT,SoTien) VALUES('PT001',65000);
SET @id=NEWID();
EXEC dbo.sp_ThanhToanHoaDon @Actor=@a,@Stamp=@s,@Code=@hdA,@ExpectedRevision=@revA,@RequestId=@id,
 @ExpectedPreviewToken=@tokenA,@ThanhToan=@pay;
IF (SELECT TrangThai FROM dbo.HOADON WHERE MaHD=@hdA)<>N'Da thanh toan'
 OR (SELECT TongTien FROM dbo.HOADON WHERE MaHD=@hdA)<>100000
 OR (SELECT GiamGia FROM dbo.HOADON WHERE MaHD=@hdA)<>35000
 OR (SELECT ThanhTien FROM dbo.HOADON WHERE MaHD=@hdA)<>65000
 OR (SELECT SUM(SoTien) FROM dbo.CHITIET_THANHTOAN WHERE MaHD=@hdA)<>65000
 THROW 53704,N'Đơn A chốt sai tiền.',1;
IF (SELECT COUNT(*) FROM dbo.CHITIET_HD WHERE MaHD=@hdA)<>2
 OR NOT EXISTS(SELECT 1 FROM dbo.CHITIET_HD WHERE MaHD=@hdA AND MaSP='SP920' AND SoLuong=2 AND GiaBan=50000 AND MaKMQuaTang IS NULL)
 OR NOT EXISTS(SELECT 1 FROM dbo.CHITIET_HD WHERE MaHD=@hdA AND MaSP='SP921' AND SoLuong=2 AND GiaBan=0
  AND MaKMQuaTang='KM92000005' AND GhiChu=N'Tặng kèm')
 THROW 53704,N'Dòng quà đơn A sai.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.HoaDonUuDaiSnapshot WHERE MaHD=@hdA AND Kind='Checkout' AND Source='Promotion'
 AND TierCode IS NULL AND PromotionCode IS NULL AND PercentDiscount=35 AND TierPercent=0 AND PromotionPercent=35
 AND TierDiscount=0 AND PromotionDiscount=35000 AND PromotionCount=3 AND Discount=35000 AND PromotionDigest IS NOT NULL)
 THROW 53704,N'Snapshot đơn A sai.',1;
IF (SELECT COUNT(*) FROM dbo.HoaDonUuDaiApDung WHERE MaHD=@hdA)<>3
 OR (SELECT SUM(Discount) FROM dbo.HoaDonUuDaiApDung WHERE MaHD=@hdA)<>35000
 OR NOT EXISTS(SELECT 1 FROM dbo.HoaDonUuDaiApDung WHERE MaHD=@hdA AND MaKM='KM92000001' AND Discount=10000 AND Audience='ALL' AND Scope='ALL')
 OR NOT EXISTS(SELECT 1 FROM dbo.HoaDonUuDaiApDung WHERE MaHD=@hdA AND MaKM='KM92000002' AND Discount=20000 AND Scope='ITEM')
 OR NOT EXISTS(SELECT 1 FROM dbo.HoaDonUuDaiApDung WHERE MaHD=@hdA AND MaKM='KM92000004' AND Discount=5000 AND Audience='WALKIN')
 THROW 53704,N'Danh sách ưu đãi đơn A sai.',1;
IF (SELECT COALESCE(SUM(SoLuong),0) FROM dbo.TIEUHAO_HOADON WHERE MaHD=@hdA AND MaNVL='NVL91')<>2
 OR (SELECT COALESCE(SUM(SoLuong),0) FROM dbo.TIEUHAO_HOADON WHERE MaHD=@hdA AND MaNVL='NVL92')<>2
 THROW 53704,N'Tiêu hao đơn A sai (quà phải trừ kho).',1;
IF (SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL91')<>(SELECT Value FROM dbo.PromoV2Baseline WHERE Name='StockNVL91')-2
 OR (SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL92')<>(SELECT Value FROM dbo.PromoV2Baseline WHERE Name='StockNVL92')-2
 THROW 53704,N'Tồn kho sau đơn A sai.',1;
IF EXISTS(SELECT 1 FROM dbo.LICHSU_DIEM WHERE MaHD=@hdA) THROW 53704,N'Đơn vãng lai không được cộng điểm.',1;
IF (SELECT COUNT(*) FROM dbo.fn_HoaDonQuaTang(@hdA))<>1
 OR NOT EXISTS(SELECT 1 FROM dbo.fn_HoaDonQuaTang(@hdA) WHERE MaKM='KM92000005' AND MaSP='SP921' AND SoLuong=2)
 THROW 53704,N'Quà đã ghi đơn A sai.',1;
EXEC dbo.sp_InvoiceDetail @Actor=@a,@Stamp=@s,@Code=@hdA;
EXEC dbo.sp_SalesReadCore @hdA;
EXEC dbo.sp_InvoiceList @Actor=@a,@Stamp=@s,@DateBasis='Paid',@FromDate=@today,@ToDate=@today;

-- ===== B: KH922 hạng VANG, SP920 x1, 5% hạng + 10% + 20% + 15% = 25000 =====
DELETE @codes; INSERT @codes EXEC dbo.sp_CapMa 'HOADON'; SELECT @hdB=Ma FROM @codes;
SET @id=NEWID();
EXEC dbo.sp_TaoHoaDon @Actor=@a,@Stamp=@s,@Code=@hdB,@RequestId=@id,@Channel=N'Mang di',
 @Customer='KH922',@Product='SP920',@Quantity=1;
DECLARE @revB binary(8)=(SELECT Revision FROM dbo.HOADON WHERE MaHD=@hdB);
DECLARE @tokenB binary(32),@discB decimal(18,0),@tierDiscB decimal(18,0),@promoDiscB decimal(18,0),
 @countB int,@giftB int,@payB decimal(38,0);
SELECT @tokenB=PreviewToken,@discB=Discount,@tierDiscB=TierDiscount,@promoDiscB=PromotionDiscount,
 @countB=PromotionCount,@giftB=GiftCount,@payB=Payable FROM dbo.fn_CheckoutPreview(@hdB);
IF @discB<>25000 OR @tierDiscB<>2500 OR @promoDiscB<>22500 OR @countB<>3 OR @giftB<>4 OR @payB<>25000
 THROW 53704,N'Preview đơn B sai.',1;
DELETE @pay; INSERT @pay(MaPT,SoTien) VALUES('PT001',25000);
SET @id=NEWID();
EXEC dbo.sp_ThanhToanHoaDon @Actor=@a,@Stamp=@s,@Code=@hdB,@ExpectedRevision=@revB,@RequestId=@id,
 @ExpectedPreviewToken=@tokenB,@ThanhToan=@pay;
IF (SELECT TrangThai FROM dbo.HOADON WHERE MaHD=@hdB)<>N'Da thanh toan'
 OR (SELECT TongTien FROM dbo.HOADON WHERE MaHD=@hdB)<>50000
 OR (SELECT GiamGia FROM dbo.HOADON WHERE MaHD=@hdB)<>25000
 OR (SELECT ThanhTien FROM dbo.HOADON WHERE MaHD=@hdB)<>25000
 THROW 53704,N'Đơn B chốt sai tiền.',1;
IF (SELECT COUNT(*) FROM dbo.CHITIET_HD WHERE MaHD=@hdB)<>5
 THROW 53704,N'Đơn B phải có 1 dòng bán + 4 dòng quà.',1;
IF (SELECT COUNT(*) FROM dbo.CHITIET_HD WHERE MaHD=@hdB AND MaSP='SP921' AND MaKMQuaTang='KM92000005' AND SoLuong=2)<>1
 OR (SELECT COUNT(*) FROM dbo.CHITIET_HD WHERE MaHD=@hdB AND MaSP='SP921' AND MaKMQuaTang='KM92000006' AND SoLuong=1)<>1
 OR (SELECT COUNT(*) FROM dbo.CHITIET_HD WHERE MaHD=@hdB AND MaSP='SP922' AND MaKMQuaTang='KM92000006' AND SoLuong=1)<>1
 OR (SELECT COUNT(*) FROM dbo.CHITIET_HD WHERE MaHD=@hdB AND MaSP='SP922' AND MaKMQuaTang='KM92000007' AND SoLuong=1)<>1
 OR EXISTS(SELECT 1 FROM dbo.CHITIET_HD WHERE MaHD=@hdB AND MaKMQuaTang IS NOT NULL AND (GiaBan<>0 OR GhiChu<>N'Tặng kèm'))
 THROW 53704,N'Các dòng quà đơn B sai (mỗi KM một lần, cùng món ghi đủ).',1;
IF NOT EXISTS(SELECT 1 FROM dbo.HoaDonUuDaiSnapshot WHERE MaHD=@hdB AND Kind='Checkout' AND Source='TierAndPromotion'
 AND TierCode='VANG' AND PercentDiscount=50 AND TierPercent=5 AND PromotionPercent=45 AND TierDiscount=2500
 AND PromotionDiscount=22500 AND PromotionCount=3 AND Discount=25000)
 THROW 53704,N'Snapshot đơn B sai.',1;
IF (SELECT SUM(Discount) FROM dbo.HoaDonUuDaiApDung WHERE MaHD=@hdB)<>22500
 OR NOT EXISTS(SELECT 1 FROM dbo.HoaDonUuDaiApDung WHERE MaHD=@hdB AND MaKM='KM92000003' AND Discount=7500 AND Audience='TIER')
 OR NOT EXISTS(SELECT 1 FROM dbo.HoaDonUuDaiApDung WHERE MaHD=@hdB AND MaKM='KM92000001' AND Discount=5000)
 OR NOT EXISTS(SELECT 1 FROM dbo.HoaDonUuDaiApDung WHERE MaHD=@hdB AND MaKM='KM92000002' AND Discount=10000)
 THROW 53704,N'Danh sách ưu đãi đơn B sai.',1;
IF (SELECT SoLuong FROM dbo.TIEUHAO_HOADON WHERE MaHD=@hdB AND MaNVL='NVL91')<>1
 OR (SELECT SoLuong FROM dbo.TIEUHAO_HOADON WHERE MaHD=@hdB AND MaNVL='NVL92')<>7
 THROW 53704,N'Tiêu hao đơn B phải là NVL91 1, NVL92 7.',1;
IF (SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL91')<>(SELECT Value FROM dbo.PromoV2Baseline WHERE Name='StockNVL91')-3
 OR (SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL92')<>(SELECT Value FROM dbo.PromoV2Baseline WHERE Name='StockNVL92')-9
 THROW 53704,N'Tồn kho sau đơn B sai.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.LICHSU_DIEM WHERE MaHD=@hdB AND MaKH='KH922' AND SoDiem=25 AND Loai=N'Cong')
 OR (SELECT DiemTichLuy FROM dbo.KHACHHANG WHERE MaKH='KH922')<>25
 THROW 53704,N'Điểm đơn B phải là 25, không tính quà.',1;

-- ===== C: tạm ngưng KM tặng sau preview → token cũ bị từ chối 52201 =====
DELETE @codes; INSERT @codes EXEC dbo.sp_CapMa 'HOADON'; SELECT @hdC=Ma FROM @codes;
SET @id=NEWID();
EXEC dbo.sp_TaoHoaDon @Actor=@a,@Stamp=@s,@Code=@hdC,@RequestId=@id,@Channel=N'Mang di',
 @Product='SP920',@Quantity=1;
DECLARE @revC binary(8)=(SELECT Revision FROM dbo.HOADON WHERE MaHD=@hdC);
DECLARE @tokenC binary(32),@tokenCPaused binary(32),@tokenCResumed binary(32);
SELECT @tokenC=PreviewToken FROM dbo.fn_CheckoutPreview(@hdC);
IF (SELECT GiftCount FROM dbo.fn_CheckoutPreview(@hdC))<>1 THROW 53704,N'Đơn C chưa có quà dự kiến.',1;
DECLARE @rev5 binary(8)=(SELECT Revision FROM dbo.KHUYENMAI WHERE MaKM='KM92000005');
SET @id=NEWID();
EXEC dbo.sp_PromotionWrite @Actor=@a,@Stamp=@s,@Action='Pause',@Code='KM92000005',@RequestId=@id,
 @ExpectedRevision=@rev5;
SELECT @tokenCPaused=PreviewToken FROM dbo.fn_CheckoutPreview(@hdC);
IF @tokenCPaused=@tokenC OR (SELECT GiftCount FROM dbo.fn_CheckoutPreview(@hdC))<>0
 OR (SELECT Discount FROM dbo.fn_CheckoutPreview(@hdC))<>17500
 THROW 53704,N'Tạm dừng quà phải đổi token và bỏ quà dự kiến.',1;
DELETE @pay; INSERT @pay(MaPT,SoTien) VALUES('PT001',32500);
BEGIN TRY
 SET @id=NEWID();
 EXEC dbo.sp_ThanhToanHoaDon @Actor=@a,@Stamp=@s,@Code=@hdC,@ExpectedRevision=@revC,@RequestId=@id,
  @ExpectedPreviewToken=@tokenC,@ThanhToan=@pay;
 THROW 53704,N'Token cũ phải bị từ chối.',1;
END TRY
BEGIN CATCH
 IF ERROR_NUMBER()<>52201 THROW;
END CATCH;
IF (SELECT TrangThai FROM dbo.HOADON WHERE MaHD=@hdC)<>N'Chua thanh toan'
 OR EXISTS(SELECT 1 FROM dbo.CHITIET_THANHTOAN WHERE MaHD=@hdC)
 OR EXISTS(SELECT 1 FROM dbo.HoaDonUuDaiSnapshot WHERE MaHD=@hdC)
 OR (SELECT COUNT(*) FROM dbo.CHITIET_HD WHERE MaHD=@hdC)<>1
 THROW 53704,N'Checkout lỗi token vẫn để lại dữ liệu.',1;
DECLARE @rev5b binary(8)=(SELECT Revision FROM dbo.KHUYENMAI WHERE MaKM='KM92000005');
SET @id=NEWID();
EXEC dbo.sp_PromotionWrite @Actor=@a,@Stamp=@s,@Action='Resume',@Code='KM92000005',@RequestId=@id,
 @ExpectedRevision=@rev5b;
SELECT @tokenCResumed=PreviewToken FROM dbo.fn_CheckoutPreview(@hdC);
IF @tokenCResumed=@tokenC OR @tokenCResumed=@tokenCPaused OR (SELECT GiftCount FROM dbo.fn_CheckoutPreview(@hdC))<>1
 THROW 53704,N'Bật lại quà phải có token mới và quà trở lại.',1;

-- ===== D: thiếu nguyên liệu do quà → 52203, rollback sạch =====
DELETE @codes; INSERT @codes EXEC dbo.sp_CapMa 'HOADON'; SELECT @hdD=Ma FROM @codes;
SET @id=NEWID();
EXEC dbo.sp_TaoHoaDon @Actor=@a,@Stamp=@s,@Code=@hdD,@RequestId=@id,@Channel=N'Mang di',
 @Product='SP920',@Quantity=1;
DECLARE @revD binary(8)=(SELECT Revision FROM dbo.HOADON WHERE MaHD=@hdD);
DECLARE @tokenD binary(32); SELECT @tokenD=PreviewToken FROM dbo.fn_CheckoutPreview(@hdD);
UPDATE dbo.NGUYENVATLIEU SET SoLuongTon=0 WHERE MaNVL='NVL92';
BEGIN TRY
 SET @id=NEWID();
 EXEC dbo.sp_ThanhToanHoaDon @Actor=@a,@Stamp=@s,@Code=@hdD,@ExpectedRevision=@revD,@RequestId=@id,
  @ExpectedPreviewToken=@tokenD,@ThanhToan=@pay;
 THROW 53704,N'Thiếu nguyên liệu phải báo 52203.',1;
END TRY
BEGIN CATCH
 IF ERROR_NUMBER()<>52203 THROW;
END CATCH;
IF (SELECT TrangThai FROM dbo.HOADON WHERE MaHD=@hdD)<>N'Chua thanh toan'
 OR EXISTS(SELECT 1 FROM dbo.CHITIET_THANHTOAN WHERE MaHD=@hdD)
 OR EXISTS(SELECT 1 FROM dbo.HoaDonUuDaiSnapshot WHERE MaHD=@hdD)
 OR (SELECT COUNT(*) FROM dbo.CHITIET_HD WHERE MaHD=@hdD)<>1
 OR (SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL92')<>0
 THROW 53704,N'Checkout thiếu tồn không rollback sạch.',1;
UPDATE dbo.NGUYENVATLIEU SET SoLuongTon=(SELECT Value FROM dbo.PromoV2Baseline WHERE Name='StockNVL92')-9
 WHERE MaNVL='NVL92';
EXEC dbo.sp_InvoiceDetail @Actor=@a,@Stamp=@s,@Code=@hdD;
IF (SELECT COUNT(*) FROM dbo.fn_HoaDonQuaTang(@hdD))<>1
 OR NOT EXISTS(SELECT 1 FROM dbo.fn_HoaDonQuaTang(@hdD) WHERE MaKM='KM92000005' AND MaSP='SP921' AND SoLuong=2)
 THROW 53704,N'Đơn mở D phải trả quà dự kiến.',1;

-- ===== Bảo vệ lịch sử =====
BEGIN TRY
 UPDATE dbo.CHITIET_HD SET SoLuong=3 WHERE MaHD=@hdA AND MaKMQuaTang='KM92000005';
 THROW 53704,N'Phải chặn sửa dòng quà đã trả.',1;
END TRY
BEGIN CATCH
 IF ERROR_NUMBER()<>51001 THROW;
END CATCH;
BEGIN TRY
 DELETE dbo.CHITIET_HD WHERE MaHD=@hdA AND MaKMQuaTang='KM92000005';
 THROW 53704,N'Phải chặn xóa dòng quà đã trả.',1;
END TRY
BEGIN CATCH
 IF ERROR_NUMBER()<>51001 THROW;
END CATCH;
BEGIN TRY
 UPDATE dbo.CHITIET_HD SET SoLuong=3 WHERE MaHD=@hdA AND MaKMQuaTang IS NULL;
 THROW 53704,N'Phải chặn sửa dòng bán của hóa đơn đã trả.',1;
END TRY
BEGIN CATCH
 IF ERROR_NUMBER()<>51001 THROW;
END CATCH;
BEGIN TRY
 UPDATE dbo.CHITIET_THANHTOAN SET SoTien=1 WHERE MaHD=@hdA;
 THROW 53704,N'Phải chặn sửa thanh toán đã chốt.',1;
END TRY
BEGIN CATCH
 IF ERROR_NUMBER()<>51002 THROW;
END CATCH;
BEGIN TRY
 DELETE dbo.CHITIET_THANHTOAN WHERE MaHD=@hdA;
 THROW 53704,N'Phải chặn xóa thanh toán đã chốt.',1;
END TRY
BEGIN CATCH
 IF ERROR_NUMBER()<>51002 THROW;
END CATCH;
BEGIN TRY
 UPDATE dbo.HoaDonUuDaiSnapshot SET RecordedAt=SYSDATETIME() WHERE MaHD=@hdA;
 THROW 53704,N'Phải chặn sửa snapshot.',1;
END TRY
BEGIN CATCH
 IF ERROR_NUMBER()<>52207 THROW;
END CATCH;
BEGIN TRY
 DELETE dbo.HoaDonUuDaiApDung WHERE MaHD=@hdA;
 THROW 53704,N'Phải chặn xóa danh sách ưu đãi.',1;
END TRY
BEGIN CATCH
 IF ERROR_NUMBER()<>53307 THROW;
END CATCH;
DECLARE @rev5c binary(8)=(SELECT Revision FROM dbo.KHUYENMAI WHERE MaKM='KM92000005');
SET @id=NEWID();
BEGIN TRY
 EXEC dbo.sp_PromotionWrite @Actor=@a,@Stamp=@s,@Action='Delete',@Code='KM92000005',@RequestId=@id,
  @ExpectedRevision=@rev5c;
 THROW 53704,N'Phải chặn xóa KM tặng đã dùng.',1;
END TRY
BEGIN CATCH
 IF ERROR_NUMBER()<>52006 THROW;
END CATCH;
DECLARE @rev1 binary(8)=(SELECT Revision FROM dbo.KHUYENMAI WHERE MaKM='KM92000001');
SET @id=NEWID();
BEGIN TRY
 EXEC dbo.sp_PromotionWrite @Actor=@a,@Stamp=@s,@Action='Delete',@Code='KM92000001',@RequestId=@id,
  @ExpectedRevision=@rev1;
 THROW 53704,N'Phải chặn xóa KM giảm đã dùng.',1;
END TRY
BEGIN CATCH
 IF ERROR_NUMBER()<>52006 THROW;
END CATCH;

-- Dữ liệu đã trả vẫn nguyên sau các lần bị chặn.
IF (SELECT COUNT(*) FROM dbo.CHITIET_HD WHERE MaHD=@hdA)<>2
 OR (SELECT SUM(SoTien) FROM dbo.CHITIET_THANHTOAN WHERE MaHD=@hdA)<>65000
 OR (SELECT COUNT(*) FROM dbo.HoaDonUuDaiApDung WHERE MaHD=@hdA)<>3
 OR (SELECT COUNT(*) FROM dbo.HoaDonUuDaiSnapshot WHERE MaHD=@hdA)<>1
 THROW 53704,N'Lần chặn đã làm đổi dữ liệu hóa đơn đã trả.',1;
IF @@TRANCOUNT<>0 OR XACT_STATE()<>0 THROW 53704,N'Giao dịch không sạch.',1;
