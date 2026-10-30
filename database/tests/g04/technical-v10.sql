-- Dependency smoke only. Not a T14 acceptance result.
IF DB_NAME() NOT LIKE 'CoffeeManager_Test_G04_%' OR OBJECT_ID('dbo.G04TestMarker') IS NULL THROW 52107,'Owned test database required',1;
UPDATE dbo.TAIKHOAN SET HoatDong=1 WHERE MaNV='NV001';
DECLARE @actor int,@stamp binary(8),@id uniqueidentifier=NEWID(),@revision binary(8),@code varchar(10);
SELECT @actor=MaTK,@stamp=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001';
DECLARE @codes TABLE(Loai varchar(20),Ma char(10)); INSERT @codes EXEC dbo.sp_CapMa 'HOADON'; SELECT @code=Ma FROM @codes;
EXEC dbo.sp_TaoHoaDon @Actor=@actor,@Stamp=@stamp,@Code=@code,@RequestId=@id,@Channel=N'Mang di',@Product='SP001',@Quantity=1;
SELECT @revision=Revision FROM dbo.HOADON WHERE MaHD=@code;
SET @id=NEWID();
EXEC dbo.sp_SuaDongHoaDon @Actor=@actor,@Stamp=@stamp,@Code=@code,@ExpectedRevision=@revision,@RequestId=@id,@Action='Add',@Product='SP002',@Quantity=1,@Note=N'Kỹ thuật';
EXEC dbo.sp_SalesOrderGet @actor,@stamp,@code;
EXEC dbo.sp_SalesReconcile @actor,@stamp,@id,@code,'SalesLine';
IF NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD=@code AND TongTien=55000) THROW 52107,'Smoke total differs',1;
