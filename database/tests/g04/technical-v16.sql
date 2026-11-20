-- Technical pending-payment dependency smoke only; test-owned demo database.
IF DB_NAME() NOT LIKE 'CoffeeManager_Test_G04_%' OR OBJECT_ID('dbo.G04TestMarker') IS NULL THROW 52107,'Owned test database required',1;
UPDATE dbo.TAIKHOAN SET HoatDong=1 WHERE MaNV IN ('NV001','NV004');
DECLARE @a int,@s binary(8),@cash int,@cashStamp binary(8),@id uniqueidentifier=NEWID(),@r binary(8),@code varchar(10);
SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001';
SELECT @cash=MaTK,@cashStamp=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV004';
IF OBJECT_ID('dbo.sp_YeuCauThanhToan') IS NULL OR OBJECT_ID('dbo.sp_MoLaiHoaDon') IS NULL
 OR OBJECT_ID('dbo.sp_SalesCheckPayable') IS NULL THROW 52107,'Pending-payment objects missing',1;
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE name='CHK_HD_TrangThai' AND definition LIKE N'%Cho thanh toan%')
 THROW 52107,'Status check missing Cho thanh toan',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.HOADON') AND name='UX_HD_BanMo' AND filter_definition LIKE N'%Cho thanh toan%')
 THROW 52107,'Open-table unique index missing Cho thanh toan',1;
DECLARE @openBefore int=(SELECT COUNT(*) FROM dbo.HOADON WHERE TrangThai IN (N'Chua thanh toan',N'Cho thanh toan'));

-- Đơn tại bàn chuyển Chờ thanh toán, khóa sửa và hiện đúng trên sơ đồ bàn.
DECLARE @codes TABLE(Loai varchar(20),Ma char(10)); INSERT @codes EXEC dbo.sp_CapMa 'HOADON'; SELECT @code=Ma FROM @codes;
EXEC dbo.sp_TaoHoaDon @Actor=@a,@Stamp=@s,@Code=@code,@RequestId=@id,@Channel=N'Tai quan',@TableCode='B0001',@Product='SP001',@Quantity=1;
SELECT @r=Revision FROM dbo.HOADON WHERE MaHD=@code;
SET @id=NEWID(); EXEC dbo.sp_YeuCauThanhToan @a,@s,@code,@r,@id;
IF NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD=@code AND TrangThai=N'Cho thanh toan' AND NgayThanhToan IS NULL)
 THROW 52108,'RequestPayment did not move to Cho thanh toan',1;
IF NOT EXISTS(SELECT 1 FROM dbo.G03OperationReceipt WHERE RequestId=@id AND Operation='SalesRequestPay' AND EntityCode=@code)
 THROW 52108,'SalesRequestPay receipt missing',1;
IF NOT EXISTS(SELECT 1 FROM dbo.vw_TrangThaiBan WHERE MaBan='B0001' AND TrangThai=N'Cho thanh toan')
 THROW 52108,'vw_TrangThaiBan missing Cho thanh toan',1;
SELECT @r=Revision FROM dbo.HOADON WHERE MaHD=@code;
BEGIN TRY
 DECLARE @other char(10); DELETE @codes; INSERT @codes EXEC dbo.sp_CapMa 'HOADON'; SELECT @other=Ma FROM @codes; SET @id=NEWID();
 EXEC dbo.sp_TaoHoaDon @Actor=@a,@Stamp=@s,@Code=@other,@RequestId=@id,@Channel=N'Tai quan',@TableCode='B0001',@Product='SP001',@Quantity=1;
 THROW 52108,'Second open order accepted on busy table',1;
END TRY BEGIN CATCH IF ERROR_NUMBER()<>52104 THROW; END CATCH;
BEGIN TRY
 SET @id=NEWID(); EXEC dbo.sp_SuaDongHoaDon @Actor=@a,@Stamp=@s,@Code=@code,@ExpectedRevision=@r,@RequestId=@id,@Action='Add',@Product='SP001',@Quantity=1;
 THROW 52108,'Edit accepted while awaiting payment',1;
END TRY BEGIN CATCH IF ERROR_NUMBER()<>52102 THROW; END CATCH;

