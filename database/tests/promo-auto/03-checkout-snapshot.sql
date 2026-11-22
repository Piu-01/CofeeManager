-- 03: chốt đơn, snapshot ưu đãi + danh sách áp dụng, token đổi khi tạm dừng, trigger bảo vệ.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF DB_NAME() NOT LIKE 'CoffeeManager_Test_PromoAuto[_]%' OR OBJECT_ID('dbo.PromoAutoTestMarker') IS NULL
 THROW 53704,N'Chỉ DB test PromoAuto có marker.',1;
DECLARE @a int,@s binary(8),@today date=CONVERT(date,SYSDATETIME()),@id uniqueidentifier=NEWID();
SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001';
DECLARE @codes TABLE(Loai varchar(20),Ma char(10));
INSERT @codes EXEC dbo.sp_CapMa 'HOADON';
DECLARE @hd char(10); SELECT @hd=Ma FROM @codes;
EXEC dbo.sp_TaoHoaDon @Actor=@a,@Stamp=@s,@Code=@hd,@RequestId=@id,@Channel=N'Mang di',@Customer='KH902',@Product='SP001',@Quantity=2;
IF (SELECT GiamGia FROM dbo.HOADON WHERE MaHD=@hd)<>15000
 THROW 53704,N'Đơn mới phải tự giảm 15000 (hạng 2500 + FIRST 10000 + ALL 2500).',1;
DECLARE @rev binary(8)=(SELECT Revision FROM dbo.HOADON WHERE MaHD=@hd);
DECLARE @token binary(32),@discount decimal(18,0),@tierDiscount decimal(18,0),@promoDiscount decimal(18,0),@count int;
SELECT @token=PreviewToken,@discount=Discount,@tierDiscount=TierDiscount,@promoDiscount=PromotionDiscount,@count=PromotionCount
 FROM dbo.fn_CheckoutPreview(@hd);
IF @token IS NULL OR @discount<>15000 OR @tierDiscount<>2500 OR @promoDiscount<>12500 OR @count<>2
 THROW 53704,N'Preview chốt đơn sai số ưu đãi.',1;
EXEC dbo.sp_SalesCheckoutPreview @a,@s,@hd,@rev;

-- Tạm dừng ưu đãi ALL 5%: tổng đổi, token đổi.
DECLARE @kmRev binary(8)=(SELECT Revision FROM dbo.KHUYENMAI WHERE MaKM='KM90000003');
DECLARE @pauseId uniqueidentifier=NEWID();
EXEC dbo.sp_PromotionWrite @Actor=@a,@Stamp=@s,@Action='Pause',@Code='KM90000003',@RequestId=@pauseId,@ExpectedRevision=@kmRev;
DECLARE @tokenPaused binary(32),@discountPaused decimal(18,0);
SELECT @tokenPaused=PreviewToken,@discountPaused=Discount FROM dbo.fn_CheckoutPreview(@hd);
IF @discountPaused<>12500 OR @tokenPaused=@token
 THROW 53704,N'Tạm dừng ALL 5% phải giảm còn 12500 và token phải đổi.',1;
DECLARE @pay dbo.DanhSachThanhToan; INSERT @pay(MaPT,SoTien) VALUES('PT001',35000);
DECLARE @checkoutId uniqueidentifier=NEWID();
BEGIN TRY
 EXEC dbo.sp_ThanhToanHoaDon @Actor=@a,@Stamp=@s,@Code=@hd,@ExpectedRevision=@rev,@RequestId=@checkoutId,
  @ExpectedPreviewToken=@token,@ThanhToan=@pay;
 THROW 53704,N'Token cũ phải bị từ chối.',1;
END TRY
BEGIN CATCH
 IF ERROR_NUMBER()<>52201 THROW;
END CATCH;
IF EXISTS(SELECT 1 FROM dbo.CHITIET_THANHTOAN WHERE MaHD=@hd) OR EXISTS(SELECT 1 FROM dbo.HoaDonUuDaiSnapshot WHERE MaHD=@hd)
 OR (SELECT TrangThai FROM dbo.HOADON WHERE MaHD=@hd)<>N'Chua thanh toan'
 THROW 53704,N'Checkout lỗi token vẫn để lại dữ liệu.',1;

