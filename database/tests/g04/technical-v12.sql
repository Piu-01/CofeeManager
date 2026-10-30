-- Technical checkout dependency smoke only; test-owned demo database.
IF DB_NAME() NOT LIKE 'CoffeeManager_Test_G04_%' OR OBJECT_ID('dbo.G04TestMarker') IS NULL THROW 52207,'Owned test database required',1;
UPDATE dbo.TAIKHOAN SET HoatDong=1 WHERE MaNV='NV001';
DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@r binary(8),@code varchar(10);
SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001';
DECLARE @codes TABLE(Loai varchar(20),Ma char(10)); INSERT @codes EXEC dbo.sp_CapMa 'HOADON'; SELECT @code=Ma FROM @codes;
EXEC dbo.sp_TaoHoaDon @Actor=@a,@Stamp=@s,@Code=@code,@RequestId=@id,@Channel=N'Mang di',@Product='SP001',@Quantity=1;
SELECT @r=Revision FROM dbo.HOADON WHERE MaHD=@code;
DECLARE @preview TABLE(Code char(10),Revision binary(8),Total decimal(18,0),Ship decimal(18,0),Discount decimal(18,0),Payable decimal(38,0),Source varchar(20),PercentDiscount int,PartnerPercent decimal(5,2),PartnerFee decimal(18,0),Points bigint,PreviewToken binary(32));
INSERT @preview EXEC dbo.sp_SalesCheckoutPreview @a,@s,@code,@r;
DECLARE @token binary(32),@payable decimal(18,0),@payments dbo.DanhSachThanhToan;
SELECT @token=PreviewToken,@payable=Payable FROM @preview; INSERT @payments VALUES('PT001',@payable,NULL); SET @id=NEWID();
EXEC dbo.sp_ThanhToanHoaDon @a,@s,@code,@r,@id,@token,@payments;
EXEC dbo.sp_SalesOrderGet @a,@s,@code;
IF NOT EXISTS(SELECT 1 FROM dbo.HoaDonUuDaiSnapshot WHERE MaHD=@code AND Kind='Checkout') THROW 52207,'Snapshot missing',1;

SET @id=NEWID(); DELETE @codes; INSERT @codes EXEC dbo.sp_CapMa 'HOADON'; SELECT @code=Ma FROM @codes;
EXEC dbo.sp_TaoHoaDon @Actor=@a,@Stamp=@s,@Code=@code,@RequestId=@id,@Channel=N'Mang di',@Product='SP001',@Quantity=1;
SELECT @r=Revision FROM dbo.HOADON WHERE MaHD=@code;
SET @id=NEWID(); EXEC dbo.sp_HuyHoaDon @a,@s,@code,@r,@id;
IF NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD=@code AND TrangThai=N'Huy') THROW 52301,'Cancel smoke failed',1;
