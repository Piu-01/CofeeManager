IF DB_NAME() NOT LIKE 'CoffeeManager_Test_G03_%' OR OBJECT_ID('dbo.G03TestMarker') IS NULL THROW 52999,'Test DB required',1;
DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@code char(5),@rev binary(8);SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001';
EXEC dbo.sp_CustomerCreate @Actor=@a,@Stamp=@s,@RequestId=@id,@Name=N'Khách acceptance',@Phone='   ';
SELECT @code=EntityCode FROM dbo.G03OperationReceipt WHERE RequestId=@id;
IF NOT EXISTS(SELECT 1 FROM dbo.KHACHHANG k JOIN dbo.HANGTHANHVIEN h ON h.MaHang=k.MaHang WHERE k.MaKH=@code AND k.SDT IS NULL AND k.DiemTichLuy=0 AND h.DiemToiThieu=0) THROW 52999,'Customer initialization mismatch',1;
SELECT @rev=Revision FROM dbo.KHACHHANG WHERE MaKH=@code;SET @id=NEWID();EXEC dbo.sp_CustomerOpen @Actor=@a,@Stamp=@s,@RequestId=@id,@Code=@code,@ExpectedRevision=@rev,@Points=300,@Source=N'Acceptance opening';
IF NOT EXISTS(SELECT 1 FROM dbo.KHACHHANG k JOIN dbo.HANGTHANHVIEN h ON h.MaHang=k.MaHang WHERE k.MaKH=@code AND k.DiemTichLuy=300 AND h.DiemToiThieu=300) THROW 52999,'Opening tier mismatch',1;
SELECT @rev=Revision FROM dbo.KHACHHANG WHERE MaKH=@code;SET @id=NEWID();EXEC dbo.sp_CustomerUpdate @Actor=@a,@Stamp=@s,@RequestId=@id,@Code=@code,@ExpectedRevision=@rev,@Name=N'Đổi tên khách acceptance',@Phone=NULL;
IF (SELECT DiemTichLuy FROM dbo.KHACHHANG WHERE MaKH=@code)<>300 THROW 52999,'Profile changed points',1;