-- Bật lại: tổng về 15000, token mới, chốt thành công.
DECLARE @kmRev2 binary(8)=(SELECT Revision FROM dbo.KHUYENMAI WHERE MaKM='KM90000003');
DECLARE @resumeId uniqueidentifier=NEWID();
EXEC dbo.sp_PromotionWrite @Actor=@a,@Stamp=@s,@Action='Resume',@Code='KM90000003',@RequestId=@resumeId,@ExpectedRevision=@kmRev2;
DECLARE @newToken binary(32); SELECT @newToken=PreviewToken FROM dbo.fn_CheckoutPreview(@hd);
IF @newToken=@tokenPaused OR @newToken=@token THROW 53704,N'Token sau khi bật lại phải mới.',1;
DECLARE @nvl1 decimal(18,4)=(SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL01');
DECLARE @nvl3 decimal(18,4)=(SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL03');
DECLARE @checkoutId2 uniqueidentifier=NEWID();
EXEC dbo.sp_ThanhToanHoaDon @Actor=@a,@Stamp=@s,@Code=@hd,@ExpectedRevision=@rev,@RequestId=@checkoutId2,
 @ExpectedPreviewToken=@newToken,@ThanhToan=@pay;

-- Snapshot + danh sách + điểm + kho.
IF NOT EXISTS(SELECT 1 FROM dbo.HoaDonUuDaiSnapshot WHERE MaHD=@hd AND Kind='Checkout' AND Source='TierAndPromotion'
 AND TierPercent=5 AND PromotionPercent=25 AND PercentDiscount=30 AND TierDiscount=2500 AND PromotionDiscount=12500
 AND PromotionCount=2 AND PromotionDigest IS NOT NULL AND Discount=15000)
 THROW 53704,N'Snapshot ưu đãi sau chốt sai.',1;
IF (SELECT COUNT(*) FROM dbo.HoaDonUuDaiApDung WHERE MaHD=@hd)<>2
 OR (SELECT SUM(Discount) FROM dbo.HoaDonUuDaiApDung WHERE MaHD=@hd)<>12500
 OR NOT EXISTS(SELECT 1 FROM dbo.HoaDonUuDaiApDung WHERE MaHD=@hd AND MaKM='KM90000001' AND Discount=10000)
 OR NOT EXISTS(SELECT 1 FROM dbo.HoaDonUuDaiApDung WHERE MaHD=@hd AND MaKM='KM90000003' AND Discount=2500)
 THROW 53704,N'Danh sách ưu đãi áp dụng sau chốt sai.',1;
IF (SELECT TrangThai FROM dbo.HOADON WHERE MaHD=@hd)<>N'Da thanh toan'
 OR (SELECT SUM(SoTien) FROM dbo.CHITIET_THANHTOAN WHERE MaHD=@hd)<>35000
 THROW 53704,N'Đơn chưa chốt đúng 35000.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.LICHSU_DIEM WHERE MaHD=@hd AND MaKH='KH902' AND SoDiem=35)
 THROW 53704,N'Điểm mua phải là FLOOR((50000-15000)/1000)=35.',1;
IF (SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL01')<>@nvl1-0.036
 OR (SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL03')<>@nvl3-0.02
 THROW 53704,N'Tiêu hao kho không khớp công thức SP001 x2.',1;

-- Bảo vệ lịch sử: sửa/xóa danh sách áp dụng và snapshot đều bị chặn.
BEGIN TRY
 UPDATE dbo.HoaDonUuDaiApDung SET [Percent]=1 WHERE MaHD=@hd;
 THROW 53704,N'Phải chặn sửa danh sách ưu đãi.',1;
END TRY
BEGIN CATCH
 IF ERROR_NUMBER()<>53307 THROW;
END CATCH;
BEGIN TRY
 DELETE dbo.HoaDonUuDaiApDung WHERE MaHD=@hd;
 THROW 53704,N'Phải chặn xóa danh sách ưu đãi.',1;
END TRY
BEGIN CATCH
 IF ERROR_NUMBER()<>53307 THROW;
END CATCH;
BEGIN TRY
 UPDATE dbo.HoaDonUuDaiSnapshot SET RecordedAt=SYSDATETIME() WHERE MaHD=@hd;
 THROW 53704,N'Phải chặn sửa snapshot.',1;
END TRY
BEGIN CATCH
 IF ERROR_NUMBER()<>52207 THROW;
END CATCH;

-- Đọc lại sau chốt: POS read + chi tiết hóa đơn vẫn chạy; danh sách đọc từ bảng snapshot.
EXEC dbo.sp_SalesOrderGet @a,@s,@hd;
EXEC dbo.sp_InvoiceDetail @a,@s,@hd;
DECLARE @closed TABLE(MaKM char(10),Name nvarchar(200),[Percent] int,BaseAmount decimal(18,0),Discount decimal(18,0),
 PromotionRevision binary(8),Audience varchar(10),Scope varchar(4));
INSERT @closed SELECT * FROM dbo.fn_HoaDonUuDaiApDung(@hd);
IF (SELECT COUNT(*) FROM @closed)<>2 OR (SELECT SUM(Discount) FROM @closed)<>12500
 THROW 53704,N'Danh sách ưu đãi đọc sau chốt sai.',1;
IF @@TRANCOUNT<>0 OR XACT_STATE()<>0 THROW 53704,N'Giao dịch không sạch.',1;