-- Thu tiền từ Chờ thanh toán bằng đúng cổng payable.
DECLARE @preview TABLE(Code char(10),Revision binary(8),Total decimal(18,0),Ship decimal(18,0),Discount decimal(18,0),Payable decimal(38,0),Source varchar(20),PercentDiscount int,PartnerPercent decimal(5,2),PartnerFee decimal(18,0),Points bigint,PreviewToken binary(32));
INSERT @preview EXEC dbo.sp_SalesCheckoutPreview @a,@s,@code,@r;
DECLARE @token binary(32),@payable decimal(18,0),@payments dbo.DanhSachThanhToan;
SELECT @token=PreviewToken,@payable=Payable FROM @preview; INSERT @payments VALUES('PT001',@payable,NULL); SET @id=NEWID();
EXEC dbo.sp_ThanhToanHoaDon @a,@s,@code,@r,@id,@token,@payments;
IF NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD=@code AND TrangThai=N'Da thanh toan' AND NgayThanhToan IS NOT NULL)
 THROW 52108,'Checkout from Cho thanh toan failed',1;
IF NOT EXISTS(SELECT 1 FROM dbo.HoaDonUuDaiSnapshot WHERE MaHD=@code AND Kind='Checkout') THROW 52108,'Checkout snapshot missing',1;

-- Hủy từ Chờ thanh toán, rồi mở lại: CASH bị chặn, ADMIN mở lại về Chưa thanh toán.
SET @id=NEWID(); DELETE @codes; INSERT @codes EXEC dbo.sp_CapMa 'HOADON'; SELECT @code=Ma FROM @codes;
EXEC dbo.sp_TaoHoaDon @Actor=@a,@Stamp=@s,@Code=@code,@RequestId=@id,@Channel=N'Mang di',@Product='SP001',@Quantity=1;
SELECT @r=Revision FROM dbo.HOADON WHERE MaHD=@code;
SET @id=NEWID(); EXEC dbo.sp_YeuCauThanhToan @a,@s,@code,@r,@id;
SELECT @r=Revision FROM dbo.HOADON WHERE MaHD=@code;
BEGIN TRY
 SET @id=NEWID(); EXEC dbo.sp_MoLaiHoaDon @cash,@cashStamp,@code,@r,@id;
 THROW 52108,'CASH reopened pending order',1;
END TRY BEGIN CATCH IF ERROR_NUMBER()<>51605 THROW; END CATCH;
SET @id=NEWID(); EXEC dbo.sp_MoLaiHoaDon @a,@s,@code,@r,@id;
IF NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD=@code AND TrangThai=N'Chua thanh toan') THROW 52109,'Reopen did not restore Chua thanh toan',1;
IF NOT EXISTS(SELECT 1 FROM dbo.G03OperationReceipt WHERE RequestId=@id AND Operation='SalesReopen' AND EntityCode=@code)
 THROW 52109,'SalesReopen receipt missing',1;
SELECT @r=Revision FROM dbo.HOADON WHERE MaHD=@code;
SET @id=NEWID(); EXEC dbo.sp_SuaDongHoaDon @Actor=@a,@Stamp=@s,@Code=@code,@ExpectedRevision=@r,@RequestId=@id,@Action='Add',@Product='SP001',@Quantity=1;
IF (SELECT COUNT(*) FROM dbo.CHITIET_HD WHERE MaHD=@code)<>2 THROW 52109,'Edit after reopen failed',1;

-- Hủy từ Chờ thanh toán.
SET @id=NEWID(); DELETE @codes; INSERT @codes EXEC dbo.sp_CapMa 'HOADON'; SELECT @code=Ma FROM @codes;
EXEC dbo.sp_TaoHoaDon @Actor=@a,@Stamp=@s,@Code=@code,@RequestId=@id,@Channel=N'Mang di',@Product='SP001',@Quantity=1;
SELECT @r=Revision FROM dbo.HOADON WHERE MaHD=@code;
SET @id=NEWID(); EXEC dbo.sp_YeuCauThanhToan @a,@s,@code,@r,@id;
SELECT @r=Revision FROM dbo.HOADON WHERE MaHD=@code;
SET @id=NEWID(); EXEC dbo.sp_HuyHoaDon @a,@s,@code,@r,@id;
IF NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD=@code AND TrangThai=N'Huy') THROW 52108,'Cancel from Cho thanh toan failed',1;

-- Đọc danh sách đơn mở và số đếm tổng quan gồm trạng thái chờ.
EXEC dbo.sp_SalesOpenOrders @a,@s;
DECLARE @dashboard TABLE(BusinessDate date,ReadAt datetime2(7),Revenue decimal(38,0),OpenOrders int,Debt decimal(38,0));
INSERT @dashboard EXEC dbo.sp_ReportDashboard @a,@s;
IF (SELECT OpenOrders FROM @dashboard)<>@openBefore+1
 THROW 52108,'Dashboard open count must include Cho thanh toan',1;
IF @@TRANCOUNT<>0 OR XACT_STATE()<>0 THROW 52108,'Transaction not clean',1;
