-- Prepared SQL subcases only; run after separate approval in an owned G04 test DB.
IF DB_NAME() NOT LIKE 'CoffeeManager_Test_G04_%' OR OBJECT_ID('dbo.G04TestMarker') IS NULL THROW 52107,'Owned test database required',1;
DECLARE @actor int,@stamp binary(8),@id uniqueidentifier=NEWID(),@code varchar(10),@rev binary(8),@line int;
SELECT @actor=MaTK,@stamp=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001';
DECLARE @codes TABLE(Loai varchar(20),Ma char(10)); INSERT @codes EXEC dbo.sp_CapMa 'HOADON'; SELECT @code=Ma FROM @codes;
EXEC dbo.sp_TaoHoaDon @Actor=@actor,@Stamp=@stamp,@Code=@code,@RequestId=@id,@Channel=N'Mang di',@Product='SP001',@Quantity=1,@Note=N'Ghi chú đầu';
IF (SELECT COUNT(*) FROM dbo.HOADON WHERE MaHD=@code AND LoaiDon=N'Mang di' AND MaKH IS NULL AND MaBan IS NULL AND TongTien=25000)<>1 THROW 52901,'T14-K01/K02/K03 takeaway mismatch',1;
SELECT @rev=Revision FROM dbo.HOADON WHERE MaHD=@code;
DECLARE @old binary(8)=@rev;
SET @id=NEWID(); EXEC dbo.sp_SuaDongHoaDon @Actor=@actor,@Stamp=@stamp,@Code=@code,@ExpectedRevision=@rev,@RequestId=@id,@Action='Add',@Product='SP001',@Quantity=2,@Note=N'Ghi chú khác';
IF (SELECT COUNT(*) FROM dbo.CHITIET_HD WHERE MaHD=@code)<>2 THROW 52901,'T14-K04 distinct lines mismatch',1;
BEGIN TRY
 SET @id=NEWID(); EXEC dbo.sp_SuaDongHoaDon @Actor=@actor,@Stamp=@stamp,@Code=@code,@ExpectedRevision=@old,@RequestId=@id,@Action='Add',@Product='SP001',@Quantity=1;
 THROW 52901,'T14-K10 stale version unexpectedly accepted',1;
END TRY BEGIN CATCH IF ERROR_NUMBER()<>52103 THROW; END CATCH;
SELECT @rev=Revision FROM dbo.HOADON WHERE MaHD=@code;
BEGIN TRY
 SET @id=NEWID(); EXEC dbo.sp_SuaDongHoaDon @Actor=@actor,@Stamp=@stamp,@Code=@code,@ExpectedRevision=@rev,@RequestId=@id,@Action='Add',@Product='SP001',@Quantity=0;
 THROW 52901,'T14-K05 zero quantity accepted',1;
END TRY BEGIN CATCH IF ERROR_NUMBER()<>52101 THROW; END CATCH;
SELECT @line=MIN(MaDong) FROM dbo.CHITIET_HD WHERE MaHD=@code;
SET @id=NEWID(); EXEC dbo.sp_SuaDongHoaDon @Actor=@actor,@Stamp=@stamp,@Code=@code,@ExpectedRevision=@rev,@RequestId=@id,@Action='Delete',@LineId=@line;
SELECT @rev=Revision FROM dbo.HOADON WHERE MaHD=@code; SELECT @line=MIN(MaDong) FROM dbo.CHITIET_HD WHERE MaHD=@code;
SET @id=NEWID(); EXEC dbo.sp_SuaDongHoaDon @Actor=@actor,@Stamp=@stamp,@Code=@code,@ExpectedRevision=@rev,@RequestId=@id,@Action='Delete',@LineId=@line;
IF NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD=@code AND TongTien=0 AND GiamGia=0 AND TrangThai=N'Chua thanh toan') THROW 52901,'T14-K12 empty order mismatch',1;
IF @@TRANCOUNT<>0 OR XACT_STATE()<>0 THROW 52901,'Transaction not clean',1;
