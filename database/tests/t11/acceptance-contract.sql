IF DB_NAME() NOT LIKE 'CoffeeManager_Test_G03_%' OR OBJECT_ID('dbo.G03TestMarker') IS NULL THROW 52999,'Test DB required',1;
DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID();SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001';
IF NOT EXISTS(SELECT 1 FROM dbo.vw_TrangThaiBan WHERE MaBan='B0003' AND TrangThai=N'Dang phuc vu' AND MaHD IS NOT NULL) THROW 52999,'Seed B0003 mismatch',1;
EXEC dbo.sp_AreaCreate @a,@s,'KV997',@id,NULL,N'Khu kiểm thử';SET @id=NEWID();EXEC dbo.sp_TableCreate @a,@s,'B9997',@id,NULL,N'Bàn kiểm thử','KV997';
IF NOT EXISTS(SELECT 1 FROM dbo.vw_TrangThaiBan WHERE MaBan='B9997' AND TrangThai=N'Trong' AND MaHD IS NULL) THROW 52999,'New table status mismatch',1;
DECLARE @rev binary(8);SELECT @rev=Revision FROM dbo.KHUVUC WHERE MaKV='KV997';SET @id=NEWID();
BEGIN TRY EXEC dbo.sp_AreaDelete @a,@s,'KV997',@id,@rev;THROW 52999,'Parent deletion accepted',1;END TRY BEGIN CATCH IF ERROR_NUMBER()<>547 THROW;END CATCH;
IF NOT EXISTS(SELECT 1 FROM dbo.BAN WHERE MaBan='B9997' AND MaKV='KV997') OR @@TRANCOUNT<>0 THROW 52999,'History/transaction mismatch',1;
