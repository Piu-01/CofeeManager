IF DB_NAME() NOT LIKE 'CoffeeManager_Test_G04_%' OR OBJECT_ID('dbo.G04TestMarker') IS NULL THROW 52501,'Owned test database required',1;
DECLARE @actor int,@stamp binary(8),@id uniqueidentifier=NEWID(),@code varchar(10),@rev binary(8);
SELECT @actor=MaTK,@stamp=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001';
DECLARE @codes TABLE(Loai varchar(20),Ma char(10)); INSERT @codes EXEC dbo.sp_CapMa 'HOADON'; SELECT @code=Ma FROM @codes;
EXEC dbo.sp_TaoHoaDon @Actor=@actor,@Stamp=@stamp,@Code=@code,@RequestId=@id,@Channel=N'Giao hang',@Recipient=N'Người nhận nghiệm thu',@Phone='0901234567',@Address=N'Địa chỉ nghiệm thu',@Product='SP001',@Quantity=1;
SELECT @rev=Revision FROM dbo.HOADON WHERE MaHD=@code;
SET @id=NEWID(); EXEC dbo.sp_ChuyenTrangThaiGiaoHang @actor,@stamp,@code,@rev,N'Cho giao',N'Dang giao',@id;
SELECT @rev=Revision FROM dbo.HOADON WHERE MaHD=@code;
BEGIN TRY
 SET @id=NEWID(); EXEC dbo.sp_ChuyenTrangThaiGiaoHang @actor,@stamp,@code,@rev,N'Dang giao',N'Cho giao',@id;
 THROW 52903,'T18-K02 backwards transition accepted',1;
END TRY BEGIN CATCH IF ERROR_NUMBER()<>52502 THROW; END CATCH;
SET @id=NEWID(); EXEC dbo.sp_ChuyenTrangThaiGiaoHang @actor,@stamp,@code,@rev,N'Dang giao',N'Da giao',@id;
IF NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD=@code AND TrangThai=N'Chua thanh toan' AND TrangThaiGiao=N'Da giao' AND NgayThanhToan IS NULL) THROW 52903,'T18 delivery inferred payment',1;
IF EXISTS(SELECT 1 FROM dbo.CHITIET_THANHTOAN WHERE MaHD=@code) OR EXISTS(SELECT 1 FROM dbo.TIEUHAO_HOADON WHERE MaHD=@code) THROW 52903,'Delivery changed finances',1;
IF @@TRANCOUNT<>0 OR XACT_STATE()<>0 THROW 52903,'Transaction not clean',1;
